using System;
using UnityEngine;
using System.Collections.Generic;

public static partial class GlobalConstants
{
    public enum RaidBossName
    {
        Nightmare = 0,
        Count
    }
    
#region Drop Table(Gold, Item, etc...)
    /// <summary>
    /// 레이드 보스 ID별로 획득 가능한 장비 ID 목록을 제공합니다. - RaidItemDropTableList[RaidBossName.이름] 으로 대체
    /// </summary>
    public static readonly Dictionary<string, List<string>> RaidItemDropTable = new()
    {
        {
            "MON_RAIDBOSS_NIGHTMARE",
            new List<string>()
            {
                "EQ_COMMON_ACCESSORY_CHARM",
                "EQ_COMMON_ARMOR_CLOTH",
                "EQ_COMMON_WEAPON_SPEAR"
            }
        }
    };

    /// <summary>
    /// 레이드 보스 ID별 골드 보상량을 제공합니다. - RaidGoldDropTableList[RaidBossName.이름] 으로 대체
    /// </summary>
    public static readonly Dictionary<string, int> RaidGoldDropTable = new()
    {
        {
            "MON_RAIDBOSS_NIGHTMARE",
            100000
        }
    };
    
    
    /// <summary>
    /// 레이드 보스 enum에 대응하는 아이템 드롭테이블을 제공합니다.
    /// </summary>
    public static readonly List<List<string>> RaidItemDropTableList = new()
    {
        // Nightmare
        new List<string>()
        {
            "EQ_COMMON_ACCESSORY_CHARM",
            "EQ_COMMON_ARMOR_CLOTH",
            "EQ_COMMON_WEAPON_SPEAR"
        },
    };

    /// <summary>
    /// 레이드 보스 enum에 대응하는 골드량을 제공합니다.
    /// </summary>
    public static readonly List<int> RaidGoldDropTableList = new()
    {
        100000,
    };

    #endregion
    
    public static RaidBossName GetRaidBossName(string bossId)
    {
        switch (bossId)
        {
            case "MON_RAIDBOSS_NIGHTMARE":
                return RaidBossName.Nightmare;
            
            default:
                PDebug.LogError("존재하지 않는 보스id입니다.");
                return RaidBossName.Count;
        }
    }

    public static int GetRaidSeason(RaidBossName boss)
    {
        return (int)boss + 1;
    }
}
