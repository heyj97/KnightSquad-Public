using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>문양 비석 지시, 5회 실패 허용, 동일 반지름 경고와 폭발을 관리한다.</summary>
public class RaidZeolitePatternMgr : MonoBehaviour
{
    private const int AllowedMistakes = 5;
    [Header("Scene references")]
    [SerializeField] private RaidCharacter m_Player;
    [SerializeField] private BoxCollider2D m_MovementBounds;
    [SerializeField] private Transform m_PlayerSpawnPoint;
    [SerializeField] private RaidZeoliteTarget[] m_Stones;
    [SerializeField] private Transform m_ExplosionRoot;
    [SerializeField] private GameObject m_MiniGameRoot;
    [SerializeField] private UnityEngine.UI.Text m_PatternNoticeTxt;
    [SerializeField] private UnityEngine.UI.Text m_PatternTimerTxt;
    [SerializeField] private RaidPatternHeartsUI m_HeartsUI;
    [Header("Pattern settings")]
    [SerializeField, Min(0.1f)] private float m_TimeLimit = 20f;
    [SerializeField, Min(0f)] private float m_NextNoticeDelay = 0.5f;
    [SerializeField, Min(0.01f)] private float m_PlayerHitFlashDuration = 0.2f;
    [Header("Explosions")]
    [SerializeField] private Sprite m_DangerSignSprite;
    [SerializeField] private GameObject m_ExplosionPrefab;
    [SerializeField, Min(0.1f)] private float m_ExplosionInterval = 3f;
    [SerializeField, Min(0.01f)] private float m_WarningDuration = 1f;
    [SerializeField, Min(0.01f)] private float m_WarningBlinkPeriod = 0.18f;
    [SerializeField, Range(0f, 1f)] private float m_WarningMinAlpha = 0.2f;
    [SerializeField, Min(1)] private int m_ExplosionsPerWave = 4;
    [Tooltip("패턴 시작 시 확정하며 모든 경고와 폭발에 동일한 월드 반지름을 사용한다.")]
    [SerializeField, Min(0.01f)] private float m_ExplosionRadius = 0.55f;
    [SerializeField, Min(1)] private int m_SpawnAttempts = 200;
    [SerializeField] private int m_WarningSortingOrder = 3;
    [SerializeField] private int m_ExplosionSortingOrder = 8;
    [SerializeField, Min(0.1f)] private float m_ExplosionVisualLifetime = 2f;
    [Header("Development only")]
    [SerializeField] private bool m_AutoStartForTesting;

    private sealed class ExplosionWave
    {
        internal readonly List<Vector2> Centers = new();
        internal readonly List<SpriteRenderer> Warnings = new();
        internal float CreatedAt;
        internal float ExplodeAt;
    }
    private readonly List<RaidZeoliteTarget> m_Order = new();
    private readonly List<IRaidAttackTarget> m_AttackTargets = new();
    private readonly List<ExplosionWave> m_Waves = new();
    private readonly List<Vector2> m_OccupiedCenters = new();
    private GameObject m_RuntimeExplosions;
    private RaidZeoliteTarget m_CurrentStone;
    private int m_DestroyedCount;
    private float m_NextNoticeAt;
    private float m_NextExplosionAt;
    private float m_PatternExplosionRadius;
    private bool m_OwnsPlayer;
    private bool m_OwnsView;
    private bool m_FailRequested;
    private bool m_AwaitingNotice;
    private Func<float, float, float> m_RandomCoordinate;
    internal bool IsRaidManaged { get; set; }
    public bool IsRunning { get; private set; }
    public float TimeRemaining { get; private set; }
    public int RemainingHearts { get; private set; }
    public bool? LastResult { get; private set; }
    public event Action<bool> OnPatternCompleted;

    private void Awake()
    {
        m_RandomCoordinate = UnityEngine.Random.Range;
        if (m_PatternNoticeTxt != null) m_PatternNoticeTxt.gameObject.SetActive(false);
    }

    private IEnumerator Start()
    {
        yield return null;
        while (SceneChangeMgr.IsTransitioning) yield return null;
        if (m_AutoStartForTesting && !IsRaidManaged &&
            BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Raid) BeginPattern();
    }

    [ContextMenu("Development/Start zeolite pattern")]
    public void BeginPattern()
    {
        if (!Application.isPlaying || !isActiveAndEnabled || SceneChangeMgr.IsTransitioning) return;
        if (BattleFlowMgr.CurrentMode == BattleFlowMgr.BattleMode.Raid && !IsRaidManaged)
        {
            Debug.LogWarning("레이드 비석 패턴은 RaidPatternMgr를 통해 시작해주세요.", this);
            return;
        }
        CancelPattern();
        try
        {
            ValidateReferences();
            foreach (RaidZeoliteTarget stone in m_Stones)
            {
                stone.Initialize(HandleStoneHit);
                m_Order.Add(stone);
                m_AttackTargets.Add(stone);
            }
            for (int i = m_Order.Count - 1; i > 0; i--)
            {
                int other = UnityEngine.Random.Range(0, i + 1);
                (m_Order[i], m_Order[other]) = (m_Order[other], m_Order[i]);
            }
            if (!m_Player.BeginControl(m_MovementBounds, m_PlayerSpawnPoint.position, m_AttackTargets))
                throw new InvalidOperationException("비석 패턴: 전용 캐릭터 초기화에 실패했습니다.");
            m_OwnsPlayer = true;
            if (!m_HeartsUI.Show(AllowedMistakes))
                throw new InvalidOperationException("비석 패턴: 하트 Image 5개를 확인해주세요.");
            m_RuntimeExplosions = new GameObject("ZeoliteRuntimeExplosions");
            m_RuntimeExplosions.transform.SetParent(m_ExplosionRoot, false);
            m_PatternExplosionRadius = m_ExplosionRadius;
            m_MiniGameRoot.SetActive(true);
            m_OwnsView = true;
            m_PatternNoticeTxt.gameObject.SetActive(true);
            TimeRemaining = m_TimeLimit;
            RemainingHearts = AllowedMistakes;
            LastResult = null;
            m_DestroyedCount = 0;
            m_FailRequested = false;
            m_AwaitingNotice = false;
            m_NextExplosionAt = Time.time + m_ExplosionInterval;
            IsRunning = true;
            IssueNotice();
            RefreshTimer();
        }
        catch (Exception exception)
        {
            Debug.LogException(exception, this);
            CancelPattern();
        }
    }

    private void ValidateReferences()
    {
        if (m_Player == null || m_MovementBounds == null || m_PlayerSpawnPoint == null ||
            m_ExplosionRoot == null || m_MiniGameRoot == null || m_PatternNoticeTxt == null ||
            m_PatternTimerTxt == null || m_HeartsUI == null || m_DangerSignSprite == null || m_ExplosionPrefab == null)
            throw new InvalidOperationException("비석 패턴: 씬 참조, DangerSign, explosion_02를 모두 연결해주세요.");
        if (transform.IsChildOf(m_MiniGameRoot.transform) || m_Player.transform.IsChildOf(m_MiniGameRoot.transform))
            throw new InvalidOperationException("비석 패턴: 매니저와 PlayerRoot는 화면 UI 밖에 배치해주세요.");
        if (!m_MovementBounds.enabled || !m_MovementBounds.gameObject.activeInHierarchy ||
            Mathf.Abs(Mathf.DeltaAngle(0f, m_MovementBounds.transform.eulerAngles.z)) > 0.01f ||
            m_MovementBounds.edgeRadius > 0f)
            throw new InvalidOperationException("비석 패턴: 회전과 Edge Radius가 없는 활성 BoxCollider2D가 필요합니다.");
        Bounds area = m_MovementBounds.bounds;
        Vector3 spawn = m_PlayerSpawnPoint.position;
        if (spawn.x < area.min.x || spawn.x > area.max.x || spawn.y < area.min.y || spawn.y > area.max.y)
            throw new InvalidOperationException("비석 패턴: 스폰 지점이 MovementBounds 밖입니다.");
        if (m_ExplosionRadius <= 0f || area.size.x < m_ExplosionRadius * 2f || area.size.y < m_ExplosionRadius * 2f)
            throw new InvalidOperationException("비석 패턴: 폭발 반지름 전체가 MovementBounds 안에 들어가야 합니다.");
        if (m_Stones == null || m_Stones.Length != 4)
            throw new InvalidOperationException("비석 패턴: 비석 4개를 연결해주세요.");
        var symbols = new HashSet<RaidZeoliteSymbol>();
        foreach (RaidZeoliteTarget stone in m_Stones)
            if (stone == null || !stone.isActiveAndEnabled || !stone.HasValidReferences || !symbols.Add(stone.Symbol))
                throw new InvalidOperationException("비석 패턴: 서로 다른 문양 4개와 각 비석의 Renderer/스프라이트를 확인해주세요.");
        if (m_DangerSignSprite.bounds.size.x <= 0f || m_DangerSignSprite.bounds.size.y <= 0f ||
            m_ExplosionPrefab.GetComponentInChildren<SpriteRenderer>(true) == null)
            throw new InvalidOperationException("비석 패턴: 경고와 폭발의 SpriteRenderer 크기를 확인해주세요.");
    }

    private void IssueNotice()
    {
        m_AwaitingNotice = false;
        m_CurrentStone = m_Order[m_DestroyedCount];
        foreach (RaidZeoliteTarget stone in m_Order)
        {
            stone.SetCurrent(stone == m_CurrentStone);
            stone.SetAcceptsHits(!stone.IsBroken);
        }
        m_PatternNoticeTxt.text = m_CurrentStone.Symbol switch
        {
            RaidZeoliteSymbol.Anger => "내 분노를 받아라!",
            RaidZeoliteSymbol.Illusion => "허상 속을 헤매어라!",
            RaidZeoliteSymbol.Darkness => "끝없는 어둠에 빠져라!",
            _ => "악몽에서 벗어날 수 없다!"
        };
    }

    private void HandleStoneHit(RaidZeoliteTarget stone)
    {
        if (!IsRunning || m_AwaitingNotice || m_FailRequested || m_DestroyedCount == 4) return;
        if (stone != m_CurrentStone) { RegisterMistake(); return; }
        stone.ApplyCorrectHit();
        if (!stone.IsBroken) return;
        m_DestroyedCount++;
        foreach (RaidZeoliteTarget other in m_Order) { other.SetAcceptsHits(false); other.SetCurrent(false); }
        m_CurrentStone = null;
        m_PatternNoticeTxt.text = string.Empty;
        m_AwaitingNotice = true;
        m_NextNoticeAt = Time.time + m_NextNoticeDelay;
    }

    private void RegisterMistake(Vector2 knockbackDirection = default)
    {
        if (!IsRunning || m_FailRequested) return;
        RemainingHearts = Mathf.Max(0, RemainingHearts - 1);
        m_HeartsUI.SetRemaining(RemainingHearts);
        m_Player.ShowHitFlash(m_PlayerHitFlashDuration);
        m_Player.ApplyPenaltyKnockback(knockbackDirection);
        m_FailRequested = RemainingHearts == 0;
    }

    private void LateUpdate()
    {
        if (!IsRunning) return;
        // 마지막 비석 파괴가 같은 프레임의 폭발/시간 만료보다 우선한다.
        if (m_DestroyedCount == 4) { Complete(true); return; }
        ResolveExplosionWaves();
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        RefreshTimer();
        if (m_FailRequested || TimeRemaining <= 0f) { Complete(false); return; }
        if (m_AwaitingNotice && Time.time >= m_NextNoticeAt) IssueNotice();
        if (Time.time >= m_NextExplosionAt)
        {
            SpawnWarningWave();
            // 긴 프레임 뒤에도 여러 묶음을 한 프레임에 몰아서 생성하지 않는다.
            m_NextExplosionAt = Time.time + Mathf.Max(0.1f, m_ExplosionInterval);
        }
    }

    private void SpawnWarningWave()
    {
        m_OccupiedCenters.Clear();
        foreach (ExplosionWave pending in m_Waves) m_OccupiedCenters.AddRange(pending.Centers);
        Bounds bounds = m_MovementBounds.bounds;
        var wave = new ExplosionWave { CreatedAt = Time.time,
            ExplodeAt = Time.time + Mathf.Max(0.01f, m_WarningDuration) };
        RaidExplosionLayout.Generate(Rect.MinMaxRect(bounds.min.x, bounds.min.y, bounds.max.x, bounds.max.y),
            m_PatternExplosionRadius, m_ExplosionsPerWave, m_SpawnAttempts, m_OccupiedCenters,
            wave.Centers, m_RandomCoordinate);
        foreach (Vector2 center in wave.Centers)
        {
            var warning = new GameObject("ExplosionWarning");
            warning.transform.SetParent(m_RuntimeExplosions.transform, false);
            warning.layer = m_Player.gameObject.layer;
            var renderer = warning.AddComponent<SpriteRenderer>();
            renderer.sprite = m_DangerSignSprite;
            renderer.sortingLayerID = m_Stones[0].SortingLayerId;
            renderer.sortingOrder = m_WarningSortingOrder;
            FitVisual(warning, center, m_PatternExplosionRadius);
            wave.Warnings.Add(renderer);
        }
        if (wave.Centers.Count > 0) m_Waves.Add(wave);
    }

    private void ResolveExplosionWaves()
    {
        for (int i = m_Waves.Count - 1; i >= 0; i--)
        {
            ExplosionWave wave = m_Waves[i];
            if (Time.time < wave.ExplodeAt)
            {
                float pulse = (1f + Mathf.Cos((Time.time - wave.CreatedAt) * 2f * Mathf.PI /
                    Mathf.Max(0.01f, m_WarningBlinkPeriod))) * 0.5f;
                foreach (SpriteRenderer warning in wave.Warnings)
                    if (warning != null)
                    {
                        Color color = warning.color;
                        color.a = Mathf.Lerp(m_WarningMinAlpha, 1f, pulse);
                        warning.color = color;
                    }
                continue;
            }
            bool hit = false;
            float nearestHitDistance = float.PositiveInfinity;
            Vector2 knockbackDirection = Vector2.zero;
            foreach (SpriteRenderer warning in wave.Warnings)
                if (warning != null) { warning.gameObject.SetActive(false); Destroy(warning.gameObject); }
            foreach (Vector2 center in wave.Centers)
            {
                SpawnExplosionVisual(center);
                Vector2 offset = (Vector2)m_Player.transform.position - center;
                float distance = offset.sqrMagnitude;
                if (distance <= m_PatternExplosionRadius * m_PatternExplosionRadius && distance < nearestHitDistance)
                {
                    hit = true;
                    nearestHitDistance = distance;
                    knockbackDirection = offset;
                }
            }
            // 같은 묶음의 여러 폭발에 닿더라도 하트는 한 번만 차감한다.
            if (hit) RegisterMistake(knockbackDirection);
            m_Waves.RemoveAt(i);
        }
    }

    private void SpawnExplosionVisual(Vector2 center)
    {
        GameObject visual = Instantiate(m_ExplosionPrefab, m_RuntimeExplosions.transform, false);
        // 원본 프리팹의 Y 오프셋을 복제본에서 제거하여 경고 중심과 일치시킨다.
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        foreach (Transform child in visual.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = m_Player.gameObject.layer;
        foreach (Collider2D collider in visual.GetComponentsInChildren<Collider2D>(true)) collider.enabled = false;
        foreach (Animator animator in visual.GetComponentsInChildren<Animator>(true))
        {
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.speed = 1f;
            animator.Rebind();
            animator.Update(0f);
        }
        foreach (SpriteRenderer renderer in visual.GetComponentsInChildren<SpriteRenderer>(true))
        {
            renderer.sortingLayerID = m_Stones[0].SortingLayerId;
            renderer.sortingOrder = m_ExplosionSortingOrder;
        }
        FitVisual(visual, center, m_PatternExplosionRadius);
        // 원본의 애니메이션 종료 이벤트를 유지하고, 이벤트가 없더라도 잔류하지 않게 한다.
        Destroy(visual, m_ExplosionVisualLifetime);
    }

    private void FitVisual(GameObject visual, Vector2 center, float radius)
    {
        SpriteRenderer[] renderers = visual.GetComponentsInChildren<SpriteRenderer>(true);
        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        float size = Mathf.Max(bounds.size.x, bounds.size.y);
        if (size > 0.0001f) visual.transform.localScale *= radius * 2f / size;
        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        Vector3 position = new Vector3(center.x, center.y, m_PlayerSpawnPoint.position.z);
        visual.transform.position += position - bounds.center;
    }

    private void RefreshTimer()
    {
        if (m_PatternTimerTxt != null) m_PatternTimerTxt.text = $"남은 시간\n{TimeRemaining:0.00}";
    }

    [ContextMenu("Development/Force success")]
    public void ForceSuccess() { if (IsRunning) Complete(true); }

    [ContextMenu("Development/Force failure")]
    public void ForceFailure() { if (IsRunning) m_FailRequested = true; }

    private void Complete(bool succeeded)
    {
        if (!IsRunning) return;
        IsRunning = false;
        LastResult = succeeded;
        m_Player.EndControl();
        foreach (RaidZeoliteTarget stone in m_Order) stone.DisableTarget();
        m_HeartsUI.Hide();
        m_PatternNoticeTxt.gameObject.SetActive(false);
        ClearExplosions();
        Debug.Log(succeeded ? "문양 비석 파괴 패턴 성공" : "문양 비석 파괴 패턴 실패", this);
        OnPatternCompleted?.Invoke(succeeded);
    }

    private void ClearExplosions()
    {
        m_Waves.Clear();
        m_OccupiedCenters.Clear();
        if (m_RuntimeExplosions == null) return;
        m_RuntimeExplosions.SetActive(false);
        m_RuntimeExplosions.transform.SetParent(null);
        Destroy(m_RuntimeExplosions);
        m_RuntimeExplosions = null;
    }

    public void CancelPattern()
    {
        IsRunning = false;
        if (m_OwnsPlayer && m_Player != null) { m_Player.EndControl(); m_Player.ClearHitFlash(); }
        if (m_OwnsView && m_MiniGameRoot != null) m_MiniGameRoot.SetActive(false);
        if (m_HeartsUI != null) m_HeartsUI.Hide();
        if (m_PatternNoticeTxt != null) { m_PatternNoticeTxt.text = string.Empty; m_PatternNoticeTxt.gameObject.SetActive(false); }
        if (m_Stones != null)
            foreach (RaidZeoliteTarget stone in m_Stones)
                if (stone != null) stone.ResetTarget();
        ClearExplosions();
        m_Order.Clear();
        m_AttackTargets.Clear();
        m_CurrentStone = null;
        m_DestroyedCount = 0;
        m_OwnsPlayer = false;
        m_OwnsView = false;
        m_FailRequested = false;
        m_AwaitingNotice = false;
        TimeRemaining = 0f;
        RemainingHearts = 0;
        LastResult = null;
    }

    private void OnDisable() => CancelPattern();
}
