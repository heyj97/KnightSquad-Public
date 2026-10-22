using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 레이드 보스 한 명의 표시 데이터와 권장 전투력 설정을 묶습니다.
/// </summary>
[Serializable]
public class BossPanelSetting
{
    /// <summary>
    /// 레이드 목록과 미리보기에 사용할 보스 마스터 데이터입니다.
    /// </summary>
    public MonsterData m_MonsterData;
    /// <summary>
    /// 레이드 UI에 표시할 권장 파티 전투력입니다.
    /// </summary>
    public int m_RecommendBattleStat;
    /// <summary>
    /// 레이드 도전 가능 여부를 판단할 최소 파티 전투력입니다.
    /// </summary>
    public int m_MinimumBattleStat;
    /// <summary>
    /// 레이드 보스 이름과 강조 UI에 적용할 고유 색상입니다.
    /// </summary>
    public Color32 m_MonsterPersonalColor;

    // 기본 생성자 함수(미사용)
    /// <summary>
    /// 레이드 목록에 표시할 보스 데이터와 전투력 기준 및 강조 색상을 설정합니다.
    /// </summary>
    public BossPanelSetting() { }

    /// <summary>
    /// 레이드 목록에 표시할 보스 데이터와 전투력 기준 및 강조 색상을 설정합니다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <param name="model">보스 미리보기로 사용할 모델 오브젝트입니다.</param>
    /// <param name="recommendBattleStat">UI에 표시할 권장 파티 전투력입니다.</param>
    /// <param name="minimumBattleStat">도전 가능 여부를 판단할 최소 파티 전투력입니다.</param>
    /// <param name="color">텍스트 또는 보스 강조 표시에 적용할 색상입니다.</param>
    public BossPanelSetting(MonsterData data, GameObject model, int recommendBattleStat, int minimumBattleStat, Color32 color)
    {
        m_MonsterData = data;
        m_RecommendBattleStat = recommendBattleStat;
        m_MinimumBattleStat = minimumBattleStat;
        m_MonsterPersonalColor = color;
    }
}

/// <summary>
/// 레이드 보스 선택, 미리보기, 드롭 정보 및 도전 확인 흐름을 관리합니다.
/// </summary>
public class RaidPanelMgr : MonoBehaviour
{
    /// <summary>
    /// 레이드 선택 화면 전체를 표시하거나 숨길 패널 루트입니다.
    /// </summary>
    [Header("UI Elements")]
    public Transform m_RaidPanel;
    /// <summary>
    /// 보스 이름을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_BossNameTxt;
    /// <summary>
    /// 설명을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_DescriptionTxt;
    /// <summary>
    /// 선택된 보스에 대한 도전 확인 메시지를 여는 버튼입니다.
    /// </summary>
    public Button m_ChallengeBtn;
    /// <summary>
    /// 이전 레이드 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_PrevRaidBtn;
    /// <summary>
    /// 다음 레이드 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_NextRaidBtn;
    /// <summary>
    /// 닫기 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_CloseBtn;

    /// <summary>
    /// 선택된 보스의 드롭 아이콘 셀을 배치하는 부모 Transform입니다.
    /// </summary>
    [Header("Drop Table")]
    public Transform m_DropTableContent;
    /// <summary>
    /// 레이드 드롭 장비 하나를 표시하기 위해 생성할 UI 셀 프리팹입니다.
    /// </summary>
    public GameObject m_DTIconCell;

    /// <summary>
    /// 선택된 보스 모델을 렌더 텍스처에 촬영하는 카메라입니다.
    /// </summary>
    [Header("Boss Renter Texture Settings")]
    public Camera m_PreviewCamera;
    /// <summary>
    /// 레이드 보스 미리보기 모델을 배치할 루트 오브젝트입니다.
    /// </summary>
    public GameObject m_PreviewBossModelRoot;
    
    /// <summary>
    /// 레이드 선택 화면에서 순환 표시할 보스 설정 목록입니다.
    /// </summary>
    [Header("Boss List")]
    public List<BossPanelSetting> m_BossPanelSettingsList = new();
    

    private int m_CurrentBossIndex = 0;
    private PartyMgr m_PartyMgr;
    private RaidBossPreview m_BossPreview;
    
#region Unity Event Method

    void Start()
    {
        if (m_ChallengeBtn != null)
            m_ChallengeBtn.onClick.AddListener(ChallengeClick);
        if (m_PrevRaidBtn != null)
            m_PrevRaidBtn.onClick.AddListener(() =>
            {
                if (m_BossPanelSettingsList == null || m_CurrentBossIndex <= 0)
                    return;
                m_CurrentBossIndex--;
                ViewRaid();
            });

        if (m_NextRaidBtn != null)
            m_NextRaidBtn.onClick.AddListener(() =>
            {
                if (m_BossPanelSettingsList == null || m_CurrentBossIndex >= m_BossPanelSettingsList.Count - 1)
                    return;
                m_CurrentBossIndex++;
                ViewRaid();
            });

        if (m_CloseBtn != null)
            m_CloseBtn.onClick.AddListener(ClosePanel);
    }


    void Update()
    {
        if (m_PartyMgr == null)
            TryBindPartyMgr();
    }

    void OnEnable()
    {
        // 보스 아바타 랜더텍스쳐용 카메라 enabled(최적화용)
        if (m_PreviewCamera != null)
            m_PreviewCamera.enabled = true;
        
        // 프리뷰 랜더텍스쳐용 루트 가져오기
        m_BossPreview = m_PreviewBossModelRoot != null
            ? m_PreviewBossModelRoot.GetComponent<RaidBossPreview>() : null;
        if (m_BossPreview == null)
            PDebug.LogError("RaidBossPreview 참조가 없습니다.", this);
        
        // 파티매니저 찾기
        TryBindPartyMgr();
        
        // 레이드 UI 갱신
        ViewRaid();
    }

    void OnDisable()
    {
        if (m_PreviewCamera != null)
            m_PreviewCamera.enabled = false;
        
        if (m_PartyMgr != null)
            m_PartyMgr.OnPartyCombatPowerChanged -= RefreshBossPreviewText;
        
        m_PartyMgr = null;
        
        if (m_BossPreview != null)
            m_BossPreview.ClearBoss();
        
        ClearDropTable();
    }
#endregion

    void ClosePanel()
    {
        if (m_BossPreview != null)
            m_BossPreview.ClearBoss();
                    
        if (m_BossNameTxt != null)
            m_BossNameTxt.text = string.Empty;
                    
        if (m_DescriptionTxt != null)
            m_DescriptionTxt.text = string.Empty;
                    
        ClearDropTable();
                    
        m_RaidPanel.gameObject.SetActive(false);
    }

    void ViewRaid()
    {
        m_CurrentBossIndex = Mathf.Clamp(m_CurrentBossIndex, 0, Math.Max(0, (m_BossPanelSettingsList.Count) - 1));
        
        PrevNextBtnCtrl();
        
        // 보스를 못 받아오면 클릭이 불가능하도록
        // to-do TryGetCurrentBoss가 실패하면 메세지박스로 오류를 띄우는걸로 수정
        if (m_ChallengeBtn != null)
            m_ChallengeBtn.interactable = TryGetCurrentBoss(out _);
        
        RefreshBossPreviewText(m_PartyMgr != null ? m_PartyMgr.PartyCombatPower : 0);
        RefreshBossInformation();
    }

    
    private void TryBindPartyMgr()
    {
        if (m_PartyMgr != null)
            return;
        
        // Inst 접근으로 UI 단독 실행 중 빈 파티를 생성하지 않는다.
        m_PartyMgr = FindFirstObjectByType<PartyMgr>();
        
        if (m_PartyMgr != null)
        {
            m_PartyMgr.OnPartyCombatPowerChanged += RefreshBossPreviewText;
            RefreshBossPreviewText(m_PartyMgr.PartyCombatPower);
        }
    }

    private bool TryGetCurrentBoss(out BossPanelSetting setting)
    {
        setting = null;

        if (m_BossPanelSettingsList == null || m_CurrentBossIndex < 0 ||
            m_CurrentBossIndex >= m_BossPanelSettingsList.Count)
        {
            PDebug.LogError("Index out of range!");
            return false;
        }
        
        setting = m_BossPanelSettingsList[m_CurrentBossIndex];
        
        return setting != null && setting.m_MonsterData != null;
    }

    void PrevNextBtnCtrl()
    {
        int count = m_BossPanelSettingsList?.Count ?? 0;
        if (m_PrevRaidBtn != null)
            m_PrevRaidBtn.gameObject.SetActive(count > 0 && m_CurrentBossIndex > 0);
        if (m_NextRaidBtn != null)
            m_NextRaidBtn.gameObject.SetActive(m_CurrentBossIndex < count - 1);
    }

    void RefreshBossPreviewText(long partyCombatPower)
    {
        if (m_DescriptionTxt == null)
            return;
        
        if (!TryGetCurrentBoss(out BossPanelSetting a_CurBossSetting))
        {
            m_DescriptionTxt.text = string.Empty;
            return;
        }
        
        m_DescriptionTxt.text = $"입장전투력: {a_CurBossSetting.m_MinimumBattleStat} pt\n" +
                                $"권장전투력: {a_CurBossSetting.m_RecommendBattleStat} pt\n" +
                                $"내 전투력: {partyCombatPower} pt";
    }

    void RefreshBossInformation()
    {
        ClearDropTable();
        
        if (!TryGetCurrentBoss(out BossPanelSetting a_CurBossSetting))
        {
            if (m_BossPreview != null)
                m_BossPreview.ClearBoss();
            
            if (m_BossNameTxt != null)
                m_BossNameTxt.text = string.Empty;
            
            PDebug.LogError("보스 정보를 받아올 수 없습니다.");
            return;
        }

        // 몬스터 랜더링 초기화
        if (m_BossPreview != null)
            m_BossPreview.ShowBoss(a_CurBossSetting.m_MonsterData.Prefab);

        // 텍스트 초기화
        if (m_BossNameTxt != null)
            m_BossNameTxt.text = a_CurBossSetting.m_MonsterData.DisplayName;

        // 드롭테이블 스크롤뷰 초기화
        // Content, DTIconCell, DTIconCell.m_Icon의 연결이 없을 때 에러
        if (m_DropTableContent == null || m_DTIconCell == null ||
            !m_DTIconCell.TryGetComponent<DTIconCellMgr>(out var a_CellPrefab) || a_CellPrefab.m_Icon == null)
        {
            PDebug.LogError("레이드 드롭 목록 또는 아이콘 프리팹 참조가 없습니다.", this);
            return;
        }

        bool a_GetDropTable = GlobalConstants.RaidItemDropTable.TryGetValue(a_CurBossSetting.m_MonsterData.Id, out List<string> a_DropItemIds);

        if (!a_GetDropTable)
        {
            PDebug.LogError($"Can not get boss drop table: {a_CurBossSetting.m_MonsterData.Id}");
            return;
        }

        foreach (string a_ItemId in a_DropItemIds)
        {
            bool a_GetItemData = MasterDataMgr.Inst.TryGetEquipment(a_ItemId, out EquipmentData a_EquipData);

            if (!a_GetItemData)
            {
                PDebug.LogError($"Item Data In Drop Table Not Found: {a_ItemId}");
                continue;
            }

            GameObject a_DropItemCell = Instantiate(m_DTIconCell, m_DropTableContent);
            a_DropItemCell.GetComponent<DTIconCellMgr>().Initialize(a_EquipData);
        }
    }

    private void ClearDropTable()
    {
        if (m_DropTableContent == null)
            return;
        foreach (DTIconCellMgr cell in m_DropTableContent.GetComponentsInChildren<DTIconCellMgr>(true))
        {
            // Destroy가 처리되는 프레임 끝까지 이전 셀이 표시되지 않게 한다.
            cell.gameObject.SetActive(false);
            Destroy(cell.gameObject);
        }
    }
    
    void ChallengeClick()
    {
        // 레퍼런스를 찾을 수 없을 시 에러발생
        if (!TryGetCurrentBoss(out BossPanelSetting a_CurBossSetting) || m_PartyMgr == null)
        {
            MessageBoxMgr.Inst.ShowMsg(
                "에러가 발생했습니다. 다시 시도해주세요", 
                "에러 발생!", 
                MessageBoxType.Error,
                ClosePanel);
            return;
        }

        // 최소 입장 전투력을 충족하지 못했을 때
        if (m_PartyMgr.PartyCombatPower < a_CurBossSetting.m_MinimumBattleStat)
        {
            MessageBoxMgr.Inst.ShowMsg(
                $"파티 전투력이 최소 입장 전투력보다 낮습니다",
                $"{a_CurBossSetting.m_MonsterData.DisplayName} 레이드 입장 불가",
                MessageBoxType.Error
                );

            return;
        }
        
        string a_Msg = "";
        
        // 입장 전투력은 충족했으나 권장 전투력보다 낮을 때 경고
        if (m_PartyMgr.PartyCombatPower < a_CurBossSetting.m_RecommendBattleStat)
            a_Msg =
                $"현재 파티 전투력이 " +
                $"<color=#{ColorUtility.ToHtmlStringRGB(a_CurBossSetting.m_MonsterPersonalColor)}>" +
                $"{a_CurBossSetting.m_MonsterData.DisplayName} 레이드</color> 권장 전투력보다 " +
                $"<color=#ff0000>{a_CurBossSetting.m_RecommendBattleStat - m_PartyMgr.PartyCombatPower}</color> 낮습니다.\n" +
                $"그래도 도전하시겠습니까?";
        
        // 권장 전투력을 충족했을 시 알림
        else
            a_Msg = $"{a_CurBossSetting.m_MonsterData.DisplayName}레이드에 도전 하시겠습니까?";
            
        // 메세지박스 출력, 확인 클릭 시 레이드 입장 메서드 연결, 취소 클릭 시 아무것도 하지않고 메세지박스 꺼짐
        MessageBoxMgr.Inst.ShowMsg(
            a_Msg, 
            $"{a_CurBossSetting.m_MonsterData.DisplayName} 레이드 입장",
            MessageBoxType.Submit, 
            () => OnRaidEnterSubmitClick(a_CurBossSetting.m_MonsterData)
        );
    }

    void OnRaidEnterSubmitClick(MonsterData bossData)
    {
        
        StartCoroutine(EnterRaidCoroutine(bossData));
    }


    private IEnumerator EnterRaidCoroutine(MonsterData bossData)
    {
        OnRaidUIMgr raidUI = FindFirstObjectByType<OnRaidUIMgr>();
        if (raidUI != null)
            raidUI.EnterRaidField(bossData);
        else
            MessageBoxMgr.Inst.ShowMsg("OnRaidUIMgr 연결을 확인해주세요.", "입장 실패", MessageBoxType.Error);
        yield break;
    }

}
