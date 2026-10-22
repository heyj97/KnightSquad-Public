using System.Collections.Generic;
using UnityEngine;

#region 데이터 포맷 클래스
/// <summary>
/// 유저가 보유한 단일 캐릭터의 저장 데이터
/// </summary>
/// <remarks>
///
/// <para><b>Identity</b></para>
/// <list type="bullet">
/// <item><description><c>string CharId</c> 캐릭터를 식별하기 위한 고유 ID</description></item>
/// </list>
///
/// <para><b>Progress</b></para>
/// <list type="bullet">
/// <item><description><c>int CharLv</c> 캐릭터의 강화 레벨</description></item>
/// <item><description><c>int CharDuplicateCount</c> 미사용 중복 캐릭터 수</description></item>
/// </list>
///
/// <para><b>Equipment</b></para>
/// <list type="bullet">
/// <item><description><c>string WeaponUid</c> 장착 중인 무기 아이템의 인스턴스 UID</description></item>
/// <item><description><c>string ArmorUid</c> 장착 중인 방어구 아이템의 인스턴스 UID</description></item>
/// <item><description><c>string AccessoryUid</c> 장착 중인 액세서리 아이템의 인스턴스 UID</description></item>
/// </list>
///
/// </remarks>
[System.Serializable]
public class CharData
{
    /// <summary>캐릭터를 식별하기 위한 고유 ID</summary>
    public string CharId;

    /// <summary>캐릭터의 강화 레벨</summary>
    public int CharLv;

    /// <summary>아직 레벨업에 소비하지 않은 동일 캐릭터 중복 수</summary>
    public int CharDuplicateCount;

    /// <summary>장착 중인 무기 아이템의 인스턴스 UID</summary>
    public string WeaponUid;

    /// <summary>장착 중인 방어구 아이템의 인스턴스 UID</summary>
    public string ArmorUid;

    /// <summary>장착 중인 액세서리 아이템의 인스턴스 UID</summary>
    public string AccessoryUid;

    /// <summary>지정 슬롯에 확정된 아이템 UID를 반환합니다.</summary>
    public string GetEquipmentUid(EquipType type)
    {
        switch (type)
        {
            case EquipType.Weapon: return WeaponUid ?? "";
            case EquipType.Armor: return ArmorUid ?? "";
            case EquipType.Accessory: return AccessoryUid ?? "";
            default: return "";
        }
    }

    /// <summary>검증 완료된 장비 적용 경로에서만 사용합니다.</summary>
    internal bool TrySetEquipmentUid(EquipType type, string itemUid)
    {
        switch (type)
        {
            case EquipType.Weapon: WeaponUid = itemUid ?? ""; return true;
            case EquipType.Armor: ArmorUid = itemUid ?? ""; return true;
            case EquipType.Accessory: AccessoryUid = itemUid ?? ""; return true;
            default: return false;
        }
    }

    /// <summary>
    /// JsonUtility 역직렬화 등을 위한 기본 생성자
    /// </summary>
    public CharData()
    {
        // Unity 직렬화 중에도 호출될 수 있으므로 씬 객체에 접근하지 않는다.
        CharId = "";
        CharLv = 1;
        CharDuplicateCount = 0;

        WeaponUid = "";
        ArmorUid = "";
        AccessoryUid = "";
    }

    /// <summary>
    /// CharacterData를 기반으로 신규 유저 캐릭터 데이터를 생성한다.
    /// </summary>
    /// <param name="characterData">사용자 보유 데이터의 기반이 되는 캐릭터 마스터 데이터입니다.</param>
    public CharData(CharacterData characterData)
    {
        CharId = characterData.Id;
        CharLv = 1;
        CharDuplicateCount = 0;

        WeaponUid = "";
        ArmorUid = "";
        AccessoryUid = "";
    }

    /// <summary>테스트를 위해 지정한 레벨의 캐릭터 데이터를 생성한다.</summary>
    /// <param name="characterData">사용자 보유 데이터의 기반이 되는 캐릭터 마스터 데이터입니다.</param>
    /// <param name="charLevel">테스트 또는 초기 데이터에 그대로 저장할 캐릭터 레벨입니다.</param>
    public CharData(CharacterData characterData, int charLevel) : this(characterData)
    {
        CharLv = charLevel;
    }
}

/// <summary>
/// 유저가 현재 진행 중인 스테이지 저장 데이터
/// </summary>
/// <remarks>
///
/// <para><b>Stage Progress</b></para>
/// <list type="bullet">
/// <item><description><c>int CurChapter</c> 현재 진행 중인 챕터 번호</description></item>
/// <item><description><c>int CurStage</c> 현재 진행 중인 스테이지 번호</description></item>
/// </list>
///
/// </remarks>
[System.Serializable]
public class StageSaveData
{
    /// <summary>현재 진행 중인 챕터 번호</summary>
    public int CurChapter = 1;

    /// <summary>현재 진행 중인 스테이지 번호</summary>
    public int CurStage = 1;

    // 도전별 Run과 분리하여 업데이트 후에도 최초 클리어/정산 기록을 유지한다.
    public int ClearedChapter;
    public int ClearedStage;
    public int SettledChapter;
    public int SettledStage;

    public bool HasSettled(int chapter, int stage)
        => SettledChapter > chapter || SettledChapter == chapter && SettledStage >= stage;

    /// <summary>이전 저장본의 null/완전한 기본 객체는 로드 시 유효한 초기 체크포인트로 변환한다.</summary>
    public ChapterRunSaveData Run;
}

public enum ChapterRunState { InProgress, Cleared, Defeated }

[System.Serializable]
public class PartyCheckpointHp
{
    public string CharId;
    public float Hp;
}

[System.Serializable]
public class FieldRewardReceipt
{
    public int Stage;
    public int Slot;
}

/// <summary>한 챕터 도전의 시작 체력, 지급 이력과 표시용 획득 내역이다.</summary>
[System.Serializable]
public class ChapterRunSaveData
{
    public int Version = 1;
    public string AttemptId;
    public int Chapter;
    public int CheckpointStage;
    public ChapterRunState State;
    public List<PartyCheckpointHp> EntryHp = new();
    public List<FieldRewardReceipt> Receipts = new();
    public List<ItemData> EarnedItems = new();
    public ulong EarnedGold;

    // 드롭 실패/가방 부족도 처리 완료로 기록해 재접속 후 재추첨을 막는다.
    public bool TryRecordReward(int stage, int slot)
    {
        if (State != ChapterRunState.InProgress || stage != CheckpointStage || slot < 0 || slot >= 4) return false;
        foreach (var receipt in Receipts)
            if (receipt.Stage == stage && receipt.Slot == slot) return false;
        Receipts.Add(new FieldRewardReceipt { Stage = stage, Slot = slot });
        return true;
    }
}

/// <summary>
/// 유저 인벤토리에 저장되는 단일 아이템 데이터
/// </summary>
/// <remarks>
///
/// <para><b>Identity</b></para>
/// <list type="bullet">
/// <item><description><c>string ItemId</c> 아이템 종류를 식별하기 위한 고유 ID</description></item>
/// <item><description><c>string ItemUid</c> 개별 아이템 인스턴스를 식별하기 위한 UID</description></item>
/// </list>
///
/// <para><b>Item State</b></para>
/// <list type="bullet">
/// <item><description><c>int ItemLv</c> 아이템의 강화 레벨</description></item>
/// <item><description><c>ItemType Type</c> 아이템의 종류</description></item>
/// <item><description><c>int ItemCount</c> 보유 중인 아이템 개수</description></item>
/// </list>
///
/// </remarks>
[System.Serializable]
public class ItemData
{
    /// <summary>아이템 종류를 식별하기 위한 고유 ID</summary>
    public string ItemId;

    /// <summary>
    /// 개별 아이템 인스턴스를 식별하기 위한 UID.
    /// 동일한 장비를 여러 개 보유할 수 있는 경우 각각 다른 UID를 사용한다.
    /// </summary>
    public string ItemUid;

    /// <summary>아이템의 강화 레벨</summary>
    public int ItemLv;

    /// <summary>아이템의 종류</summary>
    public ItemType Type;

    /// <summary>보유 중인 아이템 개수</summary>
    public int ItemCount;

    /// <summary>획득 시 확정한 판매가격. 0은 기존 데이터의 미설정 상태입니다.</summary>
    public int Price;
    
    /// <summary>
    /// JsonUtility 역직렬화 등을 위한 기본 생성자
    /// </summary>
    public ItemData()
    {
        // Unity 직렬화 중에도 호출될 수 있으므로 씬 객체에 접근하지 않는다.
        ItemId = "";
        ItemUid = "";
        ItemLv = 1;
        Type = ItemType.Equipment;
        ItemCount = 1;
    }

    /// <summary>
    /// ItemParentSO를 기반으로 무기 데이터를 생성한다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    public ItemData(ItemParentSO data)
    {
        ItemId = data.Id;
        ItemUid = PlayerDataMgr.Inst.CreateUid(UidType.Item);
        ItemLv = 1;
        Type = data.Type;
        ItemCount = 1;
        if (data is EquipmentData equipment)
            Price = GlobalConstants.CreateEquipmentSalePrice(equipment.Rarity);
    }
}
#endregion

#region 세이브데이터 포멧 클래스

/// <summary>
/// 사용자 계정 식별 정보와 보유 재화를 저장하는 데이터 영역입니다.
/// </summary>
[System.Serializable]
public class UserSaveData
{
    public CharacterGachaSaveData Gacha = new();
    /// <summary>
    /// 사용자 저장 영역의 마이그레이션에 사용하는 형식 버전입니다.
    /// </summary>
    public int SaveDataVersion = 1;

    /// <summary>
    /// 저장 데이터가 속한 사용자의 고유 UID입니다.
    /// </summary>
    public string Uid;
    /// <summary>
    /// 게임 UI에 표시하는 사용자 닉네임입니다.
    /// </summary>
    public string NickName;

    /// <summary>
    /// 저장 시점에 사용자가 보유한 게임 골드입니다.
    /// </summary>
    public ulong UserGold;
    /// <summary>
    /// 저장 시점에 사용자가 보유한 유료 재화입니다.
    /// </summary>
    public ulong RealMoney;
    /// <summary>별의 조각 보유량. 필드가 없는 이전 저장은 0으로 복원한다.</summary>
    public ulong StarFragments;
}

/// <summary>PlayerInfo에 저장하는 계정 공통 천장과 일일 골드 뽑기 상태.</summary>
[System.Serializable]
public class CharacterGachaSaveData
{
    public string TargetCharacterId = "";
    public int PityCount;
    public long GoldDrawDay;
    public int GoldDrawCount;

    public CharacterGachaSaveData Copy() => new CharacterGachaSaveData
    {
        TargetCharacterId = TargetCharacterId ?? "", PityCount = PityCount,
        GoldDrawDay = GoldDrawDay, GoldDrawCount = GoldDrawCount
    };
}

/// <summary>
/// 랭킹 점수, 파티 성장 및 스테이지 진행 상태를 저장하는 데이터 영역입니다.
/// </summary>
[System.Serializable]
public class ProgressSaveData
{
    /// <summary>마지막 저장 스냅샷의 서버 기준 UTC 시각. 0은 오프라인 보상 도입 전 데이터입니다.</summary>
    public long LastActiveUnixTime;

    /// <summary>
    /// 진행 저장 영역의 마이그레이션에 사용하는 형식 버전입니다.
    /// </summary>
    public int SaveDataVersion = 1;
    /// <summary>
    /// 진행 데이터가 속한 사용자의 고유 UID입니다.
    /// </summary>
    public string Uid;
    /// <summary>
    /// 파티 전투력 랭킹에 제출할 저장 점수입니다.
    /// </summary>
    public int AtkPowerRankScore;
    /// <summary>
    /// 허수아비 피해량 랭킹에 제출할 저장 점수입니다.
    /// </summary>
    public int DummyRankScore;

    /// <summary>
    /// 저장된 파티 공격력 성장 레벨입니다.
    /// </summary>
    public int PartyAtkLv;
    /// <summary>
    /// 저장된 파티 최대 체력 성장 레벨입니다.
    /// </summary>
    public int PartyHpLv;
    /// <summary>
    /// 저장된 파티 방어력 성장 레벨입니다.
    /// </summary>
    public int PartyDefLv;
    /// <summary>
    /// 저장된 파티 공격속도 성장 레벨입니다.
    /// </summary>
    public int PartyAtkSpdLv;
    /// <summary>
    /// 저장된 파티 치명타 확률 성장 레벨입니다.
    /// </summary>
    public int PartyCritChanceLv;
    /// <summary>
    /// 저장된 파티 치명타 피해 성장 레벨입니다.
    /// </summary>
    public int PartyCritDmgLv;

    /// <summary>
    /// 저장된 챕터와 스테이지 진행 상태입니다.
    /// </summary>
    public StageSaveData StageData = new();
}

/// <summary>
/// 보유 캐릭터와 고정 파티 슬롯 편성을 저장하는 데이터 영역입니다.
/// </summary>
[System.Serializable]
public class CharacterSaveData
{
    /// <summary>
    /// 캐릭터 저장 영역의 마이그레이션에 사용하는 형식 버전입니다.
    /// </summary>
    public int SaveDataVersion = 1;
    /// <summary>
    /// 캐릭터 데이터가 속한 사용자의 고유 UID입니다.
    /// </summary>
    public string Uid;
    /// <summary>
    /// 사용자가 보유한 캐릭터별 레벨, 중복 및 장비 상태 목록입니다.
    /// </summary>
    public List<CharData> UserCharacters = new();

    // 현재 파티에 배치된 캐릭터 ID
    /// <summary>
    /// 네 개의 고정 파티 슬롯 순서로 저장된 캐릭터 ID 목록입니다.
    /// </summary>
    public List<string> PartyCharacterIds = new();
}

/// <summary>
/// 사용자가 보유한 아이템 목록을 저장하는 데이터 영역입니다.
/// </summary>
[System.Serializable]
public class InventorySaveData
{
    /// <summary>
    /// 인벤토리 저장 영역의 마이그레이션에 사용하는 형식 버전입니다.
    /// </summary>
    public int SaveDataVersion = 1;
    /// <summary>
    /// 인벤토리 데이터가 속한 사용자의 고유 UID입니다.
    /// </summary>
    public string Uid;
    /// <summary>
    /// 사용자가 보유한 아이템별 UID, 레벨 및 수량 목록입니다.
    /// </summary>
    public List<ItemData> UserItems = new();
}

/// <summary>
/// 메모리 스냅샷과 기존 통합 저장본 이관에 사용하는 데이터입니다.
/// </summary>
[System.Serializable]
public class GameSaveData
{
    // 저장 형식 버전
    /// <summary>
    /// 통합 저장 형식의 마이그레이션에 사용하는 버전입니다.
    /// </summary>
    public int DataVersion = 1;

    // 서버 저장 충돌 확인용 버전
    /// <summary>
    /// 기존 통합 저장본의 저장 횟수입니다. UserData 조건부 저장에 사용하지 않습니다.
    /// </summary>
    public int Revision;

    // 마지막 저장 시각
    /// <summary>
    /// 마지막 저장 완료 시각을 나타내는 Unix 타임스탬프입니다.
    /// </summary>
    public long LastSavedUnixTime;

    // 마지막 저장 요청 정보. 응답 유실 시 같은 요청인지 확인한다.
    public string LastRequestId;
    public string LastRequestHash;

    /// <summary>
    /// 계정 식별 정보와 보유 재화를 포함하는 사용자 저장 영역입니다.
    /// </summary>
    public UserSaveData User;
    /// <summary>
    /// 랭킹, 파티 성장 및 스테이지 상태를 포함하는 진행 저장 영역입니다.
    /// </summary>
    public ProgressSaveData Progress;
    /// <summary>
    /// 보유 캐릭터와 파티 편성을 포함하는 캐릭터 저장 영역입니다.
    /// </summary>
    public CharacterSaveData Character;
    /// <summary>
    /// 보유 아이템 목록을 포함하는 인벤토리 저장 영역입니다.
    /// </summary>
    public InventorySaveData Inventory;
}

[System.Serializable]
public class SaveGameRequest
{
    public string Json;
    public SaveType Types;
    public Dictionary<string, string> Values;
    public string RequestId;
}

[System.Serializable]
public class OnlineSaveReply
{
    public string Status;
    public string Message;
    public string Json;
    public uint UserDataVersion;
    public string RequestId;
}

[System.Serializable]
public class SaveStorageObject
{
    public string Encoding;
    public string Payload;
}

/// <summary>서버 저장을 예약할 영역. 여러 영역은 | 연산자로 조합합니다.</summary>
[System.Flags]
public enum SaveType
{
    None = 0,
    Inventory = 1,
    CharacterList = 2,
    PlayerInfo = 4,
    All = Inventory | CharacterList | PlayerInfo
}

[System.Serializable]
public class PlayerInfoSaveData
{
    public int SaveDataVersion = 1;
    public string Uid;
    public UserSaveData User;
    public ProgressSaveData Progress;
    public List<string> PartyCharacterIds = new();
}

[System.Serializable]
public class CharacterListSaveData
{
    public int SaveDataVersion = 1;
    public string Uid;
    public List<CharData> UserCharacters = new();
}

#endregion
