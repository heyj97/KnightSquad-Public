using System;

/// <summary>전투·UI·고정 전투력이 공유하는 캐릭터 레벨 성장. SO 원본은 변경하지 않는다.</summary>
public static class CharacterGrowthCalc
{
    public static float GetMultiplier(SystemRarity rarity, int level)
    {
        int max = GlobalConstants.GetCharacterMaxLevel(rarity);
        int clamped = Math.Max(1, Math.Min(max, level));
        return 1f + (GlobalConstants.CharacterMaxLevelBaseMultiplier - 1f) * (clamped - 1) / (max - 1);
    }

    /// <summary>HP, ATK, AS, DEF, 치확, 치피 순서. HP/ATK/DEF만 레벨로 성장한다.</summary>
    public static float[] GetBaseStats(CharacterData data, int level)
    {
        float scale = GetMultiplier(data.Rarity, level);
        return new[] { data.BaseMaxHp * scale, data.BaseAttackPower * scale, data.BaseAttackSpeed,
            data.BaseDefense * scale, data.BaseCriticalChance, data.BaseCriticalDamageMultiplier };
    }
}
