using UnityEngine;

/// <summary>
/// Sample 캐릭터의 전용 동작을 구현하는 클래스
/// </summary>
public class SampleCharacter : CharacterBase
{
    protected override void OnInitialized()
    {
        // PDebug.Log(m_CharData.DisplayName);

        // foreach (SkillData skill in Skills)
        //     PDebug.Log(skill.Id);
    }

    protected override void Death()
    {
        PlayDeathAnimation();
    }
}
