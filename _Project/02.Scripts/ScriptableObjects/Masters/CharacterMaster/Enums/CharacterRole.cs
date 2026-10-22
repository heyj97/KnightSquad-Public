/// <summary>
/// 캐릭터의 전투 역할.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) MainDealer - 메인 딜러</description></item>
/// <item><description>(1) SubDealer - 서브 딜러</description></item>
/// <item><description>(2) Tanker - 탱커</description></item>
/// <item><description>(3) Supporter - 서포터</description></item>
/// </list>
/// </remarks>
public enum CharacterRole
{
    /// <summary>(0) MainDealer - 단일 대상에게 강한 피해를 주며 보스전에 유리.</summary>
    MainDealer = 0,

    /// <summary>(1) SubDealer - 다수의 적을 상대하는 데 유리하며 일반 스테이지에 강점.</summary>
    SubDealer = 1,

    /// <summary>(2) Tanker - 아군 대신 피해를 받아내며 도발 및 보스 스테이지의 그로기 대응에 유리.</summary>
    Tanker = 2,

    /// <summary>(3) Supporter - 아군에게 버프나 회복을 제공해 전투를 보조.</summary>
    Supporter = 3,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}