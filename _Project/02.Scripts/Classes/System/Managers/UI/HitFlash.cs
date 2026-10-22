using UnityEngine;

/// <summary>
/// 대상이 피해를 받을 때 모델 렌더러의 색상을 잠시 변경하여 피격을 표현합니다.
/// </summary>
public class HitFlash : MonoBehaviour
{
    [SerializeField] private Color m_FlashColor = new Color(1f, 0.3f, 0.3f, 1f);
    [SerializeField, Min(0.01f)] private float m_FlashDuration = 0.1f;

    private SpriteRenderer[] m_Renderers;
    private Color[] m_OriginalColors;
    private float m_RemainingTime;
    private bool m_IsFlashing;

    // 모델 생성 후 호출한다. 그림자 등 색상을 유지할 렌더러는 제외한다.
    /// <summary>
    /// 모델 렌더러와 원래 색상을 수집하되 제외 대상으로 지정한 렌더러는 보존합니다.
    /// </summary>
    /// <param name="modelRoot">효과를 적용할 렌더러를 포함하는 모델 루트입니다.</param>
    /// <param name="excludedRenderers">원래 색상을 유지하여 효과에서 제외할 렌더러 목록입니다.</param>
    public void Initialize(Transform modelRoot, params SpriteRenderer[] excludedRenderers)
    {
        RestoreColors();

        if (modelRoot == null)
        {
            m_Renderers = null;
            m_OriginalColors = null;
            return;
        }

        SpriteRenderer[] renderers = modelRoot.GetComponentsInChildren<SpriteRenderer>(true);
        var targets = new System.Collections.Generic.List<SpriteRenderer>();

        foreach (SpriteRenderer renderer in renderers)
        {
            if (excludedRenderers != null &&
                System.Array.IndexOf(excludedRenderers, renderer) >= 0)
                continue;

            targets.Add(renderer);
        }

        m_Renderers = targets.ToArray();
        m_OriginalColors = new Color[m_Renderers.Length];

        for (int i = 0; i < m_Renderers.Length; i++)
            m_OriginalColors[i] = m_Renderers[i].color;
    }

    /// <summary>
    /// 초기화된 모델 렌더러에 짧은 피격 색상 효과를 재생합니다.
    /// </summary>
    public void Play()
    {
        if (!isActiveAndEnabled || m_Renderers == null || m_Renderers.Length == 0)
            return;

        for (int i = 0; i < m_Renderers.Length; i++)
        {
            if (m_Renderers[i] == null)
                continue;

            Color color = m_OriginalColors[i] * m_FlashColor;
            color.a = m_OriginalColors[i].a;
            m_Renderers[i].color = color;
        }

        // 연속 피격은 원래 색을 다시 저장하지 않고 지속시간만 갱신한다.
        m_RemainingTime = Mathf.Max(0.01f, m_FlashDuration);
        m_IsFlashing = true;
    }

    private void Update()
    {
        if (!m_IsFlashing)
            return;

        m_RemainingTime -= Time.deltaTime;
        if (m_RemainingTime <= 0f)
            RestoreColors();
    }

    private void OnDisable()
    {
        RestoreColors();
    }

    private void OnDestroy()
    {
        RestoreColors();
    }

    private void RestoreColors()
    {
        if (m_IsFlashing && m_Renderers != null)
        {
            for (int i = 0; i < m_Renderers.Length; i++)
            {
                if (m_Renderers[i] != null)
                    m_Renderers[i].color = m_OriginalColors[i];
            }
        }

        m_IsFlashing = false;
        m_RemainingTime = 0f;
    }
}
