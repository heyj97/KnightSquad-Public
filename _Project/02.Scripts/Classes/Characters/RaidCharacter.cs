using UnityEngine;
using System.Collections.Generic;

/// <summary>레이드 미니게임 전용 이동과 단일 대상 공격. 일반 전투 상태와 별도로 제어한다.</summary>
public class RaidCharacter : MonoBehaviour
{
    [SerializeField] private SPUM_Prefabs m_Model;
    [SerializeField, Min(0f)] private float m_MoveSpeed = 3f;
    [SerializeField, Min(0.01f)] private float m_AttackRange = 1.2f;
    [SerializeField, Range(0f, 180f)] private float m_AttackHalfAngle = 70f;
    [SerializeField, Min(0.01f)] private float m_FallbackAttackDuration = 0.5f;
    [SerializeField] private bool m_ModelFacesRight = true;
    [Header("Penalty knockback")]
    [SerializeField, Min(0f)] private float m_KnockbackDistance = 0.3f;
    [SerializeField, Min(0.01f)] private float m_KnockbackDuration = 0.12f;
    [SerializeField, Min(0f)] private float m_HitInputBlockDuration = 0.25f;
    private BoxCollider2D m_Bounds;
    private IReadOnlyList<IRaidAttackTarget> m_Targets;
    private Vector3 m_ModelScale;
    private Vector2 m_Facing = Vector2.right;
    private float m_AttackRemaining;
    private PlayerState? m_Animation;
    private bool m_Initialized;
    private IReadOnlyList<BoxCollider2D> m_FreeMovementBounds;
    private System.Func<Vector3, Vector3, Vector3> m_ResolveFreeMovement;
    private float m_FreeMovementStep;
    private SpriteRenderer[] m_HitRenderers;
    private Color[] m_HitOriginalColors;
    private float m_HitFlashUntil = -1f;
    private float m_InputBlockedUntil = -1f;
    private bool m_IsKnockedBack;
    private Vector3 m_KnockbackFrom;
    private Vector3 m_KnockbackTo;
    private float m_KnockbackStartedAt;
    private float m_ActiveKnockbackDuration;
    public bool IsControllable { get; private set; }

    public bool BeginControl(BoxCollider2D movementBounds, Vector3 spawnPosition,
        IReadOnlyList<IRaidAttackTarget> targets)
    {
        EndControl();
        ClearHitFlash();
        if (movementBounds == null || !movementBounds.enabled || !movementBounds.gameObject.activeInHierarchy ||
            Mathf.Abs(Mathf.DeltaAngle(0f, movementBounds.transform.eulerAngles.z)) > 0.01f)
        {
            Debug.LogError("RaidCharacter: 활성화된 회전 없는 MovementBounds가 필요합니다.", this);
            return false;
        }
        if (!TryInitializeModel(true)) return false;
        m_Bounds = movementBounds;
        m_Targets = targets;
        m_Facing = Vector2.right;
        SetModelFacing();
        transform.position = ClampPosition(spawnPosition);
        m_Animation = null;
        PlayAnimation(PlayerState.IDLE);
        IsControllable = true;
        return true;
    }

    private bool TryInitializeModel(bool requiresAttack)
    {
        if (m_Model == null) m_Model = GetComponentInChildren<SPUM_Prefabs>();
        if (m_Model == null || m_Model._anim == null || m_Model._anim.runtimeAnimatorController == null ||
            m_Model.IDLE_List.Count == 0 || m_Model.IDLE_List[0] == null ||
            m_Model.MOVE_List.Count == 0 || m_Model.MOVE_List[0] == null ||
            requiresAttack && (m_Model.ATTACK_List.Count == 0 || m_Model.ATTACK_List[0] == null))
        {
            Debug.LogError("RaidCharacter: SPUM 모델과 IDLE/MOVE/ATTACK 클립을 확인해주세요.", this);
            return false;
        }
        if (!m_Initialized)
        {
            m_ModelScale = m_Model.transform.localScale;
            m_Model.OverrideControllerInit();
            m_Initialized = true;
        }
        return true;
    }

    internal bool PrepareForPreview(Vector3 spawnPosition)
    {
        EndControl();
        ClearHitFlash();
        if (!TryInitializeModel(false)) return false;
        m_Facing = Vector2.right;
        SetModelFacing();
        transform.position = spawnPosition;
        m_Animation = null;
        PlayAnimation(PlayerState.IDLE);
        return true;
    }

    internal bool BeginFreeMovement(IReadOnlyList<BoxCollider2D> movementBounds, Vector3 spawnPosition,
        System.Func<Vector3, Vector3, Vector3> resolveMovement, float movementStep)
    {
        if (!PrepareForPreview(spawnPosition)) return false;
        if (movementBounds == null || movementBounds.Count == 0 || resolveMovement == null) return false;
        foreach (BoxCollider2D bounds in movementBounds)
            if (!IsValidMovementBounds(bounds)) return false;
        m_FreeMovementBounds = movementBounds;
        m_ResolveFreeMovement = resolveMovement;
        m_FreeMovementStep = Mathf.Max(0.01f, movementStep);
        if (!IsFreeSegmentAllowed(spawnPosition, spawnPosition))
        {
            EndControl();
            return false;
        }
        IsControllable = true;
        return true;
    }

    public void EndControl()
    {
        IsControllable = false;
        m_IsKnockedBack = false;
        m_InputBlockedUntil = -1f;
        m_AttackRemaining = 0f;
        m_Targets = null;
        m_FreeMovementBounds = null;
        m_ResolveFreeMovement = null;
        if (m_Initialized && m_Model != null && m_Model._anim != null) PlayAnimation(PlayerState.IDLE);
    }

    private void Update()
    {
        if (m_HitRenderers != null && Time.time >= m_HitFlashUntil) ClearHitFlash();
        if (!IsControllable) return;
        if (m_FreeMovementBounds != null)
        {
            foreach (BoxCollider2D bounds in m_FreeMovementBounds)
                if (!IsValidMovementBounds(bounds)) { EndControl(); return; }
        }
        else if (m_Bounds == null || !m_Bounds.enabled || !m_Bounds.gameObject.activeInHierarchy)
        {
            EndControl();
            return;
        }
        if (SceneChangeMgr.IsTransitioning || PlayerDataMgr.IsProgressBlocked)
            return;
        if (m_IsKnockedBack)
        {
            UpdateKnockback();
            return;
        }
        if (MessageBoxMgr.IsBlockingInput) return;
        if (Time.time < m_InputBlockedUntil) return;
        if (m_AttackRemaining > 0f)
        {
            m_AttackRemaining = Mathf.Max(0f, m_AttackRemaining - Time.deltaTime);
            if (m_AttackRemaining == 0f) PlayAnimation(PlayerState.IDLE);
            return;
        }
        Vector2 movement = new Vector2(
            (Input.GetKey(KeyCode.RightArrow) ? 1 : 0) - (Input.GetKey(KeyCode.LeftArrow) ? 1 : 0),
            (Input.GetKey(KeyCode.UpArrow) ? 1 : 0) - (Input.GetKey(KeyCode.DownArrow) ? 1 : 0)).normalized;
        if (movement.x != 0f)
        {
            m_Facing = movement.x > 0f ? Vector2.right : Vector2.left;
            SetModelFacing();
        }
        if (m_FreeMovementBounds != null)
        {
            MoveFreely(movement);
            return;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            PlayAnimation(PlayerState.ATTACK);
            AnimationClip clip = m_Model.ATTACK_List[0];
            m_AttackRemaining = clip != null ? Mathf.Max(0.01f, clip.length / Mathf.Max(0.01f, m_Model._anim.speed))
                : m_FallbackAttackDuration;
            FindNearestTarget()?.ReceiveHit();
            return;
        }
        transform.position = ClampPosition(transform.position + (Vector3)(movement * m_MoveSpeed * Time.deltaTime));
        PlayAnimation(movement == Vector2.zero ? PlayerState.IDLE : PlayerState.MOVE);
    }

    private void MoveFreely(Vector2 movement)
    {
        Vector3 displacement = movement * (m_MoveSpeed * Time.deltaTime);
        int steps = Mathf.Max(1, Mathf.CeilToInt(displacement.magnitude / m_FreeMovementStep));
        Vector3 step = displacement / steps;
        bool moved = false;
        for (int i = 0; i < steps && IsControllable; i++)
        {
            Vector3 previous = transform.position;
            Vector3 candidate = previous + step;
            if (!IsFreeSegmentAllowed(previous, candidate)) break;
            Vector3 resolved = m_ResolveFreeMovement(previous, candidate);
            transform.position = resolved;
            moved |= (resolved - previous).sqrMagnitude > 0.000001f;
            // 위험 판정으로 복귀한 뒤 같은 프레임의 나머지 이동은 처리하지 않는다.
            if ((resolved - candidate).sqrMagnitude > 0.000001f) break;
        }
        if (IsControllable)
            PlayAnimation(moved && Time.time >= m_InputBlockedUntil ? PlayerState.MOVE : PlayerState.IDLE);
    }

    private static bool IsValidMovementBounds(BoxCollider2D bounds) => bounds != null && bounds.enabled &&
        bounds.gameObject.activeInHierarchy &&
        Mathf.Abs(Mathf.DeltaAngle(0f, bounds.transform.eulerAngles.z)) <= 0.01f;

    private bool IsFreeSegmentAllowed(Vector3 from, Vector3 to)
    {
        // 각 사각형과 겹치는 선분 구간의 합집합으로 검사한다. 떨어진 영역 사이를 건너뛰지 않는다.
        float covered = 0f;
        for (int pass = 0; pass < m_FreeMovementBounds.Count; pass++)
        {
            foreach (BoxCollider2D collider in m_FreeMovementBounds)
            {
                Bounds bounds = collider.bounds;
                float enter = 0f, exit = 1f;
                if (ClipMovementAxis(from.x, to.x - from.x, bounds.min.x, bounds.max.x, ref enter, ref exit) &&
                    ClipMovementAxis(from.y, to.y - from.y, bounds.min.y, bounds.max.y, ref enter, ref exit) &&
                    enter <= covered + 0.000001f)
                    covered = Mathf.Max(covered, exit);
            }
            if (covered >= 1f) return true;
        }
        return false;
    }

    private static bool ClipMovementAxis(float origin, float delta, float min, float max,
        ref float enter, ref float exit)
    {
        if (Mathf.Abs(delta) < 0.000001f) return origin >= min && origin <= max;
        float first = (min - origin) / delta, second = (max - origin) / delta;
        enter = Mathf.Max(enter, Mathf.Min(first, second));
        exit = Mathf.Min(exit, Mathf.Max(first, second));
        return enter <= exit;
    }

    /// <summary>방향이 없으면 바라보는 방향 반대로 밀린다. 길찾기 자유이동에는 적용하지 않는다.</summary>
    internal void ApplyPenaltyKnockback(Vector2 direction = default)
    {
        if (!IsControllable || m_FreeMovementBounds != null || !IsValidMovementBounds(m_Bounds)) return;
        if (direction.sqrMagnitude < 0.000001f) direction = -m_Facing;
        m_AttackRemaining = 0f;
        m_KnockbackFrom = transform.position;
        m_KnockbackTo = ClampPosition(m_KnockbackFrom + (Vector3)(direction.normalized * m_KnockbackDistance));
        m_KnockbackStartedAt = Time.time;
        m_ActiveKnockbackDuration = Mathf.Max(0.01f, m_KnockbackDuration);
        m_IsKnockedBack = true;
        BlockControlFor(Mathf.Max(m_HitInputBlockDuration, m_ActiveKnockbackDuration));
        // IDLE 파라미터만 설정하면 SPUM의 현재 ATTACK 상태가 끝날 때까지 남으므로 직접 중단한다.
        m_Model._anim.ResetTrigger("2_Attack");
        if (m_Model._anim.HasState(0, Animator.StringToHash("Base Layer.IDLE")))
        {
            m_Model._anim.Play("IDLE", 0, 0f);
            m_Model._anim.Update(0f);
        }
    }

    private void UpdateKnockback()
    {
        float progress = Mathf.Clamp01((Time.time - m_KnockbackStartedAt) / m_ActiveKnockbackDuration);
        float eased = 1f - (1f - progress) * (1f - progress);
        transform.position = ClampPosition(Vector3.Lerp(m_KnockbackFrom, m_KnockbackTo, eased));
        if (progress >= 1f) m_IsKnockedBack = false;
    }

    internal void BlockControlFor(float duration)
    {
        m_InputBlockedUntil = Mathf.Max(m_InputBlockedUntil, Time.time + Mathf.Max(0f, duration));
        if (m_Initialized) PlayAnimation(PlayerState.IDLE);
    }

    internal void ShowHitFlash(float duration)
    {
        if (m_Model == null) return;
        if (m_HitRenderers == null)
        {
            m_HitRenderers = m_Model.GetComponentsInChildren<SpriteRenderer>(true);
            m_HitOriginalColors = new Color[m_HitRenderers.Length];
            for (int i = 0; i < m_HitRenderers.Length; i++)
                m_HitOriginalColors[i] = m_HitRenderers[i].color;
        }
        m_HitFlashUntil = Time.time + Mathf.Max(0.01f, duration);
        for (int i = 0; i < m_HitRenderers.Length; i++)
            if (m_HitRenderers[i] != null)
                m_HitRenderers[i].color = new Color(1f, 0.15f, 0.15f, m_HitOriginalColors[i].a);
    }

    internal void ClearHitFlash()
    {
        if (m_HitRenderers == null) return;
        for (int i = 0; i < m_HitRenderers.Length; i++)
            if (m_HitRenderers[i] != null) m_HitRenderers[i].color = m_HitOriginalColors[i];
        m_HitRenderers = null;
        m_HitOriginalColors = null;
        m_HitFlashUntil = -1f;
    }

    private IRaidAttackTarget FindNearestTarget()
    {
        IRaidAttackTarget nearest = null;
        float bestDistance = m_AttackRange * m_AttackRange;
        float minimumDot = Mathf.Cos(m_AttackHalfAngle * Mathf.Deg2Rad);
        if (m_Targets == null) return null;
        foreach (IRaidAttackTarget target in m_Targets)
        {
            if (target == null || target is UnityEngine.Object unityTarget && unityTarget == null || !target.CanBeHit)
                continue;
            Vector2 offset = target.Position - transform.position;
            float distance = offset.sqrMagnitude;
            if (distance > bestDistance || distance > 0.0001f && Vector2.Dot(m_Facing, offset.normalized) < minimumDot)
                continue;
            nearest = target;
            bestDistance = distance;
        }
        return nearest;
    }

    private Vector3 ClampPosition(Vector3 position)
    {
        Bounds bounds = m_Bounds.bounds;
        position.x = Mathf.Clamp(position.x, bounds.min.x, bounds.max.x);
        position.y = Mathf.Clamp(position.y, bounds.min.y, bounds.max.y);
        return position;
    }

    private void SetModelFacing()
    {
        if (Mathf.Abs(m_Facing.x) < 0.001f) return;
        Vector3 scale = m_ModelScale;
        scale.x = Mathf.Abs(scale.x) * ((m_Facing.x > 0f) == m_ModelFacesRight ? 1f : -1f);
        m_Model.transform.localScale = scale;
    }

    private void PlayAnimation(PlayerState state)
    {
        if (m_Animation == state) return;
        m_Model.PlayAnimation(state, 0);
        m_Animation = state;
    }

    private void OnDisable()
    {
        EndControl();
        ClearHitFlash();
    }
}
