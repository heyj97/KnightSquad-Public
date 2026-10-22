using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 파티 편성, 성장 강화 및 파티 전투력 갱신 흐름을 관리합니다.
/// </summary>
public class PartyMgr : G_Singleton<PartyMgr>
{
    private const int MaxPartyCount = 4;

    private readonly PartyStat m_PartyStat = new();
    /// <summary>
    /// 파티 전체에 적용되는 성장 보정값을 가져옵니다.
    /// </summary>
    public PartyStat Stat => m_PartyStat;

    /// <summary>
    /// 현재 편성된 모든 파티원의 고정 전투력 합계를 가져옵니다.
    /// </summary>
    public long PartyCombatPower { get; private set; }
    /// <summary>
    /// 파티 고정 전투력 합계가 실제로 변경되었을 때 새 값을 전달합니다.
    /// </summary>
    public event System.Action<long> OnPartyCombatPowerChanged;

    private readonly List<CharacterBase> m_PartyCharacters = new();
    public IReadOnlyList<CharacterBase> PartyCharacters => m_PartyCharacters;
    private bool m_IsPartyInitialized;
    private bool m_IsApplyingComposition;
    private bool m_IsUpgradingPartyStat;
    private bool m_IsApplyingEquipment;

    public bool TryGetPartyCharacter(string charId, out CharacterBase character)
    {
        character = m_PartyCharacters.Find(value => value != null && value.IsInitialized &&
            value.UserCharData != null && value.UserCharData.CharId == charId);
        return character != null;
    }

    internal bool CanApplyEquipment => m_IsPartyInitialized &&
        !m_IsApplyingComposition && !m_IsUpgradingPartyStat && !m_IsApplyingEquipment;

    internal void BeginEquipmentBatch() => m_IsApplyingEquipment = true;

    internal void EndEquipmentBatch()
    {
        m_IsApplyingEquipment = false;
        RecalculateCombatPower();
    }
    /// <summary>
    /// 파티원 생성과 성장 능력치 적용이 완료되었는지 나타냅니다.
    /// </summary>
    public bool IsPartyInitialized => m_IsPartyInitialized;

    /// <summary>
    /// 파티 성장 강화에서 올릴 능력치 종류를 나타냅니다.
    /// </summary>
    public enum PartyUpgradeType
    {
        /// <summary>
        /// 파티원의 최대 체력 성장 레벨을 강화합니다.
        /// </summary>
        MaxHp,
        /// <summary>
        /// 파티원의 공격력 성장 레벨을 강화합니다.
        /// </summary>
        Attack,
        /// <summary>
        /// 파티원의 공격속도 성장 레벨을 강화합니다.
        /// </summary>
        AttackSpeed,
        /// <summary>
        /// 파티원의 방어력 성장 레벨을 강화합니다.
        /// </summary>
        Defense,
        /// <summary>
        /// 파티원의 치명타 확률 성장 레벨을 강화합니다.
        /// </summary>
        CriticalChance,
        /// <summary>
        /// 파티원의 치명타 피해 성장 레벨을 강화합니다.
        /// </summary>
        CriticalDamage
    }
    
    
    [Header("Party Spawn point")]
    [FormerlySerializedAs("SpawnpointsRoot")]
    [SerializeField] private Transform m_SpawnpointsRoot;
    
    private void OnEnable()
    {
        GlobalValue.OnPartyGrowthChanged += RefreshPartyGrowth;
        GlobalValue.OnCharacterDataChanged += RefreshCharacterLevels;
        // 비활성 중 바뀐 성장값도 적용. 전투력 구독은 이 동기화 이후 연결한다.
        if (m_IsPartyInitialized)
        {
            RefreshCharacterLevels();
            RefreshPartyGrowth();
        }
        m_PartyStat.OnPartyStatsChanged += RecalculateCombatPower;
        foreach (CharacterBase character in m_PartyCharacters)
            if (character != null)
                character.OnCharEquipmentChanged += RecalculateCombatPower;
        RecalculateCombatPower();
    }

    private void OnDisable()
    {
        GlobalValue.OnPartyGrowthChanged -= RefreshPartyGrowth;
        GlobalValue.OnCharacterDataChanged -= RefreshCharacterLevels;
        m_PartyStat.OnPartyStatsChanged -= RecalculateCombatPower;
        foreach (CharacterBase character in m_PartyCharacters)
            if (character != null)
                character.OnCharEquipmentChanged -= RecalculateCombatPower;
    }

    /// <summary>GlobalValue 성장 변경 이벤트로 호출한다. 초기 생성은 Start에서 처리한다.</summary>
    public void RefreshPartyGrowth()
    {
        if (!m_IsPartyInitialized)
            return;
        TryInitializePartyStat();
    }

    /// <summary>
    /// 파티 강화 종류의 현재 레벨과 다음 강화 비용을 검증하여 조회합니다.
    /// </summary>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <param name="level">현재 강화 레벨입니다.</param>
    /// <param name="cost">다음 강화에 필요한 골드입니다.</param>
    /// <param name="message">실패 원인 또는 처리 결과를 설명하는 메시지입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryGetPartyUpgradeInfo(PartyUpgradeType type,
        out int level, out ulong cost, out string message)
    {
        level = 0;
        cost = 0;
        message = "";
        if (!isActiveAndEnabled || !m_IsPartyInitialized || string.IsNullOrEmpty(GlobalValue.Uid))
        {
            message = "파티가 아직 준비되지 않았습니다.";
            return false;
        }
        if (m_IsApplyingComposition)
        {
            message = "파티 편성 변경이 진행 중입니다.";
            return false;
        }
        switch (type)
        {
            case PartyUpgradeType.MaxHp: level = GlobalValue.PartyMaxHpLv; break;
            case PartyUpgradeType.Attack: level = GlobalValue.PartyAttackPowerLv; break;
            case PartyUpgradeType.AttackSpeed: level = GlobalValue.PartyAttackSpeedLv; break;
            case PartyUpgradeType.Defense: level = GlobalValue.PartyDefenseLv; break;
            case PartyUpgradeType.CriticalChance: level = GlobalValue.PartyCriticalChanceLv; break;
            case PartyUpgradeType.CriticalDamage: level = GlobalValue.PartyCriticalDamageMultiplierLv; break;
            default:
                message = "지원하지 않는 강화 종류입니다.";
                return false;
        }
        if (level >= GlobalConstants.MaxPartyGrowthLevel)
        {
            message = "이미 최대 레벨입니다.";
            return false;
        }
        if (level < 1 || level - 1 >= GlobalConstants.PartyGoldSpendTable.Count ||
            GlobalConstants.PartyGoldSpendTable[level - 1] < 0)
        {
            message = "강화 레벨 또는 비용 정보가 올바르지 않습니다.";
            return false;
        }
        cost = (ulong)GlobalConstants.PartyGoldSpendTable[level - 1];
        return true;
    }

    /// <summary>
    /// 확인 시점의 레벨을 재검증하고 골드를 차감하여 파티 능력치 강화를 적용합니다.
    /// </summary>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <param name="confirmedLevel">사용자가 확인 창에서 승인했을 당시의 강화 레벨입니다.</param>
    /// <param name="message">실패 원인 또는 처리 결과를 설명하는 메시지입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    /// <exception cref="System.InvalidOperationException">현재 상태에서 작업을 완료할 수 없는 경우 발생합니다.</exception>
    public bool TryUpgradePartyStat(PartyUpgradeType type, int confirmedLevel, out string message)
    {
        if (PlayerDataMgr.IsProgressBlocked)
        {
            message = "온라인 저장 문제를 해결한 뒤 다시 시도해주세요.";
            return false;
        }
        if (m_IsUpgradingPartyStat || m_IsApplyingEquipment)
        {
            message = "파티 강화가 진행 중입니다.";
            return false;
        }
        m_IsUpgradingPartyStat = true;
        try
        {
            if (!TryGetPartyUpgradeInfo(type, out int level, out ulong cost, out message))
                return false;
            if (level != confirmedLevel)
            {
                message = "강화 레벨이 변경되었습니다. 다시 확인해주세요.";
                return false;
            }
            if (!GlobalValue.TrySpendUserGold(cost))
            {
                message = "소지골드가 부족합니다!";
                return false;
            }
            string statName;
            switch (type)
            {
                case PartyUpgradeType.MaxHp:
                    statName = "최대체력"; GlobalValue.SetPartyHpLv(level + 1); break;
                case PartyUpgradeType.Attack:
                    statName = "공격력"; GlobalValue.SetPartyAtkLv(level + 1); break;
                case PartyUpgradeType.AttackSpeed:
                    statName = "공격속도"; GlobalValue.SetPartyAtkSpdLv(level + 1); break;
                case PartyUpgradeType.Defense:
                    statName = "방어력"; GlobalValue.SetPartyDefLv(level + 1); break;
                case PartyUpgradeType.CriticalChance:
                    statName = "치명타 확률"; GlobalValue.SetPartyCritChanceLv(level + 1); break;
                case PartyUpgradeType.CriticalDamage:
                    statName = "치명타 데미지"; GlobalValue.SetPartyCritDmgLv(level + 1); break;
                default: throw new System.InvalidOperationException("검증되지 않은 강화 종류입니다.");
            }
            // 성장 setter의 이벤트가 능력치와 전투력을 갱신한다.
            message = $"파티 {statName}을 강화했습니다.\n\n레벨: {level} -> {level + 1}";
            PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
            PlayerDataMgr.RequestImmediateSave();
            return true;
        }
        finally
        {
            m_IsUpgradingPartyStat = false;
        }
    }

    /// <summary>편집 시작값과 현재값을 재검증하고 성장/골드를 한 번에 확정합니다.</summary>
    public bool TryApplyPartyUpgradePlan(int[] original, int[] target, out string message)
    {
        message = "";
        if (PlayerDataMgr.IsProgressBlocked || !isActiveAndEnabled || !m_IsPartyInitialized ||
            string.IsNullOrEmpty(GlobalValue.Uid) || m_IsUpgradingPartyStat || m_IsApplyingComposition || m_IsApplyingEquipment)
        { message = "파티가 준비되지 않았거나 다른 변경을 처리 중입니다."; return false; }
        int[] current = GetPartyGrowthLevels();
        if (original == null || original.Length != 6)
        { message = "강화 편집 데이터가 유효하지 않습니다."; return false; }
        for (int i = 0; i < 6; i++)
            if (original[i] != current[i])
            { message = "강화 레벨이 변경되었습니다. 창을 다시 열어주세요."; return false; }
        if (!PartyUpgradePlan.TryGetCost(original, target, out ulong cost))
        { message = "강화 레벨 또는 비용 정보가 올바르지 않습니다."; return false; }
        if (GlobalValue.UserGold < cost)
        { message = "골드가 부족합니다"; return false; }
        m_IsUpgradingPartyStat = true;
        try
        {
            GlobalValue.ApplyPartyUpgradeState((int[])target.Clone(), cost);
            PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
            PlayerDataMgr.RequestImmediateSave();
            return true;
        }
        finally { m_IsUpgradingPartyStat = false; }
    }

    public static int[] GetPartyGrowthLevels() => new[] { GlobalValue.PartyMaxHpLv,
        GlobalValue.PartyAttackPowerLv, GlobalValue.PartyAttackSpeedLv, GlobalValue.PartyDefenseLv,
        GlobalValue.PartyCriticalChanceLv, GlobalValue.PartyCriticalDamageMultiplierLv };

    /// <summary>전체 슬롯을 검증한 뒤 런타임 편성과 확정 목록을 갱신한다.</summary>
    /// <param name="characterIds">고정 슬롯 순서로 구성된 캐릭터 ID 목록입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryApplyPartyComposition(List<string> characterIds)
    {
        if (PlayerDataMgr.IsProgressBlocked) return false;
        if (m_IsApplyingComposition || m_IsUpgradingPartyStat || m_IsApplyingEquipment || !isActiveAndEnabled || !m_IsPartyInitialized || characterIds == null || characterIds.Count != MaxPartyCount ||
            !TryGetCharacterRootPrefabs(out CharacterRootMgr[] roots))
        {
            PDebug.LogError("실행 중인 파티가 준비되지 않았습니다.");
            return false;
        }

        m_IsApplyingComposition = true;
        try
        {
            // 호출자의 목록이 적용 도중 바뀌지 않도록 복사
            characterIds = new List<string>(characterIds);
            // TrySpawnCharacterModel 내부에서 전체 슬롯을 먼저 검증한다.
            if (!TrySpawnCharacterModel(roots, characterIds))
                return false;

            foreach (CharacterBase character in m_PartyCharacters)
                if (character != null)
                    character.OnCharEquipmentChanged -= RecalculateCombatPower;
            m_PartyCharacters.Clear();
            foreach (CharacterRootMgr root in roots)
            {
                CharacterBase character = root.GetComponent<CharacterBase>();
                if (!root.gameObject.activeSelf || character == null || !character.IsInitialized)
                    continue;
                m_PartyCharacters.Add(character);
                if (isActiveAndEnabled)
                    character.OnCharEquipmentChanged += RecalculateCombatPower;
            }

            FindFirstObjectByType<BattleFlowMgr>()?.RefreshPartyRegistration();
            bool changed = GlobalValue.CommitPartyComposition(characterIds, false);
            RecalculateCombatPower();
            if (changed)
                GlobalValue.NotifyPartyCompositionApplied();
            FindFirstObjectByType<StageMgr>()?.SyncCheckpointParty(m_PartyCharacters);
            PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
            PlayerDataMgr.RequestImmediateSave();
            return true;
        }
        finally
        {
            m_IsApplyingComposition = false;
        }
    }

    /// <summary>
    /// 현재 파티원에게 최신 성장 보정값을 적용하고 전투력을 다시 계산합니다.
    /// </summary>
    public void RefreshPartyCharacters()
    {
        if (!m_IsPartyInitialized)
            return;

        foreach (CharacterBase character in m_PartyCharacters)
        {
            if (character == null)
                continue;
            
            character.RefreshCharacter();
        }
    }

    private void RefreshCharacterLevels()
    {
        if (!m_IsPartyInitialized) return;
        foreach (var character in m_PartyCharacters)
            if (character != null) character.RefreshLevelGrowth();
        RecalculateCombatPower();
    }
    

    /// <summary>등록된 파티원의 고정 전투력을 합산한다. 사망자도 포함한다.</summary>
    public void RecalculateCombatPower()
    {
        if (!m_IsPartyInitialized || m_IsApplyingEquipment)
            return;

        long total = 0;
        foreach (CharacterBase character in m_PartyCharacters)
        {
            if (character == null || !character.IsInitialized)
                continue;
            long power = PartyAtkCalc.Calculate(character.CharData, character.CharEquipments, m_PartyStat,
                character.UserCharData?.CharLv ?? 1);
            total = total > long.MaxValue - power ? long.MaxValue : total + power;
        }

        if (PartyCombatPower == total)
            return;
        PartyCombatPower = total;
        GlobalValue.SetAtkPowerRankScore((int)PartyCombatPower);
        OnPartyCombatPowerChanged?.Invoke(PartyCombatPower);
    }

    private void Start()
    {
        // 파티스탯 초기화
        if (!TryInitializePartyStat())
        {
            PDebug.LogError($"TryInitializePartyStat Failed!");
            return;
        }

        // CharacterRootMgr 배열로 불러오기
        if (!TryGetCharacterRootPrefabs(out CharacterRootMgr[] characterRoots))
        {
            PDebug.LogError("TryGetCharacterRootPrefabs Failed!");
            return;
        }

        // 배열의 캐릭터들 모델을 스폰
        if (!TrySpawnCharacterModel(characterRoots))
        {
            PDebug.LogError("TryGetCharacterRootPrefabs Failed!");
            return;
        }

        foreach (CharacterRootMgr root in characterRoots)
        {
            CharacterBase character = root.GetComponent<CharacterBase>();
            if (character == null || !character.IsInitialized)
                continue;
            m_PartyCharacters.Add(character);
            if (isActiveAndEnabled)
                character.OnCharEquipmentChanged += RecalculateCombatPower;
        }
        m_IsPartyInitialized = true;
        RecalculateCombatPower();
    }

    private bool TryInitializePartyStat()
    {
        if (string.IsNullOrEmpty(GlobalValue.Uid))
        {
            PDebug.LogError($"GlobalValue didn't initiaialized!");
            return false;
        }
        
        m_PartyStat.SetAll(
            GlobalValue.PartyMaxHpLv * GlobalConstants.HpMultiplierPerLv,
            GlobalValue.PartyAttackPowerLv * GlobalConstants.AttackMultiplierPerLv,
            GlobalValue.PartyAttackSpeedLv * GlobalConstants.AttackSpeedMultiplierPerLv,
            GlobalValue.PartyDefenseLv * GlobalConstants.DefenseMultiplierPerLv,
            GlobalValue.PartyCriticalChanceLv * GlobalConstants.CriticalChancePerLv,
            GlobalValue.PartyCriticalDamageMultiplierLv * GlobalConstants.CriticalDamageMultiplierPerLv);
        
        return true;
    }
    
    // 비활성 루트도 포함하며, Hierarchy 순서를 고정 파티 슬롯 순서로 사용한다.
    private bool TryGetCharacterRootPrefabs(out CharacterRootMgr[] characterRoots)
    {
        characterRoots = null;
        if (m_SpawnpointsRoot == null)
            return false;

        characterRoots = m_SpawnpointsRoot.GetComponentsInChildren<CharacterRootMgr>(true);
        return characterRoots.Length == MaxPartyCount;
    }

    private bool TrySpawnCharacterModel(CharacterRootMgr[] characterRoots, List<string> characterIds = null)
    {
        characterIds = characterIds ?? new List<string>(GlobalValue.PartyCharIdList);
        if (characterRoots == null || characterRoots.Length != MaxPartyCount ||
            characterIds == null || characterIds.Count != MaxPartyCount ||
            GlobalValue.UserCharDataList == null || MasterDataMgr.Inst == null)
        {
            PDebug.LogError("파티 슬롯 또는 캐릭터 데이터가 준비되지 않았습니다.");
            return false;
        }

        // 스폰 전에 모든 슬롯을 검증하여 데이터 누락 시 일부만 생성되는 것을 막는다.
        CharacterData[] masterDataBySlot = new CharacterData[MaxPartyCount];
        CharData[] userDataBySlot = new CharData[MaxPartyCount];
        int memberCount = 0;
        HashSet<string> uniqueIds = new();
        for (int i = 0; i < MaxPartyCount; i++)
        {
            if (characterRoots[i] == null || !characterRoots[i].CanChangeParty)
            {
                PDebug.LogError($"파티 {i + 1}번 슬롯의 CharacterRoot 참조가 준비되지 않았습니다.");
                return false;
            }

            string charId = characterIds[i];
            if (string.IsNullOrEmpty(charId))
                continue;

            if (!uniqueIds.Add(charId) || !MasterDataMgr.Inst.TryGetCharacter(charId, out masterDataBySlot[i]) ||
                !characterRoots[i].CanInitializeForParty(masterDataBySlot[i]))
            {
                PDebug.LogError($"파티 {i + 1}번 슬롯의 마스터 데이터를 찾지 못했습니다: {charId}");
                return false;
            }

            userDataBySlot[i] = GlobalValue.UserCharDataList.Find(data => data != null && data.CharId == charId);
            if (userDataBySlot[i] == null)
            {
                PDebug.LogError($"파티 {i + 1}번 슬롯의 소유 캐릭터 데이터를 찾지 못했습니다: {charId}");
                return false;
            }
            memberCount++;
        }

        if (memberCount == 0)
        {
            PDebug.LogError("배치된 캐릭터가 없습니다.");
            return false;
        }

        if (GameObject.Find("MonsterSpawnRoot") == null || GameObject.Find("SpawnpointsRoot") == null)
        {
            PDebug.LogError("전투 SpawnRoot가 준비되지 않았습니다.");
            return false;
        }

        bool compositionChanged = false;
        for (int i = 0; i < MaxPartyCount; i++)
        {
            CharacterBase character = characterRoots[i].GetComponent<CharacterBase>();
            if (character != null && character.IsInitialized &&
                (masterDataBySlot[i] == null || character.CharData != masterDataBySlot[i]))
                compositionChanged = true;
            if (masterDataBySlot[i] != null && (character == null || !character.IsInitialized))
                compositionChanged = true;
        }
        if (m_IsPartyInitialized && compositionChanged)
        {
            FindFirstObjectByType<TargetSelMgr>()?.ClearTargets();
            foreach (CharacterBase character in m_PartyCharacters)
                if (character != null)
                    character.CancelSkillSelectionForPartyChange();
        }

        CharacterRootMgr.PartySnapshot[] snapshots = new CharacterRootMgr.PartySnapshot[MaxPartyCount];
        bool completed = false;
        try
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                CharacterBase existing = characterRoots[i].GetComponent<CharacterBase>();
                bool unchanged = masterDataBySlot[i] != null && existing != null && existing.IsInitialized &&
                    existing.CharData == masterDataBySlot[i] && characterRoots[i].gameObject.activeSelf;
                if (unchanged)
                    continue;
                snapshots[i] = characterRoots[i].CapturePartySnapshot();
                characterRoots[i].BeginPartyChange(snapshots[i]);
                if (masterDataBySlot[i] == null)
                {
                    characterRoots[i].gameObject.SetActive(false);
                    continue;
                }

                characterRoots[i].gameObject.SetActive(true);
                if (!characterRoots[i].TryInitialize(masterDataBySlot[i], userDataBySlot[i], m_PartyStat))
                {
                    PDebug.LogError($"파티 {i + 1}번 슬롯 초기화에 실패했습니다: {characterIds[i]}");
                    return false;
                }
                if (m_IsPartyInitialized)
                    characterRoots[i].GetComponent<CharacterBase>().StartPartyChangeCooldowns();
            }
            completed = true;
            return true;
        }
        catch (System.Exception exception)
        {
            PDebug.LogError($"파티 적용 중 오류가 발생했습니다. 기존 편성을 복원합니다: {exception.Message}");
            return false;
        }
        finally
        {
            for (int i = 0; i < MaxPartyCount; i++)
            {
                if (snapshots[i] == null)
                    continue;
                if (completed)
                    characterRoots[i].CommitPartyChange(snapshots[i]);
                else
                    characterRoots[i].RollbackPartyChange(snapshots[i]);
            }
        }
    }
}
