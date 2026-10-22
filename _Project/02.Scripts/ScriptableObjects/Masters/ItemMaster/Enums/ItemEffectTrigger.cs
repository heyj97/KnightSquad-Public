/// <summary>
/// 아이템 특수효과의 발동 조건.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) OnHit - 공격 시 발동</description></item>
/// <item><description>(1) OnDamaged - 피격 시 발동</description></item>
/// <item><description>(2) OnCriticalHit - 치명타 공격 시 발동</description></item>
/// <item><description>(3) OnKill - 적 처치 시 발동</description></item>
/// </list>
/// </remarks>
public enum ItemEffectTrigger
{
    /// <summary>(0) OnHit - 공격 시 발동</summary>
    OnHit,

    /// <summary>(1) OnDamaged - 피격 시 발동</summary>
    OnDamaged,

    /// <summary>(2) OnCriticalHit - 치명타 공격 시 발동</summary>
    OnCriticalHit,

    /// <summary>(3) OnKill - 적 처치 시 발동</summary>
    OnKill,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}
