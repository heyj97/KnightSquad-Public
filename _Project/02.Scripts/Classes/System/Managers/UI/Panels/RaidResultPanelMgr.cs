using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

/// <summary>확정된 레이드 결과를 표시한다. 보상 지급은 전투 매니저에서 수행한다.</summary>
public class RaidResultPanelMgr : MonoBehaviour
{
    [Serializable]
    private class BossIllustration
    {
        public GlobalConstants.RaidBossName Boss;
        public Sprite Illustration;
    }

    [SerializeField] private TextGradient m_TitleGradient;
    [SerializeField] private Text m_BossNameTxt;
    [SerializeField] private Text m_BossSeasonTxt;
    [SerializeField] private Image m_BossIllustImg;
    [SerializeField] private Text m_ClearTimeTxt;
    [SerializeField] private Text m_DmgDealtTxt;
    [SerializeField] private RaidCharDetailCellMgr[] m_CharacterCells;
    [SerializeField] private Sprite[] m_RoleSprites;
    [SerializeField] private BossIllustration[] m_BossIllustrations;
    [SerializeField] private Transform m_RewardContent;
    [SerializeField] private GameObject m_FailRewardTxt;
    [SerializeField] private ItemCellMgr m_InventoryCellPrefab;
    [SerializeField] private Button m_RetryBtn;
    [SerializeField] private Button m_ComBackFieldBtn;
    private readonly List<ItemCellMgr> m_RewardCells = new();
    private RaidCombatResult m_Displayed;
    private OnRaidUIMgr m_Owner;

    private void Awake()
    {
        if (m_RetryBtn != null) m_RetryBtn.onClick.AddListener(Retry);
        if (m_ComBackFieldBtn != null) m_ComBackFieldBtn.onClick.AddListener(Return);
    }

    public void Show(RaidCombatResult result, OnRaidUIMgr owner)
    {
        m_Owner = owner;
        if (result == null || m_Displayed == result) return;
        m_Displayed = result;
        ClearRewards();
        var boss = GlobalConstants.GetRaidBossName(result.Boss.Id);
        if (m_TitleGradient != null)
            m_TitleGradient.SetBottomColor(GlobalConstants.GetBossColor32(boss));
        if (m_BossNameTxt != null) m_BossNameTxt.text = result.Boss.DisplayName + (result.Cleared ? " 토벌 성공" : " 토벌 실패");
        if (m_BossNameTxt != null && m_BossNameTxt.TryGetComponent<TextGradient>(out var gradient))
            gradient.SetBottomColor(GlobalConstants.GetBossColor32(boss));
        if (m_BossSeasonTxt != null)
        {
            string reason = result.Reason == BattleFlowMgr.DefeatReason.GiveUp ? "레이드 포기"
                : result.Reason == BattleFlowMgr.DefeatReason.BossTimeExpired ? "시간 초과" : "파티 전멸";
            m_BossSeasonTxt.text = result.Cleared ? $"Season {GlobalConstants.GetRaidSeason(boss)}. 레이드 보스" : "사유: " + reason;
            m_BossSeasonTxt.color = result.Cleared ? GlobalConstants.GetBossColor32(boss) : new Color32(166, 166, 166, 255);
        }
        if (m_BossIllustImg != null)
        {
            Sprite sprite = null;
            if (m_BossIllustrations != null)
                foreach (var entry in m_BossIllustrations)
                    if (entry != null && entry.Boss == boss) { sprite = entry.Illustration; break; }
            m_BossIllustImg.sprite = sprite;
            m_BossIllustImg.enabled = sprite != null;
        }
        long hundredths = (long)Math.Floor(result.ElapsedSeconds * 100.0);
        if (m_ClearTimeTxt != null) m_ClearTimeTxt.text = $"{hundredths / 6000}분 {hundredths / 100 % 60}초 {hundredths % 100:00}";
        if (m_DmgDealtTxt != null)
        {
            string totalDamage = Math.Ceiling(result.TotalDamage + result.PatternDamage).ToString("N0", CultureInfo.InvariantCulture);
            string partyDamage = Math.Ceiling(result.TotalDamage).ToString("N0", CultureInfo.InvariantCulture);
            string patternDamage = Math.Ceiling(result.PatternDamage).ToString("N0", CultureInfo.InvariantCulture);
            m_DmgDealtTxt.text = $"{totalDamage} ({partyDamage} <color=#0080E9>+{patternDamage}</color>)";
        }
        var records = new List<RaidCombatResult.CharacterRecord>(result.Characters);
        records.Sort((a, b) => { int order = b.Damage.CompareTo(a.Damage); return order != 0 ? order : a.Slot.CompareTo(b.Slot); });
        if (m_CharacterCells != null)
            for (int i = 0; i < m_CharacterCells.Length; i++)
            {
                var cell = m_CharacterCells[i];
                if (cell == null) continue;
                cell.gameObject.SetActive(i < records.Count);
                if (i < records.Count) cell.Show(records[i], result.TotalDamage, m_RoleSprites);
            }
        if (m_RewardContent != null) m_RewardContent.gameObject.SetActive(result.Cleared);
        if (m_FailRewardTxt != null) m_FailRewardTxt.SetActive(!result.Cleared);
        if (result.Cleared && m_RewardContent != null && m_InventoryCellPrefab != null)
            foreach (var item in result.Items)
            {
                var cell = Instantiate(m_InventoryCellPrefab, m_RewardContent, false);
                cell.gameObject.SetActive(true);
                cell.InitializeItem(item);
                cell.BindActions(null, null);
                m_RewardCells.Add(cell);
            }
        if (m_RetryBtn != null) m_RetryBtn.gameObject.SetActive(!result.Cleared);
    }

    public void SetButtonsInteractable(bool allowed)
    {
        if (m_RetryBtn != null) m_RetryBtn.interactable = allowed;
        if (m_ComBackFieldBtn != null) m_ComBackFieldBtn.interactable = allowed;
    }

    private void Retry() { if (m_Owner != null) m_Owner.RetryRaid(); }
    private void Return() { if (m_Owner != null) m_Owner.ReturnToField(); }
    private void ClearRewards()
    {
        foreach (var cell in m_RewardCells)
            if (cell != null) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
        m_RewardCells.Clear();
    }
    private void OnDisable() { m_Displayed = null; ClearRewards(); }
    private void OnDestroy()
    {
        if (m_RetryBtn != null) m_RetryBtn.onClick.RemoveListener(Retry);
        if (m_ComBackFieldBtn != null) m_ComBackFieldBtn.onClick.RemoveListener(Return);
    }
}
