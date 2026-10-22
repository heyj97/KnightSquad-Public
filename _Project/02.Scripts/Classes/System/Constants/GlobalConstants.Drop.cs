using System.Collections.Generic;

// 필드·레이드 드롭 규칙. 과거 골드 테이블도 보존합니다.
public static partial class GlobalConstants
{
    // 필드 아이템 드랍 확률
    /// <summary>
    /// 필드 몬스터 처치 시 장비 드롭을 시도할 확률입니다.
    /// </summary>
    public const float ItemDropRate = 0.2f;
    // 레이드에서 드랍하는 아이템 갯수
    /// <summary>
    /// 레이드 보상 화면에서 추첨하는 장비 보상 개수입니다.
    /// </summary>
    public static readonly int RaidDropItemCount = 10;

    // 추후 JSON으로 드롭테이블은 서버에서 관리
    /// <summary>
    /// 챕터 시작 구간별로 필드에서 획득 가능한 장비 ID 목록을 제공합니다.
    /// </summary>
    public static readonly Dictionary<int, List<string>> FieldItemDropTable = new()
    {
        // chapter 1~3
        {
            1,      // Chapter
            new List<string>()   // Drop Items Id
            {
                "EQ_COMMON_ACCESSORY_CHARM",
                "EQ_COMMON_ARMOR_CLOTH",
                "EQ_COMMON_WEAPON_SPEAR"
            }
        },

        // chapter 4~6
        {
            4,      // Chapter
            new List<string>()   // Drop Items Id
            {
                "",
            }
        },

        // chapter 7~9
        {
            7,      // Chapter
            new List<string>()   // Drop Items Id
            {
                "",
            }
        },
    };

    /// <summary>
    /// 과거 필드 골드 테이블입니다. 현재 지급은 GlobalConstants.Gold.cs의 GetMonsterGold를 사용합니다.
    /// </summary>
    public static readonly Dictionary<int, List<int>> FieldGoldDropTable = new()
    {
        {
            1,
            new List<int>()
            {
                10,     // Normal
                50,     // MiddleBoss
                100     // StageBoss
            }
        }
    };
}
