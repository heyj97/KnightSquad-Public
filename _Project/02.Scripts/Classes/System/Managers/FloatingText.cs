using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 피해량과 회복량을 월드 공간에 애니메이션 텍스트로 표시합니다.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class FloatingText : MonoBehaviour
{
    [SerializeField] private Text m_Text;
    [SerializeField, Min(0.01f)] private float m_Duration = 0.6f;
    [SerializeField] private float m_RiseDistance = 60f;
    [SerializeField, Range(0f, 0.99f)] private float m_FadeStart = 0.3f;

    private RectTransform m_Rect;
    private Vector3 m_BaseScale;
    private Vector2 m_StartPosition;
    private Color m_Color;
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
            PDebug.LogError("FloatingText의 UGUI Text 참조가 없습니다.", this);
            return false;
        }

        m_BaseScale = m_Rect.localScale;
        m_Text.raycastTarget = false;
        m_IsInitialized = true;
        return true;
    }

    /// <summary>
    /// 피해 또는 회복 수치와 색상·크기를 설정하여 월드 공간 텍스트 애니메이션을 재생합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="localPosition">부모 캔버스 또는 풀 루트 기준의 표시 위치입니다.</param>
    /// <param name="color">텍스트 또는 보스 강조 표시에 적용할 색상입니다.</param>
    /// <param name="scale">표시 애니메이션에 적용할 크기 배율입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool Play(string message, Vector2 localPosition, Color color, float scale = 1f)
    {
        if (!TryInitialize())
            return false;

        // 부모의 로컬 좌표 원점(피벗)을 기준으로 배치한다.
        RectTransform parent = m_Rect.parent as RectTransform;
        if (parent == null)
            return false;
        m_Rect.anchorMin = parent.pivot;
        m_Rect.anchorMax = parent.pivot;
        m_Rect.anchoredPosition3D = new Vector3(localPosition.x, localPosition.y, 0f);
        m_Rect.localScale = m_BaseScale * Mathf.Max(0.01f, scale);
        m_StartPosition = localPosition;
        m_Color = color;
        m_Text.text = message;
        m_Text.color = color;
        m_Elapsed = 0f;
        m_IsPlaying = true;
        gameObject.SetActive(true);
        return true;
    }

    private void Update()
    {
        if (!m_IsPlaying)
            return;

        // 일시정지 중에도 UI 연출은 종료된다.
        m_Elapsed += Time.unscaledDeltaTime;
        float progress = Mathf.Clamp01(m_Elapsed / Mathf.Max(0.01f, m_Duration));
        m_Rect.anchoredPosition = m_StartPosition + Vector2.up * (m_RiseDistance * progress);
        Color color = m_Color;
        color.a *= 1f - Mathf.InverseLerp(m_FadeStart, 1f, progress);
        m_Text.color = color;
        if (progress >= 1f)
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
    }
}
