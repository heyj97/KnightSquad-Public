using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 보유 캐릭터 조회와 파티 편성 편집·저장 흐름을 관리합니다.
/// </summary>
public class CollectionPanelMgr : MonoBehaviour
{
    private CharacterRole m_ActiveCharacterFilter = CharacterRole.Count;
    [Header("Collection Panel UI Elements")]
    [SerializeField] private GameObject m_CollectionPanel;
    [SerializeField] private Button m_CloseBtn;
    [SerializeField] private Button m_SaveBtn;
    [SerializeField] private Sprite[] m_CharRoleSprites = new Sprite[(int)CharacterRole.Count];
    
    [Header("Party Setting UI Elements")]
    [SerializeField] private GameObject m_CurPartyCharCell;
    [SerializeField] private Transform m_PartyCharContent;
    
    [Header("Character Collection UI Elements")]
    [SerializeField] private Button m_FilterBtnAll;
    [SerializeField] private Button m_FilterBtnMainDealer;
    [SerializeField] private Button m_FilterBtnSubDealer;
    [SerializeField] private Button m_FilterBtnTanker;
    [SerializeField] private Button m_FilterBtnSupporter;
    
    [SerializeField] private Transform m_CharCollectionContent;
    [SerializeField] private GameObject m_CollectionCharCell;

    [SerializeField] private Color32 m_FilterBtnClickedColor = new Color32(255, 204, 0, 255);
    
    [Header("Character Statistics UI Elements")]
    [SerializeField] private GameObject m_CharStatisticsPanel;

    [SerializeField] private Image m_CharIllustFrameImg;
    [SerializeField] private Image m_CharIllustImg;
    [SerializeField] private Transform m_CharStarGroup;
    

    [SerializeField] private Button m_CharDetailBtn;
    [SerializeField] private Button m_CharClearBtn;
    

    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private Text m_CharSubNameTxt;
    [SerializeField] private Image m_CharRoleIconImg;
    [SerializeField] private Text m_CharRoleTxt;
    

    [SerializeField] private Text m_CharLevelTxt;
    [SerializeField] private Image m_CharLevelGageImg;
    [SerializeField] private Text m_CharLevelGageTxt;
    

    [SerializeField] private Text m_CharMaxHpTxt;
    [SerializeField] private Text m_CharAttackPowerTxt;
    [SerializeField] private Text m_CharAttackSpeedTxt;
    [SerializeField] private Text m_CharDefenseTxt;
    [SerializeField] private Text m_CharCriticalChanceTxt;
    [SerializeField] private Text m_CharCriticalDamageMultiplierTxt;
    
    CharData m_CharData;
    CharacterData m_CharacterData;
    private List<string> m_EditPartyCharIdList;
    private int m_SelectedPartySlotIndex = -1;
    private CollectionCharCellMgr m_SelectedCollectionCell;
    private CharData m_PendingAssignCharData;
    private readonly CollectionPartyEditSession m_PartyEditSession = new();
    private CharDetailPanelMgr m_BoundDetailPanel;
    // m_CollectionPanel은 두 Prefab을 포함하는 메뉴 컨테이너이며,
    // 이 컴포넌트의 gameObject는 왕복 시 숨길 CollectionPrefab 화면이다.
    private GameObject CollectionMenuRoot => m_CollectionPanel != null ? m_CollectionPanel : gameObject;

    public Sprite GetCharacterRoleSprite(CharacterRole role)
    {
        int index = (int)role;
        return m_CharRoleSprites != null && index >= 0 && index < m_CharRoleSprites.Length
            ? m_CharRoleSprites[index] : null;
    }
    

    void Start()
    {
        // Character Collection Panel
        if (m_CloseBtn != null)
            m_CloseBtn.onClick.AddListener(CloseCollection);
        
        if (m_SaveBtn != null)
            m_SaveBtn.onClick.AddListener(SavePartyClick);
        // Character Collection Panel
        
        // Character Statistics
        if (m_CharDetailBtn != null)
            m_CharDetailBtn.onClick.AddListener(ShowDetailClick);
        
        if (m_CharClearBtn != null)
            m_CharClearBtn.onClick.AddListener(ClearFromPartyClick);
        // Character Statistics
        
        // Character Collection Filter
        if (m_FilterBtnAll != null)
            m_FilterBtnAll.onClick.AddListener(() =>
            {
                CCFilterBtnImgCtrl(CharacterRole.Count);
                FilterCharacterCollectionSection(CharacterRole.Count);
            });
        
        if (m_FilterBtnMainDealer != null)
            m_FilterBtnMainDealer.onClick.AddListener(() =>
            {
                CCFilterBtnImgCtrl(CharacterRole.MainDealer);
                FilterCharacterCollectionSection(CharacterRole.MainDealer);
            });
        
        if (m_FilterBtnSubDealer != null)
            m_FilterBtnSubDealer.onClick.AddListener(() =>
            {
                CCFilterBtnImgCtrl(CharacterRole.SubDealer);
                FilterCharacterCollectionSection(CharacterRole.SubDealer);
            });
        
        if (m_FilterBtnTanker != null)
            m_FilterBtnTanker.onClick.AddListener(() =>
            {
                FilterCharacterCollectionSection(CharacterRole.Tanker);
                CCFilterBtnImgCtrl(CharacterRole.Tanker);
            });
        
        if (m_FilterBtnSupporter != null)
            m_FilterBtnSupporter.onClick.AddListener(() =>
            {
                FilterCharacterCollectionSection(CharacterRole.Supporter);
                CCFilterBtnImgCtrl(CharacterRole.Supporter);
            });
        // Character Collection Filter
    }

    void Update()
    {
        if (MessageBoxMgr.IsBlockingInput)
            return;
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
            CancelCollectionSelection();
    }

    void OnEnable()
    {
        GlobalValue.OnCharacterDataChanged += RefreshCharacterGrowth;
        bool returning = m_PartyEditSession.IsSuspended;
        string returnCharId = m_PartyEditSession.ReturnCharacterId;
        m_EditPartyCharIdList = m_PartyEditSession.Open(GlobalValue.PartyCharIdList);
        if (!returning && TryBindDetailPanel()) m_CharDetailPanel.CancelForMenuExit();

        ClearCharDetailSection();
        InitializeCurPartySection();
        CCFilterBtnImgCtrl(CharacterRole.Count);
        InitializeCharacterCollectionSection();
        m_SelectedPartySlotIndex = -1;
        if (returning && !string.IsNullOrEmpty(returnCharId))
            ShowCharacterStats(GlobalValue.UserCharDataList.Find(value =>
                value != null && value.CharId == returnCharId));
    }

    void OnDisable()
    {
        GlobalValue.OnCharacterDataChanged -= RefreshCharacterGrowth;
        ClearCharDetailSection();
        ClearCurPartySection();
        ClearCharacterCollectionSection();
        m_SelectedPartySlotIndex = -1;
        if (!m_PartyEditSession.IsSuspended)
        {
            m_PartyEditSession.End();
            m_EditPartyCharIdList = null;
        }
    }

    private void OnDestroy()
    {
        if (m_BoundDetailPanel != null) m_BoundDetailPanel.OnClosed -= ReturnFromDetail;
        if (m_CloseBtn != null) m_CloseBtn.onClick.RemoveListener(CloseCollection);
        if (m_SaveBtn != null) m_SaveBtn.onClick.RemoveListener(SavePartyClick);
        if (m_CharDetailBtn != null) m_CharDetailBtn.onClick.RemoveListener(ShowDetailClick);
    }

    /// <summary>상위 메뉴 종료 시에도 숨겨진 편성 편집본을 폐기합니다.</summary>
    public void CloseCollection()
    {
        m_PartyEditSession.End();
        m_EditPartyCharIdList = null;
        if (TryBindDetailPanel()) m_CharDetailPanel.CancelForMenuExit();
        CollectionMenuRoot.SetActive(false);
    }

    public void OpenCollection()
    {
        CloseCollection();
        // 상세보기에서 떠날 때 화면의 activeSelf가 false로 남아 있을 수 있다.
        // 부모를 열기 전에 컬렉션 화면을 기본 활성 상태로 준비한다.
        gameObject.SetActive(true);
        CollectionMenuRoot.SetActive(true);
    }

    void SavePartyClick()
    {
        if (m_EditPartyCharIdList == null)
            return;

        // 편성 검증과 적용은 PartyMgr 한 곳에서 수행
        PartyMgr a_PartyMgr = FindFirstObjectByType<PartyMgr>();
        if (a_PartyMgr == null ||
            !a_PartyMgr.TryApplyPartyComposition(new List<string>(m_EditPartyCharIdList)))
        {
            PDebug.LogError("실제 파티 배치 변경에 실패했습니다. 편집 내용을 유지합니다.");
            return;
        }

        // 닫힐 패널 정리는 OnDisable에서 수행
        PDebug.Log("파티 편성과 전투력을 갱신했습니다.");
        CloseCollection();
    }

    #region 현재 파티 섹션 메서드

    void InitializeCurPartySection()
    {
        // 레퍼런스 에러
        if (m_PartyCharContent == null || m_CurPartyCharCell == null)
        {
            PDebug.LogError("m_PartyCharContent or m_CurPartyCharCell is null");
            return;
        }

        // 셀이 있으면 모두 삭제
        if (m_PartyCharContent.childCount > 0)
            ClearCurPartySection();
        
        // 파티 수만큼 Spawn 후 등록된 아이디를 기반으로 cell initialize
        for (int i = 0; i < m_EditPartyCharIdList.Count; i++)
        {
            string a_CharId = m_EditPartyCharIdList[i];
            CharData a_CharData = null;

            if (!string.IsNullOrEmpty(a_CharId))
            {
                a_CharData = GlobalValue.UserCharDataList.Find(
                    a_Char => a_Char != null && a_Char.CharId == a_CharId);

                if (a_CharData == null)
                    PDebug.LogError(
                        $"보유 캐릭터 데이터를 찾을 수 없습니다: {a_CharId}");
            }

            GameObject a_Cell =
                Instantiate(m_CurPartyCharCell, m_PartyCharContent);

            CurPartyCharCellMgr a_CellMgr =
                a_Cell.GetComponent<CurPartyCharCellMgr>();

            if (a_CellMgr == null)
            {
                PDebug.LogError("CurPartyCharCellMgr is missing");
                Destroy(a_Cell);
                continue;
            }

            a_CellMgr.OnCellClicked += HandleCurPartyCharCellClicked;
            a_CellMgr.InitializeCell(a_CharData, i);
            a_CellMgr.SetAssignOutline(m_PendingAssignCharData != null);
        }
    }

    void HandleCurPartyCharCellClicked(CharData charData, int slotIndex)
    {
        if (m_PendingAssignCharData != null)
        {
            AssignPendingCharacterToSlot(slotIndex);
            return;
        }

        CancelCollectionSelection();
        ShowCharacterStats(charData);
        // 빈 슬롯 선택 동작 유지
        if (charData == null)
            m_SelectedPartySlotIndex = slotIndex;
    }
    

    void AssignPendingCharacterToSlot(int slotIndex)
    {
        if (m_PendingAssignCharData == null || m_EditPartyCharIdList == null ||
            slotIndex < 0 || slotIndex >= m_EditPartyCharIdList.Count)
            return;

        string a_CharId = m_PendingAssignCharData.CharId;
        CharData a_OwnedCharData = GlobalValue.UserCharDataList.Find(
            a_Char => a_Char != null && a_Char.CharId == a_CharId);

        if (a_OwnedCharData == null ||
            !MasterDataMgr.Inst.TryGetCharacter(a_CharId, out CharacterData a_MasterData) ||
            a_MasterData == null)
        {
            PDebug.LogError("배치할 캐릭터 데이터를 찾을 수 없습니다.");
            CancelCollectionSelection();
            return;
        }

        // 이미 배치된 캐릭터는 이전 슬롯을 비우고 이동
        for (int i = 0; i < m_EditPartyCharIdList.Count; i++)
            if (i != slotIndex && m_EditPartyCharIdList[i] == a_CharId)
                m_EditPartyCharIdList[i] = "";

        m_EditPartyCharIdList[slotIndex] = a_CharId;
        CancelCollectionSelection();
        InitializeCurPartySection();
        RefreshCollectionPartyStatus();
        HandleCurPartyCharCellClicked(a_OwnedCharData, slotIndex);
    }

    void ClearCurPartySection()
    {
        if (m_PartyCharContent == null) return;
        foreach (Transform a_Cell in m_PartyCharContent)
        {
            CurPartyCharCellMgr a_CellMgr =
                a_Cell.GetComponent<CurPartyCharCellMgr>();

            if (a_CellMgr != null)
                a_CellMgr.OnCellClicked -= HandleCurPartyCharCellClicked;

            a_Cell.gameObject.SetActive(false);
            Destroy(a_Cell.gameObject);
        }
    }

    #endregion

    #region 캐릭터 정보 섹션 메서드

    void ShowCharacterStats(CharData charData)
    {
        ClearCharDetailSection();
        if (charData == null)
            return;

        if (!MasterDataMgr.Inst.TryGetCharacter(
                charData.CharId, out CharacterData a_CharacterData) || a_CharacterData == null)
        {
            PDebug.LogError("마스터 데이터에서 캐릭터를 찾을 수 없습니다.");
            return;
        }

        m_CharData = charData;
        m_CharacterData = a_CharacterData;
        InitializeCharDetailSection();
    }

    void InitializeCharDetailSection()
    {
        if (m_CharData == null || m_CharacterData == null)
            return;

        m_SelectedPartySlotIndex = m_EditPartyCharIdList?.IndexOf(m_CharData.CharId) ?? -1;
        if (m_CharClearBtn != null)
            m_CharClearBtn.gameObject.SetActive(m_SelectedPartySlotIndex >= 0);
        
        m_CharStatisticsPanel.SetActive(true);
        
        // Character Illust Group
        m_CharIllustFrameImg.color = GlobalConstants.GetRarityColor32(m_CharacterData.Rarity);
        m_CharIllustImg.sprite = m_CharacterData.Illustration;

        for (int i = 0; i < m_CharStarGroup.childCount; i++)
            m_CharStarGroup.GetChild(i).gameObject.SetActive(i <= (int)m_CharacterData.Rarity);
        
        // Character Info Group
        string[] a_CharName = CharNameSplitter(m_CharacterData.DisplayName);
        m_CharNameTxt.text = a_CharName[1];
        m_CharSubNameTxt.text = a_CharName[0];
        m_CharRoleIconImg.sprite = m_CharRoleSprites[(int)m_CharacterData.Role];
        m_CharRoleTxt.text = GlobalConstants.GetKorRoleName(m_CharacterData.Role);
        m_CharRoleTxt.color = GlobalConstants.GetRoleColor32(m_CharacterData.Role);
        
        // Character Level Group
        int a_MaxLevel = GlobalConstants.GetCharacterMaxLevel(m_CharacterData.Rarity);
        m_CharLevelTxt.text = $"<b>Lv. {m_CharData.CharLv}</b>/{a_MaxLevel}";
        if (m_CharData.CharLv >= a_MaxLevel)
        {
            m_CharLevelGageImg.fillAmount = 1f;
            m_CharLevelGageTxt.text = "MAX (100.0%)";
        }
        else
        {
            int a_Required = GlobalConstants.GetCharacterLevelDuplicateRequirement(
                m_CharacterData.Rarity, m_CharData.CharLv + 1);
            int a_Owned = Mathf.Max(0, m_CharData.CharDuplicateCount);
            float a_Progress = (float)a_Owned / a_Required;
            m_CharLevelGageImg.fillAmount = Mathf.Clamp01(a_Progress);
            m_CharLevelGageTxt.text = $"{a_Owned}/{a_Required} ({a_Progress * 100f:F1}%)";
        }
       
        // Character Stats Group
        float scale = CharacterGrowthCalc.GetMultiplier(m_CharacterData.Rarity, m_CharData.CharLv);
        m_CharMaxHpTxt.text = $"최대체력 {(int)(m_CharacterData.BaseMaxHp * scale)}";
        m_CharAttackPowerTxt.text = $"공격력 {(int)(m_CharacterData.BaseAttackPower * scale)}";
        m_CharAttackSpeedTxt.text = $"공격속도 {m_CharacterData.BaseAttackSpeed:N2}";
        m_CharDefenseTxt.text = $"방어력 {(int)(m_CharacterData.BaseDefense * scale)}";
        m_CharCriticalChanceTxt.text = $"치명타 확률 {m_CharacterData.BaseCriticalChance:N1}%";
        m_CharCriticalDamageMultiplierTxt.text = $"치명타 증폭 {m_CharacterData.BaseCriticalDamageMultiplier:N1}%";
    }

    private void RefreshCharacterGrowth()
    {
        // 편성 편집 목록은 유지하고 표시만 재생성한다.
        string selected = m_CharData?.CharId;
        CancelCollectionSelection();
        InitializeCurPartySection();
        FilterCharacterCollectionSection(m_ActiveCharacterFilter);
        if (!string.IsNullOrEmpty(selected))
            ShowCharacterStats(GlobalValue.UserCharDataList.Find(c => c != null && c.CharId == selected));
    }

    string[] CharNameSplitter(string name)
    {
        if (string.IsNullOrEmpty(name))
            return Array.Empty<string>();
        
        int lastSpace = name.LastIndexOf(' ');

        if (lastSpace < 0)
            return new[] { "", name };

        return new[]
        {
            name[..lastSpace].TrimEnd(),
            name[(lastSpace + 1)..]
        };
    }
    
    void ClearCharDetailSection()
    {
        m_CharStatisticsPanel.SetActive(false);
        
        m_CharIllustFrameImg.color = Color.white;
        m_CharIllustImg.sprite = null;
        
        m_CharNameTxt.text = "";
        m_CharSubNameTxt.text = "";
        m_CharRoleIconImg.sprite = null;
        m_CharRoleTxt.text = "";
        m_CharRoleTxt.color = Color.white;
        
        m_CharLevelTxt.text = "";
        m_CharLevelGageImg.fillAmount = 0.0f;
        m_CharLevelGageTxt.text = "";
        
        m_CharMaxHpTxt.text = "";
        m_CharAttackPowerTxt.text = "";
        m_CharAttackSpeedTxt.text = "";
        m_CharDefenseTxt.text = "";
        m_CharCriticalChanceTxt.text = "";
        m_CharCriticalDamageMultiplierTxt.text = "";
        
        foreach(Transform a_Star in m_CharStarGroup)
            a_Star.gameObject.SetActive(false);

        m_CharData = null;
        m_CharacterData = null;
        m_SelectedPartySlotIndex = -1;
        if (m_CharClearBtn != null)
            m_CharClearBtn.gameObject.SetActive(false);
    }

    [SerializeField] private CharDetailPanelMgr m_CharDetailPanel;

    void ShowDetailClick()
    {
        if (MessageBoxMgr.IsBlockingInput || m_CharData == null || !m_PartyEditSession.IsOpen) return;
        if (!TryBindDetailPanel())
        { PDebug.LogError("CharDetailPrefab에 CharDetailPanelMgr를 연결해주세요."); return; }
        if (m_CharDetailPanel.transform.IsChildOf(transform) ||
            transform.IsChildOf(m_CharDetailPanel.transform))
        { PDebug.LogError("컬렉션과 상세보기 패널은 서로 독립된 패널이어야 합니다."); return; }
        string charId = m_CharData.CharId;
        if (!m_CharDetailPanel.BeginEdit(charId)) return;
        m_PartyEditSession.Suspend(charId);
        gameObject.SetActive(false);
    }

    private bool TryBindDetailPanel()
    {
        if (m_CharDetailPanel == null)
            m_CharDetailPanel = FindFirstObjectByType<CharDetailPanelMgr>(FindObjectsInactive.Include);
        if (m_CharDetailPanel == null) return false;
        if (m_BoundDetailPanel != m_CharDetailPanel)
        {
            if (m_BoundDetailPanel != null) m_BoundDetailPanel.OnClosed -= ReturnFromDetail;
            m_BoundDetailPanel = m_CharDetailPanel;
            // 컬렉션이 비활성 상태여도 상세보기 종료 통지를 유지한다.
            m_BoundDetailPanel.OnClosed += ReturnFromDetail;
        }
        return true;
    }

    private void ReturnFromDetail(bool saved)
    {
        if (!m_PartyEditSession.IsOpen || !m_PartyEditSession.IsSuspended || !enabled ||
            !CollectionMenuRoot.activeInHierarchy) return;
        gameObject.SetActive(true);
    }

    void ClearFromPartyClick()
    {
        if (m_CharData == null || m_EditPartyCharIdList == null)
            return;

        int a_SlotIndex = m_EditPartyCharIdList.IndexOf(m_CharData.CharId);
        if (a_SlotIndex < 0)
        {
            m_SelectedPartySlotIndex = -1;
            if (m_CharClearBtn != null)
                m_CharClearBtn.gameObject.SetActive(false);
            return;
        }

        CancelCollectionSelection();
        m_EditPartyCharIdList[a_SlotIndex] = "";

        InitializeCurPartySection();
        RefreshCollectionPartyStatus();
        ClearCharDetailSection();

    }
    
    #endregion

    #region 보유 캐릭터 섹션 메서드

    void HandleCollectionCellClicked(CollectionCharCellMgr cell)
    {
        if (m_SelectedCollectionCell != null)
            m_SelectedCollectionCell.CancelAssignToPartyClick();

        m_SelectedCollectionCell = cell;
        m_PendingAssignCharData = null;
        
        RefreshPartyAssignOutline();
        ShowCharacterStats(cell != null ? cell.UserCharData : null);
    }

    void HandleAssignClick(CharData charData)
    {
        if (charData != null)
            m_PendingAssignCharData = charData;
        
        RefreshPartyAssignOutline();
    }

    void CancelCollectionSelection()
    {
        if (m_SelectedCollectionCell != null)
            m_SelectedCollectionCell.CancelAssignToPartyClick();

        m_SelectedCollectionCell = null;
        m_PendingAssignCharData = null;
        
        RefreshPartyAssignOutline();
    }

    void RefreshCollectionPartyStatus()
    {
        foreach (Transform a_Cell in m_CharCollectionContent)
        {
            CollectionCharCellMgr a_CellMgr = a_Cell.GetComponent<CollectionCharCellMgr>();
            if (a_CellMgr != null)
                a_CellMgr.RefreshPartyStatus(m_EditPartyCharIdList);
        }
    }

    void InitializeCharacterCollectionSection()
    {
        ClearCharacterCollectionSection();
        
        if (m_CharCollectionContent == null || m_CollectionCharCell == null)
        {
            PDebug.LogError("보유캐릭터 Content 또는 Cell 레퍼런스 누락");
            return;
        }

        if (GlobalValue.UserCharDataList.Count < 1)
            return;

        foreach (CharData a_CharData in GlobalValue.UserCharDataList)
        {
            GameObject a_CharCell = Instantiate(m_CollectionCharCell, m_CharCollectionContent);
            CollectionCharCellMgr a_CellMgr = a_CharCell.GetComponent<CollectionCharCellMgr>();
            a_CellMgr.InitializeCell(a_CharData);
            a_CellMgr.OnCellClicked += HandleCollectionCellClicked;
            a_CellMgr.OnAssignClick += HandleAssignClick;
            a_CellMgr.RefreshPartyStatus(m_EditPartyCharIdList);
        }
    }

    void ClearCharacterCollectionSection()
    {
        CancelCollectionSelection();
        if (m_CharCollectionContent == null)
            return;

        foreach (Transform a_Cell in m_CharCollectionContent)
        {
            CollectionCharCellMgr a_CellMgr = a_Cell.GetComponent<CollectionCharCellMgr>();
            if (a_CellMgr != null)
            {
                a_CellMgr.OnCellClicked -= HandleCollectionCellClicked;
                a_CellMgr.OnAssignClick -= HandleAssignClick;
            }

            a_Cell.gameObject.SetActive(false);
            Destroy(a_Cell.gameObject);
        }
    }

    void FilterCharacterCollectionSection(CharacterRole role)
    {
        m_ActiveCharacterFilter = role;
        if (m_CharCollectionContent == null || m_CollectionCharCell == null)
        {
            PDebug.LogError("보유캐릭터 Content 또는 Cell 레퍼런스 누락");
            return;
        }
        
        List<string> a_CharacterDataList = new();
        
        foreach (CharData a_CharData in GlobalValue.UserCharDataList)
        {
            if (MasterDataMgr.Inst.TryGetCharacter(a_CharData.CharId, out CharacterData a_CharacterData))
                if (a_CharacterData.Role == role)
                    a_CharacterDataList.Add(a_CharacterData.Id);
        }
        

        if (role == CharacterRole.Count)
        {
            InitializeCharacterCollectionSection();
            return;
        }
        
        ClearCharacterCollectionSection();
        
        foreach (string a_CharId in a_CharacterDataList)
        {
            GameObject a_Cell = Instantiate(m_CollectionCharCell, m_CharCollectionContent);
            CollectionCharCellMgr a_CellMgr = a_Cell.GetComponent<CollectionCharCellMgr>();
            a_CellMgr.InitializeCell(GlobalValue.UserCharDataList.Find(charData => charData.CharId == a_CharId));
            a_CellMgr.OnCellClicked += HandleCollectionCellClicked;
            a_CellMgr.OnAssignClick += HandleAssignClick;
            a_CellMgr.RefreshPartyStatus(m_EditPartyCharIdList);
        }
    }

    void CCFilterBtnImgCtrl(CharacterRole role)
    {
        m_ActiveCharacterFilter = role;
        if (m_FilterBtnAll == null ||
            m_FilterBtnMainDealer == null ||
            m_FilterBtnSubDealer == null ||
            m_FilterBtnTanker == null ||
            m_FilterBtnSupporter == null)
        {
            PDebug.LogError("Button Reference is null");
            return;
        }
        
        m_FilterBtnAll.image.color = Color.white;
        m_FilterBtnMainDealer.image.color = Color.white;
        m_FilterBtnSubDealer.image.color = Color.white;
        m_FilterBtnTanker.image.color = Color.white;
        m_FilterBtnSupporter.image.color = Color.white;
        
        switch (role)
        {
            case CharacterRole.MainDealer:
                m_FilterBtnMainDealer.image.color = m_FilterBtnClickedColor;
                break;
            
            case CharacterRole.SubDealer:
                m_FilterBtnSubDealer.image.color = m_FilterBtnClickedColor;
                break;
            
            case CharacterRole.Tanker:
                m_FilterBtnTanker.image.color = m_FilterBtnClickedColor;
                break;
            
            case CharacterRole.Supporter:
                m_FilterBtnSupporter.image.color = m_FilterBtnClickedColor;
                break;
            
            case CharacterRole.Count:
                m_FilterBtnAll.image.color = m_FilterBtnClickedColor;
                break;
            
            default:
                PDebug.LogError("Invalid Character Role");
                break;
        }
    }
    #endregion
    
    void RefreshPartyAssignOutline()
    {
        if (m_PartyCharContent == null)
            return;

        bool a_IsPending = m_PendingAssignCharData != null;

        foreach (Transform a_Cell in m_PartyCharContent)
        {
            CurPartyCharCellMgr a_CellMgr =
                a_Cell.GetComponent<CurPartyCharCellMgr>();

            if (a_CellMgr != null)
                a_CellMgr.SetAssignOutline(a_IsPending);
        }
    }
}

/// <summary>컬렉션 표시 수명과 독립적인 편성 편집본입니다. 상세보기 왕복만 목록을 보존합니다.</summary>
internal sealed class CollectionPartyEditSession
{
    public bool IsOpen { get; private set; }
    public bool IsSuspended { get; private set; }
    public string ReturnCharacterId { get; private set; } = "";
    private List<string> m_CharacterIds;

    public List<string> Open(IEnumerable<string> actualIds)
    {
        if (!IsOpen || !IsSuspended)
            m_CharacterIds = new List<string>(actualIds);
        IsOpen = true;
        IsSuspended = false;
        ReturnCharacterId = "";
        return m_CharacterIds;
    }

    public void Suspend(string charId)
    {
        if (!IsOpen) return;
        IsSuspended = true;
        ReturnCharacterId = charId;
    }

    public void End()
    {
        IsOpen = false;
        IsSuspended = false;
        ReturnCharacterId = "";
        m_CharacterIds = null;
    }
}
