using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 캐릭터 모델 생성, 런타임 초기화 및 선택 강조 표시를 연결합니다.
/// </summary>
public class CharacterRootMgr : MonoBehaviour
{
    private const int MaxVisibleBuffCount = 4;

    private CharacterBase m_CharacterBase;
    private bool m_IsInitialized;
    private readonly List<BuffIconCellMgr> m_BuffIconCells = new();

    [Header("Character model root")]
    [SerializeField] private GameObject m_CharModelRoot;

    [Header("Character Info UI elements")]
    [SerializeField] private Image m_CurHpImg;
    [SerializeField] private Image m_CharTypeIconImg;
    [SerializeField] private Sprite[] CharTypeIcons;
    [SerializeField] private Text m_CharNameTxt;

    [Header("Buff UI elements")]
    [SerializeField] private GameObject m_SkillCellPrefab;
    [SerializeField] private Transform m_BuffGroup;
    [SerializeField] private Text m_BuffCountTxt;

    private SpriteRenderer m_ShadowRenderer;
    private bool m_IsShadowSelected;
    private bool m_IsBuffTargetHighlighted;
    private HitFlash m_HitFlash;

    private void Awake()
    {
        m_CharacterBase = GetComponent<CharacterBase>();
        m_HitFlash = GetComponent<HitFlash>();
    }

    private void OnEnable()
    {
        if (m_CharacterBase == null)
            m_CharacterBase = GetComponent<CharacterBase>();

        if (m_CharacterBase == null)
            return;

        m_CharacterBase.OnCharHpChanged += UpdateHpUI;
        m_CharacterBase.OnCharDamageTaken += HandleDamageTaken;
        m_CharacterBase.OnCharBuffChanged += BuffIconCtrl;
        m_CharacterBase.OnCharTargetableDisabled += HandleTargetableDisabled;

        // 타겟 선택 이벤트 구독
        TargetSelMgr.OnTargetSelected += HandleTargetSelected;
        TargetSelMgr.OnTargetDeselected += HandleTargetDeselected;
    }

    private void Update()
    {
        if (m_IsInitialized)
            RefreshBuffDurations();
    }

    private void OnDisable()
    {
        if (m_CharacterBase != null)
        {
            m_CharacterBase.OnCharHpChanged -= UpdateHpUI;
            m_CharacterBase.OnCharDamageTaken -= HandleDamageTaken;
            m_CharacterBase.OnCharBuffChanged -= BuffIconCtrl;
            m_CharacterBase.OnCharTargetableDisabled -= HandleTargetableDisabled;
        }

        TargetSelMgr.OnTargetSelected -= HandleTargetSelected;
        TargetSelMgr.OnTargetDeselected -= HandleTargetDeselected;
    }

    /// <summary>
    /// 런타임 파티 교체에 필요한 캐릭터 구성 요소가 모두 준비되어 있는지 나타냅니다.
    /// </summary>
    public bool CanChangeParty
    {
        get
        {
            CharacterBase character = GetComponent<CharacterBase>();
            return character != null && character.CanInitializeForParty &&
                GetComponent<HitFlash>() != null && m_CharModelRoot != null;
        }
    }

    /// <summary>
    /// 캐릭터 마스터 데이터가 현재 루트에서 런타임 모델로 생성 가능한지 검사합니다.
    /// </summary>
    /// <param name="masterData">모델과 기본 능력치를 제공하는 마스터 데이터입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public bool CanInitializeForParty(CharacterData masterData)
    {
        return CanChangeParty && masterData != null && masterData.Prefab != null &&
            masterData.Prefab.GetComponentInChildren<SPUM_Prefabs>(true) != null;
    }

    /// <summary>
    /// 파티 교체 전에 기존 캐릭터 모델과 런타임 구독을 정리합니다.
    /// </summary>
    public void ClearForPartyChange()
    {
        if (m_CharacterBase == null)
            m_CharacterBase = GetComponent<CharacterBase>();
        m_CharacterBase?.ClearForPartyChange();
        m_IsInitialized = false;
        ClearBuffIconCells();
        ClearCharacterModels();
        m_ShadowRenderer = null;
        m_IsShadowSelected = false;
        m_IsBuffTargetHighlighted = false;
    }

    internal sealed class PartySnapshot
    {
        internal CharacterBase.PartySnapshot Character;
        internal bool Active, Initialized;
        internal readonly List<Transform> Models = new();
        internal readonly List<bool> ModelActive = new();
        internal readonly List<Transform> Effects = new();
        internal readonly List<bool> EffectActive = new();
    }

    // 기존 외형과 이펙트를 파괴하지 않고 보관하여 실패 시 그대로 복원한다.
    internal PartySnapshot CapturePartySnapshot()
    {
        if (m_CharacterBase == null)
            m_CharacterBase = GetComponent<CharacterBase>();
        if (m_HitFlash == null)
            m_HitFlash = GetComponent<HitFlash>();
        return new PartySnapshot
        {
            Character = m_CharacterBase.CapturePartySnapshot(),
            Active = gameObject.activeSelf, Initialized = m_IsInitialized
        };
    }

    internal void BeginPartyChange(PartySnapshot snapshot)
    {
        DetachChildren(m_CharModelRoot.transform, snapshot.Models, snapshot.ModelActive);
        DetachChildren(m_CharacterBase.SkillEffectTarget, snapshot.Effects, snapshot.EffectActive);
        ClearForPartyChange();
    }

    private static void DetachChildren(Transform parent, List<Transform> children, List<bool> active)
    {
        if (parent == null)
            return;
        while (parent.childCount > 0)
        {
            Transform child = parent.GetChild(0);
            children.Add(child);
            active.Add(child.gameObject.activeSelf);
            child.gameObject.SetActive(false);
            child.SetParent(null, false);
        }
    }

    internal void RollbackPartyChange(PartySnapshot snapshot)
    {
        ClearForPartyChange();
        for (int i = 0; i < snapshot.Models.Count; i++)
        {
            snapshot.Models[i].SetParent(m_CharModelRoot.transform, false);
            snapshot.Models[i].gameObject.SetActive(snapshot.ModelActive[i]);
        }
        for (int i = 0; i < snapshot.Effects.Count; i++)
        {
            snapshot.Effects[i].SetParent(m_CharacterBase.SkillEffectTarget, false);
            snapshot.Effects[i].gameObject.SetActive(snapshot.EffectActive[i]);
        }
        gameObject.SetActive(snapshot.Active);
        m_IsInitialized = snapshot.Initialized;
        m_CharacterBase.RestorePartySnapshot(snapshot.Character);
        if (snapshot.Models.Count > 0)
        {
            InitializeShadow(snapshot.Models[0]);
            m_HitFlash.Initialize(snapshot.Models[0], m_ShadowRenderer);
        }
        if (m_IsInitialized)
        {
            InitializeCharUI();
            BuffIconCtrl();
        }
    }

    internal void CommitPartyChange(PartySnapshot snapshot)
    {
        foreach (Transform model in snapshot.Models)
            Destroy(model.gameObject);
        foreach (Transform effect in snapshot.Effects)
            Destroy(effect.gameObject);
    }

    /// <summary>
    /// 마스터·사용자 데이터와 파티 성장값으로 캐릭터 모델 및 런타임 컴포넌트를 초기화합니다.
    /// </summary>
    /// <param name="masterData">모델과 기본 능력치를 제공하는 마스터 데이터입니다.</param>
    /// <param name="userData">레벨과 장비 등 사용자 소유 상태를 담은 데이터입니다.</param>
    /// <param name="partyStat">캐릭터 능력치에 적용할 파티 성장 보정값입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryInitialize(CharacterData masterData, CharData userData, PartyStat partyStat)
    {
        if (m_IsInitialized)
            return true;

        if (m_CharacterBase == null)
            m_CharacterBase = GetComponent<CharacterBase>();
        if (m_HitFlash == null)
            m_HitFlash = GetComponent<HitFlash>();
        if (m_CharacterBase == null || masterData == null || userData == null ||masterData.Prefab == null)
        {
            PDebug.LogError("캐릭터 초기화 참조가 누락되었습니다.");
            return false;
        }

        // 최초 초기화의 잔여 모델만 정리. 교체 정리는 ClearForPartyChange에서 수행.
        if (m_CharModelRoot.transform.childCount > 0)
            ClearCharacterModels();
        
        // 받은 캐릭터 데이터 내 Prefab을 스폰
        // Instantiate(masterData.Prefab, m_CharModelRoot.transform);
        GameObject model = Instantiate(masterData.Prefab, m_CharModelRoot.transform);
        m_CharModelRoot.SetActive(true);

        InitializeShadow(model.transform);
        m_HitFlash.Initialize(model.transform, m_ShadowRenderer);

        if (!m_CharacterBase.Initialize(masterData, userData, partyStat))
        {
            PDebug.LogError("m_CharacterBase didn't initialized!");
            return false;
        }

        InitializeCharUI();
        BuffIconCtrl();
        
        m_IsInitialized = true;
        return true;
    }

    private void HandleTargetSelected(ITargetable target)
    {
        // 선택된 대상이 이 CharacterRoot의 캐릭터인지 확인
        if (!ReferenceEquals(target, m_CharacterBase))
            return;

        SetShadowSelected(true);
    }

    private void HandleTargetDeselected(ITargetable target)
    {
        // 선택 해제된 대상이 이 캐릭터인지 확인
        if (!ReferenceEquals(target, m_CharacterBase))
            return;

        SetShadowSelected(false);
    }

    private void InitializeShadow(Transform modelRoot)
    {
        Transform shadow = modelRoot.Find("UnitRoot/Shadow/Shadow");

        if (shadow == null)
        {
            PDebug.LogError("Shadow/Shadow를 찾지 못했습니다.", this);
            return;
        }

        if (!shadow.TryGetComponent(out m_ShadowRenderer))
        {
            PDebug.LogError("Shadow SpriteRenderer를 찾지 못했습니다.", this);
        }
    }

    private void ClearCharacterModels()
    {
        for (int i = m_CharModelRoot.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = m_CharModelRoot.transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }

    private void InitializeCharUI()
    {
        CharacterData charData = m_CharacterBase.CharData;
        UpdateHpUI(m_CharacterBase.CurHp, m_CharacterBase.MaxHp);

        int roleIndex = (int)charData.Role;
        
        if (m_CharTypeIconImg != null && CharTypeIcons != null && roleIndex >= 0 && roleIndex < CharTypeIcons.Length)
            m_CharTypeIconImg.sprite = CharTypeIcons[roleIndex];

        if (m_CharNameTxt != null)
            m_CharNameTxt.text = charData.DisplayName;
    }

    private void UpdateHpUI(float currentHp, float maxHp)
    {
        if (m_CurHpImg == null)
            return;

        m_CurHpImg.fillAmount = maxHp > 0f
            ? Mathf.Clamp01(currentHp / maxHp)
            : 0f;
    }

    private void HandleDamageTaken(float damage, bool isCritical)
    {
        m_HitFlash?.Play();
        if (TextPoolMgr.Inst == null)
            return;

        TextPoolMgr.Inst.ShowDamage(
            m_CharacterBase.MonPosition,
            damage,
            isCritical,
            isAlly: true);
    }

    private void BuffIconCtrl()
    {
        ClearBuffIconCells();

        if (m_CharacterBase == null || m_BuffGroup == null)
            return;

        int buffCount = m_CharacterBase.CurBuffedSkillsList.Count;
        m_BuffGroup.gameObject.SetActive(buffCount > 0);

        int overflowCount = Mathf.Max(0, buffCount - MaxVisibleBuffCount);
        if (m_BuffCountTxt != null)
        {
            m_BuffCountTxt.text = $"+{overflowCount}";
            m_BuffCountTxt.gameObject.SetActive(overflowCount > 0);
        }

        if (m_SkillCellPrefab == null)
        {
            if (buffCount > 0)
                PDebug.LogError("BuffIconCell 프리팹이 설정되지 않았습니다.");
            return;
        }

        int visibleCount = Mathf.Min(buffCount, MaxVisibleBuffCount);
        for (int i = 0; i < visibleCount; i++)
        {
            GameObject skillCell = Instantiate(m_SkillCellPrefab, m_BuffGroup, false);
            
            if (!skillCell.TryGetComponent(out BuffIconCellMgr cellMgr))
            {
                PDebug.LogError("BuffIconCellMgr 컴포넌트를 찾을 수 없습니다.");
                Destroy(skillCell);
                continue;
            }

            SkillData skill = m_CharacterBase.CurBuffedSkillsList[i];
            float remainingDuration = i < m_CharacterBase.CurBuffedSkillsDurationList.Count
                ? m_CharacterBase.CurBuffedSkillsDurationList[i]
                : 0f;

            cellMgr.Initialize(skill.Icon, skill.BuffTime, remainingDuration);
            m_BuffIconCells.Add(cellMgr);
        }
    }

    private void RefreshBuffDurations()
    {
        int refreshCount = Mathf.Min(
            m_BuffIconCells.Count,
            m_CharacterBase.CurBuffedSkillsDurationList.Count);

        for (int i = 0; i < refreshCount; i++)
        {
            SkillData skill = m_CharacterBase.CurBuffedSkillsList[i];
            m_BuffIconCells[i].SetBuffDuration(
                skill.BuffTime,
                m_CharacterBase.CurBuffedSkillsDurationList[i]);
        }
    }

    private void ClearBuffIconCells()
    {
        foreach (BuffIconCellMgr cell in m_BuffIconCells)
        {
            if (cell != null)
                Destroy(cell.gameObject);
        }

        m_BuffIconCells.Clear();
    }

    private void SetShadowSelected(bool isSelected)
    {
        m_IsShadowSelected = isSelected;
        RefreshShadowColor();
    }

    /// <summary>
    /// 단일 버프 대상 선택 중인 아군의 그림자 강조 표시를 전환합니다.
    /// </summary>
    /// <param name="isOn">강조 표시를 활성화할지 여부입니다.</param>
    public void SetBuffTargetHighlighted(bool isOn)
    {
        m_IsBuffTargetHighlighted = isOn;
        RefreshShadowColor();
    }

    private void RefreshShadowColor()
    {
        if (m_ShadowRenderer == null)
            return;

        bool canTarget =
            m_CharacterBase != null &&
            m_CharacterBase.isActiveAndEnabled &&
            !m_CharacterBase.IsDead &&
            m_CharacterBase.IsTargetable;

        if (!canTarget)
            m_ShadowRenderer.color = new Color32(0, 0, 0, 143);
        else if (m_IsBuffTargetHighlighted)
            m_ShadowRenderer.color = new Color32(70, 220, 100, 180);
        else if (m_IsShadowSelected)
            m_ShadowRenderer.color = new Color32(255, 139, 0, 143);
        else
            m_ShadowRenderer.color = new Color32(0, 0, 0, 143);
    }
    
    private void HandleTargetableDisabled(ITargetable target)
    {
        m_IsShadowSelected = false;
        m_IsBuffTargetHighlighted = false;
        RefreshShadowColor();
    }
}
