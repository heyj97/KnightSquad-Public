using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

/// <summary>
/// 피해, 회복 및 시스템 메시지용 텍스트 오브젝트 풀을 제공합니다.
/// </summary>
public class TextPoolMgr : MonoBehaviour
{
    /// <summary>
    /// 현재 씬에서 피해·회복 텍스트 풀을 제공하는 인스턴스를 가져옵니다.
    /// </summary>
    public static TextPoolMgr Inst { get; private set; }

    [Header("UI Roots")]
    [SerializeField] private RectTransform m_CombatTextRoot;
    [SerializeField] private RectTransform m_SystemMessageRoot;
    [SerializeField] private Camera m_WorldCamera;

    [Header("Prefabs")]
    [SerializeField] private FloatingText m_FloatingTextPrefab;
    [SerializeField] private SystemMessageText m_SystemMessagePrefab;

    [Header("Pool")]
    [SerializeField, Min(0)] private int m_InitialFloatingCount = 20;
    [SerializeField, Min(1)] private int m_MaxFloatingCount = 100;
    [SerializeField, Min(0)] private int m_InitialMessageCount = 5;
    [SerializeField, Min(1)] private int m_MaxMessageCount = 5;

    [Header("Combat Style")]
    [SerializeField] private Color m_DamageColor = Color.white;
    [SerializeField] private Color m_CriticalColor = new Color(1f, 0.75f, 0.1f);
    [SerializeField] private Color m_HealColor = new Color(0.3f, 1f, 0.4f);
    [SerializeField, Min(0.01f)] private float m_CriticalScale = 1.3f;
    [SerializeField] private Vector3 m_WorldOffset = new Vector3(0f, 0.8f, 0f);
    [SerializeField, Min(0f)] private float m_HorizontalSpread = 15f;
    [SerializeField] private Color m_AllyDamageColor = new Color(1f, 0.25f, 0.25f, 1f);

    [Header("System Messages")]
    [SerializeField, Min(0f)] private float m_DuplicateInterval = 0.5f;
    [SerializeField, Min(0f)] private float m_SystemMessageSpacing = 30f;

    private readonly List<FloatingText> m_FloatingPool = new();
    private readonly List<SystemMessageText> m_MessagePool = new();
    private readonly List<SystemMessageText> m_ActiveMessages = new();
    private string m_LastMessage;
    private float m_LastMessageTime = float.NegativeInfinity;


    private void Awake()
    {
        if (Inst != null && Inst != this)
        {
            PDebug.LogError("TextPoolMgr가 중복 배치되었습니다.", this);
            enabled = false;
            return;
        }
        Inst = this;
        // 기존 씬에 저장된 풀 설정과 무관하게 최대 5개를 표시한다.
        m_MaxMessageCount = 5;
    }

    private void Start()
    {
        if (Inst != this)
            return;
        if (m_FloatingTextPrefab != null && m_CombatTextRoot != null)
            for (int i = m_FloatingPool.Count; i < Mathf.Clamp(m_InitialFloatingCount, 0, Mathf.Max(1, m_MaxFloatingCount)); i++)
                m_FloatingPool.Add(CreateEntry(m_FloatingTextPrefab, m_CombatTextRoot));
        if (m_SystemMessagePrefab != null && m_SystemMessageRoot != null)
            for (int i = m_MessagePool.Count; i < Mathf.Clamp(m_InitialMessageCount, 0, Mathf.Max(1, m_MaxMessageCount)); i++)
                m_MessagePool.Add(CreateEntry(m_SystemMessagePrefab, m_SystemMessageRoot));
    }

    /// <summary>
    /// 피해량 텍스트를 풀에서 가져와 지정 위치에 표시합니다.
    /// </summary>
    /// <param name="worldPosition">월드 오브젝트를 기준으로 텍스트를 표시할 위치입니다.</param>
    /// <param name="damage">텍스트로 표시할 최종 피해량입니다.</param>
    /// <param name="isCritical">치명타로 처리된 피해인지 여부입니다.</param>
    /// <param name="isAlly">아군 대상에 표시하는 텍스트인지 여부입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool ShowDamage(
        Vector3 worldPosition,
        float damage,
        bool isCritical = false,
        bool isAlly = false)
    {
        if (!IsPositiveFinite(damage))
            return false;

        Color color;

        if (isAlly)
            color = m_AllyDamageColor;
        else
            color = isCritical ? m_CriticalColor : m_DamageColor;

        return ShowFloating(
            worldPosition,
            FormatAmount(damage),
            color,
            isCritical ? m_CriticalScale : 1f);
    }

    /// <summary>
    /// 회복량 텍스트를 풀에서 가져와 지정 위치에 표시합니다.
    /// </summary>
    /// <param name="worldPosition">월드 오브젝트를 기준으로 텍스트를 표시할 위치입니다.</param>
    /// <param name="healAmount">텍스트로 표시할 회복량입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool ShowHeal(Vector3 worldPosition, float healAmount)
    {
        if (!IsPositiveFinite(healAmount))
            return false;
        return ShowFloating(worldPosition, "+" + FormatAmount(healAmount), m_HealColor, 1f);
    }

    private bool ShowFloating(Vector3 worldPosition, string message, Color color, float scale)
    {
        if (!isActiveAndEnabled || Inst != this || m_CombatTextRoot == null ||
            !m_CombatTextRoot.gameObject.activeInHierarchy || m_FloatingTextPrefab == null)
            return false;
        if (!TryGetLocalPosition(worldPosition + m_WorldOffset, out Vector2 position))
            return false;

        FloatingText entry = GetEntry(m_FloatingPool, m_FloatingTextPrefab,
            m_CombatTextRoot, Mathf.Max(1, m_MaxFloatingCount));
        if (entry == null)
            return false;
        position.x += Random.Range(-m_HorizontalSpread, m_HorizontalSpread);
        return entry.Play(message, position, color, scale);
    }

    /// <summary>
    /// 시스템 안내 텍스트를 풀에서 가져와 지정 메시지를 표시합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool ShowSystemMessage(string message)
    {
        if (!isActiveAndEnabled || Inst != this ||
            string.IsNullOrWhiteSpace(message) ||
            m_SystemMessageRoot == null ||
            !m_SystemMessageRoot.gameObject.activeInHierarchy ||
            m_SystemMessagePrefab == null)
        {
            return false;
        }

        float now = Time.unscaledTime;

        if (message == m_LastMessage &&
            now - m_LastMessageTime < m_DuplicateInterval)
        {
            return false;
        }

        // 풀의 인덱스가 아닌 실제 표시 순서로 가장 오래된 메시지를 찾는다.
        m_ActiveMessages.RemoveAll(current => current == null || !current.gameObject.activeSelf);
        while (m_ActiveMessages.Count >= Mathf.Max(1, m_MaxMessageCount))
        {
            m_ActiveMessages[0].gameObject.SetActive(false);
            m_ActiveMessages.RemoveAt(0);
        }

        SystemMessageText entry = GetEntry(
            m_MessagePool,
            m_SystemMessagePrefab,
            m_SystemMessageRoot,
            Mathf.Max(1, m_MaxMessageCount));

        if (entry == null)
            return false;

        // 새 메시지는 항상 기본 위치에서 시작한다.
        if (!entry.Play(message, Vector2.zero))
            return false;

        // 기존 메시지들을 한 줄씩 위로 이동
        for (int i = 0; i < m_ActiveMessages.Count; i++)
        {
            SystemMessageText current = m_ActiveMessages[i];
            current.MoveUp(m_SystemMessageSpacing);
        }

        m_ActiveMessages.Add(entry);
        m_LastMessage = message;
        m_LastMessageTime = now;

        return true;
    }

    private bool TryGetLocalPosition(Vector3 worldPosition, out Vector2 localPosition)
    {
        localPosition = default;
        if (m_WorldCamera == null)
            m_WorldCamera = Camera.main;
        if (m_WorldCamera == null)
            return false;

        Canvas canvas = m_CombatTextRoot.GetComponentInParent<Canvas>();
        if (canvas == null)
            return false;
        canvas = canvas.rootCanvas;
        Camera uiCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (canvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            return false;

        Vector3 screenPosition = m_WorldCamera.WorldToScreenPoint(worldPosition);
        if (screenPosition.z <= 0f)
            return false;
        return RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_CombatTextRoot, screenPosition, uiCamera, out localPosition);
    }

    private static T CreateEntry<T>(T prefab, RectTransform parent) where T : Component
    {
        T entry = Instantiate(prefab, parent, false);
        entry.gameObject.SetActive(false);
        return entry;
    }

    private static T GetEntry<T>(List<T> pool, T prefab, RectTransform parent, int maxCount)
        where T : Component
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i] == null)
                pool[i] = CreateEntry(prefab, parent);
            if (!pool[i].gameObject.activeSelf)
                return pool[i];
        }
        if (pool.Count >= maxCount)
            return null;
        T entry = CreateEntry(prefab, parent);
        pool.Add(entry);
        return entry;
    }

    private static bool IsPositiveFinite(float value)
    {
        return value > 0f && !float.IsInfinity(value) && !float.IsNaN(value);
    }

    private static string FormatAmount(float value)
    {
        // 소수 피해가 0으로 보이지 않게 하며 실제 전투 수치는 변경하지 않는다.
        return Mathf.Max(1f, Mathf.Round(value)).ToString("0", CultureInfo.InvariantCulture);
    }

    private void OnDisable()
    {
        foreach (FloatingText entry in m_FloatingPool)
            if (entry != null)
                entry.gameObject.SetActive(false);
        foreach (SystemMessageText entry in m_MessagePool)
            if (entry != null)
                entry.gameObject.SetActive(false);
        m_ActiveMessages.Clear();
        m_LastMessage = null;
        m_LastMessageTime = float.NegativeInfinity;
    }

    private void OnDestroy()
    {
        foreach (FloatingText entry in m_FloatingPool)
            if (entry != null)
                Destroy(entry.gameObject);
        foreach (SystemMessageText entry in m_MessagePool)
            if (entry != null)
                Destroy(entry.gameObject);
        if (Inst == this)
            Inst = null;
    }
}
