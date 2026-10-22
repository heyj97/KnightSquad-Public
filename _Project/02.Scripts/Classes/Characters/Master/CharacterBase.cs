using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

/// <summary>
/// 캐릭터의 전투 능력치, 장비, 스킬 및 대상 선택 상태를 관리하는 런타임 기반 클래스입니다.
/// </summary>
public abstract class CharacterBase : MonoBehaviour,
    IHealable,
    IDamageable,
    ITargetable,
    ISkillUser,
    IAttackable
{
#region 스크립트용 Private 변수

    private float m_MaxHp;
    private float m_CurHp;

    private bool m_IsDead;


    private bool m_IsTargetable = true;


    private bool m_CanAttack;


    private bool m_CanUseSkill = true;

    private List<SkillData> m_SkillsList = new();

    private List<float> m_CurSkillCoolDownList = new();


    private float m_AttackPower;

    private float m_AttackSpeed;


    private float m_Defense;


    private float m_CriticalChance;

    private float m_CriticalDamageMultiplier;


    private float m_BuffedMaxHpRate;

    private float m_BuffedAttackPowerRate;

    private float m_BuffedAttackSpeedRate;

    private float m_BuffedDefenseRate;

    private float m_BuffedCriticalChanceRate;

    private float m_BuffedCriticalDamageMultiplierRate;
    

    [Header("Character Model Root GOBJ")]
    [SerializeField] protected GameObject m_CharModelRoot;

    [Header("Death Presentation")]
    [SerializeField] private GameObject m_CharacterInfoRoot;
    [SerializeField, Min(0f)] private float m_DeathHideDelay = 1.5f;
    

    [SerializeField] protected CharacterData m_CharData;

    protected CharData m_UserCharData;

    protected PartyStat m_PartyStat;

    protected Dictionary<EquipType, EquipmentData> m_CharEquipments = new();

    protected List<SkillData> m_CurBuffedSkillsList = new();

    protected List<float> m_CurBuffedSkillsDurationList = new();
    

    private bool m_IsTargetSelecting;
    

    private SkillData m_PendingSkill;

    [Header("Skill Variables")]
    [SerializeField, Tooltip("Single Damage 스킬의 클릭 판정에 사용할 몬스터 Layer")]
    private LayerMask m_MonsterLayer;

    [SerializeField, Tooltip("Single Buff 스킬의 클릭 판정에 사용할 캐릭터 Layer")]
    private LayerMask m_CharLayer;

    [SerializeField, Tooltip("현재 생성된 몬스터들을 관리하는 부모 Prefab")]
    private Transform m_MonsterSpawnRoot;

    [SerializeField, Tooltip("현재 생성된 플레이어블 캐릭터들을 관리하는 부모 Prefab")]
    private Transform m_CharacterSpawnRoot;


    private float m_AttackCoolDown;
    private float m_DeathHideAt;

    

    [Header("SPUM Animation")]
    [SerializeField] private SPUM_Prefabs m_Spum;

    

    GameObject m_CharacterRecoverEffect;


    private bool m_IsInitialized;
#endregion

#region 외부접근용 public 변수
    /// <summary>캐릭터의 현재 최대 체력</summary>
    public float MaxHp => m_MaxHp;

    /// <summary>캐릭터의 현재 체력</summary>
    public float CurHp => m_CurHp;

    /// <summary>캐릭터의 사망 여부</summary>
    public bool IsDead => m_IsDead;
    /// <summary>
    /// 마스터 데이터와 사용자 데이터가 적용되어 전투에 참여할 준비가 끝났는지 나타냅니다.
    /// </summary>
    public bool IsInitialized => m_IsInitialized;

    /// <summary>캐릭터의 타겟 지정 가능 여부</summary>
    public bool IsTargetable => m_IsInitialized && isActiveAndEnabled && !m_IsDead && m_IsTargetable;

    /// <summary>캐릭터의 공격 가능 여부</summary>
    public bool CanAttack => m_CanAttack;
    /// <summary>캐릭터의 스킬 사용 가능 여부</summary>
    public bool CanUseSkill => m_CanUseSkill;


    /// <summary>캐릭터가 보유한 스킬 목록</summary>
    public IReadOnlyList<SkillData> SkillsList => m_SkillsList;

    /// <summary>캐릭터가 보유한 스킬의 현재 쿨타임 목록</summary>
    public IReadOnlyList<float> SkillsCoolDownList => m_CurSkillCoolDownList;

    /// <summary>캐릭터에게 현재 적용중인 버프 스킬 목록</summary>
    public IReadOnlyList<SkillData> CurBuffedSkillsList => m_CurBuffedSkillsList;

    /// <summary>캐릭터에게 현재 적용중인 버프 스킬 지속시간 목록</summary>
    public IReadOnlyList<float> CurBuffedSkillsDurationList => m_CurBuffedSkillsDurationList;


    /// <summary>장비와 버프가 적용된 캐릭터의 현재 공격력</summary>
    public float AttackPower => m_AttackPower;

    /// <summary>장비와 버프가 적용된 캐릭터의 현재 공격속도</summary>
    public float AttackSpeed => m_AttackSpeed;

    /// <summary>장비와 버프가 적용된 캐릭터의 현재 방어력</summary>
    public float Defense => m_Defense;

    /// <summary>장비와 버프가 적용된 캐릭터의 현재 치명타 확률</summary>
    public float CriticalChance => m_CriticalChance;

    /// <summary>장비와 버프가 적용된 캐릭터의 현재 치명타 데미지 배율</summary>
    public float CriticalDamageMultiplier => m_CriticalDamageMultiplier;


    /// <summary>캐릭터의 기본 데이터를 가지고 있는 Scriptable Object</summary>
    public CharacterData CharData => m_CharData;

    /// <summary>유저가 보유한 캐릭터의 개별 저장 데이터</summary>
    public CharData UserCharData => m_UserCharData;

    /// <summary>캐릭터가 현재 장착중인 아이템 목록</summary>
    public IReadOnlyDictionary<EquipType, EquipmentData> CharEquipments => m_CharEquipments;


    /// <summary>현재 적용중인 최대 체력 버프 배율의 합</summary>
    public float BuffedMaxHpRate => m_BuffedMaxHpRate;

    /// <summary>현재 적용중인 공격력 버프 배율의 합</summary>
    public float BuffedAttackPowerRate => m_BuffedAttackPowerRate;

    /// <summary>현재 적용중인 공격속도 버프 배율의 합</summary>
    public float BuffedAttackSpeedRate => m_BuffedAttackSpeedRate;

    /// <summary>현재 적용중인 방어력 버프 배율의 합</summary>
    public float BuffedDefenseRate => m_BuffedDefenseRate;

    /// <summary>현재 적용중인 치명타 확률 버프 배율의 합</summary>
    public float BuffedCriticalChanceRate => m_BuffedCriticalChanceRate;

    /// <summary>현재 적용중인 치명타 데미지 버프 배율의 합</summary>
    public float BuffedCriticalDamageMultiplierRate => m_BuffedCriticalDamageMultiplierRate;

    /// <summary>
    /// 대상 선택과 거리 계산에 사용하는 캐릭터의 월드 위치를 가져옵니다.
    /// </summary>
    public Vector3 MonPosition => transform.position;


    [Header("Skill Effect")]
    [SerializeField] private Transform m_SkillEffectTarget;

    /// <summary>
    /// 스킬 이펙트를 로컬 Transform 기준으로 배치할 부모 지점을 가져옵니다.
    /// </summary>
    public Transform SkillEffectTarget => m_SkillEffectTarget;
    // 스킬이펙트 스폰을 위한 변수

    /// <summary>
    /// 이 캐릭터가 단일 아군 버프의 대상을 기다리는 중인지 나타냅니다.
    /// </summary>
    public bool IsSelectingBuffTarget =>
        m_IsTargetSelecting &&
        m_PendingSkill != null &&
        m_PendingSkill.Type == SkillType.Buff;


    private int m_TargetSelectingEndedFrame = -1;

    // 스킬 대상 선택에 사용한 클릭으로 일반 선택이 바뀌는 것을 방지
    /// <summary>
    /// 현재 스킬 대상 선택 상태가 일반 대상 선택 입력을 점유하는지 나타냅니다.
    /// </summary>
    public bool BlocksNormalTargetSelection =>
        m_IsTargetSelecting ||
        m_TargetSelectingEndedFrame == Time.frameCount;
#endregion

#region 이벤트 함수
    // 체력 변경
    /// <summary>
    /// 캐릭터의 현재 체력 또는 최대 체력이 바뀌었을 때 현재 값과 최대 값을 전달합니다.
    /// </summary>
    public event Action<float, float> OnCharHpChanged;

    // 스탯 변경
    /// <summary>
    /// 장비, 성장 또는 버프 적용으로 캐릭터의 최종 능력치가 갱신되었을 때 발생합니다.
    /// </summary>
    public event Action OnCharStatsChanged;

    // 버프 목록 변경
    /// <summary>
    /// 캐릭터의 활성 버프 목록이나 남은 지속 시간이 갱신되었을 때 발생합니다.
    /// </summary>
    public event Action OnCharBuffChanged;

    // 사망
    /// <summary>
    /// 캐릭터가 사망 처리된 직후 해당 런타임 인스턴스를 전달합니다.
    /// </summary>
    public event Action<CharacterBase> OnCharDead;

    // 타겟 지정 불가
    /// <summary>
    /// 사망 등으로 캐릭터가 더 이상 선택 대상이 아니게 되었을 때 발생합니다.
    /// </summary>
    public event Action<ITargetable> OnCharTargetableDisabled;

    // 장비변경(변경시 모든 UI reflash)
    /// <summary>
    /// 캐릭터의 장착 장비 구성이 변경되어 고정 전투력을 다시 계산해야 할 때 발생합니다.
    /// </summary>
    public event Action OnCharEquipmentChanged;

    /// <summary>
    /// 캐릭터가 피해를 받은 뒤 최종 피해량과 치명타 여부를 전달합니다.
    /// </summary>
    public event Action<float, bool> OnCharDamageTaken;
#endregion

#region Unity 이벤트함수
    protected virtual void Awake()
    {
        m_CharacterRecoverEffect =
            Resources.Load<GameObject>("Effect/SkillEffect/Buff/heal_green_0");

        if (m_CharacterRecoverEffect == null)
            PDebug.LogError("캐릭터 회복 이펙트 프리팹을 찾지 못했습니다.");
    }


    protected virtual void Update()
    {
        if (!m_IsInitialized)
            return;

        if (m_IsDead)
        {
            if (m_IsTargetSelecting || m_PendingSkill != null)
                CancelTargetSelecting();

            return;
        }

        if (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused || BattleFlowMgr.IsFieldInputBlocked || SceneChangeMgr.IsTransitioning)
            return;

        // 메뉴 일시정지를 제외하면 생존 중에는 계속 시간 진행
        UpdateSkillCooldowns(Time.deltaTime);
        UpdateSkillBuffTime(Time.deltaTime);

        // 공격·버프 모두 이동 중에도 대상 선택 입력 처리
        if (m_IsTargetSelecting && m_PendingSkill != null)
        {
            switch (m_PendingSkill.Type)
            {
                case SkillType.Damage:
                    UpdateTargetMonSel();
                    break;

                case SkillType.Buff:
                    UpdateTargetCharSel();
                    break;
            }
        }

        // 자동 공격은 전투 중에만 실행
        if (!BattleFlowMgr.CanRunCombat)
            return;

        if (m_CanAttack)
            AutoAtkByAtkSpd();
    }

    protected virtual void OnDestroy()
    {
        if (m_PartyStat != null)
            m_PartyStat.OnPartyStatsChanged -= HandlePartyStatsChanged;
    }
#endregion

#region Initialize Methods
    /// <summary>
    /// 파티 편성 적용에 필요한 모델과 런타임 참조가 준비되어 있는지 나타냅니다.
    /// </summary>
    public bool CanInitializeForParty => m_CharModelRoot != null;

    /// <summary>
    /// 파티 편성을 변경하기 전에 진행 중인 스킬 대상 선택 상태를 취소합니다.
    /// </summary>
    public void CancelSkillSelectionForPartyChange()
    {
        CancelTargetSelecting();
    }

    /// <summary>
    /// 메뉴 진입 시 진행 중인 공격·버프 대상 선택 상태를 취소합니다.
    /// </summary>
    public void CancelSkillSelectionForMenu()
    {
        CancelTargetSelecting();
    }

    // 편성 변경 중에만 사용하는 메모리 스냅샷. 저장 데이터에는 기록하지 않는다.
    internal sealed class PartySnapshot
    {
        internal CharacterData MasterData;
        internal CharData UserData;
        internal PartyStat PartyStat;
        internal SPUM_Prefabs Model;
        internal bool Initialized, Dead, Targetable, CanAttack, CanUseSkill;
        internal bool ModelActive, InfoActive, HideScheduled;
        internal float Hp, AttackCooldown, HideRemaining;
        internal float[] Stats;
        internal List<SkillData> Skills, Buffs;
        internal List<float> Cooldowns, BuffDurations;
        internal Dictionary<EquipType, EquipmentData> Equipment;
    }

    internal PartySnapshot CapturePartySnapshot()
    {
        return new PartySnapshot
        {
            MasterData = m_CharData, UserData = m_UserCharData, PartyStat = m_PartyStat,
            Model = m_Spum, Initialized = m_IsInitialized, Dead = m_IsDead,
            Targetable = m_IsTargetable, CanAttack = m_CanAttack, CanUseSkill = m_CanUseSkill,
            Hp = m_CurHp, AttackCooldown = m_AttackCoolDown,
            Stats = new[] { m_MaxHp, m_AttackPower, m_AttackSpeed, m_Defense,
                m_CriticalChance, m_CriticalDamageMultiplier, m_BuffedMaxHpRate,
                m_BuffedAttackPowerRate, m_BuffedAttackSpeedRate, m_BuffedDefenseRate,
                m_BuffedCriticalChanceRate, m_BuffedCriticalDamageMultiplierRate },
            ModelActive = m_CharModelRoot != null && m_CharModelRoot.activeSelf,
            InfoActive = m_CharacterInfoRoot != null && m_CharacterInfoRoot.activeSelf,
            HideScheduled = IsInvoking(nameof(HideDeadCharacter)),
            HideRemaining = Mathf.Max(0f, m_DeathHideAt - Time.time),
            Skills = new List<SkillData>(m_SkillsList),
            Cooldowns = new List<float>(m_CurSkillCoolDownList),
            Buffs = new List<SkillData>(m_CurBuffedSkillsList),
            BuffDurations = new List<float>(m_CurBuffedSkillsDurationList),
            Equipment = new Dictionary<EquipType, EquipmentData>(m_CharEquipments)
        };
    }

    internal void RestorePartySnapshot(PartySnapshot snapshot)
    {
        CancelInvoke();
        if (m_PartyStat != null)
            m_PartyStat.OnPartyStatsChanged -= HandlePartyStatsChanged;
        m_PartyStat = null;
        m_CharData = snapshot.MasterData;
        m_UserCharData = snapshot.UserData;
        m_Spum = snapshot.Model;
        m_IsInitialized = false;
        m_IsDead = snapshot.Dead;
        m_IsTargetable = snapshot.Targetable;
        m_CanAttack = snapshot.CanAttack;
        m_CanUseSkill = snapshot.CanUseSkill;
        m_AttackCoolDown = snapshot.AttackCooldown;
        m_SkillsList = snapshot.Skills;
        m_CurSkillCoolDownList = snapshot.Cooldowns;
        m_CurBuffedSkillsList = snapshot.Buffs;
        m_CurBuffedSkillsDurationList = snapshot.BuffDurations;
        m_CharEquipments = snapshot.Equipment;
        SetPartyStat(snapshot.PartyStat);
        m_MaxHp = snapshot.Stats[0];
        m_AttackPower = snapshot.Stats[1];
        m_AttackSpeed = snapshot.Stats[2];
        m_Defense = snapshot.Stats[3];
        m_CriticalChance = snapshot.Stats[4];
        m_CriticalDamageMultiplier = snapshot.Stats[5];
        m_BuffedMaxHpRate = snapshot.Stats[6];
        m_BuffedAttackPowerRate = snapshot.Stats[7];
        m_BuffedAttackSpeedRate = snapshot.Stats[8];
        m_BuffedDefenseRate = snapshot.Stats[9];
        m_BuffedCriticalChanceRate = snapshot.Stats[10];
        m_BuffedCriticalDamageMultiplierRate = snapshot.Stats[11];
        m_IsInitialized = snapshot.Initialized;
        m_CurHp = snapshot.Hp;
        OnCharStatsChanged?.Invoke();
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
        OnCharBuffChanged?.Invoke();
        if (m_CharModelRoot != null)
            m_CharModelRoot.SetActive(snapshot.ModelActive);
        if (m_CharacterInfoRoot != null)
            m_CharacterInfoRoot.SetActive(snapshot.InfoActive);
        if (snapshot.HideScheduled)
        {
            m_DeathHideAt = Time.time + snapshot.HideRemaining;
            Invoke(nameof(HideDeadCharacter), snapshot.HideRemaining);
        }
    }

    /// <summary>
    /// 새로 편성된 캐릭터의 모든 스킬을 전체 재사용 대기시간으로 시작합니다.
    /// </summary>
    public void StartPartyChangeCooldowns()
    {
        for (int i = 0; i < m_SkillsList.Count; i++)
            m_CurSkillCoolDownList[i] = Mathf.Max(0f, m_SkillsList[i].Cooldown);
    }

    /// <summary>편성 교체 전에 기존 전투 상태와 파티 성장 구독을 해제한다.</summary>
    public void ClearForPartyChange()
    {
        CancelTargetSelecting();
        StopAllCoroutines();
        CancelInvoke();
        m_IsTargetable = false;
        OnCharTargetableDisabled?.Invoke(this);
        if (m_PartyStat != null)
            m_PartyStat.OnPartyStatsChanged -= HandlePartyStatsChanged;
        m_PartyStat = null;
        m_IsInitialized = false;
        m_CharData = null;
        m_UserCharData = null;
        m_Spum = null;
        m_CurBuffedSkillsList.Clear();
        m_CurBuffedSkillsDurationList.Clear();
        m_SkillsList.Clear();
        m_CurSkillCoolDownList.Clear();
        m_CharEquipments.Clear();
        m_PendingSkill = null;
        m_IsTargetSelecting = false;
        m_AttackCoolDown = 0f;
        m_CurHp = 0f;
        m_MaxHp = 0f;
        if (m_SkillEffectTarget != null)
            foreach (Transform effect in m_SkillEffectTarget)
            {
                effect.gameObject.SetActive(false);
                Destroy(effect.gameObject);
            }
    }

    /// <summary>
    /// 캐릭터 마스터·사용자 데이터와 파티 성장값을 적용하여 전투 런타임 상태를 초기화합니다.
    /// </summary>
    /// <param name="masterData">모델과 기본 능력치를 제공하는 마스터 데이터입니다.</param>
    /// <param name="userData">레벨과 장비 등 사용자 소유 상태를 담은 데이터입니다.</param>
    /// <param name="partyStat">캐릭터 능력치에 적용할 파티 성장 보정값입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool Initialize(
        CharacterData masterData,
        CharData userData,
        PartyStat partyStat)
    {
        if (m_IsInitialized)
            return true;

        if (masterData == null || userData == null)
        {
            PDebug.LogError($"{name}: 캐릭터 초기화 데이터가 없습니다.");
            return false;
        }

        if (m_CharModelRoot == null)
        {
            PDebug.LogError($"{name}: Character Model Root가 없습니다.");
            return false;
        }

        m_CharData = masterData;
        m_UserCharData = userData;

        m_Spum = m_CharModelRoot.GetComponentInChildren<SPUM_Prefabs>();
        if (m_Spum == null)
        {
            PDebug.LogError($"{name}: 생성된 외형에서 SPUM_Prefabs를 찾지 못했습니다.");
            return false;
        }

        GameObject a_monsterRoot = GameObject.Find("MonsterSpawnRoot");
        GameObject characterRoot = GameObject.Find("SpawnpointsRoot");
        if (a_monsterRoot == null || characterRoot == null)
        {
            PDebug.LogError($"{name}: 전투 SpawnRoot를 찾지 못했습니다.");
            return false;
        }

        m_MonsterSpawnRoot = a_monsterRoot.transform;
        m_CharacterSpawnRoot = characterRoot.transform;

        m_Spum.OverrideControllerInit();
        SetPartyStat(partyStat);
        InitializeCharacter();
        RestoreCharacterVisuals();
        m_IsInitialized = true;
        OnInitialized();
        return true;
    }

    protected virtual void OnInitialized()
    {
    }

    private void InitializeCharacter()
    {
        if (m_CharData == null)
        {
            PDebug.LogError($"{name}의 CharacterData가 설정되지 않았습니다.");
            return;
        }

        // 기본 상태 초기화
        m_IsDead = false;
        m_IsTargetable = true;
        m_CanAttack = true;
        m_CanUseSkill = true;

        InitializeSkills();
        InitializeEquipments();

        // 모든 초기 데이터를 기반으로 최종 스탯 계산
        RefreshStats(true);
    }

    // 기존 모델/장비/파티 성장값을 유지하고 전투 중 생긴 상태만 초기화한다.
    /// <summary>
    /// 재도전 시 캐릭터 상태를 안전하게 초기화할 수 있는지 나타냅니다.
    /// </summary>
    public bool CanResetForRetry => m_IsInitialized && m_CharData != null && m_Spum != null;

    /// <summary>
    /// 재도전을 위해 체력, 사망 상태, 버프와 스킬 재사용 대기시간을 초기 상태로 복원합니다.
    /// </summary>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryResetForRetry()
    {
        if (!CanResetForRetry || BattleFlowMgr.CanRunCombat)
            return false;

        CancelTargetSelecting();
        m_IsTargetSelecting = false;
        m_PendingSkill = null;
        m_CurBuffedSkillsList.Clear();
        m_CurBuffedSkillsDurationList.Clear();
        for (int i = 0; i < m_CurSkillCoolDownList.Count; i++)
            m_CurSkillCoolDownList[i] = 0f;
        m_AttackCoolDown = 0f;
        m_IsDead = false;
        m_IsTargetable = true;
        m_CanAttack = true;
        m_CanUseSkill = true;
        RefreshStats(true);
        OnCharBuffChanged?.Invoke();
        RestoreCharacterVisuals();
        PlayIdleAnimation();
        return true;
    }

    /// <summary>체크포인트 복원 전용. 사망 이벤트나 보상을 재실행하지 않는다.</summary>
    public bool TryRestoreCheckpointHp(float hp)
    {
        if (float.IsNaN(hp) || float.IsInfinity(hp) || hp < 0f || !TryResetForRetry()) return false;
        m_CurHp = Mathf.Clamp(hp, 0f, m_MaxHp);
        m_IsDead = m_CurHp <= 0f;
        m_IsTargetable = !m_IsDead;
        m_CanAttack = !m_IsDead;
        m_CanUseSkill = !m_IsDead;
        if (m_IsDead)
        {
            OnCharTargetableDisabled?.Invoke(this);
            HideDeadCharacter();
        }
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
        return true;
    }

    private void InitializeSkills()
    {
        // 스킬목록 초기화
        m_SkillsList.Clear();
        m_CurSkillCoolDownList.Clear();

        // 스킬 없으면 return
        if (m_CharData.SkillsList == null || m_CharData.SkillsList.Count <= 0)
            return;
        
        // 캐릭터 데이터 스크립트에서 스킬목록을 불러와서 추가
        foreach (SkillData skill in m_CharData.SkillsList)
        {
            // 혹시나 스킬에 빈칸이 있는거면 Continue(휴먼에러)
            if (skill == null)
                continue;
            
            m_SkillsList.Add(skill);
            m_CurSkillCoolDownList.Add(0f);
        }
    }
    

    private void InitializeEquipments()
    {
        m_CharEquipments.Clear();

        for (int i = 0; i < (int)EquipType.Count; i++)
        {
            EquipType type = (EquipType)i;
            m_CharEquipments.Add(type, null);
            string uid = m_UserCharData?.GetEquipmentUid(type);
            if (string.IsNullOrEmpty(uid))
                continue;
            if (EquipmentMgr.TryResolveEquipment(uid, type, out EquipmentData equipment, out string message))
                m_CharEquipments[type] = equipment;
            else
                PDebug.LogError(message);
        }
    }

    // 전달받은 Partystat을 적용한다
    /// <summary>
    /// 파티 성장 보정값을 교체하고 캐릭터의 최종 능력치를 다시 계산합니다.
    /// </summary>
    /// <param name="partyStat">캐릭터 능력치에 적용할 파티 성장 보정값입니다.</param>
    public void SetPartyStat(PartyStat partyStat)
    {
        if (m_PartyStat != null)
            m_PartyStat.OnPartyStatsChanged -= HandlePartyStatsChanged;

        m_PartyStat = partyStat;

        if (m_PartyStat != null)
            m_PartyStat.OnPartyStatsChanged += HandlePartyStatsChanged;

        if (m_IsInitialized)
            RefreshStats();
    }

    private void HandlePartyStatsChanged()
    {
        RefreshStats();
    }
#endregion
    
#region 캐릭터 기본 시스템 메서드
    
    // IHealable
    /// <summary>
    /// 생존한 캐릭터의 현재 체력을 최대 체력 범위 안에서 회복합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    public void Heal(float amount)
    {
        // 죽었거나 힐량이 없으면 힐 불가
        if (m_IsDead || amount <= 0f)
            return;
        
        // 최대체력 이상으로 늘어나지 않도록 보정
        m_CurHp = Mathf.Min(m_CurHp + amount, m_MaxHp);

        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
    }

    /// <summary>
    /// 전달된 공격 피해에 방어 규칙을 적용하고 체력 감소 및 사망 처리를 수행합니다.
    /// </summary>
    /// <param name="dmg">방어 적용 전에 전달되는 공격 피해량입니다.</param>
    /// <param name="isCritical">치명타로 처리된 피해인지 여부입니다.</param>
    public void TakeDmg(float dmg, bool isCritical = false)
    {
        // 이미 죽었거나 피해가 없다면 피해 처리 안 함
        if (m_IsDead || dmg <= 0f || BattleFlowMgr.IsRaidPatternPaused)
            return;

        float finalDamage = DamageCalculator.CalculateFinalDamage(
            dmg, m_Defense, GlobalConstants.CharDefenseConst);
        if (finalDamage <= 0f)
            return;

        AtkFxPoolMgr.Inst.Play(EffectType.Attack, new Vector3(MonPosition.x, MonPosition.y + 0.4f, MonPosition.z));

        ApplyDamage(finalDamage, isCritical);
    }

    /// <summary>레이드 실패 피해. 방어력과 치명타 없이 최대 HP 비율의 고정 피해를 적용한다.</summary>
    internal void TakeRaidPatternDamage(float maxHpRatio)
    {
        if (!m_IsInitialized || m_IsDead || maxHpRatio <= 0f) return;
        ApplyDamage(m_MaxHp * Mathf.Clamp01(maxHpRatio), false);
    }

    private void ApplyDamage(float finalDamage, bool isCritical)
    {
        // 현재 체력 감소
        m_CurHp -= finalDamage;

        // 체력이 0 아래로 내려가지 않도록 제한
        m_CurHp = Mathf.Max(0f, m_CurHp);
        
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
        OnCharDamageTaken?.Invoke(finalDamage, isCritical);

        // 사망 판정
        if (m_CurHp <= 0f)
        {
            // 죽음여부 bool값 true
            m_IsDead = true;
            // 타게팅 불가능하도록 타겟가능 여부 false
            m_IsTargetable = false;

            OnCharTargetableDisabled?.Invoke(this);
            // 스킬사용 불가능 하도록 스킬사용 여부 false
            m_CanUseSkill = false;
            m_CanAttack = false;

            // 사망 즉시 버프 제거 및 스킬 쿨타임 초기화
            ResetSkillsOnDeath();

            OnCharDead?.Invoke(this);
            Death();
            ScheduleDeathHide(); // 추가
        }
        else
        {
            // PlayDamagedAnimation();
            return;
        }
    }

    protected abstract void Death();
    
    private void ScheduleDeathHide()
    {
        CancelInvoke(nameof(HideDeadCharacter));
        m_DeathHideAt = Time.time + m_DeathHideDelay;

        if (m_DeathHideDelay <= 0f)
            HideDeadCharacter();
        else
            Invoke(nameof(HideDeadCharacter), m_DeathHideDelay);
    }

    private void HideDeadCharacter()
    {
        // 대기 중 부활했다면 숨기지 않는다.
        if (!m_IsDead)
            return;

        if (m_CharModelRoot != null)
            m_CharModelRoot.SetActive(false);

        if (m_CharacterInfoRoot != null)
            m_CharacterInfoRoot.SetActive(false);
    }

    private void RestoreCharacterVisuals()
    {
        // 빠르게 부활한 뒤 이전 사망 예약으로 다시 숨겨지는 것을 방지한다.
        CancelInvoke(nameof(HideDeadCharacter));

        if (m_CharModelRoot != null)
            m_CharModelRoot.SetActive(true);

        if (m_CharacterInfoRoot != null)
            m_CharacterInfoRoot.SetActive(true);
    }
    
    private void AutoAtkByAtkSpd()
    {
        // 공격 쿨타임 감소
        m_AttackCoolDown -= Time.deltaTime;

        // 아직 공격 시간이 아니면 종료
        if (m_AttackCoolDown > 0f)
            return;

        // 공격 가능한 대상 탐색
        if (!TryFindAtkTarget(out IDamageable target))
            return;

        // 공격 성공(타겟이 없거나 이미 죽은상태가 아니면 true가 반환된다.)
        if (!Attack(target))
            return;

        // 다음 공격까지 기다릴 시간(공격속도의 최저는 0.01)
        m_AttackCoolDown = 1f / Mathf.Max(0.01f, m_AttackSpeed);
    }

    private bool TryFindAtkTarget(out IDamageable target)
    {
        target = null;
        MonsterBase[] a_MonsterGroup = m_MonsterSpawnRoot.GetComponentsInChildren<MonsterBase>();

        if (a_MonsterGroup == null || a_MonsterGroup.Length <= 0)
            return false;
        
        float minDistance = float.MaxValue;
        target = null;

        foreach(MonsterBase mon in a_MonsterGroup)
        {
            // 몬스터가 죽거나 타겟팅이 불가능한 경우 다음 몬스터로 넘어간다
            if (mon == null || mon.IsDead || !mon.IsTargetable)
                continue;

            // 최단거리 거리비교
            float sqrDist = (this.transform.position - mon.transform.position).sqrMagnitude;
        
            if (minDistance < sqrDist)
                continue;

            // 최단거리의 몬스터를 target에 담아 반환
            minDistance = sqrDist;
            target = mon;
        }

        // 기본 return값은 target이 있는지를 비교하여 반환
        return target != null;
    }

    /// <summary>
    /// 현재 공격력과 치명타 규칙으로 피해를 계산하여 유효한 대상에게 일반 공격을 적용합니다.
    /// </summary>
    /// <param name="target">피해를 적용할 생존 대상입니다.</param>
    /// <returns>공격이 적용되면 <see langword="true"/>, 전투 상태나 대상이 유효하지 않으면 <see langword="false"/>입니다.</returns>
    public bool Attack(IDamageable target)
    {

        // 공격 불가능
        if (!BattleFlowMgr.CanRunCombat || m_IsDead || target == null || target.IsDead)
            return false;


        bool isCritical = DamageCalculator.IsCritical(m_CriticalChance, Random.value);

        float damage = DamageCalculator.CalculateRawDamage(
            m_AttackPower, 1f, isCritical, m_CriticalDamageMultiplier);

        EffectType fxType = EffectType.Attack;

        if (isCritical)
        {
            fxType = EffectType.CriticalAttack;
        }

        PlayAttackAnimation();
        AtkFxPoolMgr.Inst.Play(fxType, target.MonPosition + new Vector3(0, 0.4f));
        // 피해 전달
        if (target is MonsterBase monster)
            monster.TakeDmgFromCharacter(damage, isCritical, this);
        else
            target.TakeDmg(damage, isCritical);
        

        // 데미지 텍스트 표시
        // DmgtxtMgr.Inst.ShowDmg(damage, isCritical);
        // To-do: DmgTxtMgr구현

        // 공격 성공
        return true;
    }

    // 챕터 변경 시 부활 및 완전 회복.
    // 생존자의 버프와 쿨타임은 유지한다.
    /// <summary>
    /// 장비, 파티 성장 및 활성 버프를 반영하여 현재 능력치를 다시 계산합니다.
    /// </summary>
    public void RefreshCharacter()
    {
        if (!m_IsInitialized || m_Spum == null)
            return;

        bool wasDead = m_IsDead;

        if (wasDead)
        {
            m_IsDead = false;
            m_IsTargetable = true;
            m_CanAttack = true;
            m_CanUseSkill = true;

            RestoreCharacterVisuals();
            PlayIdleAnimation();
        }

        m_CurHp = m_MaxHp;
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);

        if (m_CharacterRecoverEffect != null && SkillEffectTarget != null)
            Instantiate(m_CharacterRecoverEffect, SkillEffectTarget, false);
    }
#endregion

#region 스킬 관련 시스템 메서드
    /// <summary>
    /// 지정 슬롯의 스킬을 확인하여 공격 스킬을 실행하거나 버프 대상 선택을 시작합니다.
    /// </summary>
    /// <param name="idx">보유 스킬 목록에서 사용할 0부터 시작하는 슬롯 인덱스입니다.</param>
    public void UseSkill(int idx)
    {
        if (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused || BattleFlowMgr.IsFieldInputBlocked || SceneChangeMgr.IsTransitioning)
            return;

        if (!m_IsInitialized || m_IsDead)
            return;

        if (idx < 0 || idx >= m_SkillsList.Count)
            return;

        SkillData skillData = m_SkillsList[idx];

        // 모든 스킬은 사용 시도 시점에 쿨타임 검사
        if (m_CurSkillCoolDownList[idx] > 0f)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("스킬 쿨타임 중입니다.");
            return;
        }

        // 쿨타임이 끝난 단일 스킬은 이동 중에도 대상 선택 가능
        if (skillData.TargetType == SkillTargetType.Single)
        {
            StartTargetSelecting(skillData);
            return;
        }

        // 즉시 실행되는 공격 스킬의 전투 상태 제한
        if (skillData.Type == SkillType.Damage &&
            (!BattleFlowMgr.CanRunCombat || !m_CanUseSkill))
            return;

        // 이 아래 기존 Damage/Buff switch와 성공 시 쿨타임 설정 유지
        bool isSkExecuteSuccess = false;

        // 스킬 타입에 따른 스킬사용메서드로 연결
        switch (skillData.Type)
        {
            // 공격형 스킬
            case SkillType.Damage:
                isSkExecuteSuccess = TryExecuteDmgSkill(skillData);
                break;

            // 버프형 스킬
            case SkillType.Buff:
                isSkExecuteSuccess = TryExecuteBuffSkill(skillData);
                break;

            // 에러처리    
            default:
                PDebug.LogError($"{name} : 지원하지 않는 SkillType입니다. {skillData.Type}");
                return;
            }
            
        // 스킬이 사용되고 나서 스킬 쿨타임을 적용
        if (isSkExecuteSuccess)
            m_CurSkillCoolDownList[idx] = skillData.Cooldown;
    }

    private void StartTargetSelecting(SkillData skillData)
    {
        CancelTargetSelecting();
        m_IsTargetSelecting = true;
        m_PendingSkill = skillData;

        switch(m_PendingSkill.Type)
        {
            case SkillType.Damage:
                // 여기서 몬스터들에게 타겟 가능 UI 표시
                TargetableUI(m_IsTargetSelecting, SkillType.Damage);
                break;
            
            case SkillType.Buff:
                TargetableUI(m_IsTargetSelecting, SkillType.Buff);
                break;

            default:
                PDebug.LogError("구현되지 않은 스킬타입입니다.");
                break;
        }
    }

    private void CancelTargetSelecting()
    {
        if (m_PendingSkill == null)
            return;

        // 추가: 선택 대기를 끝낸 프레임 기록
        m_TargetSelectingEndedFrame = Time.frameCount;

        SkillType tmpSkType = m_PendingSkill.Type;

        m_IsTargetSelecting = false;
        m_PendingSkill = null;

        TargetableUI(false, tmpSkType);
    }

    private void TargetableUI(bool isOn, SkillType skillType)
    {
        switch(skillType)
        {
            case SkillType.Damage:
            {
                if (m_MonsterSpawnRoot == null)
                {
                    PDebug.LogError("몬스터 스폰루트가 연결되지 않았습니다!");
                    return;
                }
            
                MonsterBase[] monsters = m_MonsterSpawnRoot.GetComponentsInChildren<MonsterBase>(true);

                foreach (MonsterBase monster in monsters)
                {
                    if (monster.TryGetComponent(out MonsterRootMgr rootMgr))
                        rootMgr.SetDamageTargetHighlighted(isOn);
                }
                break;
            }

            case SkillType.Buff:
            {
                if (m_CharacterSpawnRoot == null)
                {
                    PDebug.LogError("캐릭터 스폰루트가 연결되지 않았습니다!");
                    return;
                }

                CharacterBase[] characters = m_CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>(true);

                foreach (CharacterBase character in characters)
                {
                    if (character.TryGetComponent(out CharacterRootMgr rootMgr))
                        rootMgr.SetBuffTargetHighlighted(isOn);
                }
                break;
            }

            default:
                PDebug.LogError("구현되지 않은 스킬타입입니다.");
                break;
        }
    }


    private void ExecutePendingDamageSkill(MonsterBase target)
    {
        if (m_PendingSkill == null ||
            m_PendingSkill.Type != SkillType.Damage ||
            m_IsDead ||
            target == null ||
            target.IsDead ||
            !target.IsTargetable)
            return;

        int skillIndex = m_SkillsList.IndexOf(m_PendingSkill);

        if (skillIndex < 0)
            return;

        // 발동 불가능하면 선택 대기 유지
        if (!BattleFlowMgr.CanRunCombat || !m_CanUseSkill)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("스킬 사용이 불가능한 상태입니다.");
            return;
        }

        if (m_CurSkillCoolDownList[skillIndex] > 0f)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("스킬 쿨타임 중입니다.");
            return;
        }

        if (!TryDealSkillDamage(
                target,
                m_PendingSkill.EffectMultiplier,
                out bool isCritical,
                out float finalDmg))
            return;

        SpawnSkillEffect(m_PendingSkill, target.SkillEffectTarget);

        m_CurSkillCoolDownList[skillIndex] = m_PendingSkill.Cooldown;
        CancelTargetSelecting(); // 사용 완료
    }

    private void ExecutePendingBuffSkill(CharacterBase target)
    {
        if (m_PendingSkill == null ||
            m_IsDead ||
            target == null ||
            target.IsDead)
            return;

        int skillIndex = m_SkillsList.IndexOf(m_PendingSkill);

        if (skillIndex < 0)
            return;

        // 생존 여부는 위에서 검사하며, 여기서는 쿨타임만 확인
        if (m_CurSkillCoolDownList[skillIndex] > 0f)
            return;

        if (!target.TryApplyBuff(m_PendingSkill))
            return;

        m_CurSkillCoolDownList[skillIndex] = m_PendingSkill.Cooldown;
        CancelTargetSelecting(); // 사용 완료로 선택 모드 종료
    }

    protected void UpdateSkillCooldowns(float deltaTime)
    {
        for (int i = 0; i < m_CurSkillCoolDownList.Count; i++)
        {
            // 이미 사용 가능한 스킬
            if (m_CurSkillCoolDownList[i] <= 0f)
                continue;

            // 남은 쿨타임 감소
            m_CurSkillCoolDownList[i] -= deltaTime;

            // 음수가 되지 않도록 보정
            if (m_CurSkillCoolDownList[i] <= 0f)
                m_CurSkillCoolDownList[i] = 0f;
        }
    }
    
    private void UpdateSkillBuffTime(float deltaTime)
    {
        bool isBuffRemoved = false;

        for (int i = m_CurBuffedSkillsDurationList.Count - 1; i >= 0; i--)
        {
            m_CurBuffedSkillsDurationList[i] -= deltaTime;

            if (m_CurBuffedSkillsDurationList[i] > 0f)
                continue;

            m_CurBuffedSkillsList.RemoveAt(i);
            m_CurBuffedSkillsDurationList.RemoveAt(i);

            isBuffRemoved = true;
        }

        // 버프가 하나라도 제거됐을 때 딱 한 번만 재계산
        if (isBuffRemoved)
        {
            RefreshStats();

            // 버프 UI 등에 변경 사실 전달
            OnCharBuffChanged?.Invoke();
        }
    }

    protected virtual bool TryExecuteDmgSkill(SkillData skillData)
    {
        if (!TryGetDamageSkillTargets(skillData, out List<MonsterBase> targets))
            return false;

        bool hasSuccess = false;

        foreach (MonsterBase target in targets)
        {
            if (!TryDealSkillDamage(
                    target,
                    skillData.EffectMultiplier,
                    out bool isCritical,
                    out float finalDmg))
                continue;

            SpawnSkillEffect(skillData, target.SkillEffectTarget);
            hasSuccess = true;
        }

        return hasSuccess;
    }

    protected virtual bool TryExecuteBuffSkill(SkillData skillData)
    {
        if (!TryGetBuffSkillTargets(skillData, out List<CharacterBase> targets))
            return false;

        bool hasSuccess = false;

        foreach (CharacterBase target in targets)
        {
            if (target.IsDead)
                continue;

            if (target.TryApplyBuff(skillData))
                hasSuccess = true;
        }

        return hasSuccess;
    }

    protected void RemoveBuff(SkillData skillData)
    {
        if (skillData == null || skillData.Type != SkillType.Buff)
            return;

        int buffIndex = m_CurBuffedSkillsList.IndexOf(skillData);

        if (buffIndex < 0)
            return;

        // 버프와 지속시간은 같은 Index로 관리
        m_CurBuffedSkillsList.RemoveAt(buffIndex);
        m_CurBuffedSkillsDurationList.RemoveAt(buffIndex);

        RefreshStats();
        OnCharBuffChanged?.Invoke();
    }
    
    protected bool TryDealSkillDamage(IDamageable target, float skillMultiplier, out bool isCritical, out float finalDmg)
    {
        isCritical = false;
        finalDmg = 0;

        if (!BattleFlowMgr.CanRunCombat || m_IsDead || target == null || target.IsDead)
            return false;

        isCritical = DamageCalculator.IsCritical(m_CriticalChance, Random.value);
        float damage = DamageCalculator.CalculateRawDamage(
            m_AttackPower, skillMultiplier, isCritical, m_CriticalDamageMultiplier);

        // 기존 반환 계약 유지: finalDmg는 방어 적용 전 피해이며 실제 체력 감소량이 아니다.
        finalDmg = damage;
        // 대상에게 피해 전달
        if (target is MonsterBase monster)
            monster.TakeDmgFromCharacter(damage, isCritical, this);
        else
            target.TakeDmg(damage, isCritical);

        return true;
    }
    
    protected bool TryGetDamageSkillTargets(SkillData skillData, out List<MonsterBase> targets)
    {
        targets = new List<MonsterBase>();

        if (skillData == null ||
            skillData.Type != SkillType.Damage ||
            m_MonsterSpawnRoot == null)
            return false;

        if (skillData.TargetType != SkillTargetType.Random &&
            skillData.TargetType != SkillTargetType.Global)
            return false;

        MonsterBase[] monsters = m_MonsterSpawnRoot.GetComponentsInChildren<MonsterBase>();

        foreach (MonsterBase monster in monsters)
        {
            if (monster == null ||
                !monster.isActiveAndEnabled ||
                monster.IsDead ||
                !monster.IsTargetable)
                continue;

            targets.Add(monster);
        }

        if (targets.Count == 0)
            return false;

        if (skillData.TargetType == SkillTargetType.Random)
        {
            MonsterBase selected = targets[Random.Range(0, targets.Count)];

            targets.Clear();
            targets.Add(selected);
        }

        return true;
    }

    protected bool TryGetBuffSkillTargets(
        SkillData skillData,
        out List<CharacterBase> targets)
    {
        targets = new List<CharacterBase>();

        if (skillData == null ||
            skillData.Type != SkillType.Buff ||
            m_CharacterSpawnRoot == null)
            return false;

        if (skillData.TargetType != SkillTargetType.Random &&
            skillData.TargetType != SkillTargetType.Global)
            return false;

        CharacterBase[] characters =
            m_CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();

        foreach (CharacterBase character in characters)
        {
            if (character == null ||
                !character.isActiveAndEnabled ||
                !character.IsInitialized ||
                character.IsDead ||
                !character.IsTargetable)
                continue;

            targets.Add(character);
        }

        if (targets.Count == 0)
            return false;

        if (skillData.TargetType == SkillTargetType.Random)
        {
            CharacterBase selected = targets[Random.Range(0, targets.Count)];

            targets.Clear();
            targets.Add(selected);
        }

        return true;
    }
    
    /// <summary>
    /// 유효한 버프 스킬을 대상 캐릭터에게 적용하고 기존 동일 버프의 지속 시간을 갱신합니다.
    /// </summary>
    /// <param name="skillData">사용하거나 적용할 스킬 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryApplyBuff(SkillData skillData)
    {
        if (skillData == null || skillData.Type != SkillType.Buff)
            return false;

        // 이미 적용중인 버프인지 확인
        int buffIndex = m_CurBuffedSkillsList.IndexOf(skillData);

        if (buffIndex >= 0)
        {
            // 이미 적용 중이면 기존 버프의 지속시간만 초기화
            m_CurBuffedSkillsDurationList[buffIndex] = skillData.BuffTime;
        }
        else
        {
            // 처음 적용되는 버프라면 버프와 지속시간을 같은 Index로 추가
            m_CurBuffedSkillsList.Add(skillData);
            m_CurBuffedSkillsDurationList.Add(skillData.BuffTime);
        }


        // 버프 적용 후 스탯 갱신
        RefreshStats();

        // 이벤트함수에 버프변경 알림
        OnCharBuffChanged?.Invoke();
        SpawnSkillEffect(skillData, m_SkillEffectTarget);

        return true;
    }
    
    private void AddBuffMultiplier(SkillData skill)
    {
        switch (skill.BuffStat)
        {
            case SkillBuffStat.None:
                break;

            case SkillBuffStat.MaxHp:
                m_BuffedMaxHpRate += skill.EffectMultiplier;
                break;

            case SkillBuffStat.AtkPower:
                m_BuffedAttackPowerRate += skill.EffectMultiplier;
                break;

            case SkillBuffStat.AtkSpeed:
                m_BuffedAttackSpeedRate += skill.EffectMultiplier;
                break;

            case SkillBuffStat.DefPower:
                m_BuffedDefenseRate += skill.EffectMultiplier;
                break;

            case SkillBuffStat.CritChance:
                m_BuffedCriticalChanceRate += skill.EffectMultiplier;
                break;

            case SkillBuffStat.CritDmg:
                m_BuffedCriticalDamageMultiplierRate += skill.EffectMultiplier;
                break;
        }
    }

    private void ResetSkillsOnDeath()
    {
        CancelTargetSelecting();

        m_CurBuffedSkillsList.Clear();
        m_CurBuffedSkillsDurationList.Clear();

        for (int i = 0; i < m_CurSkillCoolDownList.Count; i++)
            m_CurSkillCoolDownList[i] = 0f;

        // 버프가 제거된 능력치로 갱신
        RefreshStats();
        OnCharBuffChanged?.Invoke();
    }

    private void SpawnSkillEffect(SkillData skillData, Transform effectTarget)
    {
        if (skillData == null ||
            skillData.EffectPrefab == null ||
            effectTarget == null)
            return;

        Instantiate(skillData.EffectPrefab, effectTarget, false);
    }

    /// <summary>
    /// 고정 파티 슬롯 번호로 단일 버프 대상을 선택하여 스킬 적용을 시도합니다.
    /// </summary>
    /// <param name="target">공격, 회복 또는 스킬 효과를 적용할 대상입니다.</param>
    public void SelectBuffTargetByKey(CharacterBase target)
    {
        if (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused || BattleFlowMgr.IsFieldInputBlocked || SceneChangeMgr.IsTransitioning)
            return;

        if (!IsSelectingBuffTarget || m_IsDead)
            return;

        if (target == null ||
            target.IsDead ||
            !target.IsTargetable)
            return;

        ExecutePendingBuffSkill(target);
    }
#endregion

#region 장비 관련 시스템 메서드
    // 일괄 저장에서는 모든 캐릭터 적용 완료 전까지 이벤트를 발생시키지 않는다.
    internal void ApplyEquipmentBatch(IReadOnlyDictionary<EquipType, EquipmentData> equipments)
    {
        m_CharEquipments = new Dictionary<EquipType, EquipmentData>();
        for (int i = 0; i < (int)EquipType.Count; i++)
            m_CharEquipments[(EquipType)i] = equipments[(EquipType)i];
        RefreshStats(false, false);
    }

    internal void NotifyEquipmentBatchApplied()
    {
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
        OnCharStatsChanged?.Invoke();
        OnCharEquipmentChanged?.Invoke();
    }

    internal void RestoreEquipmentBatchSnapshot(PartySnapshot snapshot)
    {
        m_CharEquipments = snapshot.Equipment;
        m_MaxHp = snapshot.Stats[0];
        m_AttackPower = snapshot.Stats[1];
        m_AttackSpeed = snapshot.Stats[2];
        m_Defense = snapshot.Stats[3];
        m_CriticalChance = snapshot.Stats[4];
        m_CriticalDamageMultiplier = snapshot.Stats[5];
        m_BuffedMaxHpRate = snapshot.Stats[6];
        m_BuffedAttackPowerRate = snapshot.Stats[7];
        m_BuffedAttackSpeedRate = snapshot.Stats[8];
        m_BuffedDefenseRate = snapshot.Stats[9];
        m_BuffedCriticalChanceRate = snapshot.Stats[10];
        m_BuffedCriticalDamageMultiplierRate = snapshot.Stats[11];
        m_CurHp = snapshot.Hp;
    }

    /// <summary>
    /// 사용자 장비 UID를 확인해 지정 장비 슬롯에 장착하고 능력치를 갱신합니다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryEquipItem(EquipmentData data)
    {
        if (data == null || data.EquipType == EquipType.Count)
            return false;

        m_CharEquipments[data.EquipType] = data;
        RefreshStats();

        OnCharEquipmentChanged?.Invoke();

        return true;
    }

    /// <summary>
    /// 지정 장비 슬롯의 아이템을 해제하고 능력치를 갱신합니다.
    /// </summary>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryUnequipItem(EquipType type)
    {
        // 존재하지 않는 슬롯이거나 이미 비어 있는 경우
        if (!m_CharEquipments.TryGetValue(type, out EquipmentData removed) || removed == null)
            return false;


        m_CharEquipments[type] = null;
        RefreshStats();

        OnCharEquipmentChanged?.Invoke();
        
        return true;
    }
#endregion

#region 유틸 시스템 메서드
    /// <summary>레벨 성장만 갱신. 현재 체력·사망 상태·버프·쿨타임·모델을 보존한다.</summary>
    public void RefreshLevelGrowth()
    {
        if (!m_IsInitialized || m_CharData == null) return;
        float hp = m_CurHp;
        RefreshStats(false, false);
        m_CurHp = Mathf.Clamp(hp, 0f, m_MaxHp);
        OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
        OnCharStatsChanged?.Invoke();
    }

    private void RefreshStats(bool restoreFullHealth = false, bool notify = true)
    {
        // 기존 최대 체력 기준 현재 체력 비율 저장
        float hpRatio = m_MaxHp > 0f
            ? m_CurHp / m_MaxHp
            : 1f;

        // 1. 기본 스탯 초기화
        float levelScale = CharacterGrowthCalc.GetMultiplier(m_CharData.Rarity, m_UserCharData?.CharLv ?? 1);
        m_MaxHp = m_CharData.BaseMaxHp * levelScale;
        m_AttackPower = m_CharData.BaseAttackPower * levelScale;
        m_AttackSpeed = m_CharData.BaseAttackSpeed;
        m_Defense = m_CharData.BaseDefense * levelScale;
        m_CriticalChance = m_CharData.BaseCriticalChance;
        m_CriticalDamageMultiplier = m_CharData.BaseCriticalDamageMultiplier;
        
        // 2. 장비 스탯 적용
        foreach (EquipmentData equipment in m_CharEquipments.Values)
        {
            if (equipment == null)
                continue;

            m_MaxHp += equipment.BonusHp;
            m_AttackPower += equipment.BonusAttack;
            m_AttackSpeed += equipment.BonusAttackSpeed;
            m_Defense += equipment.BonusDefense;
            m_CriticalChance += equipment.BonusCriticalChance;
            m_CriticalDamageMultiplier += equipment.BonusCriticalDamageMultiplier;
        }

        
        // 3. 버프 배율 초기화
        m_BuffedMaxHpRate = 0f;
        m_BuffedAttackPowerRate = 0f;
        m_BuffedAttackSpeedRate = 0f;
        m_BuffedDefenseRate = 0f;
        m_BuffedCriticalChanceRate = 0f;
        m_BuffedCriticalDamageMultiplierRate = 0f;
        
        // 4. 현재 버프들의 배율을 종류별로 합산
        foreach (SkillData skill in m_CurBuffedSkillsList)
        {
            if (skill == null)
                continue;

            AddBuffMultiplier(skill);
        }
        
        // 5. 파티 증폭스텟 버프에 추가
        if (m_PartyStat != null)
        {
            m_BuffedMaxHpRate += m_PartyStat.HpMultiplier;
            m_BuffedAttackPowerRate += m_PartyStat.AttackMultiplier;
            m_BuffedAttackSpeedRate += m_PartyStat.AttackSpeedMultiplier;
            m_BuffedDefenseRate += m_PartyStat.DefenseMultiplier;
            m_BuffedCriticalChanceRate += m_PartyStat.CriticalChanceBonus;
        }

        // 6. 합산된 버프를 최종 스탯에 한 번만 적용
        m_MaxHp *= 1f + m_BuffedMaxHpRate;
        m_AttackPower *= 1f + m_BuffedAttackPowerRate;
        m_AttackSpeed *= 1f + m_BuffedAttackSpeedRate;
        m_Defense *= 1f + m_BuffedDefenseRate;
        m_CriticalChance += m_BuffedCriticalChanceRate;
        m_CriticalChance = Mathf.Clamp01(m_CriticalChance);
        m_CriticalDamageMultiplier *= 1f + m_BuffedCriticalDamageMultiplierRate;

        if (m_PartyStat != null)
            m_CriticalDamageMultiplier += m_PartyStat.CriticalDamageBonus;
        
        // 7. 최대 체력이 변경되었다면 기존 체력 비율 유지
        m_CurHp = restoreFullHealth ? m_MaxHp : Mathf.Clamp(m_MaxHp * hpRatio, 0f, m_MaxHp);

        if (notify)
        {
            OnCharHpChanged?.Invoke(m_CurHp, m_MaxHp);
            OnCharStatsChanged?.Invoke();
        }
    }

    private void UpdateTargetMonSel()
    {
        // UI 위에서도 우클릭으로 취소
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargetSelecting();
            return;
        }

        if (UIMgr.IsPointerOverUIObject())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (Camera.main == null)
            return;

        Vector2 worldPos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        int targetMask = m_CharLayer.value | m_MonsterLayer.value;
        Collider2D hit = Physics2D.OverlapPoint(worldPos, targetMask);

        // 빈 곳 클릭은 선택 유지
        if (hit == null)
            return;

        // 공격 대상으로 아군을 클릭하면 취소
        if (hit.GetComponentInParent<CharacterBase>() != null)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("공격 스킬은 적에게 사용할 수 있습니다.");
            CancelTargetSelecting();
            return;
        }

        MonsterBase monster = hit.GetComponentInParent<MonsterBase>();

        if (monster == null || monster.IsDead || !monster.IsTargetable)
            return;

        ExecutePendingDamageSkill(monster);
    }

    private void UpdateTargetCharSel()
    {
        // UI 위에서도 우클릭으로 취소 가능
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            CancelTargetSelecting();
            return;
        }

        if (UIMgr.IsPointerOverUIObject())
            return;

        if (!Input.GetMouseButtonDown(0))
            return;

        if (Camera.main == null)
            return;

        Vector2 worldPos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // 아군과 몬스터 모두 검출해야 잘못된 대상 클릭도 처리 가능
        int targetMask = m_CharLayer.value | m_MonsterLayer.value;
        Collider2D hit = Physics2D.OverlapPoint(worldPos, targetMask);

        // 빈 곳 클릭은 선택 대기 유지
        if (hit == null)
            return;

        // 버프 대상으로 몬스터를 클릭하면 취소
        if (hit.GetComponentInParent<MonsterBase>() != null)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("버프 스킬은 아군에게 사용할 수 있습니다.");
            CancelTargetSelecting();
            return;
        }

        CharacterBase character =
            hit.GetComponentInParent<CharacterBase>();

        if (character == null ||
            character.IsDead ||
            !character.IsTargetable)
        {
            return;
        }

        ExecutePendingBuffSkill(character);
    }
#endregion

#region 오디오 플레이
#endregion

#region 애니메이션 플레이
/// <summary>
/// 스테이지 전환 이동 상태에 맞춰 캐릭터 모델 애니메이션을 전환합니다.
/// </summary>
/// <param name="moving">이동 애니메이션을 재생할지 여부입니다.</param>
public void SetTransitionMoving(bool moving)
{
    if (!m_IsInitialized || m_IsDead || m_Spum == null)
        return;
    // 연출 수정 위치: 캐릭터의 이동/도착 애니메이션을 변경한다.
    if (moving)
        PlayMoveAnimation();
    else
        PlayIdleAnimation();
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

protected void PlayDamagedAnimation(int index = 0)
{
    m_Spum.PlayAnimation(PlayerState.DAMAGED, index);
}

protected virtual void PlayDeathAnimation(int index = 0)
{
    m_Spum.PlayAnimation(PlayerState.DEATH, index);
}
#endregion
}
