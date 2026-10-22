using System.Collections;
using UnityEngine;

/// <summary>활성화 루트에 부착하고, 배경을 제외한 실제 창을 대상으로 연결한다.</summary>
[DisallowMultipleComponent]
public class UIPopupEffect : MonoBehaviour
{
    [SerializeField] private RectTransform m_PopupTarget;
    [SerializeField] private bool m_PlayOnEnable = true;
    [SerializeField, Min(0.01f)] private float m_Duration = 0.2f;
    [SerializeField, Min(0.01f)] private float m_StartScale = 0.85f;
    [SerializeField, Min(1f)] private float m_PeakScale = 1.03f;

    private RectTransform m_CachedTarget;
    private Vector3 m_BaseScale;
    private Coroutine m_PopupCoroutine;
    private int m_LastPlayFrame = -1;

    private void OnEnable()
    {
        if (m_PlayOnEnable) GlobalUIMgr.PlayPopup(this);
    }

    public void Play()
    {
        if (!isActiveAndEnabled || m_PopupTarget == null) return;
        // OnEnable과 명시적인 표시 요청이 같은 프레임에 겹쳐도 한 번만 재생한다.
        if (m_LastPlayFrame == Time.frameCount) return;
        StopAndRestore();
        if (m_CachedTarget != m_PopupTarget)
        {
            m_CachedTarget = m_PopupTarget;
            m_BaseScale = m_CachedTarget.localScale;
        }
        m_LastPlayFrame = Time.frameCount;
        m_PopupCoroutine = StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float duration = Mathf.Max(0.01f, m_Duration);
        float elapsed = 0f;
        while (elapsed < duration && m_CachedTarget != null)
        {
            float scale = GlobalUIMgr.EvaluatePopupScale(elapsed / duration, m_StartScale, m_PeakScale);
            m_CachedTarget.localScale = m_BaseScale * scale;
            yield return null;
            elapsed += Time.unscaledDeltaTime;
        }
        if (m_CachedTarget != null) m_CachedTarget.localScale = m_BaseScale;
        m_PopupCoroutine = null;
    }

    private void StopAndRestore()
    {
        if (m_PopupCoroutine != null)
        {
            StopCoroutine(m_PopupCoroutine);
            m_PopupCoroutine = null;
        }
        if (m_CachedTarget != null) m_CachedTarget.localScale = m_BaseScale;
    }

    private void OnDisable()
    {
        StopAndRestore();
        m_LastPlayFrame = -1;
    }
}
