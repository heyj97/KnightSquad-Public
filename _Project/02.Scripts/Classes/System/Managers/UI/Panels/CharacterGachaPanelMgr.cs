using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>공용 캐릭터 셀을 사용해 메인·천장 후보 선택·결과 화면을 전환한다.</summary>
public class CharacterGachaPanelMgr : MonoBehaviour
{
    private enum View { Main, Targets, Results }
    [Header("Roots / Shared Cell")]
    [SerializeField] private GameObject m_MenuPanelRoot;
    [SerializeField] private GameObject m_CharacterGachaSection;
    [SerializeField] private GachaCharCellMgr m_GachaCharCellPrefab;
    [SerializeField] private Transform m_CharGachaListContent;
    [Header("Main Buttons / Gold")]
    [SerializeField] private Button m_CloseBtn;
    [SerializeField] private Button m_GoldDrawOneBtn;
    [SerializeField] private Button m_GoldDrawTenBtn;
    [SerializeField] private Text m_GoldDrawRemainTxt;
    [SerializeField] private Text m_GoldDrawOneCostTxt;
    [SerializeField] private Text m_GoldDrawTenCostTxt;
    [Header("Pity")]
    [SerializeField] private Text m_PityCountTxt;
    [SerializeField] private Image m_PityTargetIconImg;
    [SerializeField] private Button m_PityTargetSelectBtn;
    [SerializeField] private GameObject m_PityTargetSelectPanel;
    [SerializeField] private Transform m_PityTargetContent;
    [SerializeField] private Button m_PityTargetSelConfirmBtn;
    [FormerlySerializedAs("m_PityTargetCloseBtn")]
    [SerializeField] private Button m_PityTargetSelCancelBtn;
    [SerializeField] private Text m_SelCharNameTxt;
    [Header("Result")]
    [SerializeField] private GameObject m_GachaResultPanel;
    [SerializeField] private Transform m_ResultContent;
    [SerializeField] private Button m_ResultConfirmBtn;
    [SerializeField] private Text m_ResultDescTxt;
    [SerializeField] private Text m_ResultStarDustTxt;

    private readonly List<GachaCharCellMgr> m_TargetCells = new();
    private View m_View;
    private string m_CandidateId = "";
    private bool m_IsDrawing, m_CatalogReady, m_LastBusy;
    private int m_LastRemaining = -1, m_TargetVersion, m_ScreenVersion;

    private void Awake()
    {
        // 일반 배치: GachaPanel/GachaPrefab(이 컴포넌트). 공통 메뉴 컨테이너를 닫아 일시정지도 해제한다.
        if (m_MenuPanelRoot == null)
            m_MenuPanelRoot = transform.parent != null && transform.parent.name == "GachaPanel"
                ? transform.parent.gameObject : gameObject;
        BindButtons(true);
    }
    private void OnDestroy() => BindButtons(false);
    private void OnEnable()
    {
        m_ScreenVersion++;
        GlobalValue.OnGachaStateChanged += RefreshUI;
        ShowMain();
        m_CatalogReady = PopulateCatalog();
        RefreshUI();
    }
    private void OnDisable()
    {
        GlobalValue.OnGachaStateChanged -= RefreshUI;
        m_ScreenVersion++;
        ClearCandidate();
        m_CatalogReady = false;
    }
    private void Update()
    {
        if (!m_CatalogReady && PopulateCatalog())
        {
            m_CatalogReady = true;
            RefreshUI();
        }
        bool busy = m_IsDrawing || CharacterGachaMgr.IsBusy;
        if (m_LastRemaining != CharacterGachaMgr.GetRemainingGoldDraws() || m_LastBusy != busy) RefreshUI();
    }
    private void BindButtons(bool add)
    {
        Bind(m_CloseBtn, CloseClick, add);
        Bind(m_GoldDrawOneBtn, GoldOne, add); Bind(m_GoldDrawTenBtn, GoldTen, add);
        Bind(m_PityTargetSelectBtn, OpenTargets, add);
        Bind(m_PityTargetSelConfirmBtn, ConfirmTarget, add);
        Bind(m_PityTargetSelCancelBtn, CancelTargets, add);
        Bind(m_ResultConfirmBtn, ConfirmResults, add);
    }
    private static void Bind(Button button, UnityEngine.Events.UnityAction action, bool add)
    {
        if (button == null) return;
        if (add) button.onClick.AddListener(action); else button.onClick.RemoveListener(action);
    }
    private bool CanInput => !MessageBoxMgr.IsBlockingInput && !m_IsDrawing && !CharacterGachaMgr.IsBusy;
    private void GoldOne() => Draw(1);
    private void GoldTen() => Draw(10);

    private async void Draw(int count)
    {
        if (!CanInput || m_View != View.Main) return;
        if (m_CharacterGachaSection == null || m_ResultContent == null || m_GachaCharCellPrefab == null ||
            !m_GachaCharCellPrefab.HasRequiredGraphics || m_GachaResultPanel == null || m_ResultConfirmBtn == null ||
            m_ResultDescTxt == null || m_ResultStarDustTxt == null)
        { ItemSaleMgr.ShowMessage("공용 캐릭터 셀과 뽑기 결과 UI 참조를 연결해주세요."); return; }
        var master = FindFirstObjectByType<MasterDataMgr>();
        if (master == null) { ItemSaleMgr.ShowMessage("캐릭터 마스터가 준비되지 않았습니다."); return; }
        var catalog = master.GetCharacters();
        int screenVersion = m_ScreenVersion;
        m_IsDrawing = true;
        RefreshUI();
        try
        {
            var operation = await CharacterGachaMgr.DrawAsync(GachaCurrency.Gold, count);
            // 다른 메뉴로 이동했다가 돌아온 경우 이전 요청의 결과창을 뒤늦게 열지 않는다.
            if (this == null || !isActiveAndEnabled || screenVersion != m_ScreenVersion) return;
            if (!operation.Applied) { ItemSaleMgr.ShowMessage(operation.Message); return; }
            var summary = CharacterGachaDisplay.Summarize(operation.Results, catalog);
            var byId = catalog.ToDictionary(c => c.Id);
            ClearCells(m_ResultContent);
            foreach (var result in operation.Results)
                Instantiate(m_GachaCharCellPrefab, m_ResultContent).Initialize(byId[result.CharacterId], GachaCharCellMode.Result, result);
            SetText(m_ResultDescTxt, summary.Rarities);
            // TODO: 현재 별의 조각은 실제 지급 없이 로그만 출력한다. SO 지급 구현 후 실제 지급 합계와 연결한다.
            SetText(m_ResultStarDustTxt, summary.Fragments);
            SetView(View.Results);
            if (!operation.Saved) ItemSaleMgr.ShowMessage(operation.Message);
        }
        catch (Exception exception)
        {
            PDebug.LogError(exception);
            if (this != null && isActiveAndEnabled)
                ItemSaleMgr.ShowMessage("뽑기 결과 표시를 확인해주세요. 이미 적용된 결과는 재추첨하지 않습니다.");
        }
        finally
        {
            if (this != null)
            {
                m_IsDrawing = false;
                if (isActiveAndEnabled) RefreshUI();
            }
        }
    }

    private bool PopulateCatalog()
    {
        if (m_CharGachaListContent == null || m_GachaCharCellPrefab == null || !m_GachaCharCellPrefab.HasRequiredGraphics) return false;
        var master = FindFirstObjectByType<MasterDataMgr>();
        if (master == null) return false;
        var catalog = master.GetCharacters();
        if (catalog.Count == 0) return false;
        ClearCells(m_CharGachaListContent);
        foreach (var data in catalog.Where(c => c != null && !string.IsNullOrWhiteSpace(c.Id))
            .OrderByDescending(c => c.Rarity).ThenBy(c => c.Id, StringComparer.Ordinal))
            Instantiate(m_GachaCharCellPrefab, m_CharGachaListContent).Initialize(data, GachaCharCellMode.Catalog);
        return true;
    }

    private void RefreshUI()
    {
        m_LastBusy = m_IsDrawing || CharacterGachaMgr.IsBusy;
        foreach (var button in new[] { m_CloseBtn, m_GoldDrawOneBtn, m_GoldDrawTenBtn, m_PityTargetSelectBtn,
            m_PityTargetSelConfirmBtn, m_PityTargetSelCancelBtn, m_ResultConfirmBtn })
            if (button != null) button.interactable = !m_LastBusy;
        m_LastRemaining = CharacterGachaMgr.GetRemainingGoldDraws();
        SetText(m_GoldDrawRemainTxt, CharacterGachaDisplay.GoldRemaining(m_LastRemaining, GlobalConstants.CharacterGachaDailyGoldLimit));
        SetText(m_GoldDrawOneCostTxt, GlobalConstants.CharacterGachaGoldPrice.ToString("N0", CultureInfo.InvariantCulture) + " G");
        SetText(m_GoldDrawTenCostTxt, (GlobalConstants.CharacterGachaGoldPrice * 10).ToString("N0", CultureInfo.InvariantCulture) + " G");
        var master = FindFirstObjectByType<MasterDataMgr>();
        CharacterData target = null;
        if (master != null) master.TryGetCharacter(CharacterGachaMgr.SelectedTargetId, out target);
        if (m_PityTargetIconImg != null)
        {
            m_PityTargetIconImg.sprite = target != null ? target.Icon : null;
            m_PityTargetIconImg.enabled = target != null;
        }
        SetText(m_PityCountTxt, target != null
            ? CharacterGachaDisplay.PityRemaining(target.DisplayName, GlobalValue.GachaData.PityCount, GlobalConstants.CharacterGachaPityCount)
            : "확정 캐릭터를 선택해주세요.");
        RefreshCandidateUI();
    }

    private void OpenTargets()
    {
        if (!CanInput || m_View != View.Main) return;
        if (m_CharacterGachaSection == null || m_PityTargetSelectPanel == null || m_PityTargetContent == null ||
            m_GachaCharCellPrefab == null || !m_GachaCharCellPrefab.HasRequiredGraphics ||
            m_PityTargetSelConfirmBtn == null || m_PityTargetSelCancelBtn == null || m_SelCharNameTxt == null)
        { ItemSaleMgr.ShowMessage("천장 선택 UI와 공용 캐릭터 셀 참조를 연결해주세요."); return; }
        ClearCandidate();
        ClearCells(m_PityTargetContent);
        m_TargetCells.Clear();
        m_CandidateId = CharacterGachaMgr.SelectedTargetId;
        foreach (var data in CharacterGachaMgr.GetPityTargets().OrderBy(c => c.Id, StringComparer.Ordinal))
        {
            var cell = Instantiate(m_GachaCharCellPrefab, m_PityTargetContent);
            cell.Initialize(data, GachaCharCellMode.PityTarget, onSelected: SelectCandidate);
            m_TargetCells.Add(cell);
        }
        SetView(View.Targets);
        RefreshCandidateUI();
    }
    private void SelectCandidate(string id)
    {
        if (!CanInput || m_View != View.Targets || !m_TargetCells.Any(c => c != null && c.CharacterId == id)) return;
        m_CandidateId = id;
        RefreshCandidateUI(); // 메모리/서버의 확정 대상과 천장 횟수는 바꾸지 않는다.
    }
    private void RefreshCandidateUI()
    {
        if (m_View != View.Targets) return;
        var candidate = m_TargetCells.FirstOrDefault(c => c != null && c.CharacterId == m_CandidateId);
        foreach (var cell in m_TargetCells)
            if (cell != null) cell.SetSelected(cell == candidate);
        SetText(m_SelCharNameTxt, candidate != null ? $"{candidate.CharacterData.DisplayName}으로 변경하시겠어요?" : "변경할 캐릭터를 선택해주세요.");
        if (m_PityTargetSelConfirmBtn != null) m_PityTargetSelConfirmBtn.interactable = candidate != null && !m_LastBusy;
    }
    private void ConfirmTarget()
    {
        if (!CanInput || m_View != View.Targets) return;
        var candidate = m_TargetCells.FirstOrDefault(c => c != null && c.CharacterId == m_CandidateId);
        if (candidate == null) return;
        if (m_CandidateId == CharacterGachaMgr.SelectedTargetId) { ShowMain(); return; }
        string id = m_CandidateId;
        string expectedTarget = CharacterGachaMgr.SelectedTargetId;
        var expectedState = GlobalValue.GachaData;
        int oldCount = expectedState.PityCount;
        int version = m_TargetVersion;
        ItemSaleMgr.ShowDialog($"확정 획득 대상을 {candidate.CharacterData.DisplayName}으로 변경하시겠어요?\n천장 누적 횟수가 {oldCount}회에서 {oldCount / 2}회로 감소합니다.",
            MessageBoxType.Submit, () =>
            {
                if (this == null || !isActiveAndEnabled || m_View != View.Targets || version != m_TargetVersion) return;
                if (m_CandidateId != id || CharacterGachaMgr.SelectedTargetId != expectedTarget ||
                    !ReferenceEquals(GlobalValue.GachaData, expectedState) || GlobalValue.GachaData.PityCount != oldCount)
                { ItemSaleMgr.ShowMessage("천장 정보가 변경되었습니다. 다시 확인해주세요."); RefreshUI(); return; }
                if (!CharacterGachaMgr.TrySelectTarget(id, out string message)) ItemSaleMgr.ShowMessage(message);
                else { ShowMain(); RefreshUI(); }
            });
        // 메시지박스 취소 콜백은 등록하지 않는다. 후보와 변경창을 그대로 유지한다.
    }
    private void CancelTargets()
    {
        if (CanInput && m_View == View.Targets) ShowMain();
    }
    private void ConfirmResults()
    {
        if (CanInput && m_View == View.Results) ShowMain(); // 지급·차감·저장을 다시 수행하지 않는다.
    }
    private void ShowMain()
    {
        ClearCandidate();
        SetView(View.Main);
    }
    private void ClearCandidate()
    {
        m_TargetVersion++;
        m_CandidateId = "";
        foreach (var cell in m_TargetCells)
            if (cell != null) cell.SetSelected(false);
    }
    private void SetView(View view)
    {
        m_View = view;
        if (m_CharacterGachaSection != null) m_CharacterGachaSection.SetActive(view == View.Main);
        if (m_PityTargetSelectPanel != null) m_PityTargetSelectPanel.SetActive(view == View.Targets);
        if (m_GachaResultPanel != null) m_GachaResultPanel.SetActive(view == View.Results);
    }
    private void CloseClick()
    {
        if (!CanInput) return;
        ShowMain();
        (m_MenuPanelRoot != null ? m_MenuPanelRoot : gameObject).SetActive(false);
    }
    private static void SetText(Text text, string value)
    {
        if (text == null) return;
        text.supportRichText = true;
        text.text = value;
    }
    private static void ClearCells(Transform content)
    {
        foreach (Transform cell in content) { cell.gameObject.SetActive(false); Destroy(cell.gameObject); }
    }
}
