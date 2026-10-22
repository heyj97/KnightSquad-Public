using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 몬스터 모델과 전투 컴포넌트를 초기화하고 피해 대상 강조를 관리합니다.
/// </summary>
public class MonsterRootMgr : MonoBehaviour
{
    private MonsterBase m_MonsterBase;
    private bool m_IsInitialized;
    /// <summary>
    /// 같은 오브젝트에 연결된 몬스터 런타임 컴포넌트를 가져옵니다.
    /// </summary>
    public MonsterBase Monster => m_MonsterBase != null
        ? m_MonsterBase : (m_MonsterBase = GetComponent<MonsterBase>());

    [Header("Monster model root")]
    [SerializeField] private GameObject m_MonModelRoot;

    [Header("Monster Info UI elements")]
    [SerializeField] private Image m_CurHpImg;
    [FormerlySerializedAs("m_MonTypeIconImg")] [SerializeField] private Image m_MonGradeIconImg;
    [SerializeField] private Sprite[] MonGradeIcons;
    [SerializeField] private Text m_MonNameTxt;

    /// <summary>
    /// 현재 루트에 생성된 몬스터의 마스터 데이터를 참조합니다.
    /// </summary>
    [Header("Monster Data")] public MonsterData MonData;
    private HitFlash m_HitFlash;

    private SpriteRenderer m_ShadowRenderer;
    private Color m_OriginalShadowColor;

    

    void Awake()
    {
        m_MonsterBase = GetComponent<MonsterBase>();
        m_HitFlash = GetComponent<HitFlash>();
    }

    private void OnEnable()
    {
        if (m_MonsterBase == null)
            m_MonsterBase = GetComponent<MonsterBase>();

        if (m_MonsterBase == null)
            return;

        // 몬스터 체력변경 이벤트 구독
        m_MonsterBase.OnMonHpChanged += UpdateHpUI;
        m_MonsterBase.OnMonDamageTaken += HandleDamageTaken;
        m_MonsterBase.OnCharTargetableDisabled += HandleTargetableDisabled;
    }
    
    private void OnDisable()
    {
        SetDamageTargetHighlighted(false);

        if (m_MonsterBase == null)
            return;

        m_MonsterBase.OnMonHpChanged -= UpdateHpUI;
        m_MonsterBase.OnMonDamageTaken -= HandleDamageTaken;
        m_MonsterBase.OnCharTargetableDisabled -= HandleTargetableDisabled;
    }
    
    /// <summary>
    /// 몬스터 마스터 데이터로 모델과 런타임 컴포넌트를 생성하고 UI 이벤트를 연결합니다.
    /// </summary>
    /// <param name="masterData">모델과 기본 능력치를 제공하는 마스터 데이터입니다.</param>
    /// <param name="characterSpawnpointsRoot">몬스터가 공격 대상으로 탐색할 캐릭터 배치 루트입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TryInitialize(MonsterData masterData, Transform characterSpawnpointsRoot)
    {
        if (m_IsInitialized)
            return true;

        if (m_MonsterBase == null)
            m_MonsterBase = GetComponent<MonsterBase>();

        if (m_MonsterBase == null ||
            m_MonModelRoot == null ||
            masterData == null ||
            masterData.Prefab == null ||
            characterSpawnpointsRoot == null)
        {
            PDebug.LogError(
                $"몬스터 초기화 참조 누락: \n" +
                $"MonsterBase={m_MonsterBase != null}, \n" +
                $"MonModelRoot={m_MonModelRoot != null}, \n" +
                $"MasterData={masterData != null}, \n" +
                $"MonsterPrefab={masterData != null && masterData.Prefab != null}, \n" +
                $"CharacterSpawnpointsRoot={characterSpawnpointsRoot != null}"
                );

            return false;
        }

        MonData = masterData;

        ClearMonsterModels();

        GameObject spawnedModel = Instantiate(
            masterData.Prefab,
            m_MonModelRoot.transform,
            false);

        // 레이드 보스 모델만 기존 크기의 1.5배로 표시한다.
        if (masterData.Grade == MonsterGrade.RaidBoss)
        {
            spawnedModel.transform.localScale *= 1.5f;
        }

        if (!m_MonsterBase.Initialize(masterData, characterSpawnpointsRoot))
        {
            PDebug.LogError($"MonsterBase 초기화 실패: id={masterData.Id}");

            Destroy(spawnedModel);
            return false;
        }

        InitializeShadow(spawnedModel.transform);
        m_HitFlash.Initialize(spawnedModel.transform, m_ShadowRenderer);

        InitializeCharUI();

        m_IsInitialized = true;
        return true;
    }
    
    /// <summary>
    /// 몬스터 모델과 UI 구독을 정리하고 루트 오브젝트를 풀 반환 상태로 만듭니다.
    /// </summary>
    public void ResetForPool()
    {
        if (Monster != null)
            Monster.ResetForPool();
        if (m_MonModelRoot != null)
            ClearMonsterModels();
        MonData = null;
        m_IsInitialized = false;
        gameObject.SetActive(false);
    }

    private void ClearMonsterModels()
    {
        for (int i = m_MonModelRoot.transform.childCount - 1; i >= 0; i--)
        {
            GameObject child = m_MonModelRoot.transform.GetChild(i).gameObject;
            child.SetActive(false);
            Destroy(child);
        }
    }
    
    private void InitializeCharUI()
    {
        UpdateHpUI(m_MonsterBase.CurHp, m_MonsterBase.MaxHp);

        int gradeIndex = (int)MonData.Grade;
        
        if (m_MonGradeIconImg != null && MonGradeIcons != null && gradeIndex >= 0 && gradeIndex < MonGradeIcons.Length)
            m_MonGradeIconImg.sprite = MonGradeIcons[gradeIndex];

        if (m_MonNameTxt != null)
            m_MonNameTxt.text = MonData.DisplayName;
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
            m_MonsterBase.MonPosition,
            damage,
            isCritical);
    }

    private void InitializeShadow(Transform modelRoot)
    {
        m_ShadowRenderer = null;

        if (modelRoot == null)
            return;

        SpriteRenderer[] renderers =
            modelRoot.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer.gameObject.name != "Shadow")
                continue;

            m_ShadowRenderer = renderer;
            m_OriginalShadowColor = renderer.color;
            return;
        }

        PDebug.LogError(
            $"몬스터 그림자를 찾지 못했습니다. 모델: {modelRoot.name}",
            this);
    }

    /// <summary>
    /// 공격 스킬 대상으로 선택 가능한 몬스터의 그림자 강조를 전환합니다.
    /// </summary>
    /// <param name="isOn">강조 표시를 활성화할지 여부입니다.</param>
    public void SetDamageTargetHighlighted(bool isOn)
    {
        if (m_ShadowRenderer == null)
            return;

        bool canTarget =
            m_MonsterBase != null &&
            m_MonsterBase.isActiveAndEnabled &&
            !m_MonsterBase.IsDead &&
            m_MonsterBase.IsTargetable;

        m_ShadowRenderer.color = isOn && canTarget
            ? new Color32(230, 60, 60, 180)
            : m_OriginalShadowColor;
    }

    private void HandleTargetableDisabled(ITargetable target)
    {
        SetDamageTargetHighlighted(false);
    }
}
