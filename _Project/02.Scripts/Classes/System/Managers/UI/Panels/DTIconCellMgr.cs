using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 레이드 드롭 테이블의 장비 아이콘과 등급 프레임을 표시합니다.
/// </summary>
public class DTIconCellMgr : MonoBehaviour
{
    /// <summary>
    /// 프레임을 표시하는 이미지 UI 참조입니다.
    /// </summary>
    public Image m_Frame;
    /// <summary>
    /// 아이콘을 표시하는 이미지 UI 참조입니다.
    /// </summary>
    public Image m_Icon;
    
    /// <summary>
    /// 레이드 드롭 장비의 등급 프레임과 아이콘으로 셀을 초기화합니다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    public void Initialize(EquipmentData data)
    {
        m_Icon.sprite = data.Icon;
        Outline a_Outline = m_Frame.GetComponent<Outline>();
        Color32 a_RarityColor = GlobalConstants.GetRarityColor32(data.Rarity);
        a_RarityColor.a = 128;
        a_Outline.effectColor = a_RarityColor;
    }
}
