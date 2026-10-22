using System;
using UnityEngine;

// 역할·등급 표시 이름과 색상.
public static partial class GlobalConstants
{
    /// <summary>
    /// 캐릭터 역할을 UI에 표시할 한국어 이름으로 변환합니다.
    /// </summary>
    /// <param name="role">표시 규칙을 조회할 캐릭터 역할입니다.</param>
    /// <returns>UI에 표시할 캐릭터 역할의 한국어 이름입니다.</returns>
    public static string GetKorRoleName(CharacterRole role)
    {
        return role switch
        {
            CharacterRole.MainDealer => "메인딜러",
            CharacterRole.SubDealer => "보조딜러",
            CharacterRole.Tanker => "탱커",
            CharacterRole.Supporter => "서포터",
            _ => "???"
        };
    }

    public static string GetKorRarityName(SystemRarity rarity)
    {
        return rarity switch
        {
            SystemRarity.Common => "커먼",
            SystemRarity.Uncommon => "언커먼",
            SystemRarity.Rare => "레어",
            SystemRarity.Epic => "에픽",
            SystemRarity.Legendary => "레전더리",
            SystemRarity.Mystic => "신화",
            _ => throw new ArgumentOutOfRangeException(nameof(rarity), rarity, null)
        };
    }

    /// <summary>
    /// 캐릭터 역할을 UI 강조에 사용하는 색상으로 변환합니다.
    /// </summary>
    /// <param name="role">표시 규칙을 조회할 캐릭터 역할입니다.</param>
    /// <returns>지정 역할을 나타내는 UI 색상입니다.</returns>
    public static Color32 GetRoleColor32(CharacterRole role)
    {
        return role switch
        {
            CharacterRole.MainDealer => new Color32(227, 46, 46, 255),
            CharacterRole.SubDealer => new Color32(141, 81, 171, 255),
            CharacterRole.Tanker => new Color32(62, 127, 200, 255),
            CharacterRole.Supporter => new Color32(77, 200, 61, 255),
            _ => new Color32(255, 255, 255, 255)
        };
    }

    /// <summary>
    /// 등급을 UI 프레임과 텍스트에 사용하는 색상으로 변환합니다.
    /// </summary>
    /// <param name="rarity">조회할 캐릭터 또는 아이템 등급입니다.</param>
    /// <returns>지정 등급을 나타내는 UI 색상입니다.</returns>
    public static Color32 GetRarityColor32(SystemRarity rarity)
    {
        return rarity switch
        {
            SystemRarity.Common => new Color32(184, 190, 199, 255),
            SystemRarity.Uncommon => new Color32(79, 203, 107, 255),
            SystemRarity.Rare => new Color32(67, 140, 255, 255),
            SystemRarity.Epic => new Color32(168, 92, 255, 255),
            SystemRarity.Legendary => new Color32(255, 181, 46, 255),
            SystemRarity.Mystic => new Color32(255, 79, 163, 255),
            _ => new Color32(184, 190, 199, 255),
        };
    }

    /// <summary>
    /// 등급 색상을 리치 텍스트에서 사용할 16진수 문자열로 반환합니다.
    /// </summary>
    /// <param name="rarity">조회할 캐릭터 또는 아이템 등급입니다.</param>
    /// <returns>앞에 #이 없는 RGB 16진수 색상 문자열입니다.</returns>
    public static string GetRarityHexColor(SystemRarity rarity)
        => ColorUtility.ToHtmlStringRGB(GetRarityColor32(rarity));

    public static Color32 GetBossColor32(RaidBossName boss)
    {
        switch (boss)
        {
            case RaidBossName.Nightmare:
                return new Color32(216, 153, 241, 255);
            
            default:
                throw new ArgumentOutOfRangeException(nameof(boss), boss, null);
        }
    }
    
    public static string GetBossHexColor(string bossId)
        => ColorUtility.ToHtmlStringRGB(GetBossColor32(GetRaidBossName(bossId.ToUpperInvariant())));
}
