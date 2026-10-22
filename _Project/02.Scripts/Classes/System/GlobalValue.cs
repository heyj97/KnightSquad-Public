using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 로그인 이후 사용하는 플레이어 진행 데이터를 메모리에 보관하고 변경 이벤트를 제공합니다.
/// </summary>
public static class GlobalValue
{
    // =========================
    // User Info
    // =========================

    /// <summary>유저의 서버 ID</summary>
    public static string PlayFabId { get; private set; } = "";

    /// <summary>유저의 고유 ID</summary>
    public static string Uid { get; private set; } = "";

    /// <summary>유저의 닉네임</summary>
    public static string NickName { get; private set; } = "";

    /// <summary>유저가 보유 중인 게임 머니</summary>
    public static ulong UserGold { get; private set; } = 0;

    /// <summary>유저가 보유 중인 유료 재화</summary>
    public static ulong RealMoney { get; private set; } = 0;
    /// <summary>유저가 보유한 별의 조각. 아이템 인벤토리와 별도로 관리한다.</summary>
    public static ulong UserStarFragments { get; private set; } = 0;
    public static event Action<ulong> OnUserStarFragmentsChanged;
    public static CharacterGachaSaveData GachaData { get; private set; } = new();
    public static event Action OnCharacterDataChanged;
    public static event Action OnGachaStateChanged;

    public static void SetGachaData(CharacterGachaSaveData data)
        => GachaData = data?.Copy() ?? new CharacterGachaSaveData();

    // 추첨 계획 검증 후 한 번만 호출한다. 기존 CharData 참조를 유지해 런타임 장비 연결을 보존한다.
    internal static void ApplyGachaState(CharacterGachaPlan plan)
    {
        var originals = new Dictionary<string, CharData>();
        foreach (var user in UserCharDataList) originals.Add(user.CharId, user);
        foreach (var updated in plan.Characters)
        {
            if (originals.TryGetValue(updated.CharId, out var user))
            { user.CharLv = updated.CharLv; user.CharDuplicateCount = updated.CharDuplicateCount; }
            else UserCharDataList.Add(updated);
        }
        UserGold = plan.Gold;
        RealMoney = plan.RM;
        bool fragmentsChanged = UserStarFragments != plan.StarFragments;
        UserStarFragments = plan.StarFragments;
        SetGachaData(plan.State);
        NotifySaleHandlers(OnCharacterDataChanged);
        NotifySaleHandlers(OnGachaStateChanged);
        NotifyGachaCurrency(OnUserGoldChanged, UserGold);
        NotifyGachaCurrency(OnUserRealMoneyChanged, RealMoney);
        if (fragmentsChanged) NotifyGachaCurrency(OnUserStarFragmentsChanged, UserStarFragments);
    }

    internal static void ApplyGachaTarget(CharacterGachaSaveData state)
    {
        SetGachaData(state);
        NotifySaleHandlers(OnGachaStateChanged);
    }

    private static void NotifyGachaCurrency(Action<ulong> handlers, ulong value)
    {
        if (handlers == null) return;
        foreach (Action<ulong> handler in handlers.GetInvocationList())
            try { handler(value); } catch (Exception exception) { PDebug.LogError(exception); }
    }


    // =========================
    // Ranking
    // =========================

    /// <summary>유저의 전투력 랭킹 점수</summary>
    public static int AttackPowerRankScore { get; private set; } = 0;

    /// <summary>유저의 허수아비 데미지 랭킹 점수</summary>
    public static int DummyDamageRankScore { get; private set; } = 0;


    // =========================
    // Party Upgrade
    // =========================

    /// <summary>
    /// 현재 파티 최대 체력 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyMaxHpLv { get; private set; } = 0;
    /// <summary>
    /// 현재 파티 공격력 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyAttackPowerLv { get; private set; } = 0;
    /// <summary>
    /// 현재 파티 공격속도 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyAttackSpeedLv { get; private set; } = 0;
    /// <summary>
    /// 현재 파티 방어력 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyDefenseLv { get; private set; } = 0;
    /// <summary>
    /// 현재 파티 치명타 확률 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyCriticalChanceLv { get; private set; } = 0;
    /// <summary>
    /// 현재 파티 치명타 피해 성장 레벨을 가져옵니다.
    /// </summary>
    public static int PartyCriticalDamageMultiplierLv { get; private set; } = 0;


    // =========================
    // Game Data
    // =========================

    /// <summary>
    /// 현재 챕터와 스테이지 진행 상태를 가져옵니다.
    /// </summary>
    public static StageSaveData CurStageData { get; private set; } = new();

    /// <summary>
    /// 사용자가 보유한 캐릭터의 저장 데이터를 가져옵니다.
    /// </summary>
    public static List<CharData> UserCharDataList { get; private set; } = new();

    private const int MaxPartyCount = 4;

    /// <summary>
    /// 네 개의 고정 파티 슬롯에 배치된 캐릭터 ID를 읽기 전용으로 제공합니다.
    /// </summary>
    public static IReadOnlyList<string> PartyCharIdList { get; private set; } = CreateEmptyPartyList().AsReadOnly();

    private static List<string> CreateEmptyPartyList()
    {
        // 인덱스는 스폰위치를 의미하게 된다.
        return new List<string>
        {
            "",
            "",
            "",
            ""
        };
    }

    /// <summary>
    /// 사용자 인벤토리에 보관된 아이템 저장 데이터를 가져옵니다.
    /// </summary>
    public static List<ItemData> UserItemDataList { get; private set; } = new();

    // =========================
    // Events
    // =========================

    /// <summary>
    /// 골드 값이 변경되었을 때 호출
    /// 매개변수: 변경된 현재 골드
    /// </summary>
    public static event Action<ulong> OnUserGoldChanged;

    /// <summary>
    /// 유료 재화 값이 변경되었을 때 호출
    /// 매개변수: 변경된 현재 유료 재화
    /// </summary>
    public static event Action<ulong> OnUserRealMoneyChanged;
    
    
    //////////////////////////////////
    ////// 아이템관련 추가 제거 //////
    //////////////////////////////////
    /// <summary>
    /// 메모리의 사용자 인벤토리 구성이 변경되었을 때 발생합니다.
    /// </summary>
    public static event Action OnInventoryChanged;

    // 일괄 판매의 검증·복원은 호출자가 담당하고, 확정 후에만 이벤트를 통지한다.
    internal static void ApplySaleState(List<ItemData> items, ulong gold)
    {
        UserItemDataList.Clear();
        UserItemDataList.AddRange(items);
        UserGold = gold;
    }

    internal static void NotifySaleApplied()
    {
        NotifySaleHandlers(OnInventoryChanged);
        if (OnUserGoldChanged != null)
            foreach (Action<ulong> handler in OnUserGoldChanged.GetInvocationList())
                try { handler(UserGold); } catch (Exception exception) { PDebug.LogError(exception); }
    }

    // PartyMgr의 비용/레벨 검증 후 호출한다. 이벤트 전에 전체 상태를 확정한다.
    internal static void ApplyPartyUpgradeState(int[] levels, ulong cost)
    {
        UserGold -= cost;
        PartyMaxHpLv = levels[0];
        PartyAttackPowerLv = levels[1];
        PartyAttackSpeedLv = levels[2];
        PartyDefenseLv = levels[3];
        PartyCriticalChanceLv = levels[4];
        PartyCriticalDamageMultiplierLv = levels[5];
        NotifySaleHandlers(OnPartyGrowthChanged);
        if (OnUserGoldChanged != null)
            foreach (Action<ulong> handler in OnUserGoldChanged.GetInvocationList())
                try { handler(UserGold); } catch (Exception exception) { PDebug.LogError(exception); }
    }

    private static void NotifySaleHandlers(Action handlers)
    {
        if (handlers == null) return;
        foreach (Action handler in handlers.GetInvocationList())
            try { handler(); } catch (Exception exception) { PDebug.LogError(exception); }
    }
    
    /// <summary>
    /// 런타임 파티 편성이 성공적으로 확정되었을 때 발생합니다.
    /// </summary>
    public static event Action OnPartyCompositionChanged;
    /// <summary>
    /// 파티 성장 레벨 중 하나 이상이 변경되었을 때 발생합니다.
    /// </summary>
    public static event Action OnPartyGrowthChanged;

#region Setter
    // =========================
    // Setter
    // =========================

    /// <summary>
    /// 로그인한 PlayFab 계정 ID를 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPlayFabId(string value)
    {
        PlayFabId = value;
    }

    /// <summary>
    /// 현재 사용자 데이터의 공통 소유자 UID를 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetUid(string value)
    {
        Uid = value;
    }

    /// <summary>
    /// 게임 UI에 표시할 사용자 닉네임을 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetNickName(string value)
    {
        NickName = value;
    }

    /// <summary>
    /// 보유 골드를 교체하고 값이 달라진 경우 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetUserGold(ulong value)
    {
        // 값이 같으면 굳이 이벤트를 발생시키지 않음
        if (UserGold == value)
            return;

        UserGold = value;

        // 변경 사실을 구독자들에게 알림
        OnUserGoldChanged?.Invoke(UserGold);
    }

    /// <summary>
    /// 보유 유료 재화를 교체하고 값이 달라진 경우 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetRealMoney(ulong value)
    {
        if (RealMoney == value)
            return;

        RealMoney = value;

        OnUserRealMoneyChanged?.Invoke(RealMoney);
    }

    /// <summary>로그인·초기 데이터 복원용. 서버 저장을 요청하지 않는다.</summary>
    public static void SetUserStarFragments(ulong value)
    {
        if (UserStarFragments == value) return;
        UserStarFragments = value;
        NotifyGachaCurrency(OnUserStarFragmentsChanged, value);
    }

    /// <summary>보유 한도를 검사한 뒤 지급하고 PlayerInfo 일반 저장을 예약한다.</summary>
    public static bool TryAddUserStarFragments(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked || amount > ulong.MaxValue - UserStarFragments) return false;
        if (amount == 0) return true;
        SetUserStarFragments(UserStarFragments + amount);
        PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
        return true;
    }

    /// <summary>잔액을 검사한 뒤 차감하고 PlayerInfo 일반 저장을 예약한다.</summary>
    public static bool TrySpendUserStarFragments(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked || amount > UserStarFragments) return false;
        if (amount == 0) return true;
        SetUserStarFragments(UserStarFragments - amount);
        PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
        return true;
    }

    /// <summary>
    /// 파티 전투력 랭킹 점수를 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetAtkPowerRankScore(int value)
    {
        AttackPowerRankScore = value;
    }

    /// <summary>
    /// 허수아비 피해량 랭킹 점수를 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetDummyRankScore(int value)
    {
        DummyDamageRankScore = value;
    }

    /// <summary>
    /// 파티 공격력 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyAtkLv(int value)
    {
        if (PartyAttackPowerLv == value)
            return;
        PartyAttackPowerLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 파티 최대 체력 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyHpLv(int value)
    {
        if (PartyMaxHpLv == value)
            return;
        PartyMaxHpLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 파티 방어력 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyDefLv(int value)
    {
        if (PartyDefenseLv == value)
            return;
        PartyDefenseLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 파티 공격속도 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyAtkSpdLv(int value)
    {
        if (PartyAttackSpeedLv == value)
            return;
        PartyAttackSpeedLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 파티 치명타 확률 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyCritChanceLv(int value)
    {
        if (PartyCriticalChanceLv == value)
            return;
        PartyCriticalChanceLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 파티 치명타 피해 성장 레벨을 적용하고 변경 시 성장 알림을 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyCritDmgLv(int value)
    {
        if (PartyCriticalDamageMultiplierLv == value)
            return;
        PartyCriticalDamageMultiplierLv = value;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>초기 데이터와 저장 데이터의 여섯 성장값을 한 번에 적용한다.</summary>
    /// <param name="hp">적용할 최대 체력 성장 비율 또는 레벨입니다.</param>
    /// <param name="attack">적용할 공격력 성장 비율 또는 레벨입니다.</param>
    /// <param name="attackSpeed">적용할 공격속도 성장 비율 또는 레벨입니다.</param>
    /// <param name="defense">적용할 방어력 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalChance">적용할 치명타 확률 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalDamage">적용할 치명타 피해 배율 또는 성장 레벨입니다.</param>
    public static void SetPartyGrowthLevels(int hp, int attack, int attackSpeed,
        int defense, int criticalChance, int criticalDamage)
    {
        if (PartyMaxHpLv == hp && PartyAttackPowerLv == attack &&
            PartyAttackSpeedLv == attackSpeed && PartyDefenseLv == defense &&
            PartyCriticalChanceLv == criticalChance && PartyCriticalDamageMultiplierLv == criticalDamage)
            return;

        PartyMaxHpLv = hp;
        PartyAttackPowerLv = attack;
        PartyAttackSpeedLv = attackSpeed;
        PartyDefenseLv = defense;
        PartyCriticalChanceLv = criticalChance;
        PartyCriticalDamageMultiplierLv = criticalDamage;
        OnPartyGrowthChanged?.Invoke();
    }

    /// <summary>
    /// 현재 챕터와 스테이지 진행 데이터를 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetCurStageData(StageSaveData value)
    {
        CurStageData = value ?? new StageSaveData();
    }

    /// <summary>
    /// 사용자 보유 캐릭터 목록을 메모리에 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetUserCharDataList(List<CharData> value)
    {
        UserCharDataList = value != null
            ? new List<CharData>(value)
            : new List<CharData>();
    }

    /// <summary>
    /// 네 개의 고정 슬롯 편성을 검증하고 초기화된 파티가 있으면 런타임에도 적용합니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetPartyCharIdList(List<string> value)
    {
        PartyMgr party = UnityEngine.Object.FindFirstObjectByType<PartyMgr>(FindObjectsInactive.Include);
        if (party != null && party.IsPartyInitialized)
        {
            if (!party.TryApplyPartyComposition(value))
                PDebug.LogError("파티 변경에 실패했습니다. 기존 편성을 유지합니다.");
            return;
        }
        // 로그인/개발 진입 초기 데이터. 런타임 생성은 PartyMgr.Start에서 처리.
        CommitPartyComposition(value);
    }

    internal static bool CommitPartyComposition(List<string> value, bool notify = true)
    {
        List<string> a_normalizedParty = CreateEmptyPartyList();

        if (value != null)
        {
            int count = Mathf.Min(value.Count,MaxPartyCount);

            for (int i = 0; i < count; i++)
            {
                a_normalizedParty[i] = value[i] ?? "";
            }
        }

        bool a_changed = false;
        for (int i = 0; i < MaxPartyCount; i++)
            if (PartyCharIdList[i] != a_normalizedParty[i])
                a_changed = true;
        if (!a_changed)
            return false;

        PartyCharIdList = a_normalizedParty.AsReadOnly();
        if (notify)
            NotifyPartyCompositionApplied();
        return true;
    }

    internal static void NotifyPartyCompositionApplied()
    {
        OnPartyCompositionChanged?.Invoke();
    }
    
    // 특정 슬롯의 캐릭터를 변경
    /// <summary>
    /// 지정 파티 슬롯에 보유 캐릭터를 배치하고 초기화된 파티가 있으면 런타임 편성에도 적용합니다.
    /// </summary>
    /// <param name="slotIndex">고정 슬롯 또는 목록에서 사용할 인덱스입니다.</param>
    /// <param name="characterId">배치하거나 제거할 캐릭터의 마스터 ID입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TrySetPartyCharacter(int slotIndex, string characterId)
    {
        if (slotIndex < 0 || slotIndex >= MaxPartyCount)
        {
            PDebug.LogError($"잘못된 파티 슬롯입니다: {slotIndex}");
            return false;
        }

        string a_characterId = characterId ?? "";
        if (PartyCharIdList[slotIndex] == a_characterId)
            return true;
        List<string> a_party = new List<string>(PartyCharIdList);
        a_party[slotIndex] = a_characterId;
        PartyMgr party = UnityEngine.Object.FindFirstObjectByType<PartyMgr>(FindObjectsInactive.Include);
        if (party != null && party.IsPartyInitialized)
            return party.TryApplyPartyComposition(a_party);
        CommitPartyComposition(a_party);
        return true;
    }
    
    // 특정 슬롯의 캐릭터를 삭제
    /// <summary>
    /// 지정 파티 슬롯의 캐릭터를 제거하고 최소 편성 인원 규칙을 검증합니다.
    /// </summary>
    /// <param name="slotIndex">고정 슬롯 또는 목록에서 사용할 인덱스입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TryRemovePartyCharacter(int slotIndex)
    {
        return TrySetPartyCharacter(slotIndex, "");
    }

    /// <summary>
    /// 사용자 인벤토리 목록을 메모리에 적용하고 변경 이벤트를 발생시킵니다.
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public static void SetUserItemDataList(List<ItemData> value)
    {
        UserItemDataList = value != null
            ? new List<ItemData>(value)
            : new List<ItemData>();
    }
#endregion

#region UserCharDataList Add/Delete
    /// <summary>
    /// 보유 캐릭터를 추가한다.
    /// </summary>
    /// <param name="charData">셀에 표시할 사용자 캐릭터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TryAddUserCharData(CharData charData)
    {
        if (charData == null)
            return false;

        UserCharDataList.Add(charData);
        return true;
    }

    /// <summary>
    /// CharId가 일치하는 보유 캐릭터를 제거한다.
    /// </summary>
    /// <param name="charId">생성할 캐릭터를 식별하는 마스터 ID입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TryDelUserCharData(string charId)
    {
        CharData target = UserCharDataList.Find(
            charData => charData.CharId == charId
        );

        if (target == null)
            return false;

        UserCharDataList.Remove(target);

        return true;
    }
#endregion

    /// <summary>
    /// 메모리에 저장된 값을 기본값으로 초기화한다.
    /// 실제 JSON 파일은 삭제하지 않는다.
    /// </summary>
    public static void ClearVariable()
    {
        GachaData = new CharacterGachaSaveData();
        PlayFabId = "";
        Uid = "";
        NickName = "";

        UserGold = 0;
        RealMoney = 0;
        UserStarFragments = 0;

        AttackPowerRankScore = 0;
        DummyDamageRankScore = 0;

        PartyAttackPowerLv = 0;
        PartyMaxHpLv = 0;
        PartyDefenseLv = 0;
        PartyAttackSpeedLv = 0;
        PartyCriticalChanceLv = 0;
        PartyCriticalDamageMultiplierLv = 0;

        CurStageData = new StageSaveData();

        UserCharDataList.Clear();
        PartyCharIdList = CreateEmptyPartyList().AsReadOnly();
        UserItemDataList.Clear();
    }

    /////////////////////////////////
    ////// 재화관련 추가 제거 ///////
    /////////////////////////////////

    // 골드를 추가
    /// <summary>
    /// 사용자 골드 값을 현재 상태에 누적하고 변경 알림을 반영합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    public static void AddUserGold(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked) return;
        SetUserGold(UserGold + amount);
    }

    // 골드를 소모
    /// <summary>
    /// 보유 골드가 충분한지 확인한 뒤 요청 금액을 한 번 차감합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TrySpendUserGold(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked) return false;
        if (UserGold < amount)
            return false;

        SetUserGold(UserGold - amount);

        return true;
    }

    // 캐쉬를 추가
    /// <summary>
    /// 사용자 유료 재화 값을 현재 상태에 누적하고 변경 알림을 반영합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    public static void AddUserCash(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked) return;
        SetRealMoney(RealMoney + amount);
    }

    // 캐쉬를 소모
    /// <summary>
    /// 보유 유료 재화가 충분한지 확인한 뒤 요청 금액을 한 번 차감합니다.
    /// </summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TrySpendUserCash(ulong amount)
    {
        if (PlayerDataMgr.IsProgressBlocked) return false;
        if (RealMoney < amount)
            return false;

        SetRealMoney(RealMoney - amount);
        return true;
    }

    /// <summary>
    /// 장비 마스터 ID로 고유 UID를 가진 사용자 장비 데이터를 생성하여 인벤토리에 추가합니다.
    /// </summary>
    /// <param name="itemSO">인벤토리에 추가할 아이템 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TryAddEquipItem(ItemParentSO itemSO)
        => TryAddEquipItem(itemSO, out _);

    /// <summary>지급한 인스턴스를 돌려줘 획득 기록이 다른 인벤토리 변경에 영향받지 않게 한다.</summary>
    public static bool TryAddEquipItem(ItemParentSO itemSO, out ItemData addedItem)
    {
        addedItem = null;
        if (PlayerDataMgr.IsProgressBlocked) return false;
        if (itemSO == null)
        {
            PDebug.LogError("지급할 장비 데이터가 null입니다.");
            return false;
        }
        
        if (UserItemDataList.Count is GlobalConstants.InventoryMaxSlotCount or > GlobalConstants.InventoryMaxSlotCount)
        {
            TextPoolMgr.Inst.ShowSystemMessage("<color=#ff0000>인벤토리 슬롯이 꽉 찼습니다. 아이템을 획득할 수 없습니다.</color>");
            return false;
        }

        // 10칸 남으면 알림
        if (GlobalConstants.InventoryMaxSlotCount - 10 < UserItemDataList.Count)
            TextPoolMgr.Inst.ShowSystemMessage($"<color=#ff0000>인벤토리 슬롯이 {GlobalConstants.InventoryMaxSlotCount - UserItemDataList.Count}칸 남았습니다. 이후 보상은 받을 수 없습니다.</color>");
        
        if (itemSO.Type == ItemType.Equipment)
        {
            addedItem = new ItemData(itemSO);
            UserItemDataList.Add(addedItem);
            OnInventoryChanged?.Invoke();
            
            if (GlobalConstants.InventoryMaxSlotCount - UserItemDataList.Count == 0)
                TextPoolMgr.Inst.ShowSystemMessage($"<color=#ff0000>인벤토리 슬롯이 가득 찼습니다. 이후 보상은 받을 수 없습니다.</color>");
            
            return true;
        }
        
        PDebug.LogError("TryAddEquipItem은 장비아이템(ItemType.Equipment)만 추가 가능합니다.");
        return false;
    }

    // public static bool TryAddMaterialItem(ItemParentSO itemSO)

    /// <summary>
    /// 고유 UID로 사용자 인벤토리의 아이템을 찾아 제거합니다.
    /// </summary>
    /// <param name="itemUid">사용자 인벤토리에서 항목을 식별하는 고유 UID입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public static bool TryRemoveItem(string itemUid)
    {
        if (PlayerDataMgr.IsProgressBlocked) return false;
        ItemData a_TargetItem = UserItemDataList.Find(uitem => uitem.ItemUid == itemUid);

        if (a_TargetItem != null)
        {
            UserItemDataList.Remove(a_TargetItem);
            OnInventoryChanged?.Invoke();
            return true;
        }
        
        PDebug.LogError($"Uid:{itemUid}를 찾지 못했습니다.");
        return false;
    }
}
