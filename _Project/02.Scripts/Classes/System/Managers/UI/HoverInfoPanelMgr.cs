using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 마우스로 가리킨 캐릭터의 능력치와 상태를 정보 패널에 표시합니다.
/// </summary>
public class HoverInfoPanelMgr : MonoBehaviour
{
    /// <summary>
    /// 캐릭터 패널에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    [Header("Character Panel UI Elements")]
    public GameObject m_CharPanel;
    [SerializeField] private Vector2 m_MouseOffset = new(10f, 10f);
    /// <summary>
    /// 캐릭터 아이콘을 표시하는 이미지 UI 참조입니다.
    /// </summary>
    [Header("Char Base Info")]
    public Image m_CharIconImg;
    /// <summary>
    /// 캐릭터 이름을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_CharNameTxt;
    /// <summary>
    /// 현재 체력을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_CurHpTxt;
    /// <summary>
    /// 캐릭터 역할별 아이콘을 인덱스로 조회하는 스프라이트 배열입니다.
    /// </summary>
    [Header("Char Position Info")]
    public Sprite[] m_CharTypeSprites;
    /// <summary>
    /// 현재 캐릭터의 역할 아이콘을 표시하는 이미지 UI 참조입니다.
    /// </summary>
    public Image m_CharTypeImg;
    /// <summary>
    /// 캐릭터 종류를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_CharTypeTxt;
    /// <summary>
    /// 체력 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    [Header("Char Stat Info")]
    public Text m_HpInfoTxt;
    /// <summary>
    /// 공격력 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_AtkInfoTxt;
    /// <summary>
    /// 공격력 속도 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_AtkSpdInfoTxt;
    /// <summary>
    /// 방어력 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_DefInfoTxt;
    /// <summary>
    /// 치명타 확률 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_CritChanceInfoTxt;
    /// <summary>
    /// 치명타 피해 정보를 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_CritDmgInfoTxt;
    

    private CharacterBase m_HoverCharacter;
    private RectTransform m_PanelRect;

    void Awake()
    {
        m_PanelRect = m_CharPanel.GetComponent<RectTransform>();
        // 마우스 오른쪽 위로 펼쳐지도록 패널의 왼쪽 아래를 기준점으로 사용한다.
        m_PanelRect.pivot = Vector2.zero;
    }

    void LateUpdate()
    {
        if (m_HoverCharacter == null || !m_CharPanel.activeInHierarchy)
            return;

        UpdatePanelPosition();
    }

    void UpdatePanelPosition()
    {
        RectTransform parentRect = m_PanelRect.parent as RectTransform;

        // Screen Space - Overlay Canvas이므로 카메라 없이 UI 좌표로 변환한다.
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, Input.mousePosition, null, out Vector2 localPoint))
        {
            Vector2 position = localPoint + m_MouseOffset;
            m_PanelRect.position = parentRect.TransformPoint(
                new Vector3(position.x, position.y, 0f));
        }
    }

    void OnEnable()
    {
        TargetSelMgr.OnTargetHoverEnter += CharacterPanelOn;
        TargetSelMgr.OnTargetHoverExit += CharacterPanelOff;
    }

    void OnDisable()
    {
        TargetSelMgr.OnTargetHoverEnter -= CharacterPanelOn;
        TargetSelMgr.OnTargetHoverExit -= CharacterPanelOff;
        UnsubscribeCharacter();
    }

    void CharacterPanelOn(ITargetable target)
    {
        if (target is not CharacterBase character)
            return;
        
        UnsubscribeCharacter();
        m_HoverCharacter = character;
        character.OnCharHpChanged += SetCurHpTxt;
        character.OnCharStatsChanged += HandleStatsChanged;

        SetCharDataOnPanel(character);
        SetCurHpTxt(character.CurHp, character.MaxHp);
        SetStatTxt(character);
        m_CharPanel.SetActive(true);
        UpdatePanelPosition();
    }

    void SetCharDataOnPanel(CharacterBase target)
    {
        m_CharIconImg.sprite = target.CharData.Icon;
        m_CharNameTxt.text = $"<color=#{GlobalConstants.GetRarityHexColor(target.CharData.Rarity)}>{target.CharData.DisplayName}</color>";
        m_CharTypeImg.sprite = m_CharTypeSprites[(int)target.CharData.Role];
        m_CharTypeTxt.text = GlobalConstants.GetKorRoleName(target.CharData.Role);
    }
    
    void CharacterPanelOff(ITargetable target)
    {
        if (!ReferenceEquals(target, m_HoverCharacter))
            return;

        UnsubscribeCharacter();
        m_CharPanel.SetActive(false);
        ClearDataOnPanel();
    }

    void UnsubscribeCharacter()
    {
        // Unity 오브젝트가 파괴되었더라도 남아 있는 관리 객체의 이벤트를 해제한다.
        if (!ReferenceEquals(m_HoverCharacter, null))
        {
            m_HoverCharacter.OnCharHpChanged -= SetCurHpTxt;
            m_HoverCharacter.OnCharStatsChanged -= HandleStatsChanged;
        }
        m_HoverCharacter = null;
    }

    void HandleStatsChanged()
    {
        if (m_HoverCharacter != null)
            SetStatTxt(m_HoverCharacter);
    }

    void ClearDataOnPanel()
    {
        m_CharIconImg.sprite = null;
        m_CharNameTxt.text = "";
        m_CurHpTxt.text = "";
        
        m_CharTypeImg.sprite = null;
        m_CharTypeTxt.text = "";

        m_HpInfoTxt.text = "";
        m_AtkInfoTxt.text = "";
        m_AtkSpdInfoTxt.text = "";
        m_DefInfoTxt.text = "";
        m_CritChanceInfoTxt.text = "";
        m_CritDmgInfoTxt.text = "";
    }

    void SetCurHpTxt(float curHp, float maxHp)
    {
        m_CurHpTxt.text = $"<color=#00ff00>{curHp:N0} / {maxHp:N0}</color>";
    }

    void SetStatTxt(CharacterBase target)
    {
        float _incHp = IncreaseAmount(target.MaxHp, target.CharData.BaseMaxHp);
        float _incAtk = IncreaseAmount(target.AttackPower, target.CharData.BaseAttackPower);
        float _incAtkSpd = IncreaseAmount(target.AttackSpeed, target.CharData.BaseAttackSpeed);
        float _incDef = IncreaseAmount(target.Defense, target.CharData.BaseDefense);
        float _incCritChan = IncreaseAmount(target.CriticalChance, target.CharData.BaseCriticalChance);
        float _incCritDmg = IncreaseAmount(target.CriticalDamageMultiplier, target.CharData.BaseCriticalDamageMultiplier);

        m_HpInfoTxt.text = $"최대체력 : {target.MaxHp:N1}{GetIncreaseTxt(_incHp)}";
        m_AtkInfoTxt.text = $"공격력 : {target.AttackPower:N1}{GetIncreaseTxt(_incAtk)}";
        m_AtkSpdInfoTxt.text = $"공격속도 : {target.AttackSpeed:N1}{GetIncreaseTxt(_incAtkSpd)}";
        m_DefInfoTxt.text = $"방어력 : {target.Defense:N1}{GetIncreaseTxt(_incDef)}";
        m_CritChanceInfoTxt.text =$"치명타 확률 : {target.CriticalChance:N1}{GetIncreaseTxt(_incCritChan)}";
        m_CritDmgInfoTxt.text = $"치명타 증폭 : {target.CriticalDamageMultiplier:N1}{GetIncreaseTxt(_incCritDmg)}";
    }


    string GetIncreaseTxt(float value)
    {
        return value > 0f
            ? $" <color=#00ff00>(+{value:N1})</color>"
            : "";
    }

    float IncreaseAmount(float finalValue, float baseValue)
    {
        return finalValue - baseValue > 0f ? finalValue - baseValue : 0f;
    }
}
