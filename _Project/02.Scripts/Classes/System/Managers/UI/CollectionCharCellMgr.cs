using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 보유 캐릭터 정보와 파티 배치 동작을 제공하는 도감 UI 셀입니다.
/// </summary>
public class CollectionCharCellMgr : MonoBehaviour
{
    [SerializeField] private Sprite[] m_CharRoleSprites = new Sprite[(int)CharacterRole.Count];
    
    [Header("UI Elements")]
    [SerializeField] private Image m_CharFrameImg;
    [SerializeField] private Image m_CharIllustImg;
    [SerializeField] private Transform m_StarsGroup;
    [SerializeField] private Image m_CharRoleImg;
    [SerializeField] private Text m_CharLevelTxt;
    [SerializeField] private Text m_CharNameTxt;
    [SerializeField] private Image m_CharStatusImg;

    [Header("On cell click UI")] 
    [SerializeField] private Button m_CellBtn;
    [SerializeField] private Transform m_OnAssignClickPanel;
    [SerializeField] private Button m_AssignToPartyBtn;
    [FormerlySerializedAs("m_CancelOnClickBtn")] [SerializeField] private Button m_CancelOnAssignClickBtn;
    
    /// <summary>
    /// 사용자가 이 셀의 캐릭터를 파티에 배치하려고 선택했을 때 캐릭터 데이터를 전달합니다.
    /// </summary>
    public event Action<CharData> OnAssignClick;
    /// <summary>
    /// 이 도감 셀이 선택되었을 때 셀 인스턴스를 전달합니다.
    /// </summary>
    public event Action<CollectionCharCellMgr> OnCellClicked;
    private CharData m_CharData;
    /// <summary>
    /// 이 도감 셀이 표시하는 사용자 보유 캐릭터 데이터를 가져옵니다.
    /// </summary>
    public CharData UserCharData => m_CharData;
    private bool m_IsPendingCharData;
    private CharData m_PendingCharData;
    

    void Start()
    {
        if (m_CharFrameImg == null ||
            m_CharIllustImg == null ||
            m_StarsGroup == null ||
            m_CharRoleImg == null ||
            m_CharLevelTxt == null ||
            m_CharNameTxt == null ||
            m_CharStatusImg == null ||
            m_CellBtn == null ||
            m_OnAssignClickPanel == null ||
            m_AssignToPartyBtn == null ||
            m_CancelOnAssignClickBtn == null
            )
        {
            PDebug.LogError("UI Elements Not Found");
            return;
        }
        
        m_CellBtn.onClick.AddListener(CellClick);
        m_AssignToPartyBtn.onClick.AddListener(AssignToPartyClick);
        m_CancelOnAssignClickBtn.onClick.AddListener(CancelAssignToPartyClick);
    }

    /// <summary>
    /// 파티 배치 대기 중 우클릭과 ESC 입력을 확인하여 필요하면 대기를 취소합니다.
    /// </summary>
    public void Update()
    {
        if (m_IsPendingCharData)
        {
            if (Input.GetMouseButtonDown(1))
                CancelAssignToPartyClick();
        }
    }

    /// <summary>
    /// 사용자 캐릭터 데이터로 도감 셀의 이름, 역할, 능력치와 성장 상태를 채웁니다.
    /// </summary>
    /// <param name="charData">셀에 표시할 사용자 캐릭터 데이터입니다.</param>
    public void InitializeCell(CharData charData)
    {
        bool a_GetCharData = MasterDataMgr.Inst.TryGetCharacter(charData.CharId, out CharacterData a_CharDataBase);

        if (!a_GetCharData)
        {
            PDebug.LogError($"Character Data Not Found: {charData.CharId}");
            return;
        }

        m_CharData = charData;
        
        // 캐릭터 레어리티에 맞는 색상으로 Frame color 변경
        SetFrameColor(GlobalConstants.GetRarityColor32(a_CharDataBase.Rarity));

        // 케릭터 일러스트 설정
        m_CharIllustImg.sprite = a_CharDataBase.Illustration;
        
        // 캐릭터 레어리티에 맞게 StarsGroup의 자식 SetActive 설정
        for (int i = 0; i < m_StarsGroup.childCount; i++)
        {
            if (i <= (int)a_CharDataBase.Rarity)
                m_StarsGroup.GetChild(i).gameObject.SetActive(true);
            else
                m_StarsGroup.GetChild(i).gameObject.SetActive(false);
        }
        
        // 캐릭터 역할에 맞게 아이콘 설정
        m_CharRoleImg.sprite = m_CharRoleSprites[(int)a_CharDataBase.Role];

        // 캐릭터 레벨 설정
        m_CharLevelTxt.text = $"Lv. {charData.CharLv}";

        // 캐릭터 파티 배치여부 설정
        if (GlobalValue.PartyCharIdList.Contains(charData.CharId))
            m_CharStatusImg.gameObject.SetActive(true);
        else
            m_CharStatusImg.gameObject.SetActive(false);

        // 캐릭터 이름 설정
        m_CharNameTxt.text = CharNameSplitter(a_CharDataBase.DisplayName)[1];
        CancelAssignToPartyClick();
    }

    /// <summary>
    /// 현재 편집 중인 파티 목록을 기준으로 셀의 편성 상태 표시를 갱신합니다.
    /// </summary>
    /// <param name="partyCharIds">현재 편집 중인 파티의 고정 슬롯별 캐릭터 ID 목록입니다.</param>
    public void RefreshPartyStatus(System.Collections.Generic.List<string> partyCharIds)
    {
        m_CharStatusImg.gameObject.SetActive(
            m_CharData != null && partyCharIds != null &&
            partyCharIds.Contains(m_CharData.CharId));
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
    
    void SetFrameColor(Color32 color)
    {
        m_CharFrameImg.color = color;
    }

    void CellClick()
    {
        if (m_CharData == null)
            return;

        OnCellClicked?.Invoke(this);
        m_IsPendingCharData = true;
        m_PendingCharData = m_CharData;
        
        m_OnAssignClickPanel.gameObject.SetActive(true);
    }
    
    void AssignToPartyClick()
    {
        if (!m_IsPendingCharData || m_PendingCharData == null)
            return;

        OnAssignClick?.Invoke(m_PendingCharData);
        CancelAssignToPartyClick();
    }

    /// <summary>
    /// 이 셀의 파티 배치 대기 상태와 관련 UI를 취소합니다.
    /// </summary>
    public void CancelAssignToPartyClick()
    {
        m_IsPendingCharData = false;
        m_PendingCharData = null;
        
        if (m_OnAssignClickPanel != null)
            m_OnAssignClickPanel.gameObject.SetActive(false);
    }
}
