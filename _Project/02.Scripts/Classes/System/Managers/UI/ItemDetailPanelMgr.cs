using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>공통 아이템 정보창. 자신은 활성 상태로 유지하고 Background만 표시·숨김 처리합니다.</summary>
public class ItemDetailPanelMgr : MonoBehaviour
{
    [SerializeField] private GameObject m_Background;
    [SerializeField] private Image m_ItemIconImg;
    [SerializeField] private Outline m_ItemIconOutline;
    [SerializeField] private Text m_ItemNameTxt;
    [SerializeField] private Text m_ItemRarityTxt;
    [SerializeField] private Text m_EquipTypeTxt;
    [SerializeField] private Text m_ItemPriceTxt;
    [Header("Stats")]
    [SerializeField] private Text m_ItemMaxHpTxt;
    [SerializeField] private Text m_ItemAtkTxt;
    [SerializeField] private Text m_ItemAtkSpdTxt;
    [SerializeField] private Text m_ItemDefTxt;
    [SerializeField] private Text m_ItemCritChanceTxt;
    [SerializeField] private Text m_ItemCritDmgMultTxt;
    [SerializeField] private Vector2 m_MouseOffset = new Vector2(18f, -18f);

    private Component m_Owner;
    private ItemData m_Item;
    private RectTransform m_Rect;
    private Canvas m_Canvas;
    private readonly Vector3[] m_Corners = new Vector3[4];

    private void Awake()
    {
        if (m_Background == null)
        {
            Transform background = InventorySlotUI.Find<Transform>(transform, "Background");
            if (background != null) m_Background = background.gameObject;
        }
        Transform root = m_Background != null ? m_Background.transform : transform;
        if (m_ItemIconImg == null) m_ItemIconImg = InventorySlotUI.Find<Image>(root, "ItemIconImg");
        if (m_ItemIconOutline == null) m_ItemIconOutline = InventorySlotUI.Find<Outline>(root, "ItemIconFrameImg");
        if (m_ItemNameTxt == null) m_ItemNameTxt = InventorySlotUI.Find<Text>(root, "ItemNameTxt");
        if (m_ItemRarityTxt == null) m_ItemRarityTxt = InventorySlotUI.Find<Text>(root, "ItemRarityTxt");
        if (m_EquipTypeTxt == null) m_EquipTypeTxt = InventorySlotUI.Find<Text>(root, "EquipTypeTxt");
        if (m_ItemPriceTxt == null) m_ItemPriceTxt = InventorySlotUI.Find<Text>(root, "ItemPriceTxt");
        if (m_ItemMaxHpTxt == null) m_ItemMaxHpTxt = InventorySlotUI.Find<Text>(root, "ItemMaxHpTxt");
        if (m_ItemAtkTxt == null) m_ItemAtkTxt = InventorySlotUI.Find<Text>(root, "ItemAtkTxt");
        if (m_ItemAtkSpdTxt == null) m_ItemAtkSpdTxt = InventorySlotUI.Find<Text>(root, "ItemAtkSpdTxt");
        if (m_ItemDefTxt == null) m_ItemDefTxt = InventorySlotUI.Find<Text>(root, "ItemDefTxt");
        if (m_ItemCritChanceTxt == null) m_ItemCritChanceTxt = InventorySlotUI.Find<Text>(root, "ItemCritChanceTxt");
        if (m_ItemCritDmgMultTxt == null) m_ItemCritDmgMultTxt = InventorySlotUI.Find<Text>(root, "ItemCritDmgMultTxt");
        m_Rect = m_Background != null ? m_Background.GetComponent<RectTransform>() : null;
        m_Canvas = GetComponentInParent<Canvas>();
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true)) graphic.raycastTarget = false;
        foreach (CanvasGroup group in root.GetComponentsInChildren<CanvasGroup>(true)) group.blocksRaycasts = false;
        HideAll();
    }

    public void Show(Component owner, ItemData item)
    {
        if (!isActiveAndEnabled || MessageBoxMgr.IsBlockingInput || owner == null || item == null ||
            m_Rect == null || m_Canvas == null || !ItemSaleMgr.TryFindItem(item.ItemUid, out ItemData current) ||
            current != item || item.Type != ItemType.Equipment || string.IsNullOrWhiteSpace(item.ItemId))
        { HideAll(); return; }
        MasterDataMgr master = FindFirstObjectByType<MasterDataMgr>();
        if (master == null || !master.TryGetEquipment(item.ItemId, out EquipmentData equipment) || equipment == null)
        { HideAll(); return; }
        m_Owner = owner;
        m_Item = item;
        Color color = GlobalConstants.GetRarityColor32(equipment.Rarity);
        if (m_ItemIconImg != null) m_ItemIconImg.sprite = equipment.Icon;
        if (m_ItemIconOutline != null) m_ItemIconOutline.effectColor = color;
        if (m_ItemNameTxt != null)
        {
            m_ItemNameTxt.text = ItemDetailText.FormatName(equipment.DisplayName, item.ItemLv);
            m_ItemNameTxt.color = color;
        }
        if (m_ItemRarityTxt != null)
        {
            m_ItemRarityTxt.text = GlobalConstants.GetKorRarityName(equipment.Rarity);
            m_ItemRarityTxt.color = color;
        }
        if (m_EquipTypeTxt != null)
            m_EquipTypeTxt.text = equipment.EquipType == EquipType.Weapon ? "무기" :
                equipment.EquipType == EquipType.Armor ? "방어구" : "액세서리";
        if (m_ItemPriceTxt != null) m_ItemPriceTxt.text = ItemDetailText.FormatPrice(item.Price);
        SetStat(m_ItemMaxHpTxt, "최대체력", equipment.BonusHp);
        SetStat(m_ItemAtkTxt, "공격력", equipment.BonusAttack);
        SetStat(m_ItemAtkSpdTxt, "공격속도", equipment.BonusAttackSpeed);
        SetStat(m_ItemDefTxt, "방어력", equipment.BonusDefense);
        SetStat(m_ItemCritChanceTxt, "치명타 확률", equipment.BonusCriticalChance * 100f, "%");
        SetStat(m_ItemCritDmgMultTxt, "치명타 피해", equipment.BonusCriticalDamageMultiplier * 100f, "%");
        m_Background.SetActive(true);
        transform.SetAsLastSibling();
        LayoutRebuilder.ForceRebuildLayoutImmediate(m_Rect);
        UpdatePosition();
    }

    private static void SetStat(Text text, string label, float value, string unit = "")
    {
        if (text == null) return;
        bool visible = ItemDetailText.IsVisible(value);
        text.gameObject.SetActive(visible);
        text.supportRichText = true;
        text.text = visible ? ItemDetailText.FormatStat(label, value, unit) : "";
    }

    private void LateUpdate()
    {
        if (m_Owner == null || m_Item == null) { HideAll(); return; }
        if (!m_Owner.gameObject.activeInHierarchy || MessageBoxMgr.IsBlockingInput ||
            Input.mouseScrollDelta.sqrMagnitude > 0f || Input.GetMouseButton(0) ||
            !ItemSaleMgr.TryFindItem(m_Item.ItemUid, out ItemData current) || current != m_Item)
        { HideAll(); return; }
        UpdatePosition();
    }

    private void UpdatePosition()
    {
        RectTransform parent = m_Rect.parent as RectTransform;
        if (parent == null) return;
        Camera camera = m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : m_Canvas.worldCamera;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
            (Vector2)Input.mousePosition + m_MouseOffset, camera, out Vector2 point)) return;
        m_Rect.position = parent.TransformPoint(point);
        m_Rect.GetWorldCorners(m_Corners);
        Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
        Vector2 max = new Vector2(float.MinValue, float.MinValue);
        foreach (Vector3 corner in m_Corners)
        {
            Vector2 screen = RectTransformUtility.WorldToScreenPoint(camera, corner);
            min = Vector2.Min(min, screen);
            max = Vector2.Max(max, screen);
        }
        Vector2 shift = new Vector2(min.x < 0 ? -min.x : max.x > Screen.width ? Screen.width - max.x : 0,
            min.y < 0 ? -min.y : max.y > Screen.height ? Screen.height - max.y : 0);
        if (shift == Vector2.zero) return;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parent,
            (Vector2)Input.mousePosition + m_MouseOffset + shift, camera, out point))
            m_Rect.position = parent.TransformPoint(point);
    }

    public void Hide(Component owner)
    {
        if (m_Owner == owner) HideAll();
    }

    public void HideAll()
    {
        m_Owner = null;
        m_Item = null;
        if (m_Background != null) m_Background.SetActive(false);
    }

    private void OnDisable() => HideAll();
}

internal static class ItemDetailText
{
    internal static bool IsVisible(float value)
        => value != 0f && !float.IsNaN(value) && !float.IsInfinity(value);

    internal static string FormatName(string name, int level)
        => level >= 2 ? $"+{level} {name}" : name;

    internal static string FormatPrice(int price)
        => $"판매가격: {price.ToString("N0", CultureInfo.InvariantCulture)} G";

    internal static string FormatStat(string label, float value, string unit = "")
    {
        string sign = value > 0f ? "+" : "-";
        string color = value > 0f ? "00ff00" : "ff0000";
        string number = System.Math.Abs(value).ToString("0.##", CultureInfo.InvariantCulture);
        if (number == "0") number = System.Math.Abs(value).ToString("G4", CultureInfo.InvariantCulture);
        return $"{label} <color=#{color}>{sign}{number}{unit}</color>";
    }
}
