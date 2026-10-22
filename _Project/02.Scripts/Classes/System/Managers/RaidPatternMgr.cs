using System.Collections.Generic;
using UnityEngine;

/// <summary>75/50/25% HP 구간에 세 미니게임을 중복 없는 무작위 순서로 연결한다.</summary>
[DisallowMultipleComponent]
public class RaidPatternMgr : MonoBehaviour
{
    [SerializeField] private RaidIllusionPatternMgr m_IllusionPattern;
    [SerializeField] private RaidTilePatternMgr m_TilePattern;
    [SerializeField] private RaidZeolitePatternMgr m_ZeolitePattern;
    [SerializeField] private GameObject m_MiniGameRoot;
    [Header("Pattern staging")]
    [SerializeField] private CanvasGroup m_StagingGroup;
    [SerializeField] private UnityEngine.UI.Text m_StagingTxt;
    [SerializeField, TextArea] private string m_StagingMessage = "진실된 모습을 찾을 수 있겠는가";
    [SerializeField, Min(0.01f)] private float m_StagingDuration = 2f;
    [SerializeField, Min(0f)] private float m_StagingFadeInDuration = 0.5f;
    [Header("Pattern result message")]
    [SerializeField, TextArea] private string m_SuccessMessage = "제법이군";
    [SerializeField, TextArea] private string m_FailureMessage = "허상조차 파악하지 못하는구나";
    [SerializeField, Min(0f)] private float m_ResultFadeInDuration = 0.5f;
    [SerializeField, Min(0f)] private float m_ResultHoldDuration = 2f;
    [SerializeField, Min(0f)] private float m_ResultFadeOutDuration = 0.5f;
    [Header("Tile pattern messages")]
    [SerializeField, TextArea] private string m_TileStagingMessage = "살아남을 길을 기억해 보아라";
    [SerializeField, TextArea] private string m_TileSuccessMessage = "제법이군";
    [SerializeField, TextArea] private string m_TileFailureMessage = "길조차 기억하지 못하는구나";
    [Header("Illusion damage (%)")]
    [Tooltip("성공 시 보스 최대 HP에 적용할 고정 피해의 백분율. 10 = 10%")]
    [SerializeField, Range(0f, 100f)] private float m_IllusionSuccessDamagePercent = 10f;
    [Tooltip("실패 시 생존 아군 각각의 최대 HP에 적용할 고정 피해의 백분율")]
    [SerializeField, Range(0f, 100f)] private float m_IllusionFailureDamagePercent = 60f;
    [Header("Tile damage (%)")]
    [Tooltip("성공 시 보스 최대 HP에 적용할 고정 피해의 백분율. 10 = 10%")]
    [SerializeField, Range(0f, 100f)] private float m_TileSuccessDamagePercent = 10f;
    [Tooltip("실패 시 생존 아군 각각의 최대 HP에 적용할 고정 피해의 백분율")]
    [SerializeField, Range(0f, 100f)] private float m_TileFailureDamagePercent = 80f;
    [Header("Zeolite pattern messages")]
    [SerializeField, TextArea] private string m_ZeoliteStagingMessage = "문양의 부름에 응해 보아라";
    [SerializeField, TextArea] private string m_ZeoliteSuccessMessage = "제법이군";
    [SerializeField, TextArea] private string m_ZeoliteFailureMessage = "문양조차 구분하지 못하는구나";
    [Header("Zeolite damage (%)")]
    [SerializeField, Range(0f, 100f)] private float m_ZeoliteSuccessDamagePercent = 15f;
    [SerializeField, Range(0f, 100f)] private float m_ZeoliteFailureDamagePercent = 100f;
    private enum PatternKind { Illusion, Tile, Zeolite }
    private readonly List<PatternKind> m_PatternOrder = new();
    private PatternKind m_ActivePattern;
    private readonly Dictionary<Animator, float> m_AnimatorSpeeds = new();
    private BattleFlowMgr m_Flow;
    private MonsterBase m_Boss;
    private CharacterBase[] m_Party;
    private int m_LastRange = -1;
    private bool m_PendingPattern;
    private bool m_Paused;
    private bool? m_PendingResult;
    private bool m_IsStaging;
    private float m_StagingStartedAt;
    private Animator m_StagingAnimator;
    private float m_StagingAttackDuration;
    private bool m_StagingAttackFinished;
    private bool m_IsResultMessage;
    private float m_ResultMessageStartedAt;

    private void Awake()
    {
        if (m_MiniGameRoot != null && !transform.IsChildOf(m_MiniGameRoot.transform))
            m_MiniGameRoot.SetActive(false);
        HideStaging();
    }

    private void Update()
    {
        // 클리어/전멸이 확정되어 BattleFlowMgr 연결이 해제되어도 결과 문구는 끝까지 표시한다.
        if (!m_IsResultMessage || m_StagingGroup == null) return;
        float elapsed = Time.time - m_ResultMessageStartedAt;
        float holdEnd = m_ResultFadeInDuration + m_ResultHoldDuration;
        if (elapsed >= holdEnd + m_ResultFadeOutDuration)
        {
            m_IsResultMessage = false;
            HideStaging();
        }
        else if (elapsed < m_ResultFadeInDuration)
            m_StagingGroup.alpha = Mathf.Clamp01(elapsed / m_ResultFadeInDuration);
        else if (elapsed < holdEnd)
            m_StagingGroup.alpha = 1f;
        else
            m_StagingGroup.alpha = 1f - Mathf.Clamp01((elapsed - holdEnd) / m_ResultFadeOutDuration);
    }

    internal bool Initialize(BattleFlowMgr flow, MonsterBase boss, CharacterBase[] party)
    {
        Shutdown();
        if (flow == null || boss == null || party == null || m_IllusionPattern == null || m_TilePattern == null ||
            m_ZeolitePattern == null || !m_IllusionPattern.enabled || !m_TilePattern.enabled ||
            !m_ZeolitePattern.enabled || m_MiniGameRoot == null ||
            transform.IsChildOf(m_MiniGameRoot.transform) ||
            m_IllusionPattern.transform.IsChildOf(m_MiniGameRoot.transform) ||
            m_TilePattern.transform.IsChildOf(m_MiniGameRoot.transform) ||
            m_ZeolitePattern.transform.IsChildOf(m_MiniGameRoot.transform) ||
            m_StagingGroup == null || m_StagingTxt == null ||
            transform.IsChildOf(m_StagingGroup.transform) ||
            m_IllusionPattern.transform.IsChildOf(m_StagingGroup.transform) ||
            m_TilePattern.transform.IsChildOf(m_StagingGroup.transform) ||
            m_ZeolitePattern.transform.IsChildOf(m_StagingGroup.transform) ||
            transform.IsChildOf(m_ZeolitePattern.transform) ||
            transform.IsChildOf(m_IllusionPattern.transform) || transform.IsChildOf(m_TilePattern.transform) ||
            m_IllusionPattern.transform.IsChildOf(m_TilePattern.transform) ||
            m_TilePattern.transform.IsChildOf(m_IllusionPattern.transform) ||
            m_ZeolitePattern.transform.IsChildOf(m_IllusionPattern.transform) ||
            m_ZeolitePattern.transform.IsChildOf(m_TilePattern.transform) ||
            m_IllusionPattern.transform.IsChildOf(m_ZeolitePattern.transform) ||
            m_TilePattern.transform.IsChildOf(m_ZeolitePattern.transform))
            return false;
        m_Flow = flow;
        m_Boss = boss;
        m_Party = party;
        m_LastRange = -1;
        m_PatternOrder.AddRange(new[] { PatternKind.Illusion, PatternKind.Tile, PatternKind.Zeolite });
        for (int i = m_PatternOrder.Count - 1; i > 0; i--)
        {
            int other = UnityEngine.Random.Range(0, i + 1);
            (m_PatternOrder[i], m_PatternOrder[other]) = (m_PatternOrder[other], m_PatternOrder[i]);
        }
        m_IllusionPattern.IsRaidManaged = true;
        m_TilePattern.IsRaidManaged = true;
        m_ZeolitePattern.IsRaidManaged = true;
        m_IllusionPattern.OnPatternCompleted += HandlePatternCompleted;
        m_TilePattern.OnPatternCompleted += HandlePatternCompleted;
        m_ZeolitePattern.OnPatternCompleted += HandlePatternCompleted;
        CancelAndHidePatterns();
        return true;
    }

    internal bool TryQueueForCurrentHp()
    {
        if (m_Flow == null || m_Flow.State != BattleFlowMgr.BattleState.Fighting ||
            m_Boss == null || m_Boss.CurHp <= 0f)
            return false;
        if (m_PendingPattern) return true;
        float hp = m_Boss.MaxHp > 0f ? m_Boss.CurHp / m_Boss.MaxHp : 1f;
        int range = hp <= 0.25f ? 2 : hp <= 0.5f ? 1 : hp <= 0.75f ? 0 : -1;
        if (range <= m_LastRange) return false;
        // 건너뛴 상위 구간은 다시 실행하지 않는다.
        m_LastRange = range;
        m_ActivePattern = m_PatternOrder[range];
        m_PendingPattern = true;
        PauseCombat();
        return true;
    }

    private void PauseCombat()
    {
        if (m_Paused) return;
        m_Paused = true;
        m_Flow.SetRaidPatternPaused(true);
        foreach (CharacterBase character in m_Party)
            if (character != null) FreezeAnimators(character);
        FreezeAnimators(m_Boss);
    }

    private void FreezeAnimators(Component owner)
    {
        foreach (Animator animator in owner.GetComponentsInChildren<Animator>(true))
            if (!m_AnimatorSpeeds.ContainsKey(animator))
            {
                m_AnimatorSpeeds.Add(animator, animator.speed);
                animator.speed = 0f;
            }
    }

    // BattleFlowMgr가 전체 시간 만료를 확인한 뒤 호출한다.
    internal void AdvancePendingPattern()
    {
        if (m_IsResultMessage || m_Flow == null || m_Flow.State != BattleFlowMgr.BattleState.Fighting)
            return;
        if (m_PendingPattern)
        {
            m_PendingPattern = false;
            m_IsStaging = true;
            m_StagingStartedAt = Time.time;
            m_StagingGroup.alpha = 0f;
            m_StagingGroup.interactable = false;
            m_StagingGroup.blocksRaycasts = false;
            m_StagingTxt.text = m_ActivePattern switch
            {
                PatternKind.Tile => m_TileStagingMessage,
                PatternKind.Zeolite => m_ZeoliteStagingMessage,
                _ => m_StagingMessage
            };
            m_StagingGroup.gameObject.SetActive(true);
            m_StagingAttackFinished = !m_Boss.TryBeginRaidStaging(out m_StagingAnimator,
                out m_StagingAttackDuration);
            return;
        }
        if (!m_IsStaging) return;
        float elapsed = Time.time - m_StagingStartedAt;
        float fadeDuration = Mathf.Min(m_StagingFadeInDuration, m_StagingDuration);
        m_StagingGroup.alpha = fadeDuration > 0f ? Mathf.Clamp01(elapsed / fadeDuration) : 1f;
        if (!m_StagingAttackFinished && elapsed >= m_StagingAttackDuration)
        {
            m_Boss.PlayRaidStagingIdle();
            m_StagingAttackFinished = true;
        }
        if (elapsed < m_StagingDuration) return;
        EndStaging();
        CancelAndHidePatterns();
        m_MiniGameRoot.SetActive(true);
        bool started;
        switch (m_ActivePattern)
        {
            case PatternKind.Tile:
                m_TilePattern.gameObject.SetActive(true);
                m_TilePattern.BeginPattern();
                started = m_TilePattern.IsRunning;
                break;
            case PatternKind.Zeolite:
                m_ZeolitePattern.gameObject.SetActive(true);
                m_ZeolitePattern.BeginPattern();
                started = m_ZeolitePattern.IsRunning;
                break;
            default:
                m_IllusionPattern.gameObject.SetActive(true);
                m_IllusionPattern.BeginPattern();
                started = m_IllusionPattern.IsRunning;
                break;
        }
        if (!started)
        {
            Debug.LogError("레이드 패턴을 시작하지 못했습니다. Inspector 참조와 모델을 확인해주세요.", this);
            // 초기화 오류로 전투가 영구 정지하지 않도록 해당 패턴 실패로 처리한다.
            m_PendingResult = false;
        }
    }

    private void HideStaging()
    {
        if (m_StagingGroup == null || transform.IsChildOf(m_StagingGroup.transform)) return;
        m_StagingGroup.alpha = 0f;
        m_StagingGroup.gameObject.SetActive(false);
    }

    private void EndStaging()
    {
        if (m_IsStaging && m_Boss != null) m_Boss.PlayRaidStagingIdle();
        if (m_StagingAnimator != null) m_StagingAnimator.speed = 0f;
        m_StagingAnimator = null;
        m_IsStaging = false;
        m_StagingAttackFinished = false;
        HideStaging();
    }

    private void HandlePatternCompleted(bool succeeded) => m_PendingResult = succeeded;

    private void CancelAndHidePatterns()
    {
        m_IllusionPattern.CancelPattern();
        m_TilePattern.CancelPattern();
        m_ZeolitePattern.CancelPattern();
        m_IllusionPattern.gameObject.SetActive(false);
        m_TilePattern.gameObject.SetActive(false);
        m_ZeolitePattern.gameObject.SetActive(false);
    }

    private void ShowPatternResultMessage(bool succeeded, PatternKind pattern)
    {
        m_IsResultMessage = true;
        m_ResultMessageStartedAt = Time.time;
        m_StagingTxt.text = pattern switch
        {
            PatternKind.Tile => succeeded ? m_TileSuccessMessage : m_TileFailureMessage,
            PatternKind.Zeolite => succeeded ? m_ZeoliteSuccessMessage : m_ZeoliteFailureMessage,
            _ => succeeded ? m_SuccessMessage : m_FailureMessage
        };
        m_StagingGroup.alpha = m_ResultFadeInDuration > 0f ? 0f : 1f;
        m_StagingGroup.gameObject.SetActive(true);
    }

    internal void ResolveResult()
    {
        if (!m_PendingResult.HasValue || m_Flow == null ||
            m_Flow.State != BattleFlowMgr.BattleState.Fighting) return;
        bool succeeded = m_PendingResult.Value;
        PatternKind completedPattern = m_ActivePattern;
        m_PendingResult = null;
        CancelAndHidePatterns();
        m_MiniGameRoot.SetActive(false);
        float damagePercent = completedPattern switch
        {
            PatternKind.Tile => succeeded ? m_TileSuccessDamagePercent : m_TileFailureDamagePercent,
            PatternKind.Zeolite => succeeded ? m_ZeoliteSuccessDamagePercent : m_ZeoliteFailureDamagePercent,
            _ => succeeded ? m_IllusionSuccessDamagePercent : m_IllusionFailureDamagePercent
        };
        if (succeeded)
            m_Boss.TakeRaidPatternDamage(damagePercent / 100f);
        else
        {
            float failureRatio = damagePercent / 100f;
            // 사망 이벤트가 Shutdown을 호출할 수 있으므로 순회할 배열을 보관한다.
            CharacterBase[] party = m_Party;
            foreach (CharacterBase character in party)
                if (character != null && !character.IsDead)
                    character.TakeRaidPatternDamage(failureRatio);
        }
        if (m_Flow != null && m_Flow.State == BattleFlowMgr.BattleState.Fighting &&
            m_Boss != null && m_Boss.CurHp <= 0f)
            m_Boss.FinalizeRaidDeath();
        // 피해로 클리어/전멸이 확정된 경우에도 결과 문구는 표시한다.
        ShowPatternResultMessage(succeeded, completedPattern);
        // 피해 적용 중 클리어/전멸로 Shutdown된 경우 이후 전투 흐름을 실행하지 않는다.
        if (m_Flow == null || m_Flow.State != BattleFlowMgr.BattleState.Fighting || m_Boss == null) return;
        if (succeeded && TryQueueForCurrentHp()) return;
        ResumeCombat();
    }

    private void ResumeCombat()
    {
        foreach (KeyValuePair<Animator, float> entry in m_AnimatorSpeeds)
            if (entry.Key != null) entry.Key.speed = entry.Value;
        m_AnimatorSpeeds.Clear();
        m_Paused = false;
        if (m_Flow != null) m_Flow.SetRaidPatternPaused(false);
    }

    internal void Shutdown()
    {
        m_IsResultMessage = false;
        EndStaging();
        if (m_IllusionPattern != null)
        {
            m_IllusionPattern.OnPatternCompleted -= HandlePatternCompleted;
            m_IllusionPattern.IsRaidManaged = false;
            m_IllusionPattern.CancelPattern();
        }
        if (m_TilePattern != null)
        {
            m_TilePattern.OnPatternCompleted -= HandlePatternCompleted;
            m_TilePattern.IsRaidManaged = false;
            m_TilePattern.CancelPattern();
        }
        if (m_ZeolitePattern != null)
        {
            m_ZeolitePattern.OnPatternCompleted -= HandlePatternCompleted;
            m_ZeolitePattern.IsRaidManaged = false;
            m_ZeolitePattern.CancelPattern();
        }
        if (m_Flow != null && m_IllusionPattern != null && m_TilePattern != null && m_ZeolitePattern != null)
            CancelAndHidePatterns();
        if (m_MiniGameRoot != null && !transform.IsChildOf(m_MiniGameRoot.transform))
            m_MiniGameRoot.SetActive(false);
        ResumeCombat();
        m_PendingPattern = false;
        m_PendingResult = null;
        m_Flow = null;
        m_Boss = null;
        m_Party = null;
        m_LastRange = -1;
        m_PatternOrder.Clear();
    }

    private void OnDisable() => Shutdown();
}
