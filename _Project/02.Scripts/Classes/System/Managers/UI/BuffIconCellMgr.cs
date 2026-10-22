using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 활성 버프의 아이콘과 남은 지속 시간을 표시하는 UI 셀입니다.
/// </summary>
public class BuffIconCellMgr : MonoBehaviour
{
    [SerializeField] private Image m_SkillIconImg;
    [SerializeField] private Image m_BuffDurationImg;

    /// <summary>
    /// 스킬 아이콘과 전체·남은 시간을 적용하여 버프 UI 셀을 초기화합니다.
    /// </summary>
    /// <param name="skillIcon">버프 셀에 표시할 스킬 아이콘입니다.</param>
    /// <param name="totalDuration">버프가 처음 적용될 때의 전체 지속 시간입니다.</param>
    /// <param name="remainingDuration">현재 남아 있는 버프 지속 시간입니다.</param>
    public void Initialize(Sprite skillIcon, float totalDuration, float remainingDuration)
    {
        SetBuffIcon(skillIcon);
        SetBuffDuration(totalDuration, remainingDuration);
    }

    /// <summary>
    /// 버프 UI 셀에 표시할 스킬 아이콘을 교체합니다.
    /// </summary>
    /// <param name="skillIcon">버프 셀에 표시할 스킬 아이콘입니다.</param>
    public void SetBuffIcon(Sprite skillIcon)
    {
        if (m_SkillIconImg != null)
            m_SkillIconImg.sprite = skillIcon;
    }

    /// <summary>
    /// 전체 시간 대비 남은 버프 시간 비율로 지속 시간 게이지를 갱신합니다.
    /// </summary>
    /// <param name="totalDuration">버프가 처음 적용될 때의 전체 지속 시간입니다.</param>
    /// <param name="remainingDuration">현재 남아 있는 버프 지속 시간입니다.</param>
    public void SetBuffDuration(float totalDuration, float remainingDuration)
    {
        if (m_BuffDurationImg == null)
            return;

        m_BuffDurationImg.fillAmount = totalDuration > 0f
            ? 1f - Mathf.Clamp01(remainingDuration / totalDuration)
            : 0f;
    }
}
