/// <summary>
/// 몬스터의 희귀도 및 등급.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) Normal - 일반 몬스터</description></item>
/// <item><description>(1) MiddleBoss - 중간 보스</description></item>
/// <item><description>(2) StageBoss - 스테이지 보스</description></item>
/// <item><description>(3) RaidBoss - 레이드 보스</description></item>
/// </list>
/// </remarks>
public enum MonsterGrade
{
    /// <summary>(0) Normal - 일반 몬스터</summary>
    Normal = 0,

    /// <summary>(1) MiddleBoss - 중간 보스</summary>
    Elite = 1,

    /// <summary>(2) StageBoss - 스테이지 보스</summary>
    StageBoss = 2,

    /// <summary>(3) RaidBoss - 레이드 보스(컨텐츠용)</summary>
    RaidBoss = 3,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}