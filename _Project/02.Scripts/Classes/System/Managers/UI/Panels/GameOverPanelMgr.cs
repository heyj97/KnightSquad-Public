using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// 전투 패배 사유를 표시하고 재도전 또는 이전 챕터 이동을 연결합니다.
/// </summary>
public class GameOverPanelMgr : MonoBehaviour
{
    [SerializeField] private GameObject m_GameOverPanel;
    [FormerlySerializedAs("m_GameOverTxt")] [SerializeField] private Text m_ReasonTxt;
    [SerializeField] private Button m_RetryCurCahpterBtn;
    [SerializeField] private Button m_BackToPrevChapterBtn;

    private BattleFlowMgr m_BattleFlow;
    private BattleFlowMgr.BattleState? m_LastState;
    private float m_NextFindTime;

    private void Awake()
    {
        if (m_RetryCurCahpterBtn != null)
            m_RetryCurCahpterBtn.onClick.AddListener(RetryCurCahpter);

        if (m_BackToPrevChapterBtn != null)
            m_BackToPrevChapterBtn.onClick.AddListener(BackToPrevChapter);

        HideGameOver();
    }

    private void OnEnable()
    {
        m_LastState = null;
        m_NextFindTime = 0f;
    }

    private void Update()
    {
        // UIScene이 먼저 로드되는 경우 GameScene 준비 후 연결한다.
        if (m_BattleFlow == null || !m_BattleFlow.isActiveAndEnabled)
        {
            if (Time.unscaledTime < m_NextFindTime)
                return;

            m_NextFindTime = Time.unscaledTime + 0.5f;
            m_BattleFlow = FindFirstObjectByType<BattleFlowMgr>();
            m_LastState = null;

            if (m_BattleFlow == null || !m_BattleFlow.isActiveAndEnabled)
                return;
        }

        // 레이드 전용 결과창은 별도로 구현한다. 필드 재도전 UI가 복귀 버튼을 가리지 않게 한다.
        if (m_BattleFlow.Mode == BattleFlowMgr.BattleMode.Raid)
        {
            if (m_LastState != null)
            {
                HideGameOver();
                SetButtons(false, false);
                m_LastState = null;
            }
            return;
        }

        if (m_LastState == m_BattleFlow.State)
            return;

        m_LastState = m_BattleFlow.State;

        switch (m_BattleFlow.State)
        {
            case BattleFlowMgr.BattleState.Defeated:
                if (m_BattleFlow.Mode == BattleFlowMgr.BattleMode.Field &&
                    StageMgr.Inst != null && StageMgr.Inst.IsFinalStage)
                {
                    HideGameOver();
                    SetButtons(false, false);
                    break;
                }
                ShowGameOver(
                    m_BattleFlow.LastDefeatReason ==
                    BattleFlowMgr.DefeatReason.BossTimeExpired
                        ? "제한시간이 초과되었습니다."
                        : "파티가 전멸했습니다.");

                SetButtons(true, CanGoPreviousChapter());
                break;

            case BattleFlowMgr.BattleState.Failed:
                ShowGameOver(
                    "전투 준비에 실패했습니다. 데이터와 참조를 확인한 뒤 재시도해 주세요.");

                // 실패 후에는 현재 설정된 챕터를 다시 준비한다.
                SetButtons(true, false);
                break;

            case BattleFlowMgr.BattleState.Fighting:
                HideGameOver();
                break;

            default:
                SetButtons(false, false);
                break;
        }
    }

    private bool CanGoPreviousChapter()
    {
        return m_BattleFlow != null &&
               m_BattleFlow.State == BattleFlowMgr.BattleState.Defeated &&
               m_BattleFlow.Mode == BattleFlowMgr.BattleMode.Field &&
               StageMgr.Inst != null &&
               StageMgr.Inst.CurChapter > 1;
    }

    /// <summary>
    /// 전투 패배 사유를 설정하고 게임 오버 패널을 표시합니다.
    /// </summary>
    /// <param name="gameOverReason">패배 화면에 표시할 종료 사유입니다.</param>
    public void ShowGameOver(string gameOverReason)
    {
        if (m_ReasonTxt != null)
            m_ReasonTxt.text = gameOverReason;

        if (m_GameOverPanel != null)
            m_GameOverPanel.SetActive(true);
    }

    /// <summary>
    /// 표시 중인 패배 사유를 지우고 게임 오버 패널을 닫습니다.
    /// </summary>
    public void HideGameOver()
    {
        if (m_ReasonTxt != null)
            m_ReasonTxt.text = "";

        if (m_GameOverPanel != null)
            m_GameOverPanel.SetActive(false);
    }

    private void RetryCurCahpter()
    {
        MessageBoxMgr.Inst.ShowMsg("현재 챕터를 재도전 하시겠습니까?",
            "알림",
            MessageBoxType.Submit,
            () =>  RequestRestart(false),
            null);
    }

    private void BackToPrevChapter()
    {
        MessageBoxMgr.Inst.ShowMsg("이전 챕터로 돌아가시겠습니까?",
            "알림",
            MessageBoxType.Submit,
            () => RequestRestart(true),
            null);
    }

    private void RequestRestart(bool previousChapter)
    {
        if (m_BattleFlow == null ||
            !m_BattleFlow.TryRestartChapter(previousChapter))
            return;

        SetButtons(false, false);

        if (m_ReasonTxt != null)
            m_ReasonTxt.text = "전투를 준비하고 있습니다.";

        // 다음 Update에서 최신 상태를 다시 반영한다.
        m_LastState = null;
    }

    private void SetButtons(bool retry, bool previous)
    {
        if (m_RetryCurCahpterBtn != null)
            m_RetryCurCahpterBtn.gameObject.SetActive(retry);

        if (m_BackToPrevChapterBtn != null)
            m_BackToPrevChapterBtn.gameObject.SetActive(previous);
    }

    private void OnDestroy()
    {
        if (m_RetryCurCahpterBtn != null)
            m_RetryCurCahpterBtn.onClick.RemoveListener(RetryCurCahpter);

        if (m_BackToPrevChapterBtn != null)
            m_BackToPrevChapterBtn.onClick.RemoveListener(BackToPrevChapter);
    }
}
