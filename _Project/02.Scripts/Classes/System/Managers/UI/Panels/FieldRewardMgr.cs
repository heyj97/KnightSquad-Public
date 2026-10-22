using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 챕터 종료 시 획득한 골드와 장비 보상을 정산 화면에 표시합니다.
/// </summary>
public class FieldRewardMgr : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private GameObject m_FieldRewardPrefab;
    [SerializeField] private Text m_ChapterText;
    [SerializeField] private Text m_EarnedGoldText;
    [SerializeField] private Transform m_EarnedItemSvContent;
    [SerializeField] private Button m_RetryCurChapterBtn;
    [SerializeField] private Button m_ToNextChapterBtn;
    [SerializeField] private ScrollRect m_EarnedItemScrollRect;
    
    [Header("Reward Item SV")]
    [SerializeField] private GameObject m_ItemSVPrefab;

    [Header("Reward Item Effect")]
    [SerializeField] private GameObject m_NormalDropEffect;
    [SerializeField] private GameObject m_EpicDropEffect;
    
    private StageMgr m_StageMgr;
    private bool m_StageMgrInitialized;

    void Awake()
    {
        m_StageMgr = FindFirstObjectByType<StageMgr>();
    }
    

    void Start()
    {
        if (m_RetryCurChapterBtn != null)
            m_RetryCurChapterBtn.onClick.AddListener(RetryCurrentChapter);

        if (m_ToNextChapterBtn != null)
            m_ToNextChapterBtn.onClick.AddListener(GoToNextChapter);
    }


    void Update()
    {
        if (m_StageMgrInitialized == false)
            TryBindStageMgr();
    }

    void OnEnable()
    {
        TryBindStageMgr();
    }

    void OnDisable()
    {
        if (m_StageMgr != null)
            m_StageMgr.OnChapterCleared -= ShowFieldReward;

        m_StageMgrInitialized = false;
    }

    void TryBindStageMgr()
    {
        if (m_StageMgrInitialized)
            return;

        if (m_StageMgr == null)
            m_StageMgr = FindFirstObjectByType<StageMgr>();

        if (m_StageMgr == null)
            return;

        m_StageMgr.OnChapterCleared += ShowFieldReward;
        m_StageMgrInitialized = true;
        if (m_StageMgr.IsChapterCleared) ShowFieldReward(m_StageMgr.CurChapter);
    }
    
    private void RetryCurrentChapter()
    {
        RequestChapterStart(false);
    }

    private void GoToNextChapter()
    {
        RequestChapterStart(true);
    }

    private void RequestChapterStart(bool nextChapter)
    {
        BattleFlowMgr battleFlow = FindFirstObjectByType<BattleFlowMgr>();

        if (battleFlow == null ||
            !battleFlow.TryContinueAfterChapterClear(nextChapter))
            return;

        // 요청 수락 후 보상 패널을 닫는다.
        // 이후 전투 준비 실패는 기존 GameOverPanelMgr가 처리한다.
        if (m_FieldRewardPrefab != null)
            m_FieldRewardPrefab.SetActive(false);
        StopAllCoroutines();
        ClearRewardCells();
    }

    private void OnDestroy()
    {
        if (m_RetryCurChapterBtn != null)
            m_RetryCurChapterBtn.onClick.RemoveListener(RetryCurrentChapter);

        if (m_ToNextChapterBtn != null)
            m_ToNextChapterBtn.onClick.RemoveListener(GoToNextChapter);
    }
    
    void ShowFieldReward(int chapter)
    {
        if (m_StageMgr.IsFinalStage && m_StageMgr.IsFinalSettlementComplete) return;
        StopAllCoroutines();
        if (m_ChapterText != null)
            m_ChapterText.text = m_StageMgr.IsFinalStage ? "게임 클리어" : $"{chapter}챕터 클리어";
        if (m_ToNextChapterBtn != null)
            m_ToNextChapterBtn.gameObject.SetActive(chapter < GlobalConstants.FinalFieldChapter);
        if (m_RetryCurChapterBtn != null)
        {
            var label = m_RetryCurChapterBtn.GetComponentInChildren<Text>(true);
            if (label != null) label.text = m_StageMgr.IsFinalStage ? "보스 반복 도전" : "현재 챕터 재도전";
        }

        if (m_EarnedGoldText != null)
            m_EarnedGoldText.text = $"{m_StageMgr.ChapterEarnedGold:N0} G";

        // 이전 보상 표시 정리
        ClearRewardCells();
        
        if (m_FieldRewardPrefab != null)
            m_FieldRewardPrefab.SetActive(true);
        
        EarnedItemSvAdd();
    }

    private void ClearRewardCells()
    {
        if (m_EarnedItemSvContent == null) return;
        foreach (Transform child in m_EarnedItemSvContent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }

    void EarnedItemSvAdd()
    {
        ItemData[] a_RewardList = m_StageMgr.ChapterEarnedItems.ToArray();
        StartCoroutine(PlayAddEquipItemEffect(a_RewardList));
        
    }

    IEnumerator PlayAddEquipItemEffect(ItemData[] equips)
    {
        foreach (ItemData equip in equips)
        {
            var master = FindFirstObjectByType<MasterDataMgr>();
            if (equip == null || master == null || !master.TryGetEquipment(equip.ItemId, out var rule) || rule == null) continue;
            GameObject a_Item = Instantiate(m_ItemSVPrefab, m_EarnedItemSvContent);
            a_Item.GetComponent<ItemCellMgr>().InitializeItem(equip);
            
            GameObject a_EffectPrefab = rule.Rarity > SystemRarity.Rare ? m_EpicDropEffect : m_NormalDropEffect;
            
            // 아이템 드랍 효과 표시
            Instantiate(a_EffectPrefab, a_Item.transform, false);
            // 아이템 드랍 효과 표시

            Canvas.ForceUpdateCanvases();

            if (m_EarnedItemSvContent is RectTransform contentRect)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            if (m_EarnedItemScrollRect != null)
                m_EarnedItemScrollRect.horizontalNormalizedPosition = 1f;

            yield return new WaitForSecondsRealtime(0.1f);
        }
    }
    
    
}
