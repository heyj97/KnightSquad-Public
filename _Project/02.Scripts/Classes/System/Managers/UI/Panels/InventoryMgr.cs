using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 보유 아이템 목록을 인벤토리 패널에 생성하고 개수를 표시합니다.
/// </summary>
public class InventoryMgr : MonoBehaviour
{
    /// <summary>
    /// 인벤토리 패널에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    public GameObject InventoryPanel;
    /// <summary>
    /// 닫기 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_CloseBtn;
    /// <summary>
    /// 아이템 슬롯에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    public GameObject m_ItemSlot;
    // public ItemCellMgr m_ItemCellMgr;
    /// <summary>
    /// 생성된 아이템 슬롯들을 스크롤 영역에 배치하는 부모 Transform입니다.
    /// </summary>
    public Transform m_Content;
    /// <summary>
    /// 아이템 개수를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    [FormerlySerializedAs("m_ItemCounTxt")] public Text m_ItemCountTxt;


    void Start()
    {
        if (m_CloseBtn != null)
            m_CloseBtn.onClick.AddListener(() => InventoryPanel.SetActive(false));
    }

    void OnEnable()
    {
        RefreshInventory();
        GlobalValue.OnInventoryChanged += RefreshInventory;
        EquipmentMgr.OnEquipmentChanged += RefreshInventory;
        GlobalValue.OnPartyCompositionChanged += RefreshInventory;
    }

    void OnDisable()
    {
        GlobalValue.OnInventoryChanged -= RefreshInventory;
        EquipmentMgr.OnEquipmentChanged -= RefreshInventory;
        GlobalValue.OnPartyCompositionChanged -= RefreshInventory;
    }

    void RefreshInventory()
    {
        if (m_Content == null || m_ItemSlot == null) return;
        ItemSaleMgr.InitializePrices(GlobalValue.UserItemDataList);
        InventorySlotUI.ClearContent(m_Content);

        // 원본 목록은 유지하고 표시만 미착용 → 착용 순서로 안정 정렬한다.
        var sortedItems = GlobalValue.UserItemDataList
            .Where(item => item != null && item.Type == ItemType.Equipment)
            .Select(item =>
            {
                EquipmentMgr.TryGetEquippedCharacter(item.ItemUid, out string owner);
                return (Item: item, Owner: owner);
            })
            .OrderBy(entry => string.IsNullOrEmpty(entry.Owner));

        foreach (var entry in sortedItems)
        {
            GameObject _Item = Instantiate(m_ItemSlot, m_Content);
            ItemCellMgr cell = _Item.GetComponent<ItemCellMgr>();
            if (cell == null) { Destroy(_Item); continue; }
            cell.BindActions(EquipClick, SellClick);
            cell.InitializeItem(entry.Item);
            cell.SetEquippedCharacter(entry.Owner);
        }


        if (m_ItemCountTxt != null)
            m_ItemCountTxt.text = $"{GlobalValue.UserItemDataList.Count} / {GlobalConstants.InventoryMaxSlotCount}";
    }

    private void EquipClick(string uid)
    {
        if (!ItemSaleMgr.TryFindItem(uid, out _)) return;
        InformationPanelMgr manager = FindFirstObjectByType<InformationPanelMgr>();
        if (manager != null) manager.OpenCollectionPanel();
    }

    private void SellClick(string uid)
    {
        if (!ItemSaleMgr.TryGetSaleInfo(uid, null, out ItemData item,
                out EquipmentData equipment, out string message))
        { ItemSaleMgr.ShowMessage(message); return; }
        int price = item.Price;
        ItemSaleMgr.ShowDialog($"{equipment.DisplayName} 아이템을 {price:N0} G에 판매하시겠습니까?",
            MessageBoxType.Submit, () =>
            {
                if (!isActiveAndEnabled) return;
                if (!ItemSaleMgr.TrySell(uid, price, out string result)) ItemSaleMgr.ShowMessage(result);
            });
    }
}
