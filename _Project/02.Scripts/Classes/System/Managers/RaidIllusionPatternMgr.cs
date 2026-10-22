using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>환영 제거 미니게임. 단독 테스트와 RaidPatternMgr를 통한 실제 레이드 진행을 지원한다.</summary>
public class RaidIllusionPatternMgr : MonoBehaviour
{
    [Header("Scene references")]
    [SerializeField] private RaidCharacter m_Player;
    [SerializeField] private BoxCollider2D m_MovementBounds;
    [SerializeField] private Transform m_PlayerSpawnPoint;
    [SerializeField] private Transform m_TargetsRoot;
    [SerializeField] private UnityEngine.UI.Text m_PatternTimerTxt;
    [SerializeField] private UnityEngine.UI.Button m_PatternSuccessBtn;
    [SerializeField] private UnityEngine.UI.Button m_PatternFailBtn;
    [Header("Pattern settings")]
    [SerializeField, Min(0.1f)] private float m_TimeLimit = 12f;
    [SerializeField, Min(0f)] private float m_WrongHitPenalty = 2f;
    [SerializeField, Min(0.01f)] private float m_TimerPenaltyFlashDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float m_PlayerHitFlashDuration = 0.2f;
    [SerializeField] private Color m_IllusionTint = new Color(0.15f, 0.3f, 0.65f, 1f);
    [SerializeField, Min(0.01f)] private float m_FlashDuration = 0.1f;
    [SerializeField, Min(0.01f)] private float m_FadeDuration = 0.4f;
    [SerializeField, Min(0f)] private float m_MinTargetSpacing = 1f;
    [SerializeField, Min(0f)] private float m_MinSpawnDistance = 0.8f;
    [Header("Development only")]
    [SerializeField] private bool m_AutoStartForTesting;
    [Tooltip("초기화된 파티가 없는 단독 테스트에서만 사용한다.")]
    [SerializeField] private CharacterData[] m_TestCharacters;

    private readonly List<RaidIllusionTarget> m_Targets = new();
    private readonly List<GameObject> m_HiddenPreviews = new();
    private readonly List<AnimatorOverrideController> m_Controllers = new();
    private GameObject m_GeneratedRoot;
    private bool m_FailRequested;
    private Color m_TimerOriginalColor;
    private bool m_HasTimerOriginalColor;
    private float m_TimerPenaltyFlashUntil = -1f;
    public bool IsRunning { get; private set; }
    public float TimeRemaining { get; private set; }
    public int RemainingIllusions { get; private set; }
    public bool? LastResult { get; private set; }
    public event Action<bool> OnPatternCompleted;
    internal bool IsRaidManaged { get; set; }

    private void Awake()
    {
        if (m_PatternSuccessBtn != null) m_PatternSuccessBtn.onClick.AddListener(ForceSuccess);
        if (m_PatternFailBtn != null) m_PatternFailBtn.onClick.AddListener(ForceFailure);
        RefreshTimer();
    }

    private IEnumerator Start()
    {
        yield return null;
        while (SceneChangeMgr.IsTransitioning) yield return null;
        if (m_AutoStartForTesting && !IsRaidManaged &&
            BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Raid) BeginPattern();
    }

    [ContextMenu("Development/Start illusion pattern")]
    public void BeginPattern()
    {
        if (!Application.isPlaying || SceneChangeMgr.IsTransitioning || !isActiveAndEnabled) return;
        CancelPattern();
        if (m_Player == null || m_MovementBounds == null || m_PlayerSpawnPoint == null || m_TargetsRoot == null ||
            !m_MovementBounds.enabled || !m_MovementBounds.gameObject.activeInHierarchy)
        {
            Debug.LogError("환영 패턴: Player, MovementBounds, PlayerSpawnPoint, TargetsRoot를 연결해주세요.", this);
            return;
        }
        List<CharacterData> models = GetModels();
        if (models.Count == 0)
        {
            Debug.LogError("환영 패턴: 초기화된 파티 또는 단독 테스트용 CharacterData가 필요합니다.", this);
            return;
        }
        List<Vector3> positions = GetSpawnPositions();
        if (positions.Count < 8)
        {
            Debug.LogError("환영 패턴: 경계 안에 서로 충분히 떨어진 후보 위치가 최소 8개 필요합니다.", this);
            return;
        }
        int targetCount = UnityEngine.Random.Range(8, Mathf.Min(10, positions.Count) + 1);
        int illusionCount = UnityEngine.Random.Range(3, 5);
        HidePreviews();
        m_GeneratedRoot = new GameObject("IllusionRuntimeTargets");
        m_GeneratedRoot.transform.SetParent(m_TargetsRoot, false);
        try
        {
            for (int i = 0; i < targetCount; i++)
            {
                CharacterData data = models[UnityEngine.Random.Range(0, models.Count)];
                CreateTarget(data, positions[i], i < illusionCount);
            }
            TimeRemaining = m_TimeLimit;
            RemainingIllusions = illusionCount;
            LastResult = null;
            m_FailRequested = false;
            if (!m_Player.BeginControl(m_MovementBounds, m_PlayerSpawnPoint.position, m_Targets))
                throw new InvalidOperationException("전용 캐릭터 초기화에 실패했습니다.");
            IsRunning = true;
            RefreshTimer();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CancelPattern();
        }
    }

    private List<CharacterData> GetModels()
    {
        var result = new List<CharacterData>();
        PartyMgr party = FindFirstObjectByType<PartyMgr>();
        if (party != null && party.IsPartyInitialized)
        {
            foreach (CharacterBase character in party.PartyCharacters)
                if (character != null && character.IsInitialized && HasModel(character.CharData))
                    result.Add(character.CharData);
        }
        else if (m_TestCharacters != null)
            foreach (CharacterData data in m_TestCharacters)
                if (HasModel(data)) result.Add(data);
        return result;
    }

    private static bool HasModel(CharacterData data) => data != null && data.Prefab != null &&
        data.Prefab.transform.Find("UnitRoot") != null;

    private List<Vector3> GetSpawnPositions()
    {
        var candidates = new List<Vector3>();
        foreach (Transform point in m_TargetsRoot)
        {
            Vector3 position = point.position;
            Bounds bounds = m_MovementBounds.bounds;
            if (position.x < bounds.min.x || position.x > bounds.max.x ||
                position.y < bounds.min.y || position.y > bounds.max.y ||
                Vector2.Distance(position, m_PlayerSpawnPoint.position) < m_MinSpawnDistance) continue;
            candidates.Add(position);
        }
        // 후보 추첨 후 간격을 검증한다. 결과의 앞쪽 8~10개를 사용한다.
        for (int i = candidates.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            (candidates[i], candidates[other]) = (candidates[other], candidates[i]);
        }
        var result = new List<Vector3>();
        foreach (Vector3 position in candidates)
        {
            bool overlaps = result.Exists(existing => Vector2.Distance(existing, position) < m_MinTargetSpacing);
            if (!overlaps) result.Add(position);
        }
        return result;
    }

    private void CreateTarget(CharacterData data, Vector3 position, bool illusion)
    {
        var holder = new GameObject(illusion ? "IllusionTarget" : "NormalTarget");
        holder.transform.SetParent(m_GeneratedRoot.transform, false);
        holder.transform.position = position;
        Transform source = data.Prefab.transform.Find("UnitRoot");
        GameObject visual = Instantiate(source.gameObject, holder.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.SetActive(true);
        foreach (Transform child in holder.GetComponentsInChildren<Transform>(true))
            child.gameObject.layer = m_Player.gameObject.layer;
        foreach (Collider2D collider in visual.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        SPUM_Prefabs spum = data.Prefab.GetComponentInChildren<SPUM_Prefabs>(true);
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            if (animator.runtimeAnimatorController == null) continue;
            if (spum != null && spum.IDLE_List.Count > 0 && spum.IDLE_List[0] != null)
            {
                var controller = new AnimatorOverrideController(animator.runtimeAnimatorController);
                var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
                controller.GetOverrides(overrides);
                for (int i = 0; i < overrides.Count; i++)
                    if (overrides[i].Key.name == "IDLE")
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, spum.IDLE_List[0]);
                controller.ApplyOverrides(overrides);
                m_Controllers.Add(controller);
                animator.runtimeAnimatorController = controller;
            }
            animator.Rebind();
            animator.Update(0f);
        }
        RaidIllusionTarget target = holder.AddComponent<RaidIllusionTarget>();
        target.Initialize(illusion, m_IllusionTint, m_FlashDuration, m_FadeDuration, HandleHit);
        m_Targets.Add(target);
    }

    private void HidePreviews()
    {
        foreach (Transform point in m_TargetsRoot)
            foreach (Transform preview in point)
                if (preview.gameObject.activeSelf)
                {
                    m_HiddenPreviews.Add(preview.gameObject);
                    preview.gameObject.SetActive(false);
                }
    }

    private void HandleHit(RaidIllusionTarget target)
    {
        if (!IsRunning) return;
        if (target.IsIllusion) RemainingIllusions--;
        else
        {
            float previousTime = TimeRemaining;
            TimeRemaining = Mathf.Max(0f, TimeRemaining - m_WrongHitPenalty);
            if (TimeRemaining < previousTime)
                m_TimerPenaltyFlashUntil = Time.time + m_TimerPenaltyFlashDuration;
            m_Player.ShowHitFlash(m_PlayerHitFlashDuration);
            m_Player.ApplyPenaltyKnockback();
        }
        RefreshTimer();
    }

    private void LateUpdate()
    {
        if (!IsRunning) return;
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshTimer();
        // 캐릭터 Update 이후 판정하여 마지막 제거와 시간 만료가 같은 프레임이면 성공을 우선한다.
        if (RemainingIllusions == 0) Complete(true);
        else if (m_FailRequested || TimeRemaining <= 0f) Complete(false);
    }

    private void RefreshTimer()
    {
        if (m_PatternTimerTxt != null)
        {
            // 비활성 패턴을 공통 매니저가 Awake 이전에 정리하는 경우에도 원래 색을 보존한다.
            if (!m_HasTimerOriginalColor)
            {
                m_TimerOriginalColor = m_PatternTimerTxt.color;
                m_HasTimerOriginalColor = true;
            }
            m_PatternTimerTxt.text = $"남은 시간\n{TimeRemaining:0.00}";
            m_PatternTimerTxt.color = Time.time < m_TimerPenaltyFlashUntil
                ? new Color(1f, 0f, 0f, m_TimerOriginalColor.a) : m_TimerOriginalColor;
        }
    }

    public void ForceSuccess() { if (IsRunning) Complete(true); }
    public void ForceFailure() { if (IsRunning) m_FailRequested = true; }

    private void Complete(bool succeeded)
    {
        if (!IsRunning) return;
        IsRunning = false;
        m_TimerPenaltyFlashUntil = -1f;
        RefreshTimer();
        LastResult = succeeded;
        m_Player.EndControl();
        foreach (RaidIllusionTarget target in m_Targets)
            if (target != null) target.DisableTarget();
        Debug.Log(succeeded ? "환영 패턴 성공" : "환영 패턴 실패", this);
        OnPatternCompleted?.Invoke(succeeded);
    }

    public void CancelPattern()
    {
        IsRunning = false;
        m_TimerPenaltyFlashUntil = -1f;
        m_FailRequested = false;
        LastResult = null;
        TimeRemaining = 0f;
        RemainingIllusions = 0;
        if (m_Player != null) m_Player.EndControl();
        if (m_GeneratedRoot != null)
        {
            m_GeneratedRoot.SetActive(false);
            // Destroy는 프레임 끝에 실행되므로 재시작 후보 목록에 이전 생성물이 섞이지 않게 분리한다.
            m_GeneratedRoot.transform.SetParent(null);
            Destroy(m_GeneratedRoot);
            m_GeneratedRoot = null;
        }
        m_Targets.Clear();
        foreach (AnimatorOverrideController controller in m_Controllers)
            if (controller != null) Destroy(controller);
        m_Controllers.Clear();
        foreach (GameObject preview in m_HiddenPreviews)
            if (preview != null) preview.SetActive(true);
        m_HiddenPreviews.Clear();
        RefreshTimer();
    }

    private void OnDisable() => CancelPattern();

    private void OnDestroy()
    {
        if (m_PatternSuccessBtn != null) m_PatternSuccessBtn.onClick.RemoveListener(ForceSuccess);
        if (m_PatternFailBtn != null) m_PatternFailBtn.onClick.RemoveListener(ForceFailure);
    }
}
