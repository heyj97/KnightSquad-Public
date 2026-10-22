using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 재화와 전투 정보를 표시하고 주요 메뉴 패널 전환 및 일시정지를 관리합니다.
/// </summary>
public class InformationPanelMgr : MonoBehaviour
{
    /// <summary>
    /// 현재 챕터와 스테이지를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    [Header("Info group UI elements")]
    public Text m_StageInfoTxt;
    /// <summary>
    /// 현재 파티 고정 전투력을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_PartyAtkTxt;
    /// <summary>
    /// 사용자가 보유한 골드를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_UserGoldTxt;
    /// <summary>
    /// 사용자가 보유한 유료 재화를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_UserCashTxt;
    /// <summary>
    /// 사용자가 보유한 별의 조각 수량을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_UserSpItemTxt;

    /// <summary>
    /// 레이드 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    [Header("Menu group UI elements")]
    public Button m_RaidBtn;
    /// <summary>
    /// 파티 성장 강화 패널을 여는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_PartyLvUpBtn;
    /// <summary>
    /// 도감 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    [FormerlySerializedAs("m_CharacterBtn")] public Button m_CollectionBtn;
    /// <summary>
    /// 인벤토리 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_InventoryBtn;
    /// <summary>
    /// 설정 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_SettingBtn;
    [SerializeField] private Button m_GachaBtn;

    /// <summary>
    /// 패널 목록의 배치 기준으로 사용하는 Transform 참조입니다.
    /// </summary>
    [Header("Panels")]
    public Transform[] m_PanelList;
    [SerializeField] private CollectionPanelMgr m_CollectionPanelMgr;
    [SerializeField] private CharDetailPanelMgr m_CharDetailPanelMgr;

    // Panel 중복오픈 방지용 코드
    /// <summary>
    /// 정보 패널에서 전환할 수 있는 메뉴 종류를 나타냅니다.
    /// </summary>
    public enum PanelType {
        /// <summary>
        /// 레이드 보스 선택 메뉴입니다.
        /// </summary>
        Raid,
        /// <summary>
        /// 파티 성장 강화 메뉴입니다.
        /// </summary>
        PartyUp,
        /// <summary>
        /// 캐릭터 도감과 파티 편성 메뉴입니다.
        /// </summary>
        Collection,
        /// <summary>
        /// 보유 아이템을 확인하는 인벤토리 메뉴입니다.
        /// </summary>
        Inventory,
        /// <summary>
        /// 게임 설정 메뉴입니다.
        /// </summary>
        Setting,
        Gacha
    };
    

    private PartyMgr m_PartyMgr;
    private StageMgr m_StageMgr;
    /// <summary>
    /// 메뉴가 전투 입력과 시간 진행을 일시정지하고 있는지 나타냅니다.
    /// </summary>
    public static bool IsMenuPaused { get; private set; }
    public static float TimeScaleBeforeMenu { get; private set; } = 1f;
    private bool m_OwnsMenuPause;
    private float m_TimeScaleBeforeMenu = 1f;

#region Unity Event Method

    void Start()
    {
        // 플레이어 정보 초기값 설정
        if (m_StageInfoTxt != null)
            m_StageInfoTxt.text = $"{GlobalValue.CurStageData.CurChapter}-{GlobalValue.CurStageData.CurStage} 스테이지";

        if (m_UserGoldTxt != null)
            m_UserGoldTxt.text = $"{GlobalValue.UserGold} G";

        if (m_UserCashTxt != null)
            m_UserCashTxt.text = $"{GlobalValue.RealMoney} RM";

        // 버튼 메서드 대기
        if (m_RaidBtn != null)
            m_RaidBtn.onClick.AddListener(() => OpenPanel(PanelType.Raid));
        
        if (m_PartyLvUpBtn != null)
            m_PartyLvUpBtn.onClick.AddListener(() => OpenPanel(PanelType.PartyUp));

        if (m_CollectionBtn != null)
            m_CollectionBtn.onClick.AddListener(() => OpenPanel(PanelType.Collection));

        if (m_InventoryBtn != null)
            m_InventoryBtn.onClick.AddListener(() => OpenPanel(PanelType.Inventory));

        if (m_SettingBtn != null)
            m_SettingBtn.onClick.AddListener(() => OpenPanel(PanelType.Setting));
        if (m_GachaBtn != null)
            m_GachaBtn.onClick.AddListener(OpenGachaPanel);
    }

    private void Update()
    {
        // GameScene이 늦게 로드되거나 다시 로드된 경우 연결
        if (m_PartyMgr == null)
            TryBindPartyMgr();
        
        if (m_StageMgr == null)
            TryBindStageMgr();
        SetMenuPaused(HasOpenMenu());
    }

    private void OnEnable()
    {
        ResolveCollectionPanels();
        // 상세보기는 명시적인 컬렉션 진입 요청으로만 열린다.
        if (m_CharDetailPanelMgr != null && !m_CharDetailPanelMgr.IsEditing)
            m_CharDetailPanelMgr.CancelForMenuExit();
        TryBindPartyMgr();
        TryBindStageMgr();

        GlobalValue.OnUserGoldChanged += UpdateGold;
        GlobalValue.OnUserRealMoneyChanged += UpdateCash;
        GlobalValue.OnUserStarFragmentsChanged += UpdateSpItem;
        UpdateSpItem(GlobalValue.UserStarFragments);
    }

    private void OnDisable()
    {
        ClosePanel();
        SetMenuPaused(false);
        if (m_PartyMgr != null)
            m_PartyMgr.OnPartyCombatPowerChanged -= UpdateCombatPower;

        m_PartyMgr = null;

        if (m_StageMgr != null)
            m_StageMgr.OnStageChanged -= UpdateStage;

        m_StageMgr = null;

        GlobalValue.OnUserGoldChanged -= UpdateGold;
        GlobalValue.OnUserRealMoneyChanged -= UpdateCash;
        GlobalValue.OnUserStarFragmentsChanged -= UpdateSpItem;
    }
#endregion

#region Runtime Mgr Bind
    private void TryBindPartyMgr()
    {
        PartyMgr party = FindFirstObjectByType<PartyMgr>();
        if (party == null)
            return;

        m_PartyMgr = party;
        m_PartyMgr.OnPartyCombatPowerChanged += UpdateCombatPower;
        UpdateCombatPower(m_PartyMgr.PartyCombatPower);
    }

    private void TryBindStageMgr()
    {
        StageMgr stage = FindFirstObjectByType<StageMgr>();

        if (stage == null)
            return;

        m_StageMgr = stage;
        m_StageMgr.OnStageChanged += UpdateStage;
    }
#endregion

#region UI Update By Event Action
    private void UpdateCombatPower(long power)
    {
        if (m_PartyAtkTxt != null)
            m_PartyAtkTxt.text = power.ToString("N0") + " pt";
    }
    
    private void UpdateStage(int chapter, int stage)
    {
        if (m_StageInfoTxt != null)
            m_StageInfoTxt.text = $"{chapter}-{stage} 스테이지";
    }

    private void UpdateGold(ulong amount)
    {
        if (m_UserGoldTxt != null)
            m_UserGoldTxt.text = $"{amount} G";
    }

    private void UpdateCash(ulong amount)
    {
        if (m_UserCashTxt != null)
            m_UserCashTxt.text = $"{amount} RM";
    }


    private void UpdateSpItem(ulong amount)
    {
        if (m_UserSpItemTxt != null)
            m_UserSpItemTxt.text = $"{amount} 개";
    }
#endregion

#region Panel Open / Close
    public void OpenCollectionPanel() => OpenPanel(PanelType.Collection);

    void OpenPanel(PanelType panel)
    {
        if (PlayerDataMgr.IsProgressBlocked || SceneChangeMgr.IsTransitioning) return;
        if (BattleFlowMgr.IsFieldInputBlocked || BattleFlowMgr.CurrentMode == BattleFlowMgr.BattleMode.Raid)
        {
            TextPoolMgr.Inst?.ShowSystemMessage("레이드 중에는 사용 불가능한 기능입니다.");
            return;
        }
        int a_Index = (int)panel;
        if (m_PanelList == null || a_Index < 0 ||
            a_Index >= m_PanelList.Length || m_PanelList[a_Index] == null)
        {
            PDebug.LogError("메뉴 패널 연결이 올바르지 않습니다.");
            return;
        }

        SetMenuPaused(true);
        ClosePanel();
        if (panel == PanelType.Collection && m_CollectionPanelMgr != null)
            m_CollectionPanelMgr.OpenCollection();
        else
            m_PanelList[a_Index].gameObject.SetActive(true);
        if (!HasOpenMenu())
            SetMenuPaused(false);
    }

    void ClosePanel()
    {
        ResolveCollectionPanels();
        // 숨겨져 있는 컬렉션 편집본도 종료한다. 상세보기 종료 이벤트로 복귀하지 않는다.
        if (m_CollectionPanelMgr != null) m_CollectionPanelMgr.CloseCollection();
        if (m_CharDetailPanelMgr != null) m_CharDetailPanelMgr.CancelForMenuExit();
        if (m_PanelList == null)
            return;

        foreach(Transform panel in m_PanelList)
            if (panel != null)
                panel.gameObject.SetActive(false);
    }

    /// <summary>씬 전환 요청이 수락된 뒤 메뉴와 메뉴 일시정지를 정리한다.</summary>
    public void CloseMenusForRaidTransition()
    {
        ClosePanel();
        SetMenuPaused(false);
    }

    /// <summary>뽑기 진입 버튼용. PanelList의 마지막 Gacha(인덱스 5)에 패널을 연결한다.</summary>
    public void OpenGachaPanel() => OpenPanel(PanelType.Gacha);

    private bool HasOpenMenu()
    {
        ResolveCollectionPanels();
        if (m_CharDetailPanelMgr != null && m_CharDetailPanelMgr.gameObject.activeInHierarchy)
            return true;
        if (m_PanelList == null)
            return false;

        foreach (Transform panel in m_PanelList)
            if (panel != null && panel.gameObject.activeInHierarchy)
                return true;

        return false;
    }

    private void ResolveCollectionPanels()
    {
        if (m_CollectionPanelMgr == null && m_PanelList != null &&
            (int)PanelType.Collection < m_PanelList.Length && m_PanelList[(int)PanelType.Collection] != null)
            m_CollectionPanelMgr = m_PanelList[(int)PanelType.Collection]
                .GetComponentInChildren<CollectionPanelMgr>(true);
        if (m_CharDetailPanelMgr == null)
            m_CharDetailPanelMgr = FindFirstObjectByType<CharDetailPanelMgr>(FindObjectsInactive.Include);
    }

    private void SetMenuPaused(bool isPaused)
    {
        if (isPaused)
        {
            if (m_OwnsMenuPause)
                return;

            m_TimeScaleBeforeMenu = PlayerDataMgr.IsProgressBlocked ? PlayerDataMgr.ResumeTimeScale : Time.timeScale;
            TimeScaleBeforeMenu = m_TimeScaleBeforeMenu;
            m_OwnsMenuPause = true;
            IsMenuPaused = true;
            Time.timeScale = 0f;

            TargetSelMgr a_TargetMgr = FindFirstObjectByType<TargetSelMgr>();
            if (a_TargetMgr != null)
                a_TargetMgr.ClearTargets();

            foreach (CharacterBase a_Character in
                     FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
                a_Character.CancelSkillSelectionForMenu();
        }
        else
        {
            if (!m_OwnsMenuPause)
                return;

            m_OwnsMenuPause = false;
            IsMenuPaused = false;
            Time.timeScale = PlayerDataMgr.IsProgressBlocked ? 0f : m_TimeScaleBeforeMenu;
        }
    }
#endregion

}
