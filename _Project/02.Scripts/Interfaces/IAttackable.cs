/// <summary>
/// 기본 공격 기능을 가지는 객체에 적용하는 인터페이스.
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>float AttPower</c> : 기본 공격에 사용되는 공격력
/// </description>
/// </item>
///
/// <item>
/// <description>
/// <c>float AttSpd</c> : 기본 공격의 공격속도
/// </description>
/// </item>
///
/// <item>
/// <description>
/// <c>bool CanAttack</c> : 현재 기본 공격 가능 여부
/// </description>
/// </item>
/// </list>
///
/// <para><b>Methods</b></para>
/// <list type="bullet">
/// <item>
/// <description>
/// <c>bool Attack(IDamageable target, float atkMult)</c> :
/// 지정된 대상에게 공격력 배율을 적용하여 기본 공격을 수행하고 성공 여부를 반환한다.
/// </description>
/// </item>
///
/// <item>
/// <description>
/// <c>bool Attack(IDamageable target)</c> :
/// 공격력 배율을 1.0으로 적용하는 기본 공격 오버로드.
/// </description>
/// </item>
/// </list>
///
/// </remarks>
public interface IAttackable
{
    /// <summary>
    /// 기본 공격에 사용되는 공격력
    /// </summary>
    float AttackPower { get; }

    /// <summary>
    /// 기본 공격의 공격속도
    /// </summary>
    float AttackSpeed { get; }

    /// <summary>
    /// 현재 기본 공격 가능 여부
    /// </summary>
    bool CanAttack { get; }

    /// <summary>
    /// <see cref="Attack(IDamageable, float)"/>의 오버로드로,
    /// 공격력 배율 1.0을 적용하여 기본 공격을 수행한다.
    /// </summary>
    /// <param name="target">공격할 대상</param>
    /// <returns>
    /// 공격이 정상적으로 실행되었으면 true,
    /// 공격할 수 없는 상태라면 false
    /// </returns>
    bool Attack(IDamageable target);
}