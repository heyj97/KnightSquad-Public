/// <summary>
/// 스킬의 기능 타입.
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>(0) Damage</c> 대상에게 데미지를 주는 공격형 스킬</description></item>
/// <item><description><c>(1) Buff</c> 대상에게 강화 효과를 부여하는 버프형 스킬</description></item>
/// </list>
///
/// </remarks>
public enum SkillType
{
    /// <summary>(0) Damage - 대상에게 데미지를 주는 공격형 스킬</summary>
    Damage = 0,

    /// <summary>(1) Buff - 대상에게 강화 효과를 부여하는 버프형 스킬</summary>
    Buff = 1,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}