using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

// MonsterPoolMgr와 같은 오브젝트에 배치한다. 없으면 런타임에 자동 추가된다.
/// <summary>
/// 필드 및 레이드 전투의 상태 전환, 이동, 승리와 패배 흐름을 조정합니다.
/// </summary>
[DisallowMultipleComponent]
[DefaultExecutionOrder(1000)]
public class BattleFlowMgr : MonoBehaviour
{
    /// <summary>
    /// 현재 전투 흐름이 어느 단계에 있는지 나타냅니다.
    /// </summary>
    public enum BattleState {
        /// <summary>
        /// 전투 시작에 필요한 대상과 웨이브를 준비하는 상태입니다.
        /// </summary>
        Preparing,
        /// <summary>
        /// 공격과 스킬 실행이 허용되는 교전 상태입니다.
        /// </summary>
        Fighting,
        /// <summary>
        /// 다음 스테이지 위치로 파티와 몬스터가 이동하는 상태입니다.
        /// </summary>
        Transitioning,
        /// <summary>
        /// 웨이브 생성 등 준비 과정의 실패로 진행이 중단된 상태입니다.
        /// </summary>
        Failed,
        /// <summary>
        /// 파티 전멸 또는 제한 시간 종료로 전투가 패배한 상태입니다.
        /// </summary>
        Defeated,
        /// <summary>
        /// 현재 전투 또는 챕터를 완료한 상태입니다.
        /// </summary>
        Cleared,
        /// <summary>레이드 보스 HP 0으로 일반 전투를 중단했다. 최종 클리어 확정 전 상태다.</summary>
        BossDefeated
    }
    /// <summary>
    /// 전투가 일반 필드 진행인지 레이드인지 구분합니다.
    /// </summary>
    public enum BattleMode {
        /// <summary>
        /// 챕터와 스테이지를 순차 진행하는 일반 필드 전투입니다.
        /// </summary>
        Field,
        /// <summary>
        /// 제한 시간 안에 단일 보스를 상대하는 레이드 전투입니다.
        /// </summary>
        Raid
    }
    /// <summary>
    /// 이 전투 흐름 인스턴스가 현재 실행하는 필드 또는 레이드 모드를 가져옵니다.
    /// </summary>
    public BattleMode Mode { get; private set; } = BattleMode.Field;
    /// <summary>
    /// 전투가 패배로 종료된 원인을 나타냅니다.
    /// </summary>
    public enum DefeatReason {
        /// <summary>
        /// 생존한 파티원이 없어 전투가 종료되었습니다.
        /// </summary>
        PartyWipedOut,
        /// <summary>
        /// 레이드 제한 시간이 만료되어 전투가 종료되었습니다.
        /// </summary>
        BossTimeExpired,
        /// <summary>플레이어가 레이드를 포기했습니다.</summary>
        GiveUp
    }

    private static BattleFlowMgr s_Active;
    /// <summary>
    /// 활성 전투 흐름의 현재 모드를 가져오며, 매니저가 없으면 필드 모드를 반환합니다.
    /// </summary>
    public static BattleMode CurrentMode => s_Active != null ? s_Active.Mode : BattleMode.Field;
    /// <summary>
    /// 메뉴 일시정지를 포함한 현재 상태에서 공격과 스킬을 실행할 수 있는지 나타냅니다.
    /// </summary>
    public static bool CanRunCombat => !IsRaidPatternPaused && !SceneChangeMgr.IsTransitioning && !PlayerDataMgr.IsProgressBlocked && !InformationPanelMgr.IsMenuPaused &&
        (s_Active == null ||
         (s_Active.isActiveAndEnabled && s_Active.State == BattleState.Fighting));

    /// <summary>
    /// 전투 흐름 매니저가 없거나, 활성 상태에서 전투 또는 이동 중이면 Hover를 허용한다.
    /// </summary>
    public static bool CanHover => s_Active == null ||
        (s_Active.isActiveAndEnabled &&
         (s_Active.State == BattleState.Fighting || s_Active.State == BattleState.Transitioning));

    [SerializeField, Min(0f)] private float m_DeathAnimationDuration = 1.5f;
    [SerializeField, Min(0f)] private float m_TransitionDuration = 1f;
    [SerializeField] private Transform m_MonsterEntryPoint;
    [SerializeField] private bool m_WaitForTransitionSignal;
    [SerializeField] private UnityEvent m_OnTransitionStarted = new();
    [SerializeField] private UnityEvent m_OnBattleStarted = new();

    private MonsterPoolMgr m_Pool;
    private MonsterData m_RaidBoss;
    private RaidPatternMgr m_RaidPatterns;
    private bool m_IsRaidPatternPaused;
    public static bool IsRaidPatternPaused => s_Active != null && s_Active.m_IsRaidPatternPaused;

    internal void SetRaidPatternPaused(bool paused)
    {
        m_IsRaidPatternPaused = paused;
        if (paused) ClearRaidCombatSelection();
    }

    internal static bool TryHandleRaidBossDamage(MonsterBase boss)
    {
        return s_Active != null && s_Active.Mode == BattleMode.Raid &&
            s_Active.State == BattleState.Fighting && s_Active.CurrentRaidBoss == boss &&
            s_Active.m_RaidPatterns != null && s_Active.m_RaidPatterns.TryQueueForCurrentHp();
    }

    private void StopRaidPatterns()
    {
        if (m_RaidPatterns != null) m_RaidPatterns.Shutdown();
        m_RaidPatterns = null;
        m_IsRaidPatternPaused = false;
    }
    private string m_FieldMapName = "MapScene";
    private string m_CurrentMapName = "MapScene";
    private bool m_IsSwitchingMap;
    public bool IsSwitchingMap => m_IsSwitchingMap;
    public bool IsReadyForSceneReveal => !m_IsSwitchingMap &&
        (State != BattleState.Preparing || m_PendingBattleStart ||
         Mode == BattleMode.Raid && m_RaidBoss == null);
    public static bool IsFieldInputBlocked => s_Active != null &&
        (s_Active.m_IsSwitchingMap || s_Active.m_IsRaidPatternPaused ||
         s_Active.Mode == BattleMode.Raid && s_Active.State != BattleState.Fighting);

    /// <summary>보스 생성과 전투 시작 없이 레이드 필드에 입장한다.</summary>
    public bool EnterRaidField(string sceneName = "RaidScene")
    {
        return Mode == BattleMode.Field && TrySwitchMap(BattleMode.Raid, sceneName, null, 0f);
    }
    public RaidCombatResult LastRaidResult { get; private set; }
    /// <summary>레이드 장비 보상을 모두 받을 인벤토리 공간이 있는지 확인한다.</summary>
    public static bool HasRaidRewardSpace =>
        GlobalConstants.InventoryMaxSlotCount - GlobalValue.UserItemDataList.Count >= GlobalConstants.RaidDropItemCount;
    private RaidCombatResult m_RaidRecord;
    private readonly Dictionary<CharacterBase, RaidCombatResult.CharacterRecord> m_RaidDamageRecords = new();

    private void BeginRaidRecord()
    {
        LastRaidResult = null;
        m_RaidRecord = new RaidCombatResult { Boss = m_RaidBoss };
        m_RaidDamageRecords.Clear();
        var characters = m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();
        for (int i = 0; i < characters.Length; i++)
        {
            var record = new RaidCombatResult.CharacterRecord { Data = characters[i].CharData, Slot = i };
            m_RaidRecord.CharacterRecords.Add(record);
            m_RaidDamageRecords.Add(characters[i], record);
        }
    }

    internal static void RecordRaidDamage(CharacterBase attacker, MonsterBase target, float damage)
    {
        var flow = s_Active;
        if (flow == null || flow.Mode != BattleMode.Raid || flow.State != BattleState.Fighting ||
            flow.CurrentRaidBoss != target || flow.m_RaidRecord == null || attacker == null || damage <= 0f ||
            !flow.m_RaidDamageRecords.TryGetValue(attacker, out var record)) return;
        record.Damage += damage;
        flow.m_RaidRecord.TotalDamage += damage;
    }

    internal static void RecordRaidPatternDamage(MonsterBase target, float damage)
    {
        var flow = s_Active;
        if (flow == null || flow.Mode != BattleMode.Raid || flow.State != BattleState.Fighting ||
            flow.CurrentRaidBoss != target || flow.m_RaidRecord == null || damage <= 0f) return;
        flow.m_RaidRecord.PatternDamage += damage;
    }

    private void CompleteRaidRecord(bool cleared)
    {
        if (m_RaidRecord == null || LastRaidResult != null) return;
        m_RaidRecord.Cleared = cleared;
        m_RaidRecord.Reason = cleared ? null : LastDefeatReason;
        m_RaidRecord.ElapsedSeconds = Mathf.Clamp(m_RaidTimeLimit - RaidTimeRemaining, 0f, m_RaidTimeLimit);
        LastRaidResult = m_RaidRecord;
    }

    public bool TryRetryRaid()
    {
        if (Mode != BattleMode.Raid || State != BattleState.Defeated || m_RaidBoss == null ||
            m_IsSwitchingMap || SceneChangeMgr.IsTransitioning || PlayerDataMgr.IsProgressBlocked ||
            InformationPanelMgr.IsMenuPaused || !HasRaidRewardSpace || !CanResetParty()) return false;
        State = BattleState.Preparing;
        LastDefeatReason = null;
        if (!ResetPartyForModeChange()) { State = BattleState.Failed; return false; }
        BeginRaidRecord();
        if (!TryPrepareWave()) return false;
        BeginBattle();
        return State == BattleState.Fighting;
    }

    private float m_RaidTimeLimit;
    [Header("Raid basic combat")]
    [SerializeField, Min(0.01f)] private float m_RaidAttackInterval = 3f;
    [SerializeField, Range(0f, 1f)] private float m_RaidAreaAttackChance = 0.2f;
    public MonsterBase CurrentRaidBoss => Mode == BattleMode.Raid && m_Pool != null ? m_Pool.RaidBoss : null;

    /// <summary>페이드 종료 후 대기 중인 레이드에서 보스를 생성하고 전투를 시작한다.</summary>
    public bool TryStartRaidCombat(MonsterData bossData, float timeLimit = 180f)
    {
        if (Mode != BattleMode.Raid || State != BattleState.Preparing || m_IsSwitchingMap ||
            SceneChangeMgr.IsTransitioning || PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused ||
            !HasRaidRewardSpace || m_Pool == null || m_Pool.RaidBoss != null || bossData == null ||
            bossData.Grade != MonsterGrade.RaidBoss || bossData.Prefab == null ||
            float.IsNaN(timeLimit) || float.IsInfinity(timeLimit) || timeLimit <= 0f)
            return false;

        m_RaidBoss = bossData;
        m_RaidTimeLimit = timeLimit;
        RaidTimeRemaining = timeLimit;
        BeginRaidRecord();
        if (!TryPrepareWave()) return false;
        BeginBattle();
        return State == BattleState.Fighting;
    }
    /// <summary>
    /// 현재 레이드 전투의 남은 제한 시간을 초 단위로 가져옵니다.
    /// </summary>
    public float RaidTimeRemaining { get; private set; }
    /// <summary>
    /// 레이드 보스를 제한 시간 안에 처치하여 전투가 클리어되었을 때 발생합니다.
    /// </summary>
    public event System.Action<MonsterData> OnRaidCleared;
    /// <summary>
    /// 필드와 레이드 사이에서 전투 모드가 변경되었을 때 새 모드를 전달합니다.
    /// </summary>
    public event System.Action<BattleMode> OnModeChanged;
    private bool m_TransitionCompleted;
    private Vector3 m_BattlePosition;
    private bool m_HasBattlePosition;
    private bool m_IsPlayingTransition;
    private bool m_IsTransitionMoving;

    /// <summary>활성 전투 흐름에서 실제 스테이지 이동 연출이 진행 중인지 반환한다.</summary>
    public static bool IsStageMoving =>
        !SceneChangeMgr.IsTransitioning && s_Active != null && s_Active.isActiveAndEnabled && s_Active.m_IsTransitionMoving;
    private readonly HashSet<CharacterBase> m_AliveCharacters = new();
    /// <summary>
    /// 전투가 패배로 확정되었을 때 패배 원인을 전달합니다.
    /// </summary>
    public event System.Action<DefeatReason> OnBattleDefeated;
    /// <summary>
    /// 가장 최근 전투 패배의 원인을 가져옵니다.
    /// </summary>
    public DefeatReason? LastDefeatReason { get; private set; }
    /// <summary>
    /// 현재 전투에 등록된 생존 파티원 수를 가져옵니다.
    /// </summary>
    public int AliveCharacterCount => m_AliveCharacters.Count;
    /// <summary>
    /// 현재 전투 진행 상태를 가져옵니다.
    /// </summary>
    public BattleState State { get; private set; } = BattleState.Preparing;

    private void Awake()
    {
        if (s_Active != null && s_Active != this)
        {
            PDebug.LogError("BattleFlowMgr가 중복 배치되었습니다.");
            enabled = false;
            return;
        }
        s_Active = this;
        m_Pool = GetComponent<MonsterPoolMgr>();
    }

    private void OnEnable()
    {
        if (m_Pool != null)
            m_Pool.OnWaveCleared += HandleWaveCleared;
    }

    private IEnumerator Start()
    {
        // PartyMgr.Start의 동기 초기화가 끝난 다음 아군을 등록한다.
        // 이후 파티 생성이 비동기로 바뀌면 초기화 완료 신호를 기다리도록 변경한다.
        yield return null;
        if (Mode == BattleMode.Field && StageMgr.Inst != null && m_Pool != null && m_Pool.CharacterSpawnRoot != null)
        {
            var stage = StageMgr.Inst;
            var party = m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();
            if (!stage.RestoreEntryHp(party))
            { State = BattleState.Failed; PDebug.LogError("체크포인트 체력 복원에 실패했습니다."); yield break; }
            if (stage.Run.EntryHp.Count == 0) stage.CaptureEntryHp(party);
            if (stage.IsFinalStage && (stage.Run.State == ChapterRunState.Defeated ||
                stage.Run.State == ChapterRunState.Cleared && stage.IsFinalSettlementComplete))
            {
                State = BattleState.Preparing;
                yield return RepeatFinalBoss(false);
                yield break;
            }
            if (stage.Run.State == ChapterRunState.Cleared)
            { State = BattleState.Cleared; yield break; }
            if (stage.Run.State == ChapterRunState.Defeated)
            { State = BattleState.Defeated; LastDefeatReason = DefeatReason.PartyWipedOut; yield break; }
        }
        if (State == BattleState.Preparing && !m_IsSwitchingMap && TryPrepareWave())
            BeginBattle();
    }

    private bool TryPrepareWave()
    {
        if (!TryRegisterParty())
        {
            State = BattleState.Failed;
            PDebug.LogError("전투 준비 실패: 초기화된 생존 아군과 파티 스폰 참조를 확인하세요.");
            return false;
        }
        if (m_Pool != null && m_Pool.SpawnRoot != null && !m_HasBattlePosition)
        {
            m_BattlePosition = m_Pool.SpawnRoot.position;
            m_HasBattlePosition = true;
        }
        if (m_Pool == null || !(Mode == BattleMode.Field
                ? m_Pool.TrySpawnCurrentStage() : m_Pool.TrySpawnRaidBoss(m_RaidBoss)))
        {
            ClearPartySubscriptions();
            State = BattleState.Failed;
            PDebug.LogError("몬스터 스폰 실패: 데이터와 스폰 슬롯을 확인하세요.");
            return false;
        }
        if (Mode == BattleMode.Raid && m_Pool.RaidBoss != null)
            m_Pool.RaidBoss.ConfigureRaidAttack(m_RaidAttackInterval, m_RaidAreaAttackChance);
        return true;
    }

    private bool m_PendingBattleStart;

    private void BeginBattle()
    {
        if (SceneChangeMgr.IsTransitioning)
        {
            m_PendingBattleStart = true;
            return;
        }
        m_PendingBattleStart = false;
        LastDefeatReason = null;
        RaidTimeRemaining = m_RaidTimeLimit;
        State = BattleState.Fighting;
        if (Mode == BattleMode.Raid)
        {
            m_RaidPatterns = FindFirstObjectByType<RaidPatternMgr>();
            if (m_RaidPatterns == null || !m_RaidPatterns.Initialize(this, CurrentRaidBoss,
                m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>(true)))
            {
                StopRaidPatterns();
                State = BattleState.Failed;
                PDebug.LogError("레이드 패턴 매니저와 미니게임 참조를 확인해주세요.");
                return;
            }
        }
        m_OnBattleStarted.Invoke();
    }

    private bool TryRegisterParty()
    {
        ClearPartySubscriptions();
        if (m_Pool == null || m_Pool.CharacterSpawnRoot == null)
            return false;

        CharacterBase[] characters =
            m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();
        foreach (CharacterBase character in characters)
        {
            // 일부만 초기화된 파티로 전투를 시작하지 않는다.
            if (!character.IsInitialized)
            {
                ClearPartySubscriptions();
                return false;
            }
            if (character.IsDead)
                continue;
            m_AliveCharacters.Add(character);
            character.OnCharDead += HandleCharacterDied;
        }
        return m_AliveCharacters.Count > 0;
    }

    private void HandleCharacterDied(CharacterBase character)
    {
        if (!character.IsDead || !m_AliveCharacters.Remove(character))
            return;
        character.OnCharDead -= HandleCharacterDied;
        if (m_AliveCharacters.Count == 0)
            TryEndBattleAsDefeat(DefeatReason.PartyWipedOut);
    }

    // 보스 타이머도 추후 이 메서드로 패배를 통지한다.
    // 상태를 먼저 확정하므로 같은 프레임의 중복 사망/실패 통지를 무시한다.
    /// <summary>
    /// 현재 전투를 지정된 사유의 패배 상태로 종료하고 관련 알림을 발생시킵니다.
    /// </summary>
    /// <param name="reason">전투를 종료할 패배 원인입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryEndBattleAsDefeat(DefeatReason reason)
    {
        if (reason == DefeatReason.GiveUp && Mode != BattleMode.Raid)
            return false;
        if (State != BattleState.Fighting && State != BattleState.Transitioning)
            return false;
        if (State == BattleState.Transitioning)
        {
            StopAllCoroutines();
            SetTransitionMoving(false);
            RestoreBattlePosition();
        }
        State = BattleState.Defeated;
        StopRaidPatterns();
        LastDefeatReason = reason;
        if (Mode == BattleMode.Raid) CompleteRaidRecord(false);
        if (Mode == BattleMode.Raid) ClearRaidCombatSelection();
        if (Mode == BattleMode.Field) StageMgr.Inst.MarkDefeated();
        ClearPartySubscriptions();
        // 패배 UI는 다음 단계에서 연결한다. 여기서 StageClear/RetrySpawn을 호출하지 않는다.
        TextPoolMgr.Inst?.ShowSystemMessage(reason == DefeatReason.GiveUp
            ? "레이드를 포기했습니다."
            : reason == DefeatReason.BossTimeExpired ? "보스 제한시간이 초과되었습니다." : "파티가 전멸했습니다.");
        OnBattleDefeated?.Invoke(reason);
        if (Mode == BattleMode.Field && StageMgr.Inst.IsFinalStage)
        {
            State = BattleState.Preparing;
            StartCoroutine(RepeatFinalBoss(true));
        }
        return true;
    }

    private void ClearPartySubscriptions()
    {
        foreach (CharacterBase character in m_AliveCharacters)
            if (character != null)
                character.OnCharDead -= HandleCharacterDied;
        m_AliveCharacters.Clear();
    }

    /// <summary>편성 교체 후 생존자만 재등록한다. 종료된 전투는 재개하지 않는다.</summary>
    public void RefreshPartyRegistration()
    {
        if (State != BattleState.Fighting && State != BattleState.Transitioning)
        {
            ClearPartySubscriptions();
            return;
        }

        if (!TryRegisterParty())
            TryEndBattleAsDefeat(DefeatReason.PartyWipedOut);
        if (State == BattleState.Transitioning)
            SetTransitionMoving(m_IsTransitionMoving);
    }

    private void HandleWaveCleared()
    {
        if (State != BattleState.Fighting)
            return;
        if (Mode == BattleMode.Raid)
        {
            State = BattleState.BossDefeated;
            TryConfirmRaidClear();
            return;
        }
        State = BattleState.Transitioning;
        StartCoroutine(AdvanceStage());
    }

    /// <summary>최종 판정 후 성공과 보상, 결과 기록을 한 번 확정한다.</summary>
    public bool TryConfirmRaidClear()
    {
        if (Mode != BattleMode.Raid || State != BattleState.BossDefeated || m_IsSwitchingMap)
            return false;
        State = BattleState.Cleared;
        StopRaidPatterns();
        ClearPartySubscriptions();
        ClearRaidCombatSelection();
        GrantRaidRewards();
        CompleteRaidRecord(true);
        OnRaidCleared?.Invoke(m_RaidBoss);
        return true;
    }

    private void ClearRaidCombatSelection()
    {
        TargetSelMgr selector = FindFirstObjectByType<TargetSelMgr>();
        if (selector != null) selector.ClearTargets();
        if (m_Pool == null || m_Pool.CharacterSpawnRoot == null) return;
        foreach (CharacterBase character in m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>())
            character.CancelSkillSelectionForMenu();
    }

    private IEnumerator AdvanceStage()
    {
        yield return new WaitForSeconds(m_DeathAnimationDuration);

        if (Mode != BattleMode.Field)
            yield break;

        m_Pool.ClearWave();

        bool chapterCleared =
            StageMgr.Inst.CurStage >= StageMgr.Inst.MaxStage;

        if (chapterCleared)
        {
            // 보상 패널이 열리기 전에 선택 가능한 상태로 바꾼다.
            State = BattleState.Cleared;
            ClearPartySubscriptions();

            StageMgr.Inst.StageClear();
            if (StageMgr.Inst.IsFinalStage && StageMgr.Inst.IsFinalSettlementComplete)
            {
                State = BattleState.Preparing;
                yield return RepeatFinalBoss(false);
            }
            yield break;
        }

        // 일반 스테이지는 기존처럼 자동 진행한다.
        StageMgr.Inst.StageClear();
        yield return PrepareAndEnterStage();
    }

    private IEnumerator PrepareAndEnterStage()
    {
        // 런타임에 자동 추가된 경우에도 기존 씬의 출발 위치를 찾는다.
        if (m_MonsterEntryPoint == null)
        {
            GameObject entry = GameObject.Find("MonsterEntryPoint");
            if (entry != null)
                m_MonsterEntryPoint = entry.transform;
        }
        if (!m_HasBattlePosition || m_Pool == null || m_Pool.SpawnRoot == null ||
            m_MonsterEntryPoint == null ||
            m_MonsterEntryPoint.IsChildOf(m_Pool.SpawnRoot))
        {
            State = BattleState.Failed;
            PDebug.LogError("MonsterEntryPoint를 몬스터 스폰 루트 밖에 배치하고 연결하세요.");
            yield break;
        }

        m_Pool.SpawnRoot.position = m_MonsterEntryPoint.position;
        // 생성만 수행한다. 이동 완료 전에는 Fighting으로 바꾸지 않는다.
        if (!TryPrepareWave())
        {
            RestoreBattlePosition();
            yield break;
        }
        yield return PlayStageTransition();
        BeginBattle();
    }

    private IEnumerator PlayStageTransition()
    {
        m_TransitionCompleted = false;
        m_IsPlayingTransition = true;
        SetTransitionMoving(true);
        // 연출 추가 위치 1: 배경 스크롤, 카메라 이동, 이동 시작 효과음.
        m_OnTransitionStarted.Invoke();

        Vector3 startPosition = m_Pool.SpawnRoot.position;
        float elapsed = 0f;
        while (elapsed < m_TransitionDuration)
        {
            if (SceneChangeMgr.IsTransitioning) { yield return null; continue; }
            elapsed += Time.deltaTime;
            // 연출 추가 위치 2: 기본 선형 이동을 원하는 곡선/트윈으로 교체한다.
            m_Pool.SpawnRoot.position = Vector3.Lerp(startPosition, m_BattlePosition,
                Mathf.Clamp01(elapsed / m_TransitionDuration));
            yield return null;
        }
        RestoreBattlePosition();
        SetTransitionMoving(false);
        // 연출 추가 위치 3: 도착 이펙트, Idle 유지 시간 등을 여기서 기다린다.
        yield return new WaitForSeconds(0.2f);
        // 외부 연출도 기다리려면 아래 옵션을 켜고 완료 시 CompleteTransition() 호출.
        if (m_WaitForTransitionSignal)
            yield return new WaitUntil(() => m_TransitionCompleted);
        m_IsPlayingTransition = false;
    }

    private void SetTransitionMoving(bool moving)
    {
        m_IsTransitionMoving = moving;
        if (m_Pool == null)
            return;
        m_Pool.SetTransitionMoving(moving);
        if (m_Pool.CharacterSpawnRoot == null)
            return;
        foreach (CharacterBase character in
                 m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>())
            character.SetTransitionMoving(moving);
    }

    private void RestoreBattlePosition()
    {
        if (m_HasBattlePosition && m_Pool != null && m_Pool.SpawnRoot != null)
            m_Pool.SpawnRoot.position = m_BattlePosition;
    }

    // 이동 애니메이션/타임라인/트윈의 완료 콜백에서 호출한다.
    /// <summary>
    /// 스테이지 이동 연출을 끝내고 다음 전투를 시작할 수 있는 상태로 전환합니다.
    /// </summary>
    public void CompleteTransition()
    {
        if (m_IsPlayingTransition)
            m_TransitionCompleted = true;
    }

    // true는 전투 시작 완료가 아니라 요청 수락을 의미한다.
    /// <summary>
    /// 챕터 정산 이후 다음 챕터 진행을 준비하고 이동을 시작합니다.
    /// </summary>
    /// <param name="nextChapter">정산 후 진행할 다음 챕터 번호입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryContinueAfterChapterClear(bool nextChapter)
    {
        if (!isActiveAndEnabled ||
            m_IsSwitchingMap ||
            Mode != BattleMode.Field ||
            State != BattleState.Cleared ||
            StageMgr.Inst == null)
            return false;

        if (nextChapter && StageMgr.Inst.CurChapter >= GlobalConstants.FinalFieldChapter)
            return false;

        if (!CanResetParty())
        {
            PDebug.LogError("챕터 시작 실패: 파티 복구 참조를 확인하세요.");
            return false;
        }

        // 연속 클릭으로 요청이 중복 실행되지 않도록 즉시 변경한다.
        State = BattleState.Preparing;
        StartCoroutine(ContinueAfterChapterClear(nextChapter));
        return true;
    }

    private IEnumerator ContinueAfterChapterClear(bool nextChapter)
    {
        yield return null;

        if (!CanResetParty() || StageMgr.Inst == null)
        {
            State = BattleState.Failed;
            yield break;
        }

        ClearPartySubscriptions();

        TargetSelMgr selector = FindFirstObjectByType<TargetSelMgr>();
        if (selector != null)
            selector.ClearTargets();

        m_Pool.ClearWave();
        RestoreBattlePosition();

        CharacterBase[] characters =
            m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();

        foreach (CharacterBase character in characters)
        {
            if (nextChapter)
            {
                // 기존 챕터 이동 정책: 부활·완전 회복,
                // 생존자의 버프·쿨타임은 유지한다.
                character.RefreshCharacter();
            }
            else if (!character.TryResetForRetry())
            {
                // 재도전은 기존처럼 버프·쿨타임도 초기화한다.
                State = BattleState.Failed;
                yield break;
            }
        }

        if (nextChapter)
            StageMgr.Inst.ToNextChapter();
        else if (StageMgr.Inst.IsFinalStage)
        {
            StageMgr.Inst.CompleteFinalSettlement();
            StageMgr.Inst.RestartFinalBoss();
        }
        else
            StageMgr.Inst.RestartChapter();

        if (nextChapter)
        {
            State = BattleState.Transitioning;
            yield return PrepareAndEnterStage();
        }
        else
        {
            // 현재 챕터 재도전은 기존처럼 같은 위치에서 시작한다.
            if (TryPrepareWave())
                BeginBattle();
        }
    }

    // 승리/전멸 모두 같은 복구 경로를 사용하고, 실패 시에는 Failed UI로 수동 복구한다.
    private IEnumerator RepeatFinalBoss(bool waitForDeath)
    {
        if (waitForDeath) yield return new WaitForSeconds(m_DeathAnimationDuration);
        else yield return null;
        while (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused)
            yield return null;
        if (Mode != BattleMode.Field || StageMgr.Inst == null || !StageMgr.Inst.IsFinalStage)
            yield break;
        yield return ContinueAfterChapterClear(false);
    }
    
    // true 반환은 재시작 완료가 아니라 요청 수락을 의미한다.
    /// <summary>
    /// 현재 챕터를 처음부터 재시작할 수 있는지 확인하고 전투 상태를 초기화합니다.
    /// </summary>
    /// <param name="previousChapter">재도전 또는 이동 시 복원할 이전 챕터 번호입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryRestartChapter(bool previousChapter)
    {
        if (!isActiveAndEnabled || m_IsSwitchingMap)
            return false;

        // Failed 상태에서는 복구·스폰 실패 후 재시도도 허용한다.
        if (State != BattleState.Defeated && State != BattleState.Failed)
            return false;

        if (Mode == BattleMode.Field && StageMgr.Inst == null)
            return false;

        if (previousChapter)
        {
            // 실패 재시도 중 챕터가 연속해서 내려가지 않도록 제한한다.
            if (State != BattleState.Defeated ||
                Mode != BattleMode.Field ||
                StageMgr.Inst.CurChapter <= 1)
                return false;
        }

        State = BattleState.Preparing;
        StartCoroutine(RestartCurrentStage(previousChapter));
        return true;
    }

    private IEnumerator RestartCurrentStage(bool previousChapter)
    {
        // OnDead/패배 이벤트 안에서 요청되어도 기존 사망 처리가 끝난 뒤 복구한다.
        yield return null;
        ClearPartySubscriptions();
        if (m_Pool == null || m_Pool.CharacterSpawnRoot == null)
        {
            State = BattleState.Failed;
            PDebug.LogError("재도전 실패: 파티 스폰 참조가 없습니다.");
            yield break;
        }
        CharacterBase[] characters =
            m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();
        if (characters.Length == 0)
        {
            State = BattleState.Failed;
            PDebug.LogError("재도전 실패: 복구할 캐릭터가 없습니다.");
            yield break;
        }
        foreach (CharacterBase character in characters)
            if (!character.CanResetForRetry)
            {
                State = BattleState.Failed;
                PDebug.LogError("재도전 실패: 캐릭터 초기화/모델 참조를 확인하세요.");
                yield break;
            }

        TargetSelMgr targetSelector = FindFirstObjectByType<TargetSelMgr>();
        if (targetSelector != null)
            targetSelector.ClearTargets();
        m_Pool.ClearWave();
        RestoreBattlePosition();
        foreach (CharacterBase character in characters)
            if (!character.TryResetForRetry())
            {
                State = BattleState.Failed;
                PDebug.LogError("캐릭터 복구에 실패하여 전투를 중지합니다.");
                yield break;
            }

        // 현재 챕터는 유지하고 1스테이지부터 다시 시작
        if (Mode == BattleMode.Field)
        {
            if (previousChapter)
                StageMgr.Inst.ToPrevChapter();
            else if (StageMgr.Inst.IsFinalStage)
                StageMgr.Inst.RestartFinalBoss();
            else
                StageMgr.Inst.RestartChapter();
        }

        // 같은 위치에서 새 전투를 시작한다. 진행도 증가와 이동 연출은 실행하지 않는다.
        if (TryPrepareWave())
            BeginBattle();
    }

    // 데이터 수정 후 같은 스테이지를 재시도한다. StageClear는 다시 호출하지 않는다.
    /// <summary>
    /// 패배 후 재도전을 위해 파티와 스테이지를 복원하고 몬스터 생성을 다시 요청합니다.
    /// </summary>
    public void RetrySpawn()
    {
        if (isActiveAndEnabled && !m_IsSwitchingMap && State == BattleState.Failed)
        {
            State = BattleState.Transitioning;
            if (Mode == BattleMode.Raid || !m_HasBattlePosition)
            {
                if (TryPrepareWave())
                    BeginBattle();
                return;
            }
            StartCoroutine(PrepareAndEnterStage());
        }
    }

    private void Update()
    {
        if (SceneChangeMgr.IsTransitioning) return;
        if (m_PendingBattleStart)
        {
            if (State == BattleState.Preparing || State == BattleState.Transitioning) BeginBattle();
            else m_PendingBattleStart = false;
        }
        // 패턴 중에도 Fighting 상태와 전체 제한시간은 유지한다.
        if (Mode != BattleMode.Raid || State != BattleState.Fighting || m_RaidTimeLimit <= 0f)
            return;
        RaidTimeRemaining = Mathf.Max(0f, RaidTimeRemaining - Time.deltaTime);
        if (RaidTimeRemaining <= 0f)
            m_RaidTimeExpiredFrame = Time.frameCount;
    }

    private int m_RaidTimeExpiredFrame = -1;

    private void LateUpdate()
    {
        // 환영 패턴의 LateUpdate 이후 결과를 반영하고 전체 시간 만료를 판정한다.
        if (Mode == BattleMode.Raid && State == BattleState.Fighting && m_RaidPatterns != null)
            m_RaidPatterns.ResolveResult();
        if (m_RaidTimeExpiredFrame == Time.frameCount && Mode == BattleMode.Raid &&
            State == BattleState.Fighting && !SceneChangeMgr.IsTransitioning)
            TryEndBattleAsDefeat(DefeatReason.BossTimeExpired);
        if (Mode == BattleMode.Raid && State == BattleState.Fighting && m_RaidPatterns != null)
            m_RaidPatterns.AdvancePendingPattern();
    }

    private void GrantRaidRewards()
    {
        if (m_RaidBoss == null || string.IsNullOrWhiteSpace(m_RaidBoss.Id))
        {
            PDebug.LogError("레이드 보상 지급 실패: 보스 정보가 없습니다.");
            return;
        }

        bool a_GetDropTable = GlobalConstants.RaidItemDropTable.TryGetValue(m_RaidBoss.Id, out List<string> a_ItemIds);
        bool a_GetGoldTable = GlobalConstants.RaidGoldDropTable.TryGetValue(m_RaidBoss.Id, out int a_Gold);

        if (!a_GetDropTable || !a_GetGoldTable)
        {
            PDebug.LogError("Droptable or Goldtable of RaidBoss not found: " + m_RaidBoss.Id);
            return;
        }
        if (a_ItemIds == null || a_ItemIds.Count == 0)
        {
            PDebug.LogError("레이드 드롭 목록이 비어 있습니다: " + m_RaidBoss.Id);
            return;
        }
        if (a_Gold < 0 || GlobalValue.UserGold > ulong.MaxValue - (ulong)a_Gold)
        {
            PDebug.LogError("레이드 보상 골드가 음수이거나 보유 골드의 저장 범위를 초과합니다: " + m_RaidBoss.Id);
            return;
        }

        MasterDataMgr a_MasterDataMgr = FindFirstObjectByType<MasterDataMgr>();
        if (a_MasterDataMgr == null)
        {
            PDebug.LogError("레이드 보상 지급 실패: MasterDataMgr가 없습니다.");
            return;
        }

        // 지급을 시작하기 전에 모든 후보를 검증한다. 중복 ID의 추첨 비중은 유지한다.
        List<EquipmentData> a_RewardCandidates = new(a_ItemIds.Count);
        foreach (string a_ItemId in a_ItemIds)
        {
            if (string.IsNullOrWhiteSpace(a_ItemId) ||
                !a_MasterDataMgr.TryGetEquipment(a_ItemId, out EquipmentData data) ||
                data == null || data.Type != ItemType.Equipment)
            {
                PDebug.LogError("레이드 드롭 장비 데이터가 유효하지 않습니다: " + a_ItemId);
                return;
            }
            a_RewardCandidates.Add(data);
        }

        // 보상골드 지급
        GlobalValue.AddUserGold((ulong)a_Gold);
        if (m_RaidRecord != null) m_RaidRecord.Gold = (ulong)a_Gold;

        // 중복을 허용해 10회 추첨한다. 인벤토리 부족 시 나머지 지급은 중단한다.
        for (int i = 0; i < GlobalConstants.RaidDropItemCount; i++)
        {
            int a_RanNum = Random.Range(0, a_RewardCandidates.Count);
            if (!GlobalValue.TryAddEquipItem(a_RewardCandidates[a_RanNum], out ItemData addedItem))
                return;
            m_RaidRecord?.RewardItems.Add(addedItem);
        }
    }

    /// <summary>선택한 보스와 맵으로 입장한다. true는 전환 요청 수락을 의미한다.</summary>
    /// <param name="bossData">레이드에서 생성할 보스 마스터 데이터입니다.</param>
    /// <param name="raidMapSceneName">레이드 전투에서 Additive로 전환할 맵 씬 이름입니다.</param>
    /// <param name="timeLimit">레이드 전투에 적용할 제한 시간(초)입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool EnterRaid(MonsterData bossData, string raidMapSceneName, float timeLimit = 0f)
    {
        if (Mode != BattleMode.Field || !HasRaidRewardSpace || bossData == null ||
            bossData.Grade != MonsterGrade.RaidBoss || bossData.Prefab == null ||
            float.IsNaN(timeLimit) || float.IsInfinity(timeLimit) || timeLimit < 0f)
            return false;
        return TrySwitchMap(BattleMode.Raid, raidMapSceneName, bossData, timeLimit);
    }

    /// <summary>기존 일반 진행도를 보존한 채 일반 맵의 현재 스테이지로 복귀한다.</summary>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool ReturnToField()
    {
        return Mode == BattleMode.Raid &&
            TrySwitchMap(BattleMode.Field, m_FieldMapName, null, 0f);
    }

    private bool TrySwitchMap(BattleMode nextMode, string mapName, MonsterData boss, float timeLimit)
    {
        if (!isActiveAndEnabled || s_Active != this || m_IsSwitchingMap ||
            string.IsNullOrWhiteSpace(mapName) || mapName == m_CurrentMapName ||
            SceneManager.GetSceneByName(mapName).isLoaded ||
            !Application.CanStreamedLevelBeLoaded(mapName) || !CanResetParty())
            return false;
        Scene currentMap = SceneManager.GetSceneByName(m_CurrentMapName);
        if (!currentMap.IsValid() || !currentMap.isLoaded)
            return false;

        // 보스 사망 이벤트에서 바로 요청해도 현재 사망 처리가 끝난 다음 정리한다.
        m_IsSwitchingMap = true;
        m_PendingBattleStart = false;
        State = BattleState.Preparing;
        StopAllCoroutines();
        StartCoroutine(SwitchMap(nextMode, mapName, boss, timeLimit, currentMap));
        return true;
    }

    private bool CanResetParty()
    {
        if (m_Pool == null || m_Pool.CharacterSpawnRoot == null)
            return false;
        CharacterBase[] characters = m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>();
        if (characters.Length == 0)
            return false;
        foreach (CharacterBase character in characters)
            if (!character.CanResetForRetry)
                return false;
        return true;
    }

    private bool ResetPartyForModeChange()
    {
        StopRaidPatterns();
        ClearPartySubscriptions();
        m_IsPlayingTransition = false;
        SetTransitionMoving(false);
        RestoreBattlePosition();
        TargetSelMgr selector = FindFirstObjectByType<TargetSelMgr>();
        if (selector != null)
            selector.ClearTargets();
        m_Pool.ClearWave();
        foreach (CharacterBase character in m_Pool.CharacterSpawnRoot.GetComponentsInChildren<CharacterBase>())
            if (!character.TryResetForRetry())
                return false;
        return true;
    }

    private IEnumerator SwitchMap(BattleMode nextMode, string mapName, MonsterData boss,
        float timeLimit, Scene previousMap)
    {
        try
        {
            yield return null;
            if (!ResetPartyForModeChange())
            {
                State = BattleState.Failed;
                yield break;
            }

            // 기존 맵은 새 맵 로드가 성공하기 전까지 유지한다.
            AsyncOperation load = TryLoadMap(mapName);
            if (load != null)
                yield return load;
            Scene nextMap = SceneManager.GetSceneByName(mapName);
            if (load == null || !nextMap.IsValid() || !nextMap.isLoaded)
            {
                PDebug.LogError($"맵 로드 실패: {mapName}. 기존 모드에서 재시도할 수 있습니다.");
                State = BattleState.Failed;
                yield break;
            }

            AsyncOperation unload = TryUnloadMap(previousMap);
            if (unload != null)
                yield return unload;
            if (previousMap.isLoaded)
            {
                AsyncOperation rollback = TryUnloadMap(nextMap);
                if (rollback != null)
                    yield return rollback;
                State = BattleState.Failed;
                PDebug.LogError("기존 맵 해제에 실패했습니다. 씬 상태를 확인하세요.");
                yield break;
            }

            if (Mode == BattleMode.Field)
                m_FieldMapName = m_CurrentMapName;
            m_CurrentMapName = mapName;
            m_RaidBoss = boss;
            LastRaidResult = null;
            m_RaidRecord = null;
            m_RaidDamageRecords.Clear();
            m_RaidTimeLimit = timeLimit;
            RaidTimeRemaining = timeLimit;
            Mode = nextMode;
            LastDefeatReason = null;
            // 로딩 중 사용 가능한 버프와 대상 선택도 전투 시작 전에 다시 초기화한다.
            if (!CanResetParty() || !ResetPartyForModeChange())
            {
                State = BattleState.Failed;
                yield break;
            }
            OnModeChanged?.Invoke(Mode);
            if (nextMode == BattleMode.Raid && boss == null)
                yield break;
            if (nextMode == BattleMode.Raid) BeginRaidRecord();
            if (TryPrepareWave())
                BeginBattle();
        }
        finally
        {
            m_IsSwitchingMap = false;
        }
    }

    private AsyncOperation TryLoadMap(string mapName)
    {
        try
        {
            return SceneManager.LoadSceneAsync(mapName, LoadSceneMode.Additive);
        }
        catch (System.Exception exception)
        {
            PDebug.LogError($"맵 로드 요청 실패: {exception.Message}");
            return null;
        }
    }

    private AsyncOperation TryUnloadMap(Scene scene)
    {
        try
        {
            return SceneManager.UnloadSceneAsync(scene);
        }
        catch (System.Exception exception)
        {
            PDebug.LogError($"맵 해제 요청 실패: {exception.Message}");
            return null;
        }
    }

    private void OnDisable()
    {
        StopRaidPatterns();
        ClearPartySubscriptions();
        if (m_Pool != null)
            m_Pool.OnWaveCleared -= HandleWaveCleared;
        StopAllCoroutines();
        if (s_Active == this)
        {
            m_IsPlayingTransition = false;
            SetTransitionMoving(false);
            RestoreBattlePosition();
            if (m_Pool != null)
                m_Pool.ClearWave();
            State = BattleState.Failed;
        }
    }

    private void OnDestroy()
    {
        ClearPartySubscriptions();
        if (s_Active == this)
            s_Active = null;
    }
}
