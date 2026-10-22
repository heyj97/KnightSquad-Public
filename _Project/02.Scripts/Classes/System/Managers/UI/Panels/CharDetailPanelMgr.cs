using System;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>선택 캐릭터의 정보·스킬·편집 능력치와 장비·판매 편집을 표시합니다.</summary>
public class CharDetailPanelMgr : MonoBehaviour
{
    [Header("Panel Buttons")]
    [SerializeField] private Button m_SaveBtn;
    [SerializeField] private Button m_CloseBtn;

    [Header("Detail Inventory")]
    [SerializeField] private Transform m_InventoryContent;
    [SerializeField] private GameObject m_InventorySlotPrefab;
    [Tooltip("무기, 방어구, 액세서리 순서")]
    [SerializeField] private EquipCellMgr[] m_EquipmentCells = new EquipCellMgr[3];

    [Header("Character Display")]
    [SerializeField] private Image m_CharFrameImg;
    [SerializeField] private Image m_CharIllustImg;
    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private Text m_CharSubNameTxt;
    [SerializeField] private Transform m_CharStarGroup;
    [SerializeField] private Image m_CharRoleImg;
    [SerializeField] private Text m_CharRoleTxt;
    [SerializeField] private Sprite[] m_CharRoleSprites = new Sprite[4];
    [SerializeField] private Text m_LevelInfoTxt;
    [SerializeField] private Image m_GageImg;
    [SerializeField] private Text m_GageTxt;
    [Tooltip("체력, 공격력, 공격속도, 방어력, 치명타 확률, 치명타 증폭 순서")]
    [SerializeField] private Text[] m_StatTexts = new Text[6];
    [SerializeField] private Transform m_SkillsGroup;
    private readonly GameObject[] m_SkillCells = new GameObject[4];
    private readonly Image[] m_SkillIcons = new Image[4];
    private readonly Text[] m_SkillNames = new Text[4];
    private readonly Text[] m_SkillDescriptions = new Text[4];
    private CollectionPanelMgr m_CollectionDisplay;

    private EquipmentEditSession m_EditSession;
    public string SelectedCharacterId { get; private set; } = "";
    public bool IsEditing => m_EditSession != null && !m_EditSession.IsClosed;
    public string LastMessage { get; private set; } = "";
    public bool HasChanges => IsEditing && m_EditSession.HasChanges;

    // 표시·메시지 UI는 구독자가 구현한다. 이벤트를 통한 갱신은 실제 장착 정보를 변경하지 않는다.
    public event Action OnEditChanged;
    public event Action OnSaved;
    /// <summary>사용자 저장·닫기 완료에만 발생합니다. 상위 메뉴 종료에는 발생하지 않습니다.</summary>
    public event Action<bool> OnClosed;
    public event Action<string> OnOperationFailed;

    private void Awake()
    {
        Transform buttonGroup = InventorySlotUI.Find<Transform>(transform, "PanelBtnGroup");
        if (m_SaveBtn == null) m_SaveBtn = InventorySlotUI.Find<Button>(buttonGroup, "SaveBtn");
        if (m_CloseBtn == null) m_CloseBtn = InventorySlotUI.Find<Button>(buttonGroup, "CloseBtn");
        Transform scroll = InventorySlotUI.Find<Transform>(transform, "InventorySV");
        if (m_InventoryContent == null) m_InventoryContent = InventorySlotUI.Find<Transform>(scroll, "Content");
        BindCharacterUI();
        if (m_SaveBtn != null) m_SaveBtn.onClick.AddListener(SaveClick);
        if (m_CloseBtn != null) m_CloseBtn.onClick.AddListener(CloseClick);
        OnEditChanged += RefreshEditUI;
    }

    private void OnEnable()
    {
        GlobalValue.OnInventoryChanged += RefreshEditUI;
        GlobalValue.OnPartyCompositionChanged += RefreshEditUI;
        EquipmentMgr.OnEquipmentChanged += RefreshEditUI;
        GlobalValue.OnPartyGrowthChanged += RefreshCharacterDisplay;
        GlobalValue.OnCharacterDataChanged += RefreshCharacterDisplay;
        RefreshEditUI();
    }

    private void OnDisable()
    {
        GlobalValue.OnInventoryChanged -= RefreshEditUI;
        GlobalValue.OnPartyCompositionChanged -= RefreshEditUI;
        EquipmentMgr.OnEquipmentChanged -= RefreshEditUI;
        GlobalValue.OnPartyGrowthChanged -= RefreshCharacterDisplay;
        GlobalValue.OnCharacterDataChanged -= RefreshCharacterDisplay;
        ClearCharacterDisplay();
        m_EditSession?.Cancel();
        m_EditSession = null;
        SelectedCharacterId = "";
    }

    private void OnDestroy()
    {
        OnEditChanged -= RefreshEditUI;
        if (m_SaveBtn != null) m_SaveBtn.onClick.RemoveListener(SaveClick);
        if (m_CloseBtn != null) m_CloseBtn.onClick.RemoveListener(CloseClick);
    }

    /// <summary>새 편집 시작. 기존 편집은 폐기한다. 활성화만으로는 자동 초기화하지 않는다.</summary>
    public bool BeginEdit(string charId)
    {
        if (!EquipmentMgr.TryBeginEdit(out EquipmentEditSession session, out string message))
            return Fail(message);
        if (!session.ContainsCharacter(charId))
        {
            session.Cancel();
            return Fail("상세보기 대상 캐릭터를 찾을 수 없습니다.");
        }
        m_EditSession?.Cancel();
        m_EditSession = session;
        SelectedCharacterId = charId;
        LastMessage = "";
        gameObject.SetActive(true);
        OnEditChanged?.Invoke();
        return true;
    }

    /// <summary>패널 내 캐릭터 전환은 기존 편집을 유지한다.</summary>
    public bool SelectCharacter(string charId)
    {
        if (!IsEditing || !m_EditSession.ContainsCharacter(charId))
            return Fail("편집 대상 캐릭터를 찾을 수 없습니다.");
        SelectedCharacterId = charId;
        OnEditChanged?.Invoke();
        return true;
    }

    public string GetEquipmentUid(string charId, EquipType type)
        => IsEditing ? m_EditSession.GetEquipmentUid(charId, type) : "";

    public bool TryGetEquippedCharacter(string itemUid, out string charId)
    {
        charId = "";
        return IsEditing && m_EditSession.TryGetEquippedCharacter(itemUid, out charId);
    }

    public bool TryUnequipItem(string charId, EquipType type, string expectedItemUid)
    {
        if (!IsEditing) return Fail("장비 편집을 먼저 시작해주세요.");
        if (!m_EditSession.TryUnequipItem(charId, type, expectedItemUid, out string message))
            return Fail(message);
        LastMessage = "";
        OnEditChanged?.Invoke();
        return true;
    }

    /// <summary>착용자 확인 UI는 호출부에서 구현하고, 사용자 확인 후에만 allowTransfer=true로 호출한다.</summary>
    public bool TryEquipItem(string charId, EquipType type, string itemUid, bool allowTransfer = false)
    {
        if (!IsEditing) return Fail("장비 편집을 먼저 시작해주세요.");
        if (!m_EditSession.TryEquipItem(charId, type, itemUid, allowTransfer, out string message))
            return Fail(message);
        LastMessage = "";
        OnEditChanged?.Invoke();
        return true;
    }

    /// <summary>장비를 메모리에 확정하고 닫는다. 확정 결과는 공통 온라인 저장 대기로 전달한다.</summary>
    public bool TrySave()
    {
        if (!IsEditing) return Fail("저장할 편집이 없습니다.");
        if (PlayerDataMgr.IsProgressBlocked) return Fail("온라인 저장 문제를 해결한 뒤 다시 시도해주세요.");
        if (!EquipmentMgr.TryApply(m_EditSession, out string message)) return Fail(message);
        LastMessage = "";
        m_EditSession = null;
        try { OnSaved?.Invoke(); }
        finally { CompleteClose(true); }
        return true;
    }

    public void SaveClick() => TrySave();

    public void CloseClick()
    {
        if (MessageBoxMgr.IsBlockingInput) return;
        if (HasChanges)
        {
            EquipmentEditSession expectedSession = m_EditSession;
            ItemSaleMgr.ShowDialog("변경사항이 있습니다. 저장하지 않고 닫을까요?",
                MessageBoxType.Submit, () =>
                {
                    if (isActiveAndEnabled && m_EditSession == expectedSession) DiscardAndClose();
                });
            return;
        }
        DiscardAndClose();
    }

    private void DiscardAndClose()
    {
        m_EditSession?.Cancel();
        m_EditSession = null;
        SelectedCharacterId = "";
        CompleteClose(false);
    }

    private void CompleteClose(bool saved)
    {
        gameObject.SetActive(false);
        if (OnClosed == null) return;
        foreach (Action<bool> handler in OnClosed.GetInvocationList())
            try { handler(saved); } catch (Exception exception) { PDebug.LogError(exception); }
    }

    /// <summary>상위 메뉴 종료·재진입. 컬렉션 복귀 이벤트 없이 편집을 폐기합니다.</summary>
    public void CancelForMenuExit()
    {
        m_EditSession?.Cancel();
        m_EditSession = null;
        SelectedCharacterId = "";
        gameObject.SetActive(false);
    }

    private void BindCharacterUI()
    {
        Transform baseInfo = InventorySlotUI.Find<Transform>(transform, "CharBaseInfoSection");
        if (m_CharFrameImg == null) m_CharFrameImg = InventorySlotUI.Find<Image>(baseInfo, "CharImgRoot");
        if (m_CharIllustImg == null) m_CharIllustImg = InventorySlotUI.Find<Image>(baseInfo, "CSP_CharImg");
        if (m_CharNameTxt == null) m_CharNameTxt = InventorySlotUI.Find<Text>(baseInfo, "CharNameTxt");
        if (m_CharSubNameTxt == null) m_CharSubNameTxt = InventorySlotUI.Find<Text>(baseInfo, "CharSubNameTxt");
        if (m_CharStarGroup == null) m_CharStarGroup = InventorySlotUI.Find<Transform>(baseInfo, "StarGroup");
        if (m_CharRoleImg == null) m_CharRoleImg = InventorySlotUI.Find<Image>(baseInfo, "RoleIconImg");
        if (m_CharRoleTxt == null) m_CharRoleTxt = InventorySlotUI.Find<Text>(baseInfo, "RoleNameTxt");
        if (m_LevelInfoTxt == null) m_LevelInfoTxt = InventorySlotUI.Find<Text>(baseInfo, "LevelInfoTxt");
        if (m_GageImg == null) m_GageImg = InventorySlotUI.Find<Image>(baseInfo, "GageImg");
        if (m_GageTxt == null) m_GageTxt = InventorySlotUI.Find<Text>(baseInfo, "GageTxt");
        if (m_StatTexts == null || m_StatTexts.Length != 6) Array.Resize(ref m_StatTexts, 6);
        string[] statNames = { "StatTxt_MaxHp", "StatTxt_Atk", "StatTxt_AtkSpd", "StatTxt_Def", "StatTxt_CritChan", "StatTxt_CritDmgMult" };
        for (int i = 0; i < statNames.Length; i++)
            if (m_StatTexts[i] == null) m_StatTexts[i] = InventorySlotUI.Find<Text>(baseInfo, statNames[i]);
        if (m_SkillsGroup == null) m_SkillsGroup = InventorySlotUI.Find<Transform>(transform, "SkillsGroup");
        for (int i = 0; m_SkillsGroup != null && i < 4 && i < m_SkillsGroup.childCount; i++)
        {
            Transform cell = m_SkillsGroup.GetChild(i);
            m_SkillCells[i] = cell.gameObject;
            m_SkillIcons[i] = InventorySlotUI.Find<Image>(cell, "IconImg");
            m_SkillNames[i] = InventorySlotUI.Find<Text>(cell, "SkillNameTxt");
            m_SkillDescriptions[i] = InventorySlotUI.Find<Text>(cell, "SkillDescTxt");
        }
        // ExpLabel은 고정 문구이므로 읽거나 변경하지 않는다.
    }

    private void RefreshCharacterDisplay()
    {
        if (!IsEditing || !isActiveAndEnabled) return;
        CharData user = GlobalValue.UserCharDataList.Find(value => value != null && value.CharId == SelectedCharacterId);
        MasterDataMgr master = FindFirstObjectByType<MasterDataMgr>();
        if (user == null || master == null || !master.TryGetCharacter(SelectedCharacterId, out CharacterData data) || data == null)
        {
            ClearCharacterDisplay();
            PDebug.LogError("선택 캐릭터의 상세보기 데이터를 찾을 수 없습니다.");
            return;
        }
        if (m_CharFrameImg != null) m_CharFrameImg.color = GlobalConstants.GetRarityColor32(data.Rarity);
        if (m_CharIllustImg != null) m_CharIllustImg.sprite = data.Illustration;
        string fullName = data.DisplayName ?? "";
        int space = fullName.LastIndexOf(' ');
        if (m_CharNameTxt != null) m_CharNameTxt.text = space < 0 ? fullName : fullName.Substring(space + 1).Trim();
        if (m_CharSubNameTxt != null) m_CharSubNameTxt.text = space < 0 ? "" : fullName.Substring(0, space).Trim();
        for (int i = 0; m_CharStarGroup != null && i < m_CharStarGroup.childCount; i++)
            m_CharStarGroup.GetChild(i).gameObject.SetActive(i <= (int)data.Rarity);
        if (m_CharRoleTxt != null)
        {
            m_CharRoleTxt.text = GlobalConstants.GetKorRoleName(data.Role);
            m_CharRoleTxt.color = GlobalConstants.GetRoleColor32(data.Role);
        }
        if (m_CharRoleImg != null)
        {
            int index = (int)data.Role;
            Sprite sprite = m_CharRoleSprites != null && index >= 0 && index < m_CharRoleSprites.Length ? m_CharRoleSprites[index] : null;
            if (sprite == null)
            {
                if (m_CollectionDisplay == null) m_CollectionDisplay = FindFirstObjectByType<CollectionPanelMgr>(FindObjectsInactive.Include);
                if (m_CollectionDisplay != null) sprite = m_CollectionDisplay.GetCharacterRoleSprite(data.Role);
            }
            m_CharRoleImg.sprite = sprite;
        }
        int maxLevel = GlobalConstants.GetCharacterMaxLevel(data.Rarity);
        bool isMax = user.CharLv >= maxLevel;
        int requirement = isMax ? 0 : GlobalConstants.GetCharacterLevelDuplicateRequirement(data.Rarity, user.CharLv + 1);
        var growth = CharacterDetailDisplay.Growth(user.CharDuplicateCount, requirement, isMax);
        if (m_LevelInfoTxt != null) m_LevelInfoTxt.text = $"Lv. {user.CharLv}/{maxLevel}";
        if (m_GageImg != null) m_GageImg.fillAmount = growth.Fill;
        if (m_GageTxt != null) m_GageTxt.text = growth.Text;

        // 레벨 적용 기본 능력치 + 편집 장비 + 파티 강화. 임시 버프와 장비 레벨 성장 제외.
        float[] basic = CharacterGrowthCalc.GetBaseStats(data, user.CharLv);
        float[] equipmentBonus = new float[6];
        for (int i = 0; i < (int)EquipType.Count; i++)
        {
            string uid = GetEquipmentUid(user.CharId, (EquipType)i);
            if (string.IsNullOrEmpty(uid)) continue;
            if (!EquipmentMgr.TryResolveEquipment(uid, (EquipType)i, out EquipmentData equipment, out string message))
            { PDebug.LogError(message); continue; }
            equipmentBonus[0] += equipment.BonusHp;
            equipmentBonus[1] += equipment.BonusAttack;
            equipmentBonus[2] += equipment.BonusAttackSpeed;
            equipmentBonus[3] += equipment.BonusDefense;
            equipmentBonus[4] += equipment.BonusCriticalChance;
            equipmentBonus[5] += equipment.BonusCriticalDamageMultiplier;
        }
        float[] partyGrowth = {
            GlobalValue.PartyMaxHpLv * GlobalConstants.HpMultiplierPerLv,
            GlobalValue.PartyAttackPowerLv * GlobalConstants.AttackMultiplierPerLv,
            GlobalValue.PartyAttackSpeedLv * GlobalConstants.AttackSpeedMultiplierPerLv,
            GlobalValue.PartyDefenseLv * GlobalConstants.DefenseMultiplierPerLv,
            GlobalValue.PartyCriticalChanceLv * GlobalConstants.CriticalChancePerLv,
            GlobalValue.PartyCriticalDamageMultiplierLv * GlobalConstants.CriticalDamageMultiplierPerLv };
        float[] final = CharacterDetailDisplay.Calculate(basic, equipmentBonus, partyGrowth);
        string[] labels = { "최대체력", "공격력", "공격속도", "방어력", "치명타 확률", "치명타 증폭" };
        for (int i = 0; i < 6; i++)
        {
            Text text = m_StatTexts[i];
            if (text == null) continue;
            text.supportRichText = true;
            float displayScale = i >= 4 ? 100f : 1f;
            text.text = CharacterDetailDisplay.FormatStat(labels[i], basic[i] * displayScale,
                final[i] * displayScale, i >= 4 ? "%" : "");
        }
        for (int i = 0; i < 4; i++)
        {
            SkillData skill = data.SkillsList != null && i < data.SkillsList.Count ? data.SkillsList[i] : null;
            if (m_SkillIcons[i] != null) m_SkillIcons[i].sprite = skill != null ? skill.Icon : null;
            if (m_SkillNames[i] != null) m_SkillNames[i].text = skill != null ? skill.DisplayName : "";
            // TODO: 상세보기 전용 스킬 설명 파서를 이 위치에서 호출한다.
            string a_SkillDesc = skill != null ? SkillDescParser(skill) : "";
            // 현재는 Description만 표시하며 쿨타임·지속시간·효과계수 등을 추가하지 않는다.
            if (m_SkillDescriptions[i] != null) m_SkillDescriptions[i].text = skill != null ? a_SkillDesc : "";
            if (m_SkillCells[i] != null) m_SkillCells[i].SetActive(skill != null);
        }
    }

    private string SkillDescParser(SkillData skill)
    {
        string a_ParsedDesc = "";

        switch (skill.TargetType)
        {
            case SkillTargetType.Single:
                a_ParsedDesc += "단일 ";
                break;
            
            case SkillTargetType.Random:
                a_ParsedDesc += "랜덤 ";
                break;
            
            case SkillTargetType.Global:
                a_ParsedDesc += "모든 ";
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        switch (skill.Type)
        {
            case SkillType.Damage:
                a_ParsedDesc += "적에게 ";
                break;
            
            case SkillType.Buff:
                a_ParsedDesc += "아군의 ";
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }

        switch (skill.BuffStat)
        {
            case SkillBuffStat.None:
                a_ParsedDesc += "피해를 입힌다";
                break;
            
            case SkillBuffStat.MaxHp:
                a_ParsedDesc += "최대 체력 증가";
                break;
            
            case SkillBuffStat.AtkPower:
                a_ParsedDesc += "공격력 증가";
                break;
            
            case SkillBuffStat.AtkSpeed:
                a_ParsedDesc += "공격 속도 증가";
                break;
            
            case SkillBuffStat.DefPower:
                a_ParsedDesc += "방어력 증가";
                break;
            
            case SkillBuffStat.CritChance:
                a_ParsedDesc += "치명타 확률 증가";
                break;
            
            case SkillBuffStat.CritDmg:
                a_ParsedDesc += "치명타 피해 증폭 증가";
                break;
            
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        return a_ParsedDesc;
    }

    private void ClearCharacterDisplay()
    {
        if (m_CharIllustImg != null) m_CharIllustImg.sprite = null;
        if (m_CharFrameImg != null) m_CharFrameImg.color = Color.white;
        if (m_CharNameTxt != null) m_CharNameTxt.text = "";
        if (m_CharSubNameTxt != null) m_CharSubNameTxt.text = "";
        if (m_CharRoleImg != null) m_CharRoleImg.sprite = null;
        if (m_CharRoleTxt != null) m_CharRoleTxt.text = "";
        if (m_LevelInfoTxt != null) m_LevelInfoTxt.text = "";
        if (m_GageImg != null) m_GageImg.fillAmount = 0f;
        if (m_GageTxt != null) m_GageTxt.text = "";
        if (m_StatTexts != null)
            foreach (Text text in m_StatTexts) if (text != null) text.text = "";
        for (int i = 0; m_CharStarGroup != null && i < m_CharStarGroup.childCount; i++)
            m_CharStarGroup.GetChild(i).gameObject.SetActive(false);
        for (int i = 0; i < 4; i++) if (m_SkillCells[i] != null) m_SkillCells[i].SetActive(false);
    }

    private void RefreshEditUI()
    {
        if (!IsEditing || !isActiveAndEnabled) return;
        RefreshCharacterDisplay();
        CharData character = GlobalValue.UserCharDataList.Find(value =>
            value != null && value.CharId == SelectedCharacterId);
        for (int i = 0; m_EquipmentCells != null && i < m_EquipmentCells.Length && i < (int)EquipType.Count; i++)
        {
            EquipCellMgr cell = m_EquipmentCells[i];
            if (cell == null) continue;
            cell.BindDetailPanel(this);
            ItemSaleMgr.TryFindItem(GetEquipmentUid(SelectedCharacterId, (EquipType)i), out ItemData item);
            cell.InitializeEquipCell(item, character);
        }
        if (m_InventoryContent == null) return;
        if (m_InventorySlotPrefab == null)
        {
            InventoryMgr inventory = FindFirstObjectByType<InventoryMgr>(FindObjectsInactive.Include);
            if (inventory != null) m_InventorySlotPrefab = inventory.m_ItemSlot;
        }
        if (m_InventorySlotPrefab == null)
        { PDebug.LogError("상세보기 인벤토리 슬롯 프리팹을 연결해주세요."); return; }
        InventorySlotUI.ClearContent(m_InventoryContent);
        foreach (ItemData item in GlobalValue.UserItemDataList)
        {
            if (item == null || item.Type != ItemType.Equipment || m_EditSession.IsSalePending(item.ItemUid)) continue;
            GameObject instance = Instantiate(m_InventorySlotPrefab, m_InventoryContent);
            ItemCellMgr cell = instance.GetComponent<ItemCellMgr>();
            if (cell == null) { Destroy(instance); continue; }
            cell.BindActions(EquipFromInventory, SellFromInventory);
            cell.InitializeItem(item);
            TryGetEquippedCharacter(item.ItemUid, out string owner);
            cell.SetEquippedCharacter(owner);
        }
    }

    private void EquipFromInventory(string uid)
    {
        if (!IsEditing || !ItemSaleMgr.TryFindItem(uid, out ItemData item)) return;
        if (TryGetEquippedCharacter(uid, out _))
        { ItemSaleMgr.ShowMessage("이미 착용 중인 아이템입니다"); return; }
        MasterDataMgr master = FindFirstObjectByType<MasterDataMgr>();
        if (master == null || !master.TryGetEquipment(item.ItemId, out EquipmentData equipment) || equipment == null)
        { Fail("아이템 마스터 정보를 찾을 수 없습니다."); return; }
        TryEquipItem(SelectedCharacterId, equipment.EquipType, uid);
    }

    private void SellFromInventory(string uid)
    {
        if (!IsEditing) return;
        if (!ItemSaleMgr.TryGetSaleInfo(uid, m_EditSession, out ItemData item,
            out EquipmentData equipment, out string message))
        { ItemSaleMgr.ShowMessage(message); return; }
        int price = item.Price;
        EquipmentEditSession expectedSession = m_EditSession;
        ItemSaleMgr.ShowDialog($"{equipment.DisplayName} 아이템을 {price:N0} G에 판매하시겠습니까?",
            MessageBoxType.Submit, () =>
            {
                if (!isActiveAndEnabled || !IsEditing || m_EditSession != expectedSession) return;
                if (!m_EditSession.TryReserveSale(uid, price, out string result))
                { ItemSaleMgr.ShowMessage(result); return; }
                OnEditChanged?.Invoke();
            });
    }

    private bool Fail(string message)
    {
        LastMessage = message;
        PDebug.LogError(message);
        ItemSaleMgr.ShowMessage(message);
        OnOperationFailed?.Invoke(message);
        return false;
    }
}

/// <summary>임시 버프를 제외한 표시 계산·서식입니다. 실제 캐릭터 상태를 변경하지 않습니다.</summary>
internal static class CharacterDetailDisplay
{
    internal static float[] Calculate(float[] basic, float[] equipment, float[] growth)
    {
        float[] result = new float[6];
        for (int i = 0; i < 6; i++)
        {
            float value = basic[i] + equipment[i];
            float bonus = Math.Max(0f, growth[i]);
            result[i] = i < 4 ? value * (1f + bonus) : value + bonus;
        }
        result[4] = Math.Max(0f, Math.Min(1f, result[4]));
        return result;
    }

    internal static (float Fill, string Text) Growth(int duplicateCount, int requirement, bool isMax)
    {
        if (isMax) return (1f, "MAX (100.0%)");
        int owned = Math.Max(0, duplicateCount);
        float ratio = requirement > 0 ? (float)owned / requirement : 0f;
        return (Math.Max(0f, Math.Min(1f, ratio)),
            $"{owned}/{requirement} ({(ratio * 100f).ToString("F1", CultureInfo.InvariantCulture)}%)");
    }

    internal static string FormatStat(string label, float basic, float final, string unit)
    {
        string result = $"{label} <color=#ff0000>{Number(final)}{unit}</color>";
        float difference = final - basic;
        if (difference == 0f) return result;
        string sign = difference > 0f ? "+" : "-";
        return $"{result} ({Number(basic)}{unit} <color=#0000ff>{sign}{Number(Math.Abs(difference))}{unit}</color>)";
    }

    private static string Number(float value)
    {
        string number = value.ToString("0.##", CultureInfo.InvariantCulture);
        return value != 0f && (number == "0" || number == "-0")
            ? value.ToString("G4", CultureInfo.InvariantCulture) : number;
    }
}
