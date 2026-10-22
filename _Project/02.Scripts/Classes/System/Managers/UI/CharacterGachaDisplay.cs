using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

/// <summary>결과 표시 전용 계산. 지급·차감·저장을 수행하지 않는다.</summary>
internal static class CharacterGachaDisplay
{
    private static readonly string[] RarityNames = { "커먼", "언커먼", "레어", "에픽", "레전더리", "신화" };
    private static readonly string[] RarityColors = { "b8bec7", "4fcb6b", "438cff", "a85cff", "ffb62e", "ff4fa4" };

    internal static string GoldRemaining(int remaining, int limit)
        => $"오늘 골드 뽑기: {(remaining == 0 ? "<color=#ff0000>0</color>" : remaining.ToString(CultureInfo.InvariantCulture))}/{limit}회";

    internal static string PityRemaining(string name, int count, int limit)
        => $"{name} 확정 획득까지 {Math.Max(0, limit - count)}회";

    internal static (string Rarities, string Fragments, ulong FragmentCount) Summarize(
        IReadOnlyList<CharacterGachaResult> results, IReadOnlyList<CharacterData> masters)
    {
        if (results == null || masters == null) throw new ArgumentNullException();
        var byId = masters.Where(c => c != null).ToDictionary(c => c.Id, c => c);
        var counts = new int[RarityNames.Length];
        ulong fragments = 0;
        foreach (var result in results)
        {
            if (result == null || !byId.TryGetValue(result.CharacterId, out var data) ||
                (int)data.Rarity < 0 || (int)data.Rarity >= counts.Length)
                throw new ArgumentException("결과 캐릭터의 마스터 또는 등급을 확인해주세요.");
            counts[(int)data.Rarity]++;
            fragments = checked(fragments + result.StarFragments);
        }
        string rarities = string.Join(", ", Enumerable.Range(0, counts.Length).Reverse()
            .Select(i => $"<color=#{RarityColors[i]}>{RarityNames[i]}</color> {counts[i]}"));
        string fragmentText = fragments == 0 ? "별의 조각을 획득하지 않았습니다."
            : $"획득한 별의 조각 <color=#ff4fa4>{fragments.ToString("N0", CultureInfo.InvariantCulture)}</color> 개";
        return (rarities, fragmentText, fragments);
    }
}
