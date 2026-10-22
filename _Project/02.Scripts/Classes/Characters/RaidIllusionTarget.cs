using System;
using UnityEngine;

/// <summary>환영 패턴의 시각 모델만 관리한다. 실제 파티 HP·버프·선택 상태는 사용하지 않는다.</summary>
public class RaidIllusionTarget : MonoBehaviour, IRaidAttackTarget
{
    private SpriteRenderer[] m_Renderers;
    private Color[] m_Colors;
    private Action<RaidIllusionTarget> m_OnHit;
    private float m_FlashDuration;
    private float m_FadeDuration;
    private float m_HitElapsed = -1f;
    public bool IsIllusion { get; private set; }
    public bool CanBeHit { get; private set; }
    public Vector3 Position => transform.position;

    public void Initialize(bool illusion, Color tint, float flashDuration, float fadeDuration,
        Action<RaidIllusionTarget> onHit)
    {
        IsIllusion = illusion;
        m_OnHit = onHit;
        m_FlashDuration = Mathf.Max(0.01f, flashDuration);
        m_FadeDuration = Mathf.Max(0.01f, fadeDuration);
        m_Renderers = GetComponentsInChildren<SpriteRenderer>(true);
        m_Colors = new Color[m_Renderers.Length];
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            Color original = m_Renderers[i].color;
            m_Colors[i] = illusion ? original * tint : original;
            m_Renderers[i].color = m_Colors[i];
        }
        CanBeHit = true;
    }

    public void ReceiveHit()
    {
        if (!CanBeHit) return;
        if (IsIllusion) CanBeHit = false;
        m_HitElapsed = 0f;
        ApplyColor(true, 1f);
        m_OnHit?.Invoke(this);
    }

    public void DisableTarget()
    {
        CanBeHit = false;
        m_OnHit = null;
    }

    private void Update()
    {
        if (m_HitElapsed < 0f || m_Renderers == null) return;
        m_HitElapsed += Time.deltaTime;
        if (m_HitElapsed < m_FlashDuration) return;
        float alpha = IsIllusion ? 1f - Mathf.Clamp01((m_HitElapsed - m_FlashDuration) / m_FadeDuration) : 1f;
        ApplyColor(false, alpha);
        if (!IsIllusion) m_HitElapsed = -1f;
        else if (alpha <= 0f) Destroy(gameObject);
    }

    private void ApplyColor(bool flash, float alpha)
    {
        for (int i = 0; i < m_Renderers.Length; i++)
        {
            if (m_Renderers[i] == null) continue;
            Color color = flash ? Color.white : m_Colors[i];
            color.a = m_Colors[i].a * alpha;
            m_Renderers[i].color = color;
        }
    }

    private void OnDisable() => DisableTarget();
}
