/// <summary>
/// 스킬의 타겟 지정 방식.
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>(0) Single</c> 단일 대상을 지정하는 스킬</description></item>
/// <item><description><c>(1) Random</c> 대상 중 무작위로 타겟을 지정하는 스킬</description></item>
/// <item><description><c>(2) Global</c> 조건에 해당하는 모든 대상을 지정하는 스킬</description></item>
/// </list>
///
/// </remarks>
public enum SkillTargetType
{
    /// <summary>(0) Single - 단일 대상을 지정하는 스킬</summary>
    Single = 0,

    /// <summary>(1) Random - 대상 중 무작위로 타겟을 지정하는 스킬</summary>
    Random = 1,

    /// <summary>(2) Global - 조건에 해당하는 모든 대상을 지정하는 스킬</summary>
    Global = 2,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}