using System;
using UnityEngine;

public enum RaidZeoliteSymbol { Anger, Illusion, Darkness, Nightmare }

/// <summary>비석 내구도, 겹친 문양의 숨쉬기, 붉은 피격 표시와 부서진 베이스를 관리한다.</summary>
public class RaidZeoliteTarget : MonoBehaviour, IRaidAttackTarget
{
    [SerializeField] private RaidZeoliteSymbol m_Symbol;
    [SerializeField] private SpriteRenderer m_BaseRenderer;
    [SerializeField] private SpriteRenderer m_DisableSymbolRenderer;
    [SerializeField] private SpriteRenderer m_EnableSymbolRenderer;
    [Header("Sprites (Base/Disable/Enable may use the assigned Renderer sprite)")]
    [SerializeField] private Sprite m_BaseSprite;
    [SerializeField] private Sprite m_BreakSprite;
    [SerializeField] private Sprite m_DisableSprite;
    [SerializeField] private Sprite m_EnableSprite;
    [Header("Visual timing")]
    [SerializeField, Min(0.01f)] private float m_BreathPeriod = 1.2f;
    [SerializeField, Range(0f, 1f)] private float m_MinEnableAlpha;
    [SerializeField, Range(0f, 1f)] private float m_MaxEnableAlpha = 1f;
    [SerializeField, Min(0.01f)] private float m_HitFlashDuration = 0.15f;
    private Action<RaidZeoliteTarget> m_OnHit;
    private Color m_BaseColor, m_DisableColor, m_EnableColor;
    private Sprite m_DefaultBase, m_DefaultDisable, m_DefaultEnable;
    private bool m_Initialized;
    private bool m_AcceptsHits;
    private bool m_IsCurrent;
    private float m_BreathStartedAt;
    private float m_HitUntil = -1f;
    public RaidZeoliteSymbol Symbol => m_Symbol;
    public Vector3 Position => transform.position;
    public int RemainingHits { get; private set; } = 2;
    public bool IsBroken => RemainingHits == 0;
    public bool CanBeHit => m_AcceptsHits && !IsBroken && isActiveAndEnabled;
    internal int SortingLayerId => m_BaseRenderer.sortingLayerID;

    internal bool HasValidReferences => m_BaseRenderer != null && m_DisableSymbolRenderer != null &&
        m_EnableSymbolRenderer != null && m_BaseRenderer != m_DisableSymbolRenderer &&
        m_BaseRenderer != m_EnableSymbolRenderer && m_DisableSymbolRenderer != m_EnableSymbolRenderer &&
        m_BreakSprite != null && (m_BaseSprite != null || m_BaseRenderer.sprite != null) &&
        (m_DisableSprite != null || m_DisableSymbolRenderer.sprite != null) &&
        (m_EnableSprite != null || m_EnableSymbolRenderer.sprite != null);

    internal void Initialize(Action<RaidZeoliteTarget> onHit)
    {
        if (!m_Initialized)
        {
            m_BaseColor = m_BaseRenderer.color;
            m_DisableColor = m_DisableSymbolRenderer.color;
            m_EnableColor = m_EnableSymbolRenderer.color;
            m_DefaultBase = m_BaseSprite != null ? m_BaseSprite : m_BaseRenderer.sprite;
            m_DefaultDisable = m_DisableSprite != null ? m_DisableSprite : m_DisableSymbolRenderer.sprite;
            m_DefaultEnable = m_EnableSprite != null ? m_EnableSprite : m_EnableSymbolRenderer.sprite;
            m_Initialized = true;
        }
        ResetTarget();
        m_OnHit = onHit;
        m_BaseRenderer.gameObject.SetActive(true);
        m_DisableSymbolRenderer.gameObject.SetActive(true);
        m_EnableSymbolRenderer.gameObject.SetActive(true);
    }

    internal void SetCurrent(bool current)
    {
        m_IsCurrent = current && !IsBroken;
        m_BreathStartedAt = Time.time;
        if (m_Initialized) UpdateVisuals();
    }

    internal void SetAcceptsHits(bool acceptsHits) => m_AcceptsHits = acceptsHits;

    public void ReceiveHit()
    {
        if (CanBeHit) m_OnHit?.Invoke(this);
    }

    internal void ApplyCorrectHit()
    {
        if (!CanBeHit || !m_IsCurrent) return;
        RemainingHits--;
        m_HitUntil = Time.time + m_HitFlashDuration;
        if (IsBroken)
        {
            m_IsCurrent = false;
            m_AcceptsHits = false;
            m_BaseRenderer.sprite = m_BreakSprite;
            m_DisableSymbolRenderer.enabled = false;
            m_EnableSymbolRenderer.enabled = false;
        }
        UpdateVisuals();
    }

    private void Update()
    {
        if (m_Initialized) UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        if (m_BaseRenderer == null || m_EnableSymbolRenderer == null) return;
        m_BaseRenderer.color = Time.time < m_HitUntil
            ? new Color(1f, 0.15f, 0.15f, m_BaseColor.a) : m_BaseColor;
        Color enable = m_EnableColor;
        float pulse = (1f - Mathf.Cos((Time.time - m_BreathStartedAt) * 2f * Mathf.PI /
            Mathf.Max(0.01f, m_BreathPeriod))) * 0.5f;
        enable.a = m_IsCurrent && !IsBroken
            ? Mathf.Lerp(m_MinEnableAlpha, m_MaxEnableAlpha, pulse) : 0f;
        m_EnableSymbolRenderer.color = enable;
    }

    internal void DisableTarget()
    {
        m_AcceptsHits = false;
        m_IsCurrent = false;
        m_OnHit = null;
        if (m_Initialized) UpdateVisuals();
    }

    internal void ResetTarget()
    {
        DisableTarget();
        RemainingHits = 2;
        m_HitUntil = -1f;
        if (!m_Initialized) return;
        m_BaseRenderer.sprite = m_DefaultBase;
        m_BaseRenderer.color = m_BaseColor;
        m_BaseRenderer.enabled = true;
        m_DisableSymbolRenderer.sprite = m_DefaultDisable;
        m_DisableSymbolRenderer.color = m_DisableColor;
        m_DisableSymbolRenderer.enabled = true;
        m_EnableSymbolRenderer.sprite = m_DefaultEnable;
        m_EnableSymbolRenderer.enabled = true;
        UpdateVisuals();
    }

    private void OnDisable() => DisableTarget();
}
