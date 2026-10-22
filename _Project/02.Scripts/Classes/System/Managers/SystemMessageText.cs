using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게임 안내 메시지를 화면에 표시하고 애니메이션이 끝나면 반환합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class SystemMessageText : MonoBehaviour
{
    [SerializeField] private Text m_Text;
    [SerializeField] private Color m_Color = Color.white;
    [SerializeField, Min(0f)] private float m_HoldDuration = 1.5f;
    [SerializeField, Min(0.01f)] private float m_FadeDuration = 0.3f;


    private RectTransform m_Rect;
    private Vector3 m_BaseScale;
    private Vector2 m_StartPosition;
    private float m_Elapsed;
    private bool m_IsPlaying;
    private bool m_IsInitialized;

    private bool TryInitialize()
    {
        if (m_IsInitialized)
            return true;

        m_Rect = GetComponent<RectTransform>();
        if (m_Text == null)
            m_Text = GetComponentInChildren<Text>(true);
        if (m_Text == null)
        {
            PDebug.LogError("SystemMessageText의 UGUI Text 참조가 없습니다.", this);
            return false;
        }

        m_BaseScale = m_Rect.localScale;
        m_Text.raycastTarget = false;
        m_IsInitialized = true;
        return true;
    }

    /// <summary>
    /// 안내 문구와 색상·크기를 설정하여 시스템 메시지 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="localPosition">부모 캔버스 또는 풀 루트 기준의 표시 위치입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool Play(string message, Vector2 localPosition)
    {
        if (!TryInitialize())
            return false;
        RectTransform parent = m_Rect.parent as RectTransform;
        if (parent == null)
            return false;

        m_Rect.anchorMin = parent.pivot;
        m_Rect.anchorMax = parent.pivot;
        m_Rect.anchoredPosition3D = new Vector3(localPosition.x, localPosition.y, 0f);
        m_StartPosition = localPosition;
        m_Rect.localScale = m_BaseScale;
        m_Text.text = message;
        m_Text.color = m_Color;
        m_Elapsed = 0f;
        m_IsPlaying = true;
        gameObject.SetActive(true);
        return true;
    }

    private void Update()
    {
        if (!m_IsPlaying)
            return;

        m_Elapsed += Time.unscaledDeltaTime;

        float fade = Mathf.Clamp01(
            (m_Elapsed - Mathf.Max(0f, m_HoldDuration)) /
            Mathf.Max(0.01f, m_FadeDuration));

        Color color = m_Color;
        color.a *= 1f - fade;

        m_Text.color = color;

        if (fade >= 1f)
            gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        m_IsPlaying = false;
        m_Elapsed = 0f;
        if (!m_IsInitialized)
            return;
        m_Rect.localScale = m_BaseScale;
        m_Text.color = m_Color;
        m_Text.text = string.Empty;
        m_Rect.anchoredPosition = m_StartPosition;
    }
    
    /// <summary>
    /// 시스템 메시지의 현재 위치와 애니메이션 시작 기준을 함께 위로 이동합니다.
    /// </summary>
    /// <param name="amount">위쪽으로 이동할 UI 좌표 거리입니다.</param>
    public void MoveUp(float amount)
    {
        if (!TryInitialize())
            return;

        m_StartPosition += Vector2.up * amount;

        // 현재 위치도 즉시 반영
        m_Rect.anchoredPosition += Vector2.up * amount;
    }
}
