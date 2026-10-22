/// <summary>
/// 회복이 가능한 모든 오브젝트에 적용하는 Interface
/// </summary>
/// <remarks>
///
/// <para><b>Methods</b></para>
/// <list type="bullet">
/// <item><description><c>void Heal(float amount)</c> 대상의 체력을 지정된 수치만큼 회복</description></item>
/// </list>
///
/// </remarks>
public interface IHealable : IHealth
{
    /// <summary>대상의 체력을 지정된 수치만큼 회복</summary>
    /// <param name="amount">회복하거나 증감할 수량입니다.</param>
    void Heal(float amount);
}