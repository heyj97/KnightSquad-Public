using System;
using UnityEngine;
using UnityEngine.UI;

public class EquipCellMgr : MonoBehaviour
{
    [Header("Cell Settings")]
    [SerializeField] private Image m_ItemFrameImg;
    [SerializeField] private Transform m_ItemContent;

    [Header("Item Settings")]
    [SerializeField] private Transform m_StarGroup;
    [SerializeField] private Image m_ItemImg;
    [SerializeField] private Text m_ItemLvTxt;

    [Header("Cell Events")]
    [SerializeField] private Button m_CellBtn;
    [SerializeField] private Transform m_OnEquipClickPanel;
    [SerializeField] private Button m_ClearEquipBtn;
    [SerializeField] private Button m_CancelBtn;

    private ItemData m_ItemData;
    private CharData m_CharData;
    private EquipType m_EquipType;
    private CharDetailPanelMgr m_DetailPanel;
    private ItemHoverRelay m_HoverRelay;

    /// <summary>씬 간 Inspector 참조 없이 패널 소유자가 편집 요청 대상을 연결합니다.</summary>
    public void BindDetailPanel(CharDetailPanelMgr panel) => m_DetailPanel = panel;

    private void Awake()
    {
        if (m_CellBtn == null) m_CellBtn = GetComponent<Button>();
        if (m_CellBtn != null)
            m_CellBtn.onClick.AddListener(() =>
            {
                if (m_ItemData != null && m_CharData != null && m_OnEquipClickPanel != null)
                {
                    m_HoverRelay?.Hide();
                    m_OnEquipClickPanel.gameObject.SetActive(true);
                }
            });

        if (m_CancelBtn != null)
            m_CancelBtn.onClick.AddListener(() =>
            {
                if (m_OnEquipClickPanel != null) m_OnEquipClickPanel.gameObject.SetActive(false);
            });

        if (m_ClearEquipBtn != null)
            m_ClearEquipBtn.onClick.AddListener(() => ClearEquipBtnClick(m_EquipType));
        GameObject hoverTarget = m_CellBtn != null ? m_CellBtn.gameObject : gameObject;
        m_HoverRelay = hoverTarget.GetComponent<ItemHoverRelay>();
        if (m_HoverRelay == null) m_HoverRelay = hoverTarget.AddComponent<ItemHoverRelay>();
        m_HoverRelay.Bind(this, () => m_ItemData,
            () => m_OnEquipClickPanel == null || !m_OnEquipClickPanel.gameObject.activeInHierarchy);
    }

    private void OnDisable() => m_HoverRelay?.Hide();

    public void InitializeEquipCell(ItemData itemData, CharData charData)
    {
        ClearEquipCell();

        // null은 정상적인 미착용 슬롯이다.
        if (itemData == null || charData == null)
            return;

        if (itemData.Type != ItemType.Equipment ||
            string.IsNullOrWhiteSpace(itemData.ItemUid) ||
            string.IsNullOrWhiteSpace(itemData.ItemId))
        {
            PDebug.LogError("장비 셀에 전달된 아이템 정보가 유효하지 않습니다.");
            return;
        }

        if (!MasterDataMgr.Inst.TryGetEquipment(
                itemData.ItemId, out EquipmentData a_EquipData) ||
            a_EquipData == null || a_EquipData.EquipType < EquipType.Weapon ||
            a_EquipData.EquipType >= EquipType.Count)
        {
            PDebug.LogError($"장비 데이터를 조회할 수 없습니다: {itemData.ItemId}");
            return;
        }

        if (m_DetailPanel != null &&
            (!m_DetailPanel.IsEditing || m_DetailPanel.GetEquipmentUid(
                charData.CharId, a_EquipData.EquipType) != itemData.ItemUid))
        {
            PDebug.LogError("장비 셀과 상세보기 편집 상태가 일치하지 않습니다.");
            return;
        }

        SetStarActive(a_EquipData.Rarity);
        ItemFrameColorCtrl(a_EquipData.Rarity);
        m_ItemImg.sprite = a_EquipData.Icon;
        m_ItemLvTxt.text = itemData.ItemLv > 1 ? $"+{itemData.ItemLv}" : "";

        m_ItemData = itemData;
        m_EquipType = a_EquipData.EquipType;
        m_CharData = charData;

        m_ItemContent.gameObject.SetActive(true);
    }

    void ClearEquipCell()
    {
        m_HoverRelay?.Hide();
        m_ItemData = null;
        m_CharData = null;
        m_EquipType = EquipType.Count;
        if (m_OnEquipClickPanel != null) m_OnEquipClickPanel.gameObject.SetActive(false);
        ItemFrameColorCtrl(SystemRarity.Common);
        m_ItemImg.sprite = null;
        m_ItemLvTxt.text = "";

        if (m_StarGroup != null)
        {
            for (int i = 0; i < m_StarGroup.childCount; i++)
                m_StarGroup.GetChild(i).gameObject.SetActive(false);
        }

        m_ItemContent.gameObject.SetActive(false);
    }

    void SetStarActive(SystemRarity rarity)
    {
        if (m_StarGroup == null)
            return;

        int a_StarCount = (int)rarity + 1;

        for (int i = 0; i < m_StarGroup.childCount; i++)
        {
            m_StarGroup.GetChild(i).gameObject.SetActive(i < a_StarCount);
        }
    }

    void ItemFrameColorCtrl(SystemRarity rarity)
    {
        m_ItemFrameImg.color = GlobalConstants.GetRarityColor32(rarity);
    }

    void ClearEquipBtnClick(EquipType a_EquipType)
    {
        if (m_ItemData == null || m_CharData == null)
            return;
        if (m_DetailPanel == null)
        {
            PDebug.LogError("EquipCellMgr에 상세보기 편집 패널을 연결해주세요.");
            return;
        }
        if (m_DetailPanel.TryUnequipItem(m_CharData.CharId, a_EquipType, m_ItemData.ItemUid))
            ClearEquipCell();
    }
}
