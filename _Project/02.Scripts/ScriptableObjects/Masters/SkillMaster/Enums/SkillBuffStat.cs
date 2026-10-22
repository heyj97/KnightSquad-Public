/// <summary>
/// 스킬 버프로 증가시키는 스텟의 종류.
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>(0) None</c> 버프 스킬이 아닐 때 사용하는 기본값</description></item>
/// <item><description><c>(1) MaxHp</c> 최대 체력을 증가시키는 버프</description></item>
/// <item><description><c>(2) AtkPower</c> 공격력을 증가시키는 버프</description></item>
/// <item><description><c>(3) AtkSpeed</c> 공격속도를 증가시키는 버프</description></item>
/// <item><description><c>(4) DefPower</c> 방어력을 증가시키는 버프</description></item>
/// <item><description><c>(5) CritChance</c> 치명타 확률을 증가시키는 버프</description></item>
/// <item><description><c>(6) CritDmg</c> 치명타 데미지 배율을 증가시키는 버프</description></item>
/// </list>
///
/// </remarks>
public enum SkillBuffStat
{
    /// <summary>(0) None - 버프 스킬이 아닐 때 사용하는 기본값</summary>
    None = 0,

    /// <summary>(1) MaxHp - 최대 체력을 증가시키는 버프</summary>
    MaxHp = 1,

    /// <summary>(2) AtkPower - 공격력을 증가시키는 버프</summary>
    AtkPower = 2,

    /// <summary>(3) AtkSpeed - 공격속도를 증가시키는 버프</summary>
    AtkSpeed = 3,

    /// <summary>(4) DefPower - 방어력을 증가시키는 버프</summary>
    DefPower = 4,

    /// <summary>(5) CritChance - 치명타 확률을 증가시키는 버프</summary>
    CritChance = 5,

    /// <summary>(6) CritDmg - 치명타 데미지 배율을 증가시키는 버프</summary>
    CritDmg = 6,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}