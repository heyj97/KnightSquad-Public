using System.Collections.Generic;

/// <summary>레이드 한 회의 기록. 종료 후에는 전투 오브젝트와 무관하게 UI에서 읽는다.</summary>
public sealed class RaidCombatResult
{
    public sealed class CharacterRecord
    {
        public CharacterData Data { get; internal set; }
        public int Slot { get; internal set; }
        public double Damage { get; internal set; }
    }

    public MonsterData Boss { get; internal set; }
    public bool Cleared { get; internal set; }
    public BattleFlowMgr.DefeatReason? Reason { get; internal set; }
    public float ElapsedSeconds { get; internal set; }
    public double TotalDamage { get; internal set; }
    public double PatternDamage { get; internal set; }
    public ulong Gold { get; internal set; }
    internal readonly List<CharacterRecord> CharacterRecords = new();
    internal readonly List<ItemData> RewardItems = new();
    public IReadOnlyList<CharacterRecord> Characters => CharacterRecords;
    public IReadOnlyList<ItemData> Items => RewardItems;
}
