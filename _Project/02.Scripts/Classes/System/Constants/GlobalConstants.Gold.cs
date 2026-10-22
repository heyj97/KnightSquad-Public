using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

public static partial class GlobalConstants
{
    /// <summary>이 시간 이하의 미접속에는 보상을 지급하지 않습니다. 단위: 초.</summary>
    public const int OfflineMinSeconds = 5 * 60;
    
    public const int OfflineMaxSeconds = 8 * 60 * 60;
    
    public const int OfflineBaseGoldPerMinute = 60;

    public static readonly IReadOnlyDictionary<MonsterGrade, ulong> MonsterBaseGoldTable =
        new ReadOnlyDictionary<MonsterGrade, ulong>(new Dictionary<MonsterGrade, ulong>
        {
            [MonsterGrade.Normal] = 10, [MonsterGrade.Elite] = 50,
            [MonsterGrade.StageBoss] = 200, [MonsterGrade.RaidBoss] = 0
        });

    public static readonly IReadOnlyDictionary<int, decimal> ChapterGoldWeightTable =
        new ReadOnlyDictionary<int, decimal>(new Dictionary<int, decimal>
        {
            [1] = 1m, [2] = 1.5m, [3] = 2m, [4] = 2.5m, [5] = 3m,
            [6] = 3.5m, [7] = 4m, [8] = 4.5m, [9] = 5m, [10] = 5.5m
        });

    // 인덱스 0이 1스테이지. 중간 계산에서는 버리지 않고 최종 골드만 버린다.
    public static readonly IReadOnlyList<decimal> StageGoldWeightTable = Array.AsReadOnly(new[]
        { 1m, 1.02m, 1.04m, 1.06m, 1.08m, 1.10m, 1.12m, 1.14m, 1.16m, 1.18m });

    public static decimal GetProgressGoldWeight(int chapter, int stage)
    {
        if (chapter < 1 || stage < 1 || stage > StageGoldWeightTable.Count)
            throw new ArgumentOutOfRangeException(nameof(stage), "챕터는 1 이상, 스테이지는 1~10이어야 합니다.");
        // 테이블 이후 챕터도 같은 증가 규칙을 사용한다.
        decimal chapterWeight = ChapterGoldWeightTable.TryGetValue(chapter, out decimal weight)
            ? weight : 1m + (chapter - 1) * 0.5m;
        return chapterWeight * StageGoldWeightTable[stage - 1];
    }

    public static ulong GetMonsterGold(MonsterGrade grade, int chapter, int stage)
        => MonsterBaseGoldTable.TryGetValue(grade, out ulong gold)
            ? (ulong)decimal.Floor(gold * GetProgressGoldWeight(chapter, stage)) : 0;

    public static decimal GetOfflineGoldPerMinute(int chapter, int stage)
        => OfflineBaseGoldPerMinute * GetProgressGoldWeight(chapter, stage);
}
