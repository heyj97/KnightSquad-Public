/// <summary>
/// 체력이 있는 모든 오브젝트에 적용하는 Interface
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>float MaxHp</c> 대상이 가질 수 있는 최대 체력</description></item>
/// <item><description><c>float CurHp</c> 대상의 현재 체력</description></item>
/// <item><description><c>bool IsDead</c> 현재 사망 상태인지 여부</description></item>
/// </list>
///
/// </remarks>
public interface IHealth
{
    /// <summary>대상이 가질 수 있는 최대 체력</summary>
    float MaxHp { get; }

    /// <summary>대상의 현재 체력</summary>
    float CurHp { get; }

    /// <summary>현재 사망 상태인지 여부</summary>
    bool IsDead { get; }
}