using UnityEngine;
using UnityEngine.UI;
using System;

/// <summary>
/// 현재 파티의 고정 슬롯 정보와 배치 대상 강조를 표시하는 UI 셀입니다.
/// </summary>
public class CurPartyCharCellMgr : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private Sprite[] m_CharRoleSprites = new Sprite[(int)CharacterRole.Count];
    [SerializeField] private Image m_CharFrameImg;
    [SerializeField] private Image m_CharIllustImg;
    [SerializeField] private Transform m_StarsGroup;
    [SerializeField] private Text m_LevelTxt;
    [SerializeField] private Image m_RoleIconImg;
    [SerializeField] private Text m_PartyIndexTxt;

    [SerializeField] private Button m_CellBtn;
    [SerializeField] private Outline m_AssignOutline;
    
    private CharData m_CharData;
    private int m_SlotIndex = -1;

    /// <summary>
    /// 현재 파티 슬롯이 클릭되었을 때 캐릭터 데이터와 고정 슬롯 번호를 전달합니다.
    /// </summary>
    public event Action<CharData, int> OnCellClicked;
    
    void Awake()
    {
        if (m_CellBtn != null)
            m_CellBtn.onClick.AddListener(HandleCellClick);
    }

    void OnDestroy()
    {
        if (m_CellBtn != null)
            m_CellBtn.onClick.RemoveListener(HandleCellClick);

        OnCellClicked = null;
    }

    void HandleCellClick()
    {
        if (m_SlotIndex < 0)
            return;

        // 빈 슬롯에서는 m_CharData가 null이어도 전달
        OnCellClicked?.Invoke(m_CharData, m_SlotIndex);
    }
    
    /// <summary>
    /// 고정 파티 슬롯 번호와 캐릭터 데이터로 현재 파티 셀을 갱신합니다.
    /// </summary>
    /// <param name="charData">셀에 표시할 사용자 캐릭터 데이터입니다.</param>
    /// <param name="index">고정 슬롯 또는 목록에서 사용할 인덱스입니다.</param>
    public void InitializeCell(CharData charData, int index)
    {
        m_SlotIndex = index;
        m_CharData = null;

        // 빈 슬롯의 기본 표시
        m_PartyIndexTxt.text = $"{index + 1}";
        SetFrameColor(Color.white);

        m_CharIllustImg.sprite = null;
        m_CharIllustImg.enabled = false;

        m_RoleIconImg.sprite = null;
        m_RoleIconImg.enabled = false;

        m_LevelTxt.text = "";

        foreach (Transform a_Star in m_StarsGroup)
            a_Star.gameObject.SetActive(false);

        // 빈 슬롯도 버튼은 사용 가능
        m_CellBtn.interactable = true;

        if (charData == null)
            return;

        if (!MasterDataMgr.Inst.TryGetCharacter(
                charData.CharId, out CharacterData a_CharacterData)
            || a_CharacterData == null)
        {
            PDebug.LogError("Character Data Not Found");
            return;
        }

        m_CharData = charData;

        SetFrameColor(
            GlobalConstants.GetRarityColor32(a_CharacterData.Rarity));

        m_CharIllustImg.sprite = a_CharacterData.Illustration;
        m_CharIllustImg.enabled = true;

        m_RoleIconImg.sprite =
            m_CharRoleSprites[(int)a_CharacterData.Role];
        m_RoleIconImg.enabled = true;

        m_LevelTxt.text = $"Lv.{charData.CharLv}";

        for (int i = 0; i < m_StarsGroup.childCount; i++)
            m_StarsGroup.GetChild(i).gameObject.SetActive(
                i <= (int)a_CharacterData.Rarity);
    }

    void SetFrameColor(Color32 a_Color)
    {
        m_CharFrameImg.color = a_Color;
    }
    
    /// <summary>
    /// 캐릭터 배치 대상으로 선택할 수 있음을 나타내는 외곽선을 전환합니다.
    /// </summary>
    /// <param name="isActive">강조 표시를 활성화할지 여부입니다.</param>
    public void SetAssignOutline(bool isActive)
    {
        if (m_AssignOutline != null)
            m_AssignOutline.enabled = isActive;
    }
}
