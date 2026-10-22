using UnityEngine;
using UnityEngine.UI;

public class GachaResultCellMgr : MonoBehaviour
{
    [SerializeField] private Image m_CharIconImg;
    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private Text m_CharRarityTxt;
    [SerializeField] private Text m_ResultDescTxt;
    [SerializeField] private GameObject m_PityMark;

    public void Initialize(CharacterData data, CharacterGachaResult result)
    {
        if (m_CharIconImg != null) m_CharIconImg.sprite = data.Icon;
        if (m_CharNameTxt != null) { m_CharNameTxt.text = data.DisplayName; m_CharNameTxt.color = GlobalConstants.GetRarityColor32(data.Rarity); }
        if (m_CharRarityTxt != null) { m_CharRarityTxt.text = GlobalConstants.GetKorRarityName(data.Rarity); m_CharRarityTxt.color = GlobalConstants.GetRarityColor32(data.Rarity); }
        if (m_PityMark != null) m_PityMark.SetActive(result.IsPity);
        string text = result.IsNew ? "신규 획득" : result.AfterLevel > result.BeforeLevel
            ? $"레벨 상승: Lv.{result.BeforeLevel} → Lv.{result.AfterLevel}"
            : $"중복 획득: {result.DuplicateCount}개 보유";
        if (result.StarFragments > 0) text += $"\n별의 조각 {result.StarFragments:N0}개 전환 예정";
        if (m_ResultDescTxt != null) m_ResultDescTxt.text = text;
    }
}
