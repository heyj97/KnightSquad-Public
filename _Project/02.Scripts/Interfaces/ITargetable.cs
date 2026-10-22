using UnityEngine;

using System;

/// <summary>
/// 타겟팅이 가능한 모든 오브젝트에 적용하는 Interface
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>bool IsTargetable</c> 현재 타겟으로 지정할 수 있는 상태인지 여부</description></item>
/// </list>
///
/// </remarks>
public interface ITargetable
{
    /// <summary>현재 타겟으로 지정할 수 있는 상태인지 여부</summary>
    bool IsTargetable { get; }

    /// <summary>사망으로 더 이상 타겟으로 지정할 수 없을 때 발생</summary>
    event Action<ITargetable> OnCharTargetableDisabled;
}
