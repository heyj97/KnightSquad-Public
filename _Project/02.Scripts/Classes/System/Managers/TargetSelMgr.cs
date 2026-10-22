using System;
using UnityEngine;

/// <summary>
/// 마우스 위치의 타겟을 감지하고
/// Hover / 좌클릭 선택 / 우클릭 선택 취소를 처리하는 매니저
/// </summary>
public class TargetSelMgr : G_Singleton<TargetSelMgr>
{
    [Header("Target Setting")]
    [SerializeField] private LayerMask targetLayer;


    private Camera m_MainCamera;


    private ITargetable m_HoverTarget;


    private ITargetable m_SelectedTarget;

    /// <summary>
    /// 현재 선택된 타겟
    /// </summary>
    public ITargetable SelectedTarget => m_SelectedTarget;

    /// <summary>
    /// 아군 캐릭터를 숫자키의 고정 파티 슬롯과 연결하는 부모 Transform입니다.
    /// </summary>
    public ITargetable[] m_PlayerGroup;


    /// <summary>
    /// 타겟에 마우스가 올라갔을 때 발생
    /// </summary>
    public static event Action<ITargetable> OnTargetHoverEnter;

    /// <summary>
    /// 타겟에서 마우스가 빠져나갔을 때 발생
    /// </summary>
    public static event Action<ITargetable> OnTargetHoverExit;

    /// <summary>
    /// 새로운 타겟을 좌클릭하여 선택했을 때 발생
    /// </summary>
    public static event Action<ITargetable> OnTargetSelected;

    /// <summary>
    /// 현재 선택된 타겟을 해제했을 때 발생
    /// </summary>
    public static event Action<ITargetable> OnTargetDeselected;


    protected override void Awake()
    {
        base.Awake();
        // Camera.main을 매 프레임 찾지 않도록 캐싱
        m_MainCamera = Camera.main;

        TryGetCharacters(out m_PlayerGroup);
    }

    private void Update()
    {
        if (PlayerDataMgr.IsProgressBlocked || InformationPanelMgr.IsMenuPaused || BattleFlowMgr.IsFieldInputBlocked || SceneChangeMgr.IsTransitioning)
            return;

        HandleHover();
        HandleClick();
        HandleKeyInput();
    }

    private void HandleHover()
    {
        ITargetable newTarget = GetTargetUnderMouse();

        // 이전 Hover 대상과 같으면 처리할 필요 없음
        if (ReferenceEquals(newTarget, m_HoverTarget))
            return;

        // 기존 Hover 대상에서 벗어남
        if (m_HoverTarget != null)
        {
            m_HoverTarget.OnCharTargetableDisabled -= HandleHoverTargetDisabled;
            OnTargetHoverExit?.Invoke(m_HoverTarget);
        }

        // 새로운 Hover 대상 저장
        m_HoverTarget = newTarget;

        // 새로운 Hover 대상 진입
        if (m_HoverTarget != null)
        {
            m_HoverTarget.OnCharTargetableDisabled += HandleHoverTargetDisabled;
            OnTargetHoverEnter?.Invoke(m_HoverTarget);
        }
    }

    private bool TryGetCharacters(out ITargetable[] characters)
    {
        characters = null;
        GameObject spawnRoot = GameObject.FindGameObjectWithTag("PlayerSpawnPoint");
        if (spawnRoot == null)
            return false;

        // PartyMgr와 같은 루트 순서로 수집하여 빈 슬롯도 번호를 유지한다.
        CharacterRootMgr[] roots = spawnRoot.GetComponentsInChildren<CharacterRootMgr>(true);
        if (roots.Length != 4)
            return false;

        characters = new ITargetable[4];
        for (int i = 0; i < roots.Length; i++)
        {
            CharacterBase character = roots[i].GetComponent<CharacterBase>();
            if (character != null && character.IsTargetable)
                characters[i] = character;
        }
        return true;
    }

    private void HandleKeyInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectTarget();
            return;
        }

        if (m_SelectedTarget != null)
            return;

        int slotIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) slotIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) slotIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) slotIndex = 2;
        else if (Input.GetKeyDown(KeyCode.Alpha4)) slotIndex = 3;

        // 입력 시 갱신하여 Awake 이후의 스폰·비활성화 상태를 반영한다.
        if (slotIndex < 0 || !TryGetCharacters(out m_PlayerGroup) || slotIndex >= m_PlayerGroup.Length)
            return;

        SelectTarget(m_PlayerGroup[slotIndex]);
    }

    private void HandleClick()
    {
        if (UIMgr.IsPointerOverUIObject())
            return;

        // 좌클릭
        if (Input.GetMouseButtonDown(0))
        {
            bool isSkillTargetClick = m_SelectedTarget is CharacterBase character && character.BlocksNormalTargetSelection;

            if (!isSkillTargetClick)
                SelectTarget(m_HoverTarget);
        }

        // 우클릭
        if (Input.GetMouseButtonDown(1))
        {
            DeselectTarget();
        }
    }

    private void SelectTarget(ITargetable target)
    {
        // 빈 공간을 좌클릭한 경우 현재 선택 유지
        if (target == null)
            return;

        // 타겟팅 불가능한 경우 선택하지 않음
        if (!target.IsTargetable)
            return;

        // 이미 선택된 타겟이면 다시 이벤트를 발생시키지 않음
        if (ReferenceEquals(target, m_SelectedTarget))
            return;

        // 기존 선택 타겟이 있다면 선택 해제 이벤트
        if (m_SelectedTarget != null)
        {
            m_SelectedTarget.OnCharTargetableDisabled -= HandleSelectedTargetDisabled;
            OnTargetDeselected?.Invoke(m_SelectedTarget);
        }

        // 새로운 타겟 선택
        m_SelectedTarget = target;
        m_SelectedTarget.OnCharTargetableDisabled += HandleSelectedTargetDisabled;
        OnTargetSelected?.Invoke(m_SelectedTarget);
    }

    private void DeselectTarget()
    {
        if (m_SelectedTarget == null)
            return;

        // 이벤트에 전달할 기존 타겟 저장
        ITargetable previousTarget = m_SelectedTarget;

        previousTarget.OnCharTargetableDisabled -= HandleSelectedTargetDisabled;
        m_SelectedTarget = null;

        OnTargetDeselected?.Invoke(previousTarget);
    }

    /// <summary>
    /// 일반 선택과 호버 대상을 모두 해제하고 관련 강조 표시를 정리합니다.
    /// </summary>
    public void ClearTargets()
    {
        DeselectTarget();
        if (m_HoverTarget != null)
            HandleHoverTargetDisabled(m_HoverTarget);
    }

    private void HandleHoverTargetDisabled(ITargetable target)
    {
        if (!ReferenceEquals(target, m_HoverTarget))
            return;

        target.OnCharTargetableDisabled -= HandleHoverTargetDisabled;
        m_HoverTarget = null;
        OnTargetHoverExit?.Invoke(target);
    }

    private void HandleSelectedTargetDisabled(ITargetable target)
    {
        if (ReferenceEquals(target, m_SelectedTarget))
            DeselectTarget();
    }

    private ITargetable GetTargetUnderMouse()
    {
        // 기존 카메라가 파괴되었거나 아직 설정되지 않았다면 다시 검색
        if (m_MainCamera == null)
        {
            m_MainCamera = Camera.main;

            if (m_MainCamera == null)
                return null;
        }
        
        // 화면 좌표 → 월드 좌표
        Vector2 mouseWorldPos =
            m_MainCamera.ScreenToWorldPoint(Input.mousePosition);

        // Target Layer에서 마우스 위치의 Collider 검색
        Collider2D hit =
            Physics2D.OverlapPoint(mouseWorldPos, targetLayer);

        if (hit == null)
            return null;

        // Collider가 자식에 붙어 있을 수 있으므로 부모까지 검색
        ITargetable target =
            hit.GetComponentInParent<ITargetable>();

        if (target == null || !target.IsTargetable)
            return null;

        return target;
    }
}
