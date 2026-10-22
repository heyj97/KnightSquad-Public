using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// 현재 챕터와 스테이지 진행, 필드 보상 및 챕터 정산을 관리합니다.
/// </summary>
public class StageMgr : G_Singleton<StageMgr>
{
    protected override void Awake()
    {
        base.Awake();
        m_MaxStage = GlobalConstants.FieldStagesPerChapter;
        // 로그인은 메모리만 준비한다. 씬에 배치된 매니저가 실제 진행 위치를 복원한다.
        SetStageMgr(GlobalValue.CurStageData.CurChapter, GlobalValue.CurStageData.CurStage);
        EnsureRun();
    }
    [Header("Current Stage")]
    [SerializeField] private int m_MaxStage = 10;
    [SerializeField] private int m_CurChapter = 1;
    [SerializeField] private int m_CurStage = 1;

    /// <summary>
    /// 스테이지별 몬스터 등장 설정
    /// </summary>
    [Serializable]
    public class StageMonsterSetting
    {
        /// <summary>
        /// 각 스폰 슬롯에서 엘리트 몬스터가 등장할 확률
        /// </summary>
        [Range(0f, 1f)]
        public float eliteRatio;

        /// <summary>
        /// 한 웨이브에 등장 가능한 최대 엘리트 몬스터 수
        /// </summary>
        [Range(0, 4)]
        public int maxEliteCount;
    }

    [Header("Stage Monster Settings")]
    [SerializeField]
    private List<StageMonsterSetting> m_StageMonsterSettings = new()
    {
        new StageMonsterSetting { eliteRatio = 0.00f,  maxEliteCount = 0 }, // Stage 1
        new StageMonsterSetting { eliteRatio = 0.05f,  maxEliteCount = 0 }, // Stage 2
        new StageMonsterSetting { eliteRatio = 0.10f,  maxEliteCount = 1 }, // Stage 3
        new StageMonsterSetting { eliteRatio = 0.125f, maxEliteCount = 1 }, // Stage 4
        new StageMonsterSetting { eliteRatio = 0.15f,  maxEliteCount = 1 }, // Stage 5
        new StageMonsterSetting { eliteRatio = 0.175f, maxEliteCount = 2 }, // Stage 6
        new StageMonsterSetting { eliteRatio = 0.20f,  maxEliteCount = 2 }, // Stage 7
        new StageMonsterSetting { eliteRatio = 0.23f,  maxEliteCount = 2 }, // Stage 8
        new StageMonsterSetting { eliteRatio = 0.26f,  maxEliteCount = 2 }, // Stage 9
        new StageMonsterSetting { eliteRatio = 0.30f,  maxEliteCount = 0 }, // Stage 10 : 보스스테이지
    };

    /// <summary>
    /// Chapter, Stage가 변경되었을 때 발생
    /// </summary>
    public event Action<int, int> OnStageChanged;
    /// <summary>
    /// 현재 챕터의 마지막 스테이지를 완료하여 보상 정산이 가능해졌을 때 발생합니다.
    /// </summary>
    public event Action<int> OnChapterCleared;
    /// <summary>
    /// 필드 전투에서 장비 보상이 드롭되었을 때 해당 데이터를 전달합니다.
    /// </summary>
    public event Action OnItemDropped;

    /// <summary>
    /// 현재 진행 중인 챕터 번호를 가져옵니다.
    /// </summary>
    public int CurChapter => m_CurChapter;
    /// <summary>
    /// 현재 챕터 안에서 진행 중인 스테이지 번호를 가져옵니다.
    /// </summary>
    public int CurStage => m_CurStage;
    /// <summary>
    /// 한 챕터를 완료하기 위해 진행해야 하는 마지막 스테이지 번호를 가져옵니다.
    /// </summary>
    public int MaxStage => m_MaxStage;
    
    private List<EquipmentData> m_ChapterDroppedEquipsList = new List<EquipmentData>();
    /// <summary>
    /// 현재 챕터에서 획득해 정산할 장비 목록을 읽기 전용으로 제공합니다.
    /// </summary>
    public IReadOnlyList<EquipmentData> ChapterDroppedEquipsList => m_ChapterDroppedEquipsList;
    
    /// <summary>
    /// 현재 챕터 진행 중 누적한 골드 보상량을 가져옵니다.
    /// </summary>
    public ulong ChapterEarnedGold => Run.EarnedGold;
    public ChapterRunSaveData Run => EnsureRun();
    public IReadOnlyList<ItemData> ChapterEarnedItems => Run.EarnedItems;
    public bool IsChapterCleared => Run.State == ChapterRunState.Cleared;
    public bool IsFinalStage => m_CurChapter == GlobalConstants.FinalFieldChapter &&
        m_CurStage == GlobalConstants.FinalFieldStage;
    public bool IsFinalSettlementComplete => GlobalValue.CurStageData.HasSettled(
        GlobalConstants.FinalFieldChapter, GlobalConstants.FinalFieldStage);
    private bool KeepRewardHistory => !IsFinalStage || !IsFinalSettlementComplete;

    private ChapterRunSaveData EnsureRun()
    {
        var progress = GlobalValue.CurStageData;
        if (progress.Run == null)
            progress.Run = new ChapterRunSaveData { AttemptId = Guid.NewGuid().ToString("N"),
                Chapter = m_CurChapter, CheckpointStage = m_CurStage };
        return progress.Run;
    }

    /// <summary>스테이지 진입 시에만 체력을 갱신한다. 전투 중 자동 저장은 이 값을 바꾸지 않는다.</summary>
    public void CaptureEntryHp(IEnumerable<CharacterBase> characters)
    {
        Run.CheckpointStage = m_CurStage;
        Run.EntryHp.Clear();
        foreach (var character in characters)
            if (character != null && character.IsInitialized && character.UserCharData != null)
                Run.EntryHp.Add(new PartyCheckpointHp { CharId = character.UserCharData.CharId, Hp = character.CurHp });
    }

    /// <summary>모델·장비·파티 강화 초기화 이후, 생존자 등록 전에 한 번 복원한다.</summary>
    public bool RestoreEntryHp(IEnumerable<CharacterBase> characters)
    {
        foreach (var character in characters)
        {
            if (character == null || character.UserCharData == null) continue;
            var saved = Run.EntryHp.Find(value => value.CharId == character.UserCharData.CharId);
            if (saved != null && !character.TryRestoreCheckpointHp(saved.Hp)) return false;
        }
        return true;
    }

    /// <summary>편성 변경 시 기존 멤버의 진입 체력은 유지하고 새 멤버만 등록한다.</summary>
    public void SyncCheckpointParty(IEnumerable<CharacterBase> characters)
    {
        var updated = new List<PartyCheckpointHp>();
        foreach (var character in characters)
        {
            if (character == null || character.UserCharData == null) continue;
            string id = character.UserCharData.CharId;
            updated.Add(Run.EntryHp.Find(value => value.CharId == id) ?? new PartyCheckpointHp { CharId = id, Hp = character.CurHp });
        }
        Run.EntryHp = updated;
    }

    public void MarkDefeated()
    {
        Run.State = ChapterRunState.Defeated;
        var party = FindFirstObjectByType<PartyMgr>();
        if (party != null) CaptureEntryHp(party.PartyCharacters);
        if (!IsFinalStage) PlayerDataMgr.RequestImmediateSave();
    }

    /// <summary>필드 보상은 슬롯당 한 번만 메모리에 적용하고 일반 저장에 모은다.</summary>
    public void GrantFieldReward(int slot, MonsterGrade grade)
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field || PlayerDataMgr.IsProgressBlocked) return;
        if (!Run.TryRecordReward(m_CurStage, slot)) return;
        ulong gold = GlobalConstants.GetMonsterGold(grade, m_CurChapter, m_CurStage);
        AddFieldGold(gold);
        FieldItemDrop();
    }

    /// <summary>
    /// 현재 스테이지에서 등장할 엘리트 몬스터 수를 결정한다.
    /// </summary>
    /// <returns>현재 챕터와 스테이지에 배치할 엘리트 몬스터 수입니다.</returns>
    public int GetEliteMonCount()
    {
        // Stage는 1부터 시작하지만 List Index는 0부터 시작
        int stageIndex = m_CurStage - 1;

        // 잘못된 Stage 값으로 인한 IndexOutOfRange 방지
        if (stageIndex < 0 || stageIndex >= m_StageMonsterSettings.Count)
        {
            PDebug.LogError(
                $"잘못된 Stage 값입니다. \n" +
                $"Current Stage: {m_CurStage}, \n" +
                $"Setting Count: {m_StageMonsterSettings.Count}"
            );

            return 0;
        }

        StageMonsterSetting setting = m_StageMonsterSettings[stageIndex];

        int eliteMonCount = 0;

        const int spawnCount = 4;

        // 4개의 스폰 슬롯에서 각각 엘리트 등장 여부 판정
        for (int i = 0; i < spawnCount; i++)
        {
            // 최대 엘리트 수에 도달했으면 더 이상 판정할 필요 없음
            if (eliteMonCount >= setting.maxEliteCount)
                break;

            if (UnityEngine.Random.value < setting.eliteRatio)
            {
                eliteMonCount++;
            }
        }

        return eliteMonCount;
    }

        /// <summary>
    /// 현재 스테이지 클리어 처리
    /// </summary>
    public void StageClear()
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field || Run.State != ChapterRunState.InProgress)
            return;
        // 마지막 스테이지라면 챕터 클리어
        if (m_CurStage >= MaxStage)
        {
            ChapterClear();
            return;
        }

        // 다음 스테이지로 진행
        m_CurStage++;

        GlobalValue.CurStageData.CurChapter = m_CurChapter;
        GlobalValue.CurStageData.CurStage = m_CurStage;
        Run.CheckpointStage = m_CurStage;
        var party = FindFirstObjectByType<PartyMgr>();
        if (party != null) CaptureEntryHp(party.PartyCharacters);
        OnStageChanged?.Invoke(m_CurChapter, m_CurStage);
    }

    private void ChapterClear()
    {
        Run.State = ChapterRunState.Cleared;
        if (IsFinalStage)
        {
            var progress = GlobalValue.CurStageData;
            if (progress.ClearedChapter < m_CurChapter ||
                progress.ClearedChapter == m_CurChapter && progress.ClearedStage < m_CurStage)
            {
                progress.ClearedChapter = m_CurChapter;
                progress.ClearedStage = m_CurStage;
            }
            if (IsFinalSettlementComplete) return;
        }
        var party = FindFirstObjectByType<PartyMgr>();
        if (party != null) CaptureEntryHp(party.PartyCharacters);
        PlayerDataMgr.RequestImmediateSave();
        // 챕터 번호와 획득 내역을 유지한 채 보상 표시만 요청한다.
        OnChapterCleared?.Invoke(m_CurChapter);
    }

    /// <summary>
    /// 저장 데이터에서 불러온 챕터와 스테이지를 현재 진행 위치로 적용합니다.
    /// </summary>
    /// <param name="chapter">복원할 챕터 번호입니다.</param>
    /// <param name="stage">복원할 챕터 내 스테이지 번호입니다.</param>
    public void SetStageMgr(int chapter, int stage)
    {
        m_CurChapter = chapter;
        m_CurStage = stage;
    }

    /// <summary>
    /// 현재 챕터의 스테이지 진행과 임시 보상을 초기 상태로 되돌립니다.
    /// </summary>
    public void RestartChapter()
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field)
            return;
        // 현재 챕터는 그대로 두고 1스테이지로 복귀
        m_CurStage = 1;
        GlobalValue.CurStageData.Run = new ChapterRunSaveData { AttemptId = Guid.NewGuid().ToString("N"),
            Chapter = m_CurChapter, CheckpointStage = 1 };
        m_ChapterDroppedEquipsList.Clear();

        // 저장용 진행 데이터 동기화
        GlobalValue.CurStageData.CurChapter = m_CurChapter;
        GlobalValue.CurStageData.CurStage = m_CurStage;

        var party = FindFirstObjectByType<PartyMgr>();
        if (party != null) CaptureEntryHp(party.PartyCharacters);
        PlayerDataMgr.RequestImmediateSave();

        // 변경된 챕터·스테이지를 구독자에게 알림
        OnStageChanged?.Invoke(m_CurChapter, m_CurStage);
    }

    /// <summary>
    /// 이전 챕터가 존재하면 진행 위치를 해당 챕터의 시작으로 이동합니다.
    /// </summary>
    public void ToPrevChapter()
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field)
            return;
        
        m_CurChapter = Mathf.Max(1, m_CurChapter - 1);
        RestartChapter();
    }

    /// <summary>
    /// 다음 챕터로 진행 위치를 이동하고 스테이지 상태를 초기화합니다.
    /// </summary>
    public void ToNextChapter()
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field)
            return;

        if (m_CurChapter >= GlobalConstants.FinalFieldChapter) return;

        m_CurChapter++;
        RestartChapter();
    }
    
    /// <summary>
    /// 필드 드롭 테이블에서 장비 보상을 추첨하여 챕터 정산 목록에 추가합니다.
    /// </summary>
    public void FieldItemDrop()
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field)
            return;
        if (IsItemDrop())
        {
            // 장비아이템은 EQ_로 시작, 재료아이템은 MAT_로 시작
            int chapter = CurChapter;

            if (chapter < 1)
            {
                PDebug.LogError($"유효하지 않은 챕터입니다: {chapter}");
                return;
            }

            const int chaptersPerDropTable = 3;

            // 1~3 → 1, 4~6 → 4, 7~9 → 7
            int dropTableChapter = ((chapter - 1) / chaptersPerDropTable) * chaptersPerDropTable + 1;

            if (!GlobalConstants.FieldItemDropTable.TryGetValue(dropTableChapter, out List<string> droppableItem)
                || droppableItem == null
                || droppableItem.Count == 0)
            {
                PDebug.LogError(
                    $"{chapter}챕터의 드롭테이블이 없습니다. 조회 키: {dropTableChapter}");
                return;
            }

            int _ranNum = UnityEngine.Random.Range(0, droppableItem.Count);
            string _itemId = droppableItem[_ranNum];

            // 장비아이템일땐 장비 아이템 관련 메서드로 연결
            if (_itemId.StartsWith("EQ_"))
            {
                if (!MasterDataMgr.Inst.TryGetEquipment(_itemId, out EquipmentData equipData)
                    || equipData == null)
                {
                    PDebug.LogError($"드롭 장비 데이터를 찾을 수 없습니다. ItemId: {_itemId}");
                    return;
                }
                
                if (GlobalValue.TryAddEquipItem(equipData, out ItemData item))
                {
                    TextPoolMgr.Inst.ShowSystemMessage($"<color=#{GlobalConstants.GetRarityHexColor(equipData.Rarity)}>{equipData.DisplayName}</color> 획득");
                    if (KeepRewardHistory)
                    {
                        m_ChapterDroppedEquipsList.Add(equipData);
                        // 최초 정산까지만 표시용 복사본을 유지한다.
                        Run.EarnedItems.Add(JsonUtility.FromJson<ItemData>(JsonUtility.ToJson(item)));
                    }
                    OnItemDropped?.Invoke();
                }
                    
                else
                    PDebug.LogError("아이템 지급 실패!");
            }

            // 재료아이템일땐 재료아이템 관련 메서드로 연결
            else if (_itemId.StartsWith("MAT_"))
            {
                // To-do 재료 아이템 추가 시 구현예정
                // MasterDataMgr.Inst.TryGetMaterial(_itemId, out MaterialData matData);
                // GlobalValue.TryAddMatItem(matData);
                // TextPoolMgr.Inst.ShowSystemMessage($"{matData.DisplayName} 획득");
            }

        }

        else
            return;
    }
    
    /// <summary>
    /// 현재 챕터에서 정산할 필드 골드 보상에 지정 수량을 누적합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    public void AddFieldGold(ulong amount)
    {
        if (BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field)
            return;

        if (amount > ulong.MaxValue - GlobalValue.UserGold ||
            amount > ulong.MaxValue - ChapterEarnedGold)
        {
            PDebug.LogError("필드 골드 지급 실패: 저장 가능한 범위를 초과했습니다.");
            return;
        }

        if (KeepRewardHistory) Run.EarnedGold += amount;
        GlobalValue.AddUserGold(amount);
    }

    /// <summary>최초 최종 정산을 확인한 뒤 표시용 이력을 해제한다.</summary>
    public void CompleteFinalSettlement()
    {
        if (!IsFinalStage || !IsChapterCleared || IsFinalSettlementComplete) return;
        var progress = GlobalValue.CurStageData;
        // 기존 버전에서 이미 Cleared로 저장된 3-10도 새 기록 필드와 호환한다.
        if (progress.ClearedChapter < m_CurChapter ||
            progress.ClearedChapter == m_CurChapter && progress.ClearedStage < m_CurStage)
        {
            progress.ClearedChapter = m_CurChapter;
            progress.ClearedStage = m_CurStage;
        }
        progress.SettledChapter = m_CurChapter;
        progress.SettledStage = m_CurStage;
        Run.EarnedItems.Clear();
        Run.EarnedGold = 0;
        m_ChapterDroppedEquipsList.Clear();
        PlayerDataMgr.RequestImmediateSave();
    }

    /// <summary>파티 복구 후 호출한다. 최초 정산 전 이력은 유지하고 지급 기록은 새 도전으로 교체한다.</summary>
    public void RestartFinalBoss()
    {
        if (!IsFinalStage || BattleFlowMgr.CurrentMode != BattleFlowMgr.BattleMode.Field) return;
        var previous = Run;
        var next = new ChapterRunSaveData { AttemptId = Guid.NewGuid().ToString("N"),
            Chapter = m_CurChapter, CheckpointStage = m_CurStage };
        if (KeepRewardHistory)
        {
            next.EarnedItems = previous.EarnedItems;
            next.EarnedGold = previous.EarnedGold;
        }
        GlobalValue.CurStageData.Run = next;
        m_ChapterDroppedEquipsList.Clear();
        var party = FindFirstObjectByType<PartyMgr>();
        if (party != null) CaptureEntryHp(party.PartyCharacters);
        // 일반 자동저장에 모은다. 보스 반복마다 명시적 서버 저장을 발생시키지 않는다.
    }

    private bool IsItemDrop()
    {
        float _ranFloat = UnityEngine.Random.Range(0f, 1f);
        return _ranFloat < GlobalConstants.ItemDropRate;
    }
}
