using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 몬스터의 전투 능력치, 공격 대상 및 사망 상태를 관리하는 런타임 컴포넌트입니다.
/// </summary>
public class MonsterBase : MonoBehaviour, IDamageable, ITargetable, IAttackable
{
    private float m_MaxHp = 0;
    private float m_CurHp = 0;
    private bool m_IsDead = false;
    private float m_Defense = 0;
    private bool m_IsTargetable = false;
    private float m_AttackPower = 0;
    private float m_AttackSpeed = 0;
    private bool m_CanAttack = true;
    private MonsterData m_MonsterData;

    private bool m_IsInitialize = false;
    private int m_FieldRewardSlot = -1;
    internal void BindFieldRewardSlot(int slot) => m_FieldRewardSlot = slot;
    

    private static readonly int[] TargetSlotWeights =
    {
        30, // 0번 슬롯
        40, // 1번 슬롯
        10, // 2번 슬롯
        20  // 3번 슬롯
    };

    private Transform m_CharacterSpawnpointsRoot;
    private float m_AttackCoolDown;
    private float m_RaidAttackInterval = 3f;
    private float m_RaidAreaAttackChance = 0.2f;
    private readonly List<CharacterBase> m_RaidTargets = new();

    /// <summary>레이드 공격 주기와 전체 공격 확률을 설정한다. 첫 공격도 한 주기 후 실행한다.</summary>
    public void ConfigureRaidAttack(float interval, float areaAttackChance)
    {
        m_RaidAttackInterval = Mathf.Max(0.01f, interval);
        m_RaidAreaAttackChance = Mathf.Clamp01(areaAttackChance);
        m_AttackCoolDown = m_RaidAttackInterval;
    }
    

    [Header("SPUM Animation"), Tooltip("SPUM_Prefabs가 붙은 본인 오브젝트를 연결")]
    [SerializeField] private SPUM_Prefabs m_Spum;



    [Header("Skill Effect")]
    [SerializeField] private Transform m_SkillEffectTarget;

    /// <summary>
    /// 스킬 이펙트를 몬스터 모델에 부착할 부모 지점을 가져옵니다.
    /// </summary>
    public Transform SkillEffectTarget => m_SkillEffectTarget;
    // 스킬 이펙트용 변수
    
    //--- 이벤트 함수
    /// <summary>
    /// 몬스터 체력이 변경되었을 때 현재 체력과 최대 체력을 전달합니다.
    /// </summary>
    public event Action<float, float> OnMonHpChanged;
    /// <summary>
    /// 몬스터가 사망하여 더 이상 선택 대상이 아니게 되었을 때 발생합니다.
    /// </summary>
    public event Action<ITargetable> OnCharTargetableDisabled;
    /// <summary>
    /// 몬스터가 사망 처리된 직후 해당 런타임 인스턴스를 전달합니다.
    /// </summary>
    public event Action<MonsterBase> OnMonDead;
    /// <summary>
    /// 몬스터가 피해를 받은 뒤 최종 피해량과 치명타 여부를 전달합니다.
    /// </summary>
    public event Action<float, bool> OnMonDamageTaken;
    //--- 이벤트 함수

    /// <summary>
    /// 현재 몬스터가 가질 수 있는 최대 체력을 가져옵니다.
    /// </summary>
    public float MaxHp => m_MaxHp;
    /// <summary>
    /// 피해와 회복이 반영된 몬스터의 현재 체력을 가져옵니다.
    /// </summary>
    public float CurHp => m_CurHp;
    /// <summary>
    /// 몬스터가 사망 처리되어 전투에서 제외되었는지 나타냅니다.
    /// </summary>
    public bool IsDead => m_IsDead;
    /// <summary>
    /// 받는 피해 계산에 사용하는 몬스터의 현재 방어력을 가져옵니다.
    /// </summary>
    public float Defense => m_Defense;

    /// <summary>
    /// 몬스터가 현재 호버·선택 및 스킬 대상으로 지정될 수 있는지 나타냅니다.
    /// </summary>
    public bool IsTargetable => m_IsTargetable;

    /// <summary>
    /// 기본 공격 피해 계산에 사용하는 몬스터의 현재 공격력을 가져옵니다.
    /// </summary>
    public float AttackPower => m_AttackPower;

    /// <summary>
    /// 일반 공격 간격 계산에 사용하는 몬스터의 현재 공격속도를 가져옵니다.
    /// </summary>
    public float AttackSpeed => m_AttackSpeed;

    /// <summary>
    /// 몬스터가 현재 일반 공격을 실행할 수 있는 상태인지 나타냅니다.
    /// </summary>
    public bool CanAttack => m_CanAttack;
    
    /// <summary>
    /// 공격 거리와 대상 판정에 사용하는 몬스터의 월드 위치를 가져옵니다.
    /// </summary>
    public Vector3 MonPosition => transform.position;

    /// <summary>
    /// 현재 몬스터를 초기화한 마스터 데이터를 가져옵니다.
    /// </summary>
    public MonsterData MonData => m_MonsterData;
    
    protected virtual void Awake()
    {
    }

    protected virtual void Update()
    {
        if (!BattleFlowMgr.CanRunCombat || !m_IsInitialize || m_IsDead)
            return;

        m_AttackCoolDown -= Time.deltaTime;

        if (m_AttackCoolDown > 0f)
            return;

        if (m_MonsterData.Grade == MonsterGrade.RaidBoss)
        {
            TryRaidAttack();
            return;
        }

        if (!TryFindRandomTarget(out IDamageable target))
            return;

        if (!Attack(target))
            return;

        PlayAttackAnimation();

        // AttackSpeed가 초당 공격 횟수라는 기준
        m_AttackCoolDown = 1f / Mathf.Max(0.01f, m_AttackSpeed);
    }

    private void TryRaidAttack()
    {
        m_RaidTargets.Clear();
        for (int slot = 0; slot < m_CharacterSpawnpointsRoot.childCount; slot++)
            if (TryGetValidTargetAtSlot(slot, out CharacterBase character) && character.IsInitialized)
                m_RaidTargets.Add(character);
        if (m_RaidTargets.Count == 0)
            return;

        bool attacked = false;
        if (UnityEngine.Random.value < m_RaidAreaAttackChance)
        {
            foreach (CharacterBase character in m_RaidTargets)
                attacked |= Attack(character);
        }
        else
            attacked = Attack(m_RaidTargets[UnityEngine.Random.Range(0, m_RaidTargets.Count)]);

        if (!attacked) return;
        PlayAttackAnimation();
        m_AttackCoolDown = m_RaidAttackInterval;
    }

    private bool TryFindRandomTarget(out IDamageable target)
    {
        target = null;

        if (m_CharacterSpawnpointsRoot == null)
            return false;

        int slotCount = Mathf.Min(m_CharacterSpawnpointsRoot.childCount, TargetSlotWeights.Length);

        int totalWeight = 0;

        // 살아 있고 타게팅 가능한 슬롯의 가중치만 합산
        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (!TryGetValidTargetAtSlot(slotIndex, out CharacterBase character))
            {
                continue;
            }

            totalWeight += TargetSlotWeights[slotIndex];
        }

        if (totalWeight <= 0)
            return false;

        int randomValue = UnityEngine.Random.Range(0, totalWeight);
        int accumulatedWeight = 0;

        for (int slotIndex = 0; slotIndex < slotCount; slotIndex++)
        {
            if (!TryGetValidTargetAtSlot(
                    slotIndex,
                    out CharacterBase character))
            {
                continue;
            }

            accumulatedWeight += TargetSlotWeights[slotIndex];

            if (randomValue < accumulatedWeight)
            {
                target = character;
                return true;
            }
        }

        return false;
    }
    
    private bool TryGetValidTargetAtSlot(
        int slotIndex,
        out CharacterBase character)
    {
        character = null;

        if (m_CharacterSpawnpointsRoot == null)
            return false;

        if (slotIndex < 0 ||
            slotIndex >= m_CharacterSpawnpointsRoot.childCount)
        {
            return false;
        }

        Transform spawnpoint =
            m_CharacterSpawnpointsRoot.GetChild(slotIndex);

        character =
            spawnpoint.GetComponentInChildren<CharacterBase>();

        if (character == null)
            return false;

        if (character.IsDead || !character.IsTargetable)
            return false;

        return true;
    }
    
    /// <summary>
    /// 몬스터 마스터 데이터와 캐릭터 배치 루트를 적용하여 풀에서 사용할 상태로 초기화합니다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <param name="characterSpawnpointsRoot">몬스터가 공격 대상으로 탐색할 캐릭터 배치 루트입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool Initialize(MonsterData data, Transform characterSpawnpointsRoot)
    {
        if (data == null || characterSpawnpointsRoot == null)
        {
            PDebug.LogError($"초기화 참조 누락: MonsterData={data != null}, CharacterSpawnpointsRoot={characterSpawnpointsRoot != null}");

            return false;
        }

        m_Spum = GetComponentInChildren<SPUM_Prefabs>();

        if (m_Spum == null)
        {
            PDebug.LogError("SPUM_Prefabs가 없습니다.");

            return false;
        }

        m_Spum.OverrideControllerInit();

        m_MonsterData = data;
        m_CharacterSpawnpointsRoot = characterSpawnpointsRoot;

        int chapterStep = 0;

        // 필드 챕터 성장 적용. 레이드 보스는 기본 스탯 유지.
        if (data.Grade != MonsterGrade.RaidBoss)
        {
            if (StageMgr.Inst == null || StageMgr.Inst.CurChapter < 1)
            {
                PDebug.LogError("몬스터 초기화 실패: 유효한 챕터 정보가 없습니다.");
                return false;
            }

            chapterStep = StageMgr.Inst.CurChapter - 1;
        }

        float hpMultiplier = Mathf.Pow(
            GlobalConstants.FieldMonsterHpChapterFactor, chapterStep);

        float attackMultiplier = Mathf.Pow(
            GlobalConstants.FieldMonsterAttackChapterFactor, chapterStep);

        float defenseMultiplier = Mathf.Pow(
            GlobalConstants.FieldMonsterDefenseChapterFactor, chapterStep);

        // 매번 원본 데이터에서 계산하므로 재도전 시 중복 적용되지 않는다.
        m_MaxHp = data.BaseMaxHp * hpMultiplier;
        m_CurHp = m_MaxHp;
        m_Defense = data.BaseDefense * defenseMultiplier;
        m_AttackPower = data.BaseAttack * attackMultiplier;
        m_AttackSpeed = data.BaseAttackSpeed;

        m_IsDead = false;
        m_IsTargetable = true;
        m_CanAttack = true;
        m_IsInitialize = true;

        // 필드는 즉시 첫 공격, 레이드는 설정한 주기만큼 대기한다.
        m_AttackCoolDown = data.Grade == MonsterGrade.RaidBoss ? m_RaidAttackInterval : 0f;

        return true;
    }
    
    /// <summary>
    /// 전달된 피해에 방어 규칙을 적용하고 체력 감소, 피격 알림 및 사망 처리를 수행합니다.
    /// </summary>
    /// <param name="dmg">방어 적용 전에 전달되는 공격 피해량입니다.</param>
    /// <param name="isCritical">치명타로 처리된 피해인지 여부입니다.</param>
    public void TakeDmg(float dmg, bool isCritical = false)
    {
        TakeDmgFromCharacter(dmg, isCritical, null);
    }

    internal void TakeDmgFromCharacter(float dmg, bool isCritical, CharacterBase attacker)
    {
        if (!m_IsInitialize)
            return;
        
        // 이미 죽었거나 피해가 없다면 피해 처리 안 함
        if (m_IsDead || dmg <= 0f || BattleFlowMgr.IsRaidPatternPaused)
            return;

        float finalDamage = DamageCalculator.CalculateFinalDamage(
            dmg, m_Defense, GlobalConstants.CharDefenseConst);
        if (finalDamage <= 0f)
            return;

        BattleFlowMgr.RecordRaidDamage(attacker, this, Mathf.Min(m_CurHp, finalDamage));
        ApplyDamage(finalDamage, isCritical, false);
    }

    /// <summary>레이드 성공 피해. 방어력과 치명타 없이 보스 최대 HP 비율로 적용한다.</summary>
    internal void TakeRaidPatternDamage(float maxHpRatio)
    {
        if (m_IsDead || maxHpRatio <= 0f) return;
        float damage = m_MaxHp * Mathf.Clamp01(maxHpRatio);
        BattleFlowMgr.RecordRaidPatternDamage(this, Mathf.Min(m_CurHp, damage));
        ApplyDamage(damage, false, true);
    }

    private void ApplyDamage(float finalDamage, bool isCritical, bool isPatternDamage)
    {
        // 현재 체력 감소 및 하한 보정
        m_CurHp = Mathf.Max(0f, m_CurHp - finalDamage);

        OnMonHpChanged?.Invoke(m_CurHp, m_MaxHp);
        OnMonDamageTaken?.Invoke(finalDamage, isCritical);

        // 생존한 레이드 보스가 새 HP 구간에 진입하면 일반 전투를 멈추고 패턴을 예약한다.
        if (!isPatternDamage && BattleFlowMgr.TryHandleRaidBossDamage(this)) return;
        FinalizeRaidDeath();
    }

    internal void FinalizeRaidDeath()
    {
        // 사망 판정
        if (!m_IsDead && m_CurHp <= 0f)
        {
            // 죽음여부 bool값 true
            m_IsDead = true;
            // 타게팅 불가능하도록 타겟가능 여부 false
            m_IsTargetable = false;

            OnCharTargetableDisabled?.Invoke(this);

            Death();
        }
    }

    protected void Death()
    {
        PlayDeathAnimation();

        // 추후 골드추가 텍스트이펙트 추가
        // 레이드 보상은 BattleFlowMgr에서 승리 확정 시 한 번만 지급한다.
        if (BattleFlowMgr.CurrentMode == BattleFlowMgr.BattleMode.Field)
        {
            StageMgr.Inst.GrantFieldReward(m_FieldRewardSlot, m_MonsterData.Grade);
        }

        OnMonDead?.Invoke(this);
    }

    void AddGoldByMonsterGrade(MonsterGrade grade)
    {
        ulong gold;

        switch (grade)
        {
            case MonsterGrade.Normal:
                gold = 10;
                break;

            case MonsterGrade.Elite:
                gold = 50;
                break;

            case MonsterGrade.StageBoss:
                gold = 200;
                break;

            default:
                PDebug.LogError("지원하지 않는 필드 몬스터 등급입니다.");
                return;
        }

        if (StageMgr.Inst == null)
        {
            PDebug.LogError("필드 골드 지급 실패: StageMgr가 없습니다.");
            return;
        }

        StageMgr.Inst.AddFieldGold(gold);
    }


    /// <summary>
    /// 유효한 피해 대상에게 몬스터 공격력으로 계산한 피해를 적용합니다.
    /// </summary>
    /// <param name="target">공격, 회복 또는 스킬 효과를 적용할 대상입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool Attack(IDamageable target)
    {
        // 공격 불가능 상태 예외처리
        if (!BattleFlowMgr.CanRunCombat || !m_CanAttack || m_IsDead || target == null || target.IsDead || !m_IsInitialize)
            return false;

        // 몬스터는 현재 치명타 없이 공격 배율만 적용한다.
        float damage = DamageCalculator.CalculateRawDamage(m_AttackPower, 1f, false, 1f);
        target.TakeDmg(damage);

        // 공격 성공
        return true;
    }

    /// <summary>
    /// 몬스터를 오브젝트 풀에 반환할 수 있도록 런타임 참조와 상태를 정리합니다.
    /// </summary>
    public void ResetForPool()
    {
        m_IsInitialize = false;
        m_IsTargetable = false;
        m_CanAttack = false;
        m_Spum = null;
        m_CharacterSpawnpointsRoot = null;
        m_MonsterData = null;
        m_RaidTargets.Clear();
    }

    /// <summary>
    /// 스테이지 전환 상태에 맞춰 몬스터 모델의 이동 애니메이션을 전환합니다.
    /// </summary>
    /// <param name="moving">이동 애니메이션을 재생할지 여부입니다.</param>
    public void SetTransitionMoving(bool moving)
    {
        if (!m_IsInitialize || m_IsDead || m_Spum == null)
            return;
        // 연출 수정 위치: 몬스터의 이동/도착 애니메이션을 변경한다.
        if (moving)
            PlayMoveAnimation();
        else
            PlayIdleAnimation();
    }

    /// <summary>일반 전투를 재개하지 않고 전조용 Magic 공격만 1회 재생한다.</summary>
    internal bool TryBeginRaidStaging(out Animator animator, out float duration)
    {
        animator = m_Spum != null ? m_Spum._anim : null;
        duration = 0f;
        const int magicAttackIndex = 2;
        if (!m_IsInitialize || m_IsDead || animator == null ||
            animator.runtimeAnimatorController == null || m_Spum.ATTACK_List.Count <= magicAttackIndex ||
            m_Spum.ATTACK_List[magicAttackIndex] == null || m_Spum.IDLE_List.Count == 0 ||
            m_Spum.IDLE_List[0] == null)
        {
            Debug.LogError("레이드 전조: SPUM Animator와 ATTACK[2](Magic), IDLE[0]을 확인해주세요.", this);
            animator = null;
            return false;
        }
        // 전조 연출만 정상 속도로 재생한다. 원래 속도는 RaidPatternMgr가 별도로 복원한다.
        animator.speed = 1f;
        ResetRaidStagingTriggers();
        m_Spum.PlayAnimation(PlayerState.ATTACK, magicAttackIndex);
        animator.ResetTrigger("2_Attack");
        animator.Play("ATTACK", 0, 0f);
        animator.Update(0f);
        duration = m_Spum.ATTACK_List[magicAttackIndex].length;
        return true;
    }

    internal void PlayRaidStagingIdle()
    {
        if (m_Spum == null || m_Spum._anim == null || m_IsDead || m_Spum.IDLE_List.Count == 0) return;
        ResetRaidStagingTriggers();
        PlayIdleAnimation();
        m_Spum._anim.Play("IDLE", 0, 0f);
        m_Spum._anim.Update(0f);
    }

    private void ResetRaidStagingTriggers()
    {
        foreach (AnimatorControllerParameter parameter in m_Spum._anim.parameters)
            if (parameter.type == AnimatorControllerParameterType.Trigger)
                m_Spum._anim.ResetTrigger(parameter.nameHash);
    }

    protected void PlayIdleAnimation(int index = 0)
    {
        m_Spum.PlayAnimation(PlayerState.IDLE, index);
    }

    protected void PlayMoveAnimation(int index = 0)
    {
        m_Spum.PlayAnimation(PlayerState.MOVE, index);
    }

    protected virtual void PlayAttackAnimation(int index = 0)
    {
        m_Spum.PlayAnimation(PlayerState.ATTACK, index);
    }

    protected virtual void PlayDeathAnimation(int index = 0)
    {
        m_Spum.PlayAnimation(PlayerState.DEATH, index);
    }

}
