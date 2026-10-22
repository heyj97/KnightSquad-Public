using System;
using System.Collections.Generic;
using System.Linq;

public enum GachaCurrency { Gold, RM }

public class CharacterGachaResult
{
    public string CharacterId;
    public bool IsNew, IsPity;
    public int BeforeLevel, AfterLevel, DuplicateCount;
    public ulong StarFragments;
}

/// <summary>검증된 추첨·성장 결과. 확정 전에는 원래 보유 데이터를 변경하지 않는다.</summary>
public class CharacterGachaPlan
{
    public List<CharData> Characters;
    public CharacterGachaSaveData State;
    public ulong Gold, RM, StarFragments;
    public List<CharacterGachaResult> Results = new();
}

/// <summary>씬/통신에 의존하지 않는 추첨·자동 성장 계산. random은 0 이상 1 미만이어야 한다.</summary>
public static class CharacterGachaEngine
{
    public static long GetGoldDrawDay(long unixTime)
        => (unixTime + GlobalConstants.CharacterGachaResetUtcOffsetHours * 3600L) / 86400L;

    public static bool IsValidState(CharacterGachaSaveData state)
        => state == null || (state.PityCount >= 0 && state.PityCount < GlobalConstants.CharacterGachaPityCount &&
            state.GoldDrawDay >= 0 && state.GoldDrawCount >= 0 && state.GoldDrawCount <= GlobalConstants.CharacterGachaDailyGoldLimit &&
            (state.PityCount == 0 || !string.IsNullOrWhiteSpace(state.TargetCharacterId)));

    public static int GetPityAfterTargetChange(int count, string oldId, string newId)
        => string.IsNullOrEmpty(oldId) || oldId == newId ? count : count / 2;

    public static List<CharacterData> GetTargets(IReadOnlyList<CharacterData> masters)
        => GlobalConstants.CharacterGachaPityTargetIds.Select(id => masters.FirstOrDefault(c => c != null &&
            c.Id == id && c.Rarity == SystemRarity.Mystic)).Where(c => c != null).Distinct().ToList();

    public static bool TrySelectTarget(CharacterGachaSaveData original, string id,
        IReadOnlyList<CharacterData> masters, out CharacterGachaSaveData state, out string message)
    {
        state = null; message = "";
        if (!IsValidState(original) || !GetTargets(masters).Any(c => c.Id == id))
        { message = "선택할 수 없는 천장 대상입니다."; return false; }
        state = original?.Copy() ?? new CharacterGachaSaveData();
        if (state.TargetCharacterId != id)
        {
            state.PityCount = GetPityAfterTargetChange(state.PityCount, state.TargetCharacterId, id);
            state.TargetCharacterId = id;
        }
        return true;
    }

    public static bool TryCreatePlan(IReadOnlyList<CharacterData> masters, IReadOnlyList<CharData> owned,
        CharacterGachaSaveData original, ulong gold, ulong rm, GachaCurrency currency, int count,
        long unixTime, Func<double> random, out CharacterGachaPlan plan, out string message, ulong starFragments = 0)
    {
        plan = null; message = "";
        if ((count != 1 && count != 10) || !Enum.IsDefined(typeof(GachaCurrency), currency) || unixTime <= 0 ||
            masters == null || owned == null || random == null || !IsValidState(original))
        { message = "뽑기 요청 또는 저장 데이터가 유효하지 않습니다."; return false; }
        var masterMap = new Dictionary<string, CharacterData>();
        foreach (var master in masters)
            if (master == null || string.IsNullOrWhiteSpace(master.Id) || (int)master.Rarity < 0 || (int)master.Rarity > (int)SystemRarity.Mystic ||
                !masterMap.TryAdd(master.Id, master))
            { message = "캐릭터 마스터의 ID 또는 등급을 확인해주세요."; return false; }
        var targets = GetTargets(masters);
        if (targets.Count == 0 || targets.Count != GlobalConstants.CharacterGachaPityTargetIds.Distinct().Count())
        { message = "천장 대상 ID의 마스터 등록 및 최고등급 여부를 확인해주세요."; return false; }
        var state = original?.Copy() ?? new CharacterGachaSaveData();
        if (string.IsNullOrEmpty(state.TargetCharacterId)) state.TargetCharacterId = targets[0].Id;
        if (!targets.Any(c => c.Id == state.TargetCharacterId))
        { message = "저장된 천장 대상을 다시 선택해주세요."; return false; }
        long today = GetGoldDrawDay(unixTime);
        if (state.GoldDrawDay > today) { message = "서버 날짜를 확인한 뒤 다시 시도해주세요."; return false; }
        if (state.GoldDrawDay != today) { state.GoldDrawDay = today; state.GoldDrawCount = 0; }
        ulong cost = checked((currency == GachaCurrency.Gold ? GlobalConstants.CharacterGachaGoldPrice : GlobalConstants.CharacterGachaRMPrice) * (ulong)count);
        if (currency == GachaCurrency.Gold)
        {
            int remaining = GlobalConstants.CharacterGachaDailyGoldLimit - state.GoldDrawCount;
            if (remaining == 0)
            { message = "오늘의 골드 뽑기 횟수를 모두 사용했습니다."; return false; }
            if (remaining < count)
            { message = $"남은 골드 뽑기 횟수가 부족합니다. (남은 횟수: {remaining}회)"; return false; }
            if (gold < cost)
            { message = "골드가 부족합니다!"; return false; }
        }
        if (currency == GachaCurrency.RM && rm < cost) { message = "RM이 부족합니다!"; return false; }
        var characters = new List<CharData>();
        var byId = new Dictionary<string, CharData>();
        foreach (var user in owned)
        {
            if (user == null || !masterMap.TryGetValue(user.CharId ?? "", out var master) || user.CharLv < 1 ||
                user.CharLv > GlobalConstants.GetCharacterMaxLevel(master.Rarity) || user.CharDuplicateCount < 0 || byId.ContainsKey(user.CharId))
            { message = "보유 캐릭터 데이터를 확인해주세요."; return false; }
            var copy = new CharData { CharId = user.CharId, CharLv = user.CharLv, CharDuplicateCount = user.CharDuplicateCount,
                WeaponUid = user.WeaponUid, ArmorUid = user.ArmorUid, AccessoryUid = user.AccessoryUid };
            characters.Add(copy); byId.Add(copy.CharId, copy);
        }
        var groups = masters.GroupBy(c => c.Rarity).ToDictionary(g => g.Key, g => g.OrderBy(c => c.Id, StringComparer.Ordinal).ToList());
        if (!groups.ContainsKey(SystemRarity.Common)) { message = "Common 등급 캐릭터를 등록해주세요."; return false; }
        var rates = new double[6]; double total = 0;
        for (int i = 0; i < rates.Length; i++)
        {
            double rate = GlobalConstants.GetCharacterGachaRarityRate((SystemRarity)i);
            if (double.IsNaN(rate) || rate < 0) { message = "뽑기 확률표를 확인해주세요."; return false; }
            rates[groups.ContainsKey((SystemRarity)i) ? i : 0] += rate;
            total += rate;
        }
        if (Math.Abs(total - 1) > 0.00001) { message = "뽑기 확률 합계는 100%여야 합니다."; return false; }
        var result = new CharacterGachaPlan { Characters = characters, State = state,
            Gold = currency == GachaCurrency.Gold ? gold - cost : gold,
            RM = currency == GachaCurrency.RM ? rm - cost : rm, StarFragments = starFragments };
        for (int draw = 0; draw < count; draw++)
        {
            bool pity = state.PityCount + 1 >= GlobalConstants.CharacterGachaPityCount;
            CharacterData chosen;
            if (pity) chosen = masterMap[state.TargetCharacterId];
            else
            {
                double roll = Sample(random); int rarity = 0; double cumulative = rates[0];
                while (rarity < rates.Length - 1 && roll >= cumulative) cumulative += rates[++rarity];
                var pool = groups[(SystemRarity)rarity];
                chosen = pool[Math.Min(pool.Count - 1, (int)(Sample(random) * pool.Count))];
            }
            state.PityCount = chosen.Id == state.TargetCharacterId ? 0 : state.PityCount + 1;
            bool isNew = !byId.TryGetValue(chosen.Id, out var user);
            if (isNew)
            {
                user = new CharData(chosen); byId.Add(chosen.Id, user); characters.Add(user);
            }
            var item = new CharacterGachaResult { CharacterId = chosen.Id, IsNew = isNew, IsPity = pity, BeforeLevel = isNew ? 0 : user.CharLv };
            if (!isNew)
            {
                long duplicates = (long)user.CharDuplicateCount + 1;
                int max = GlobalConstants.GetCharacterMaxLevel(chosen.Rarity);
                while (user.CharLv < max)
                {
                    int required = GlobalConstants.GetCharacterLevelDuplicateRequirement(chosen.Rarity, user.CharLv + 1);
                    if (duplicates < required) break;
                    duplicates -= required; user.CharLv++;
                }
                if (user.CharLv == max)
                {
                    item.StarFragments = (ulong)duplicates * (ulong)GlobalConstants.CharacterStarFragmentTable[chosen.Rarity];
                    duplicates = 0;
                }
                user.CharDuplicateCount = checked((int)duplicates);
            }
            item.AfterLevel = user.CharLv; item.DuplicateCount = user.CharDuplicateCount;
            if (item.StarFragments > ulong.MaxValue - result.StarFragments)
            { message = "별의 조각 보유 한도를 초과합니다."; return false; }
            result.StarFragments += item.StarFragments;
            result.Results.Add(item);
        }
        if (currency == GachaCurrency.Gold) state.GoldDrawCount += count;
        plan = result;
        return true;
    }

    private static double Sample(Func<double> random)
    {
        double value = random();
        if (double.IsNaN(value) || value < 0 || value >= 1) throw new ArgumentOutOfRangeException(nameof(random));
        return value;
    }
}
