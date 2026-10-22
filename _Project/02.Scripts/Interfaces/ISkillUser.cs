using System.Collections.Generic;

/// <summary>
/// 스킬이 있는 모든 오브젝트에 적용하는 Interface
/// </summary>
/// <remarks>
///
/// <para><b>Variables</b></para>
/// <list type="bullet">
/// <item><description><c>bool CanUseSkill</c> 현재 스킬을 사용할 수 있는 상태인지 여부</description></item>
/// <item><description><c>List<SkillData> Skills</c> 스킬 리스트</description></item>
/// </list>
///
/// <para><b>Methods</b></para>
/// <list type="bullet">
/// <item><description><c>void UseSkill(int skillIdx)</c> 지정된 인덱스의 스킬을 실행</description></item>
/// </list>
///
/// </remarks>
public interface ISkillUser
{
    /// <summary>현재 스킬을 사용할 수 있는 상태인지 여부</summary>
    bool CanUseSkill { get; }
    /// <summary>
    /// 사용자가 선택하고 실행할 수 있는 스킬 목록을 읽기 전용으로 제공합니다.
    /// </summary>
    IReadOnlyList<SkillData> SkillsList { get; }

    /// <summary>지정된 인덱스의 스킬을 실행</summary>
    /// <param name="skillIdx">보유 스킬 목록에서 사용할 0부터 시작하는 슬롯 인덱스입니다.</param>
    void UseSkill(int skillIdx);
}