using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 사용자 아이템이나 마스터 아이템 데이터를 인벤토리 셀에 표시합니다.
/// </summary>
public class ItemCellMgr : MonoBehaviour
{
    /// <summary>
    /// 아이템 마스터에 설정된 아이콘을 표시하는 이미지 UI 참조입니다.
    /// </summary>
    public Image m_Icon;
    /// <summary>
    /// 마스터 데이터만으로 초기화한 셀이 표시하는 아이템 정보입니다.
    /// </summary>
    public ItemParentSO m_ItemData;
    /// <summary>
    /// 사용자 인벤토리 데이터로 초기화한 셀이 표시하는 소유 상태입니다.
    /// </summary>
    public ItemData m_UserItemData;

    [Header("Slot Menu")]
    [SerializeField] private Button m_SlotBtn;
    [SerializeField] private GameObject m_OnSlotClickPanel;
    [SerializeField] private Button m_EquipBtn;
    [SerializeField] private Button m_SellBtn;
    [SerializeField] private Button m_CancelBtn;
    [SerializeField] private Image m_IconFrameImg;
    [SerializeField] private Image m_EqCharProfileImg;
    [SerializeField] private Outline m_PartyOutline;
    [SerializeField] private Color m_EquippedColor = new Color(1f, 0.75f, 0.25f, 1f);
    private Action<string> m_OnEquip, m_OnSell;
    private static ItemCellMgr s_OpenMenu;
    private ItemHoverRelay m_HoverRelay;

    private void Awake()
    {
        // 기존 프리팹 이름으로 미연결 참조를 찾는다. Inspector 연결값이 우선한다.
        if (m_SlotBtn == null) m_SlotBtn = InventorySlotUI.Find<Button>(transform, "SlotBG");
        if (m_OnSlotClickPanel == null)
        {
            Transform panel = InventorySlotUI.Find<Transform>(transform, "OnSlotClickPanel");
            if (panel != null) m_OnSlotClickPanel = panel.gameObject;
        }
        if (m_EquipBtn == null) m_EquipBtn = InventorySlotUI.Find<Button>(transform, "EquipBtn");
        if (m_SellBtn == null) m_SellBtn = InventorySlotUI.Find<Button>(transform, "SellBtn");
        if (m_CancelBtn == null) m_CancelBtn = InventorySlotUI.Find<Button>(transform, "CancelBtn");
        if (m_IconFrameImg == null) m_IconFrameImg = InventorySlotUI.Find<Image>(transform, "IconFrameImg");
        if (m_EqCharProfileImg == null) m_EqCharProfileImg = InventorySlotUI.Find<Image>(transform, "EqCharProfileImg");
        if (m_PartyOutline == null && m_IconFrameImg != null) m_PartyOutline = m_IconFrameImg.GetComponent<Outline>();
        if (m_SlotBtn != null) m_SlotBtn.onClick.AddListener(OpenMenu);
        if (m_EquipBtn != null) m_EquipBtn.onClick.AddListener(EquipClick);
        if (m_SellBtn != null) m_SellBtn.onClick.AddListener(SellClick);
        if (m_CancelBtn != null) m_CancelBtn.onClick.AddListener(CloseMenu);
        // SlotBG와 형제인 장식 이미지가 버튼의 피킹을 가로채지 않도록 한다.
        foreach (Graphic graphic in GetComponentsInChildren<Graphic>(true))
            if (graphic.GetComponentInParent<Button>() == null &&
                (m_OnSlotClickPanel == null || !graphic.transform.IsChildOf(m_OnSlotClickPanel.transform)))
                graphic.raycastTarget = false;
        CloseMenu();
        if (m_SlotBtn != null)
        {
            m_HoverRelay = m_SlotBtn.GetComponent<ItemHoverRelay>();
            if (m_HoverRelay == null) m_HoverRelay = m_SlotBtn.gameObject.AddComponent<ItemHoverRelay>();
            m_HoverRelay.Bind(this, () => m_UserItemData,
                () => m_OnSlotClickPanel == null || !m_OnSlotClickPanel.activeInHierarchy);
        }
    }

    private void OnDisable()
    {
        m_HoverRelay?.Hide();
        CloseMenu();
    }

    private void OnDestroy()
    {
        if (m_SlotBtn != null) m_SlotBtn.onClick.RemoveListener(OpenMenu);
        if (m_EquipBtn != null) m_EquipBtn.onClick.RemoveListener(EquipClick);
        if (m_SellBtn != null) m_SellBtn.onClick.RemoveListener(SellClick);
        if (m_CancelBtn != null) m_CancelBtn.onClick.RemoveListener(CloseMenu);
    }

    public void BindActions(Action<string> onEquip, Action<string> onSell)
    {
        m_OnEquip = onEquip;
        m_OnSell = onSell;
    }

    public void SetEquippedCharacter(string charId)
    {
        Sprite profile = null;
        if (!string.IsNullOrEmpty(charId))
        {
            MasterDataMgr master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
            if (master != null && master.TryGetCharacter(charId, out CharacterData character) && character != null)
                profile = character.Icon;
        }
        bool equipped = !string.IsNullOrEmpty(charId);
        bool inParty = false;
        foreach (string id in GlobalValue.PartyCharIdList)
            if (equipped && id == charId) inParty = true;
        if (m_IconFrameImg != null)
        {
            m_IconFrameImg.gameObject.SetActive(equipped);
            m_IconFrameImg.color = inParty ? m_EquippedColor : Color.gray;
            m_IconFrameImg.raycastTarget = false;
        }
        if (m_EqCharProfileImg != null)
        {
            m_EqCharProfileImg.sprite = profile;
            m_EqCharProfileImg.gameObject.SetActive(equipped);
            m_EqCharProfileImg.raycastTarget = false;
        }
        if (m_PartyOutline != null) m_PartyOutline.enabled = inParty;
        // 착용 장비도 판매 버튼을 클릭하여 안내를 볼 수 있다.
        if (m_SellBtn != null) m_SellBtn.interactable = true;
    }

    private void OpenMenu()
    {
        if (MessageBoxMgr.IsBlockingInput || m_UserItemData == null || m_OnSlotClickPanel == null ||
            (m_OnEquip == null && m_OnSell == null)) return; // 보상 기록 셀은 호버만 제공한다.
        m_HoverRelay?.Hide();
        if (s_OpenMenu != null && s_OpenMenu != this) s_OpenMenu.CloseMenu();
        s_OpenMenu = this;
        m_OnSlotClickPanel.SetActive(true);
        m_OnSlotClickPanel.transform.SetAsLastSibling();
    }

    public void CloseMenu()
    {
        if (m_OnSlotClickPanel != null) m_OnSlotClickPanel.SetActive(false);
        if (s_OpenMenu == this) s_OpenMenu = null;
    }

    private void EquipClick()
    {
        if (MessageBoxMgr.IsBlockingInput || m_UserItemData == null) return;
        string uid = m_UserItemData.ItemUid;
        CloseMenu();
        m_OnEquip?.Invoke(uid);
    }

    private void SellClick()
    {
        if (MessageBoxMgr.IsBlockingInput || m_UserItemData == null) return;
        string uid = m_UserItemData.ItemUid;
        CloseMenu();
        m_OnSell?.Invoke(uid);
    }

    private void ClearItem()
    {
        m_HoverRelay?.Hide();
        CloseMenu();
        m_UserItemData = null;
        m_ItemData = null;
        if (m_Icon != null) m_Icon.sprite = null;
        SetEquippedCharacter("");
    }

    /// <summary>
    /// 사용자 소유 데이터 또는 마스터 데이터에서 장비 정보를 찾아 아이콘과 셀 상태를 초기화합니다.
    /// </summary>
    /// <param name="itData">셀에 표시할 사용자 또는 마스터 아이템 데이터입니다.</param>
    public void InitializeItem(ItemData itData)
    {
        ClearItem();
        if (itData == null || string.IsNullOrWhiteSpace(itData.ItemUid) ||
            string.IsNullOrWhiteSpace(itData.ItemId)) return;
        if (itData.Type == ItemType.Equipment)
        {
            bool isEquipment = MasterDataMgr.Inst.TryGetEquipment(itData.ItemId, out EquipmentData equipData);
            if (!isEquipment)
            {
                PDebug.LogError("아이템 정보를 불러오는 데에 실패했습니다.");
                return;
            }

            m_UserItemData = itData;
            m_ItemData = equipData;
            m_Icon.sprite = equipData.Icon;
        }
    }
    
    /// <summary>
    /// 사용자 소유 데이터 또는 마스터 데이터에서 장비 정보를 찾아 아이콘과 셀 상태를 초기화합니다.
    /// </summary>
    /// <param name="itData">셀에 표시할 사용자 또는 마스터 아이템 데이터입니다.</param>
    public void InitializeItem(ItemParentSO itData)
    {
        ClearItem();
        if (itData == null) return;
        if (itData.Type == ItemType.Equipment)
        {
            bool isEquipment = MasterDataMgr.Inst.TryGetEquipment(itData.Id, out EquipmentData equipData);
            if (!isEquipment)
            {
                PDebug.LogError("아이템 정보를 불러오는 데에 실패했습니다.");
                return;
            }

            m_ItemData = equipData;
            m_Icon.sprite = equipData.Icon;
        }
    }
}

internal static class InventorySlotUI
{
    internal static T Find<T>(Transform root, string name) where T : Component
    {
        if (root == null) return null;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            if (child.name == name) return child.GetComponent<T>();
        return null;
    }

    internal static void ClearContent(Transform content)
    {
        if (content == null) return;
        // 루트 셀만 제거한다. 중첩된 버튼·이미지를 개별 삭제하지 않는다.
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            GameObject child = content.GetChild(i).gameObject;
            child.SetActive(false);
            UnityEngine.Object.Destroy(child);
        }
    }
}
