using System;
using UnityEngine;
using UnityEngine.UI;

public class GachaPityTargetCellMgr : MonoBehaviour
{
    [SerializeField] private Button m_SelectBtn;
    [SerializeField] private Image m_CharIconImg;
    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private GameObject m_SelectedMark;
    private Action m_Select;

    public void Initialize(CharacterData data, bool selected, Action onSelect)
    {
        m_Select = onSelect;
        if (m_SelectBtn != null) { m_SelectBtn.onClick.RemoveListener(Select); m_SelectBtn.onClick.AddListener(Select); }
        if (m_CharIconImg != null) m_CharIconImg.sprite = data.Icon;
        if (m_CharNameTxt != null) { m_CharNameTxt.text = data.DisplayName; m_CharNameTxt.color = GlobalConstants.GetRarityColor32(data.Rarity); }
        if (m_SelectedMark != null) m_SelectedMark.SetActive(selected);
    }
    private void Select() => m_Select?.Invoke();
    private void OnDestroy()
    {
        if (m_SelectBtn != null) m_SelectBtn.onClick.RemoveListener(Select);
        m_Select = null;
    }
}
