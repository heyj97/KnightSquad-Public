using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택된 캐릭터의 스킬과 재사용 대기시간을 표시하고 사용 입력을 전달합니다.
/// </summary>
public class SkillPanelMgr : MonoBehaviour
{
    // targetselmgr의 타겟구독
    // 타겟의 보유스킬을 스킬패널에 나열

    /// <summary>
    /// 선택된 캐릭터의 스킬 슬롯들을 배치하는 부모 Transform입니다.
    /// </summary>
    [Header("UI Elements")]
    public Transform m_SkillFrame;
    /// <summary>
    /// 고정 스킬 슬롯별 아이콘을 표시하는 이미지 UI 배열입니다.
    /// </summary>
    public Image[] m_SkillImages = new Image[4];
    /// <summary>
    /// 고정 스킬 슬롯별 재사용 대기시간을 표시하는 이미지 UI 배열입니다.
    /// </summary>
    public Image[] m_SkillCools = new Image[4];
    /// <summary>
    /// 고정 스킬 슬롯별 부모 마스크 이미지입니다.
    /// </summary>
    public Image[] m_SkillMasks = new Image[4];

    private CharacterBase m_SelectedCharacter;
    private int m_CharacterSelectedFrame = -1;

    [SerializeField]
    private KeyCode[] m_SkillKeys = new KeyCode[4]
    {
        KeyCode.Alpha1,
        KeyCode.Alpha2,
        KeyCode.Alpha3,
        KeyCode.Alpha4
    };

    private Transform m_PartySpawnRoot;


    void Start()
    {
        
    }

    void Update()
    {
        if (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused || BattleFlowMgr.IsFieldInputBlocked || SceneChangeMgr.IsTransitioning)
            return;

        if (m_SelectedCharacter == null)
            return;

        // 캐릭터를 선택한 입력으로 스킬까지 실행되지 않도록 방지
        if (m_CharacterSelectedFrame == Time.frameCount)
            return;

        for (int i = 0; i < m_SkillKeys.Length; i++)
        {
            if (!Input.GetKeyDown(m_SkillKeys[i]))
                continue;

            if (m_SelectedCharacter.IsSelectingBuffTarget)
            {
                // i번째 파티원을 가져와 대상 확정
                CharacterBase target = GetPartyCharacterAtSlot(i);
                m_SelectedCharacter.SelectBuffTargetByKey(target);
            }
            else
            {
                UseSelectedSkill(i);
            }

            break;
        }
    }

    void LateUpdate()
    {
        UpdateSkillCooldownUI();
    }

    void OnEnable()
    {
        TargetSelMgr.OnTargetSelected += TargetSelected;
        TargetSelMgr.OnTargetDeselected += TargetDeselected;
    }

    void OnDisable()
    {
        TargetSelMgr.OnTargetSelected -= TargetSelected;
        TargetSelMgr.OnTargetDeselected -= TargetDeselected;
    }

    private CharacterBase GetPartyCharacterAtSlot(int index)
    {
        if (index < 0 || index >= 4)
            return null;

        // UI 씬이 먼저 로드될 수 있으므로 필요할 때 찾는다.
        if (m_PartySpawnRoot == null)
        {
            GameObject spawnRoot =
                GameObject.FindGameObjectWithTag("PlayerSpawnPoint");

            if (spawnRoot == null)
                return null;

            m_PartySpawnRoot = spawnRoot.transform;
        }

        if (index >= m_PartySpawnRoot.childCount)
            return null;

        Transform slot = m_PartySpawnRoot.GetChild(index);
        CharacterBase character =
            slot.GetComponentInChildren<CharacterBase>(true);

        if (character == null ||
            !character.IsInitialized ||
            !character.isActiveAndEnabled)
            return null;

        return character;
    }

    private void TargetSelected(ITargetable target)
    {
        m_SelectedCharacter = target as CharacterBase;
        m_CharacterSelectedFrame = Time.frameCount;

        if (m_SelectedCharacter != null)
            ActiveFrame(true, m_SelectedCharacter.SkillsList.ToArray());
        else
            ActiveFrame(false);

        UpdateSkillCooldownUI();
    }

    private void TargetDeselected(ITargetable target)
    {
        m_SelectedCharacter = null;
        UpdateSkillCooldownUI();
        ActiveFrame(false);
    }

    void ActiveFrame(bool isOn, SkillData[] skills = null)
    {
        if (isOn)
        {
            ActiveSkillImg(true, skills);
        }
        else
        {
            ActiveSkillImg(false);
            ClearActivatedImgsSprite();
        }

        m_SkillFrame.gameObject.SetActive(isOn);
    }

    void ActiveSkillImg(bool isOn, SkillData[] skills = null)
    {
        for (int i = 0; i < m_SkillImages.Length; i++)
        {
            bool hasSkill = isOn &&
                            skills != null &&
                            i < skills.Length;

            Image skillImage = m_SkillImages[i];

            if (skillImage != null)
            {
                skillImage.sprite = hasSkill ? skills[i].Icon : null;
                skillImage.gameObject.SetActive(hasSkill);
            }

            if (i < m_SkillMasks.Length && m_SkillMasks[i] != null)
                m_SkillMasks[i].gameObject.SetActive(hasSkill);
        }
    }

    void ClearActivatedImgsSprite()
    {
        foreach(Image img in m_SkillImages)
        {
            img.sprite = null;
        }
    }

    private void UpdateSkillCooldownUI()
    {
        for (int i = 0; i < m_SkillCools.Length; i++)
        {
            Image coolImage = m_SkillCools[i];

            if (coolImage == null)
                continue;

            float fill = 0f;

            if (m_SelectedCharacter != null &&
                i < m_SelectedCharacter.SkillsList.Count &&
                i < m_SelectedCharacter.SkillsCoolDownList.Count)
            {
                float remaining = m_SelectedCharacter.SkillsCoolDownList[i];
                float cooldown = m_SelectedCharacter.SkillsList[i].Cooldown;

                if (cooldown > 0f)
                    fill = Mathf.Clamp01(remaining / cooldown);
            }

            coolImage.fillAmount = fill;
        }
    }

    /// <summary>
    /// 선택된 캐릭터에게 지정 슬롯의 스킬 사용 또는 대상 선택 시작을 요청합니다.
    /// </summary>
    /// <param name="index">고정 슬롯 또는 목록에서 사용할 인덱스입니다.</param>
    public void UseSelectedSkill(int index)
    {
        if (!isActiveAndEnabled ||
            m_SkillFrame == null ||
            !m_SkillFrame.gameObject.activeInHierarchy)
            return;

        if (m_SelectedCharacter == null || m_SelectedCharacter.IsDead)
            return;

        if (index < 0 || index >= m_SelectedCharacter.SkillsList.Count)
            return;

        m_SelectedCharacter.UseSkill(index);
    }
}
