using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>레이드 전용 UI와 필드 왕복을 관리한다. 항상 활성인 UIScene 오브젝트에 배치한다.</summary>
public class OnRaidUIMgr : MonoBehaviour
{
    [SerializeField] private GameObject m_OnRaidUI;
    [SerializeField] private Button m_GiveupBtn;
    [Header("Raid result UI")]
    [SerializeField] private GameObject m_DefaultRaidPanel;
    [SerializeField] private GameObject m_RaidResultPanel;
    [SerializeField] private RaidResultPanelMgr m_ResultPanelMgr;
    [SerializeField] private Text m_ResultStatusTitleTxt;
    [SerializeField] private Text m_StatusDescTxt;
    [SerializeField] private Button m_ComeBackFieldBtn;
    private BattleFlowMgr.BattleState? m_DisplayedResult;
    private BattleFlowMgr.DefeatReason? m_DisplayedReason;
    [Header("Raid combat UI")]
    [SerializeField] private Text m_RemainTimeTxt;
    [SerializeField] private Image m_BossCurHpImg;
    [SerializeField] private Text m_BossCurHpTxt;
    private MonsterBase m_BoundBoss;
    private int m_LastRemainingSeconds = -1;
    private Coroutine m_Transition;
    private BattleFlowMgr m_Flow;
    private System.Action m_GiveUpConfirmCallback;

    private void Awake()
    {
        if (m_GiveupBtn != null)
            m_GiveupBtn.onClick.AddListener(GiveUpRaid);
        if (m_ComeBackFieldBtn != null)
            m_ComeBackFieldBtn.onClick.AddListener(ReturnToField);
        UpdateBossHp(0f, 0f);
        RefreshUI();
    }

    private void Update()
    {
        if (m_Flow == null)
            m_Flow = FindFirstObjectByType<BattleFlowMgr>();
        RefreshUI();
        RefreshCombatUI();
    }

    private void RefreshUI()
    {
        bool inRaid = BattleFlowMgr.CurrentMode == BattleFlowMgr.BattleMode.Raid;
        bool hasResult = inRaid && m_Flow != null &&
            (m_Flow.State == BattleFlowMgr.BattleState.Cleared || m_Flow.State == BattleFlowMgr.BattleState.Defeated);
        if (hasResult)
            CloseGiveUpConfirmation();
        SetPanelActive(m_DefaultRaidPanel, inRaid && !hasResult);
        SetPanelActive(m_RaidResultPanel, hasResult);
        if (hasResult)
            RefreshResult();
        else
            ResetResult();
        if (m_OnRaidUI != null && m_OnRaidUI.activeSelf != inRaid)
            m_OnRaidUI.SetActive(inRaid);
        if (m_GiveupBtn != null)
            m_GiveupBtn.interactable = inRaid && !hasResult && m_Transition == null &&
                !PlayerDataMgr.IsProgressBlocked && m_Flow != null && m_Flow.State == BattleFlowMgr.BattleState.Fighting &&
                !m_Flow.IsSwitchingMap && !SceneChangeMgr.IsTransitioning;
        if (m_ComeBackFieldBtn != null)
            m_ComeBackFieldBtn.interactable = hasResult && m_Transition == null &&
                !PlayerDataMgr.IsProgressBlocked && !m_Flow.IsSwitchingMap && !SceneChangeMgr.IsTransitioning;
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null && panel.activeSelf != active) panel.SetActive(active);
    }

    private void RefreshResult()
    {
        if (m_ResultPanelMgr == null && m_RaidResultPanel != null)
            m_ResultPanelMgr = m_RaidResultPanel.GetComponent<RaidResultPanelMgr>();
        if (m_ResultPanelMgr != null)
        {
            m_ResultPanelMgr.Show(m_Flow.LastRaidResult, this);
            m_ResultPanelMgr.SetButtonsInteractable(m_Transition == null && !PlayerDataMgr.IsProgressBlocked &&
                !m_Flow.IsSwitchingMap && !SceneChangeMgr.IsTransitioning && !MessageBoxMgr.IsBlockingInput);
            return;
        }
        if (m_DisplayedResult == m_Flow.State && m_DisplayedReason == m_Flow.LastDefeatReason) return;
        m_DisplayedResult = m_Flow.State;
        m_DisplayedReason = m_Flow.LastDefeatReason;
        bool cleared = m_Flow.State == BattleFlowMgr.BattleState.Cleared;
        if (m_ResultStatusTitleTxt != null)
            m_ResultStatusTitleTxt.text = cleared ? "레이드 성공" : "레이드 실패";
        string reason = m_Flow.LastDefeatReason == BattleFlowMgr.DefeatReason.GiveUp ? "포기"
            : m_Flow.LastDefeatReason == BattleFlowMgr.DefeatReason.BossTimeExpired ? "시간 초과" : "전멸";
        if (m_StatusDescTxt != null)
            m_StatusDescTxt.text = cleared ? "레이드를 성공적으로 클리어했습니다."
                : $"레이드를 클리어하지 못했습니다.\n실패 사유: {reason}";
    }

    private void ResetResult()
    {
        m_DisplayedResult = null;
        m_DisplayedReason = null;
        if (m_ResultStatusTitleTxt != null) m_ResultStatusTitleTxt.text = "";
        if (m_StatusDescTxt != null) m_StatusDescTxt.text = "";
    }

    private void GiveUpRaid()
    {
        if (m_Flow == null || m_Transition != null || m_Flow.IsSwitchingMap ||
            SceneChangeMgr.IsTransitioning || PlayerDataMgr.IsProgressBlocked ||
            m_Flow.Mode != BattleFlowMgr.BattleMode.Raid ||
            m_Flow.State != BattleFlowMgr.BattleState.Fighting ||
            MessageBoxMgr.IsBlockingInput)
            return;

        m_GiveUpConfirmCallback = () =>
        {
            m_GiveUpConfirmCallback = null;

            // 확인 전에 전투가 끝났거나 씬 전환이 시작됐다면 무시한다.
            if (this == null || m_Flow == null ||
                m_Transition != null || m_Flow.IsSwitchingMap ||
                SceneChangeMgr.IsTransitioning || PlayerDataMgr.IsProgressBlocked ||
                m_Flow.Mode != BattleFlowMgr.BattleMode.Raid ||
                m_Flow.State != BattleFlowMgr.BattleState.Fighting)
                return;

            if (m_Flow.TryEndBattleAsDefeat(BattleFlowMgr.DefeatReason.GiveUp))
                RefreshUI();
        };

        MessageBoxMgr.Inst.ShowMsg(
            "레이드를 포기하시겠습니까?",
            "레이드 포기",
            MessageBoxType.Submit,
            m_GiveUpConfirmCallback,
            () => m_GiveUpConfirmCallback = null
        );
    }

    private void CloseGiveUpConfirmation()
    {
        System.Action callback = m_GiveUpConfirmCallback;
        m_GiveUpConfirmCallback = null;
        if (callback != null && MessageBoxMgr.Inst != null)
            MessageBoxMgr.Inst.TryCloseMessage(callback);
    }

    public void EnterRaidField(MonsterData bossData)
    {
        if (bossData == null || bossData.Grade != MonsterGrade.RaidBoss || bossData.Prefab == null)
        {
            MessageBoxMgr.Inst.ShowMsg("레이드 보스 데이터와 Prefab 연결을 확인해주세요.", "입장 실패", MessageBoxType.Error);
            return;
        }
        if (m_Transition == null && !PlayerDataMgr.IsProgressBlocked && !SceneChangeMgr.IsTransitioning)
        {
            if (!CheckRaidRewardSpace()) return;
            m_Transition = StartCoroutine(Transition(true, bossData));
        }
    }

    public void ReturnToField()
    {
        if (m_Flow == null || m_Flow.Mode != BattleFlowMgr.BattleMode.Raid ||
            (m_Flow.State != BattleFlowMgr.BattleState.Cleared && m_Flow.State != BattleFlowMgr.BattleState.Defeated))
            return;
        if (m_Transition == null && !PlayerDataMgr.IsProgressBlocked && !SceneChangeMgr.IsTransitioning)
            m_Transition = StartCoroutine(Transition(false, null));
    }

    public void RetryRaid()
    {
        if (m_Flow == null || m_Transition != null || MessageBoxMgr.IsBlockingInput) return;
        if (!CheckRaidRewardSpace()) return;
        if (!m_Flow.TryRetryRaid())
            MessageBoxMgr.Inst.ShowMsg("레이드 재도전에 실패했습니다. 파티와 씬 상태를 확인해주세요.", "재도전 실패", MessageBoxType.Error);
        RefreshUI();
        RefreshCombatUI();
    }

    private bool CheckRaidRewardSpace()
    {
        if (BattleFlowMgr.HasRaidRewardSpace) return true;
        MessageBoxMgr.Inst.ShowMsg(
            $"인벤토리를 {GlobalConstants.RaidDropItemCount}칸 이상 비워주세요.",
            "레이드 입장 불가",
            MessageBoxType.Info);
        return false;
    }

    private IEnumerator Transition(bool entering, MonsterData bossData)
    {
        // Coroutine 참조를 저장한 뒤 요청을 처리하여 중복 입력을 막는다.
        yield return null;
        bool succeeded = false;
        bool accepted = SceneChangeMgr.SceneFade(SwitchField(entering, bossData != null ? bossData.DisplayName : ""), result => succeeded = result);
        if (accepted)
            while (SceneChangeMgr.IsTransitioning) yield return null;
        // 화면이 완전히 밝아진 뒤 생성하므로 준비 중 공격이나 타이머 소모가 없다.
        if (accepted && succeeded && entering)
            succeeded = m_Flow != null && m_Flow.TryStartRaidCombat(bossData, 180f);
        m_Transition = null;
        RefreshUI();
        RefreshCombatUI();
        if (!accepted || !succeeded)
            MessageBoxMgr.Inst.ShowMsg("씬 전환에 실패했습니다. Console과 씬 상태를 확인해주세요.", "이동 실패", MessageBoxType.Error);
    }

    private void RefreshCombatUI()
    {
        MonsterBase boss = m_Flow != null ? m_Flow.CurrentRaidBoss : null;
        if (m_BoundBoss != boss)
        {
            UnbindBoss();
            m_BoundBoss = boss;
            if (m_BoundBoss != null)
                m_BoundBoss.OnMonHpChanged += UpdateBossHp;
            UpdateBossHp(boss != null ? boss.CurHp : 0f, boss != null ? boss.MaxHp : 0f);
        }
        int seconds = m_Flow != null && m_Flow.Mode == BattleFlowMgr.BattleMode.Raid
            ? Mathf.CeilToInt(Mathf.Max(0f, m_Flow.RaidTimeRemaining)) : 0;
        if (m_LastRemainingSeconds == seconds) return;
        m_LastRemainingSeconds = seconds;
        if (m_RemainTimeTxt != null)
            m_RemainTimeTxt.text = $"남은 시간\n{seconds / 60:00} : {seconds % 60:00}";
    }

    private void UpdateBossHp(float currentHp, float maxHp)
    {
        if (m_BossCurHpImg != null)
            m_BossCurHpImg.fillAmount = maxHp > 0f ? Mathf.Clamp01(currentHp / maxHp) : 0f;
        if (m_BossCurHpTxt != null)
            m_BossCurHpTxt.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }

    private void UnbindBoss()
    {
        if (m_BoundBoss != null)
            m_BoundBoss.OnMonHpChanged -= UpdateBossHp;
        m_BoundBoss = null;
    }

    private IEnumerator SwitchField(bool entering, string bossName)
    {
        m_Flow = FindFirstObjectByType<BattleFlowMgr>();
        InformationPanelMgr info = FindFirstObjectByType<InformationPanelMgr>();
        bool accepted = m_Flow != null && info != null &&
            (entering ? m_Flow.EnterRaidField() : m_Flow.ReturnToField());
        if (!accepted)
            throw new System.InvalidOperationException("씬 등록과 파티 상태, UI 연결을 확인해주세요.");

        try
        {
            MessageBoxMgr.Inst.ShowLoadingMsg(entering ? $"{bossName} 레이드에 입장하는 중" : "필드로 복귀하는 중");
            info.CloseMenusForRaidTransition();
            while (m_Flow != null && m_Flow.IsSwitchingMap)
                yield return null;
        }
        finally
        {
            if (MessageBoxMgr.Inst != null) MessageBoxMgr.Inst.CloseLoadingMsg();
        }
        RefreshUI();
        if (m_Flow == null || m_Flow.State == BattleFlowMgr.BattleState.Failed ||
            m_Flow.Mode != (entering ? BattleFlowMgr.BattleMode.Raid : BattleFlowMgr.BattleMode.Field))
            throw new System.InvalidOperationException("씬 전환 실패: Console과 씬 상태를 확인해주세요.");
    }

    private void OnDisable()
    {
        CloseGiveUpConfirmation();
        ResetResult();
        SetPanelActive(m_DefaultRaidPanel, false);
        SetPanelActive(m_RaidResultPanel, false);
        UnbindBoss();
        m_LastRemainingSeconds = -1;
        UpdateBossHp(0f, 0f);
        if (m_Transition != null)
        {
            StopCoroutine(m_Transition);
            m_Transition = null;
            if (MessageBoxMgr.Inst != null) MessageBoxMgr.Inst.CloseLoadingMsg();
        }
        if (m_OnRaidUI != null) m_OnRaidUI.SetActive(false);
    }

    private void OnDestroy()
    {
        if (m_GiveupBtn != null)
            m_GiveupBtn.onClick.RemoveListener(GiveUpRaid);
        if (m_ComeBackFieldBtn != null)
            m_ComeBackFieldBtn.onClick.RemoveListener(ReturnToField);
    }
}
