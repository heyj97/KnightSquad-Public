using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>여섯 종류의 강화를 편집한 뒤 저장 버튼에서 메모리에 일괄 확정합니다.</summary>
public class PartyLvUpPanelMgr : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] GameObject m_PartyLvUpPanel;
    [SerializeField] Button m_CloseBtn;
    [SerializeField] Button m_SaveBtn;
    [SerializeField] Text m_EsGoldCostTxt;
    [Header("Party Lv Up Cell")]
    [SerializeField] Button m_MaxHpUpBtn;
    [SerializeField] Button m_AtkUpBtn;
    [SerializeField] Button m_AtkSpdUpBtn;
    [SerializeField] Button m_DefUpBtn;
    [SerializeField] Button m_CritChanceUpBtn;
    [SerializeField] Button m_CritDmgUpBtn;
    [Header("Party Stat Lv Text")]
    [SerializeField] Text m_MaxHpLvText;
    [SerializeField] Text m_AtkLvText;
    [SerializeField] Text m_AtkSpdLvText;
    [SerializeField] Text m_DefLvText;
    [SerializeField] Text m_CritChanceLvText;
    [SerializeField] Text m_CritDmgLvText;

    [Serializable]
    public class EstimateCell
    {
        public GameObject Root;
        public Text LevelText;
        public Text DescriptionText;
        [NonSerialized] public string DescriptionTemplate;
    }
    [Header("Estimate: MaxHp / Atk / AtkSpd / Def / CritChance / CritDmgMult")]
    [SerializeField] EstimateCell[] m_EstimateCells = new EstimateCell[6];
    private Button[] m_Buttons;
    private UnityAction[] m_LeftActions;
    private PartyLvUpCellInput[] m_RightInputs;
    private int[] m_Original, m_Target;
    private int m_Session;
    private bool m_Applying;
    private static readonly string[] StatNames = { "최대 HP", "공격력", "공격속도", "방어력", "치명타 확률", "치명타 피해" };
    private static readonly float[] Increases = { GlobalConstants.HpMultiplierPerLv, GlobalConstants.AttackMultiplierPerLv,
        GlobalConstants.AttackSpeedMultiplierPerLv, GlobalConstants.DefenseMultiplierPerLv,
        GlobalConstants.CriticalChancePerLv, GlobalConstants.CriticalDamageMultiplierPerLv };
    public bool HasChanges => m_Original != null && !m_Original.SequenceEqual(m_Target);

    private void Start()
    {
        m_Buttons = new[] { m_MaxHpUpBtn, m_AtkUpBtn, m_AtkSpdUpBtn, m_DefUpBtn, m_CritChanceUpBtn, m_CritDmgUpBtn };
        m_LeftActions = new UnityAction[6];
        m_RightInputs = new PartyLvUpCellInput[6];
        for (int i = 0; i < 6; i++)
        {
            int index = i;
            if (m_Buttons[i] == null) continue;
            m_LeftActions[i] = () => EditLevel(index, 1);
            m_Buttons[i].onClick.AddListener(m_LeftActions[i]);
            m_RightInputs[i] = m_Buttons[i].GetComponent<PartyLvUpCellInput>();
            if (m_RightInputs[i] == null) m_RightInputs[i] = m_Buttons[i].gameObject.AddComponent<PartyLvUpCellInput>();
            m_RightInputs[i].BindRightClick(() => EditLevel(index, -1));
        }
        if (m_SaveBtn != null) m_SaveBtn.onClick.AddListener(SaveClick);
        if (m_CloseBtn != null) m_CloseBtn.onClick.AddListener(CloseClick);
        RefreshUI();
    }
    private void OnEnable()
    {
        m_Session++;
        m_Original = PartyMgr.GetPartyGrowthLevels();
        m_Target = (int[])m_Original.Clone();
        GlobalValue.OnUserGoldChanged += OnGoldChanged;
        GlobalValue.OnPartyGrowthChanged += OnGrowthChanged;
        RefreshUI();
    }
    private void OnDisable()
    {
        GlobalValue.OnUserGoldChanged -= OnGoldChanged;
        GlobalValue.OnPartyGrowthChanged -= OnGrowthChanged;
        m_Session++;
        m_Original = m_Target = null;
    }
    private void OnDestroy()
    {
        if (m_Buttons != null)
            for (int i = 0; i < 6; i++)
            {
                if (m_Buttons[i] != null) m_Buttons[i].onClick.RemoveListener(m_LeftActions[i]);
                if (m_RightInputs[i] != null) m_RightInputs[i].ClearBinding();
            }
        if (m_SaveBtn != null) m_SaveBtn.onClick.RemoveListener(SaveClick);
        if (m_CloseBtn != null) m_CloseBtn.onClick.RemoveListener(CloseClick);
    }
    private void OnGoldChanged(ulong _) => RefreshUI();
    private void OnGrowthChanged()
    {
        if (m_Applying) return;
        if (!HasChanges)
        {
            m_Original = PartyMgr.GetPartyGrowthLevels();
            m_Target = (int[])m_Original.Clone();
        }
        RefreshUI();
    }
    private void EditLevel(int index, int delta)
    {
        if (!isActiveAndEnabled || m_Target == null || MessageBoxMgr.IsBlockingInput || PlayerDataMgr.IsProgressBlocked) return;
        if (delta > 0 && m_Target[index] >= GlobalConstants.MaxPartyGrowthLevel ||
            delta < 0 && m_Target[index] <= m_Original[index]) return;
        int previous = m_Target[index];
        m_Target[index] += delta;
        if (!PartyUpgradePlan.TryGetCost(m_Original, m_Target, out ulong cost))
        { m_Target[index] = previous; ItemSaleMgr.ShowMessage("강화 레벨 또는 비용 정보가 올바르지 않습니다."); return; }
        if (delta > 0 && cost > GlobalValue.UserGold)
        { m_Target[index] = previous; ItemSaleMgr.ShowMessage("골드가 부족합니다"); return; }
        RefreshUI();
    }
    public void SaveClick()
    {
        if (m_Applying || !isActiveAndEnabled || m_Target == null || MessageBoxMgr.IsBlockingInput) return;
        if (!HasChanges) { ClosePanel(); return; }
        var party = FindFirstObjectByType<PartyMgr>();
        if (party == null) { ItemSaleMgr.ShowMessage("파티가 아직 준비되지 않았습니다."); return; }
        m_Applying = true;
        try
        {
            if (!party.TryApplyPartyUpgradePlan(m_Original, m_Target, out string message))
            { ItemSaleMgr.ShowMessage(message); return; }
            ClosePanel();
        }
        finally { m_Applying = false; }
    }
    public void CloseClick()
    {
        if (m_Applying || MessageBoxMgr.IsBlockingInput) return;
        if (!HasChanges) { ClosePanel(); return; }
        int session = m_Session;
        ItemSaleMgr.ShowDialog("변경사항이 있습니다. 적용하지 않고 닫을까요?", MessageBoxType.Submit,
            () => { if (isActiveAndEnabled && m_Session == session) ClosePanel(); });
    }
    private void ClosePanel() => (m_PartyLvUpPanel != null ? m_PartyLvUpPanel : gameObject).SetActive(false);
    private void RefreshUI()
    {
        if (m_Target == null) return;
        var texts = new[] { m_MaxHpLvText, m_AtkLvText, m_AtkSpdLvText, m_DefLvText, m_CritChanceLvText, m_CritDmgLvText };
        int[] actual = PartyMgr.GetPartyGrowthLevels();
        for (int i = 0; i < 6; i++)
        {
            if (texts[i] != null)
            {
                texts[i].text = actual[i].ToString();
                texts[i].color = actual[i] < 2 ? Color.white : GlobalConstants.GetRarityColor32(
                    actual[i] >= 10 ? SystemRarity.Mystic : actual[i] >= 8 ? SystemRarity.Legendary :
                    actual[i] >= 6 ? SystemRarity.Epic : actual[i] >= 4 ? SystemRarity.Rare : SystemRarity.Uncommon);
            }
            if (m_EstimateCells == null || i >= m_EstimateCells.Length || m_EstimateCells[i] == null) continue;
            var cell = m_EstimateCells[i];
            if (cell.Root != null) cell.Root.SetActive(m_Target[i] != m_Original[i]);
            if (cell.LevelText != null) cell.LevelText.text = $"{StatNames[i]} 증가 (레벨 <color=#00ff00>{m_Target[i]}</color>)";
            if (cell.DescriptionText != null)
            {
                if (cell.DescriptionTemplate == null) cell.DescriptionTemplate = cell.DescriptionText.text;
                string amount = (m_Target[i] * Increases[i] * 100f).ToString("N1");
                // 문장은 유지하고 최초 '%' 앞 숫자만 최종 강화량으로 교체한다.
                var pattern = new System.Text.RegularExpressions.Regex(
                    @"[+-]?\d+(?:[.,]\d+)?(?=(?:</color>)?\s*%)", System.Text.RegularExpressions.RegexOptions.None,
                    TimeSpan.FromMilliseconds(100));
                cell.DescriptionText.text = pattern.Replace(cell.DescriptionTemplate, amount, 1);
            }
        }
        if (m_EsGoldCostTxt != null)
        {
            PartyUpgradePlan.TryGetCost(m_Original, m_Target, out ulong cost);
            m_EsGoldCostTxt.text = $"예상 소비골드: <color=#ff0000>{cost:N0}</color> G";
        }
    }
}

/// <summary>현재 레벨에서 목표 레벨까지 모든 단계 비용을 합산합니다.</summary>
public static class PartyUpgradePlan
{
    public static bool TryGetCost(int[] original, int[] target, out ulong cost)
    {
        cost = 0;
        ulong total = 0;
        if (original == null || target == null || original.Length != 6 || target.Length != 6) return false;
        for (int i = 0; i < 6; i++)
        {
            if (original[i] < 1 || target[i] < original[i] || target[i] > GlobalConstants.MaxPartyGrowthLevel) return false;
            for (int level = original[i]; level < target[i]; level++)
            {
                int index = level - 1;
                if (index >= GlobalConstants.PartyGoldSpendTable.Count || GlobalConstants.PartyGoldSpendTable[index] < 0) return false;
                ulong step = (ulong)GlobalConstants.PartyGoldSpendTable[index];
                if (ulong.MaxValue - total < step) return false;
                total += step;
            }
        }
        cost = total;
        return true;
    }
}
