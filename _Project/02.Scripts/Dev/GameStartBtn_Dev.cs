using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 개발 환경에서 테스트 데이터를 준비하고 게임 씬 구성을 로드합니다.
/// </summary>
public class GameStartBtn_Dev : MonoBehaviour
{ 
    Button m_LoginBtn;
    AudioListener m_AudioListener;
    EventSystem m_EventSystem;
    bool m_IsLoading = false;

    /// <summary>
    /// 개발 시작 데이터의 첫 번째 테스트 캐릭터 마스터입니다.
    /// </summary>
    [Header("Test Characters")]
    public CharacterData cdata1;
    /// <summary>
    /// 개발 시작 데이터의 두 번째 테스트 캐릭터 마스터입니다.
    /// </summary>
    public CharacterData cdata2;
    /// <summary>
    /// 개발 시작 데이터의 세 번째 테스트 캐릭터 마스터입니다.
    /// </summary>
    public CharacterData cdata3;
    /// <summary>
    /// 개발 시작 데이터의 네 번째 테스트 캐릭터 마스터입니다.
    /// </summary>
    public CharacterData cdata4;

    /// <summary>
    /// 개발 시작 시 보유 캐릭터와 초기 파티를 구성할 테스트 캐릭터 목록입니다.
    /// </summary>
    public CharacterData[] cDatas;

    /// <summary>
    /// 개발 시작 인벤토리에 추가할 첫 번째 테스트 아이템 마스터입니다.
    /// </summary>
    [Header("Test Items")]
    public ItemParentSO idata1;
    /// <summary>
    /// 개발 시작 인벤토리에 추가할 두 번째 테스트 아이템 마스터입니다.
    /// </summary>
    public ItemParentSO idata2;
    /// <summary>
    /// 개발 시작 인벤토리에 추가할 세 번째 테스트 아이템 마스터입니다.
    /// </summary>
    public ItemParentSO idata3;


    void Start()
    {
        m_LoginBtn = this.GetComponent<Button>();
        m_AudioListener = Camera.main != null ? Camera.main.GetComponent<AudioListener>() : null;
        m_EventSystem = GameObject.Find("EventSystem").GetComponent<EventSystem>();

        if (m_LoginBtn != null)
            m_LoginBtn.onClick.AddListener(MsgTest);
    }

    void MsgTest()
    {
        MessageBoxMgr.Inst.ShowMsg(
            "개발자 모드로 접속하시겠습니까?",
            "개발자 모드",
            MessageBoxType.Submit,
            StartGame,
            null
            );
    }

    void StartGame()
    {
        if (m_IsLoading)
            return;

        if (PlayerDataMgr.Inst.IsLoading || PlayerDataMgr.Inst.IsSaving || NetworkMgr.Inst.IsRequesting)
        {
            ItemSaleMgr.ShowMessage("서버 요청이 끝난 뒤 개발 모드를 시작해주세요.");
            return;
        }

        PlayerDataMgr.Inst.EnterDevelopmentMode();
        ApplyToMemory();
        StartCoroutine(EnterGame());
        return;
    }

    IEnumerator EnterGame()
    {
        m_IsLoading = true;
        m_LoginBtn.interactable = false;
        bool accepted = SceneChangeMgr.SceneFade(LoadGameScenes(), succeeded =>
        {
            if (this == null || succeeded) return;
            m_IsLoading = false;
            m_LoginBtn.interactable = true;
            PDebug.LogError("인게임 씬 전환에 실패했습니다. 씬 로드 상태를 확인해주세요.");
        });
        if (!accepted)
        {
            m_IsLoading = false;
            m_LoginBtn.interactable = true;
        }
        yield break;
    }

    // TitleScene을 해제해도 영구 페이드 실행자가 이 작업을 끝까지 실행한다.
    IEnumerator LoadGameScenes()
    {
        foreach (string sceneName in new[] { "UIScene", "MapScene", "GameScene" })
        {
            if (!Application.CanStreamedLevelBeLoaded(sceneName) || SceneManager.GetSceneByName(sceneName).isLoaded)
                throw new System.InvalidOperationException($"씬 등록 또는 중복 로드를 확인해주세요: {sceneName}");
        }

        Scene titleScene = gameObject.scene;
        
        
        m_EventSystem.enabled = false;
        m_AudioListener.enabled = false;
        
        // 1. UI부터 로드
        yield return SceneManager.LoadSceneAsync(
            "UIScene",
            LoadSceneMode.Additive
        );

        // 2. Map 로드
        yield return SceneManager.LoadSceneAsync(
            "MapScene",
            LoadSceneMode.Additive
        );

        // 3. 마지막에 게임 로직 씬 로드
        // GameScene의 Awake/Start 시점에는
        // UIScene과 MapScene이 이미 존재함
        yield return SceneManager.LoadSceneAsync(
            "GameScene",
            LoadSceneMode.Additive
        );

        Scene gameScene = SceneManager.GetSceneByName("GameScene");

        if (gameScene.IsValid() && gameScene.isLoaded)
        {
            SceneManager.SetActiveScene(gameScene);
        }

        // PartyMgr와 BattleFlowMgr의 Start 초기화가 끝날 프레임을 확보한다.
        yield return null;
        yield return null;
        BattleFlowMgr flow = FindFirstObjectByType<BattleFlowMgr>();
        if (flow == null)
            throw new System.InvalidOperationException("GameScene에 BattleFlowMgr가 없습니다.");
        float waitStarted = Time.realtimeSinceStartup;
        while (!flow.IsReadyForSceneReveal)
        {
            if (Time.realtimeSinceStartup - waitStarted > 10f)
                throw new System.TimeoutException("인게임 초기화가 10초 내에 완료되지 않았습니다.");
            yield return null;
        }
        if (flow.State == BattleFlowMgr.BattleState.Failed)
            throw new System.InvalidOperationException("인게임 전투 준비에 실패했습니다.");

        // 4. Title 제거
        yield return SceneManager.UnloadSceneAsync(titleScene);
    }

    private void ApplyToMemory()
    {
        List<CharData> charData = new();
        
        foreach (CharacterData cdata in cDatas)
            charData.Add(new CharData(cdata));

        List<string> partyIds = new()
        {
            cDatas[1].Id,
            cDatas[2].Id,
            cDatas[0].Id,
            cDatas[3].Id,
        };

        List<ItemData> itemData = new()
        {
            new ItemData(idata1),
            new ItemData(idata2),
            new ItemData(idata3),
        };

        GlobalValue.ClearVariable();
        GlobalValue.SetPlayFabId("3A2D1950336B178D");
        GlobalValue.SetUid("USR_d348d18a91544e69a378fc423ae7a832");
        GlobalValue.SetNickName("테스트");
        GlobalValue.SetUserGold(10000);
        GlobalValue.SetRealMoney(1000);
        GlobalValue.SetUserStarFragments(0);
        GlobalValue.SetAtkPowerRankScore(100);
        GlobalValue.SetDummyRankScore(1000);
        GlobalValue.SetPartyGrowthLevels(
            hp: 1, attack: 1, attackSpeed: 1,
            defense: 1, criticalChance: 1, criticalDamage: 1);
        GlobalValue.SetCurStageData(new StageSaveData(){CurChapter = 1, CurStage = 1});
        GlobalValue.SetUserCharDataList(charData);
        GlobalValue.SetPartyCharIdList(partyIds);
        GlobalValue.SetUserItemDataList(itemData);

        StageMgr.Inst.SetStageMgr(1, 1);
    }

    /// <summary>
    /// GUID를 사용해 유저 또는 아이템용 고유 UID를 생성한다.
    /// </summary>
    /// <param name="type">UID 앞에 붙일 종류별 접두사</param>
    /// <returns>USR_, ITEM_ 등의 접두사가 적용된 UID</returns>
    public string CreateUid(UidType type = UidType.None)
    {
        string prefix = type == UidType.User ? "USR_" :
            type == UidType.Item ? "ITEM_" : string.Empty;
        return $"{prefix}{Guid.NewGuid():N}";
    }
}
