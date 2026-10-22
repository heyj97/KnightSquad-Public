using UnityEngine;

/// <summary>
/// 데미지를 받을 수 있는 모든 오브젝트에 적용하는 Interface
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>float Def</c> 전달받은 데미지를 감소시키는 데 사용하는 방어력</description></item>
/// </list>
///
/// <para><b>Method</b></para>
/// <list type="bullet">
/// <item><description><c>void TakeDmg(float dmg)</c> 전달받은 데미지와 자신의 방어 스탯을 기반으로 최종 피해량을 계산하여 적용</description></item>
/// </list>
///
/// </remarks>
public interface IDamageable : IHealth
{
    /// <summary>전달받은 데미지를 감소시키는 데 사용하는 방어력</summary>
    float Defense { get; }

    /// <summary>
    /// 공격 거리와 대상 판정에 사용할 피해 대상의 월드 위치를 가져옵니다.
    /// </summary>
    Vector3 MonPosition { get; }

    /// <summary>전달받은 데미지와 자신의 방어 스탯을 기반으로 최종 피해량을 계산하여 적용</summary>
    /// <param name="dmg">공격자가 계산하여 전달한 데미지</param>
    /// <param name="isCritical">치명타로 처리된 피해인지 여부입니다.</param>
    void TakeDmg(float dmg, bool isCritical = false);
}