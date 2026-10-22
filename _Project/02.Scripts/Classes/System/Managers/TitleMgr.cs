using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면의 로그인과 회원가입 UI 흐름을 관리합니다.
/// </summary>
public class TitleMgr : MonoBehaviour
{
    /// <summary>
    /// 로그인과 회원가입 패널 전환 시 크기를 조절할 배경 RectTransform입니다.
    /// </summary>
    public RectTransform BackgroundTR;
    [Header("Start screen")]
    [SerializeField] private Text m_InfoTxt;
    [SerializeField] private GameObject m_UserLoginPanel;
    [SerializeField] private Button m_QuitULPanelBtn;
    [SerializeField, Min(0.1f)] private float m_InfoBlinkPeriod = 1.2f;
    private bool m_WaitingForStart;
    private int m_StartScreenFrame;
    private int m_OpenLoginFrame = -1;
    private float m_BlinkStarted;
    
    /// <summary>
    /// 로그인 패널에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    [Header("Login Panel")]
    public GameObject m_LoginPanel;
    /// <summary>
    /// 로그인에 사용할 계정 ID를 입력받는 UI 참조입니다.
    /// </summary>
    public InputField m_IdIfd;
    /// <summary>
    /// 로그인에 사용할 비밀번호를 입력받는 UI 참조입니다.
    /// </summary>
    public InputField m_PwIfd;
    /// <summary>
    /// 로그인 성공 후 계정 ID를 로컬에 기억할지 선택하는 토글입니다.
    /// </summary>
    public Toggle m_IdSvToggle;
    /// <summary>
    /// 로그인 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_LoginBtn;
    /// <summary>
    /// 로그인 패널에서 회원가입 패널로 전환하는 버튼입니다.
    /// </summary>
    public Button m_ToRegisterBtn;
    
    /// <summary>
    /// 회원가입 패널에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    [Header("Register Panel")]
    public GameObject m_RegisterPanel;
    /// <summary>
    /// 새로 등록할 계정 ID를 입력받는 UI 참조입니다.
    /// </summary>
    public InputField new_IdIfd;
    /// <summary>
    /// 새 계정의 비밀번호를 입력받는 UI 참조입니다.
    /// </summary>
    public InputField new_PwIfd;
    /// <summary>
    /// 새 계정 비밀번호를 한 번 더 확인하는 입력 UI 참조입니다.
    /// </summary>
    public InputField new_PwCheckIfd;
    /// <summary>
    /// 새 계정에 사용할 게임 닉네임을 입력받는 UI 참조입니다.
    /// </summary>
    public InputField new_Nickname;
    /// <summary>
    /// 회원가입 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_RegisterBtn;
    /// <summary>
    /// 회원가입 취소 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_RegisterCancelBtn;
    
    /// <summary>
    /// 타이틀 씬을 내릴 때 비활성화하여 중복 오디오 리스너를 방지할 참조입니다.
    /// </summary>
    [Header("Async Sceneload Error Prevent")]
    public AudioListener m_AudioListener;
    /// <summary>
    /// Additive 씬 전환 중 입력을 잠시 비활성화할 EventSystem 참조입니다.
    /// </summary>
    public EventSystem m_EventSystem;
    

    private bool m_IsLoading;

    

    void Start()
    {
        if (m_InfoTxt == null || m_UserLoginPanel == null || m_QuitULPanelBtn == null)
        {
            PDebug.LogError("TitleMgr의 Info Txt, User Login Panel, Quit UL Panel Btn을 연결해주세요.");
            enabled = false;
            return;
        }
        if (m_LoginBtn != null)
            m_LoginBtn.onClick.AddListener(LoginClick);
        if (m_ToRegisterBtn != null)
            m_ToRegisterBtn.onClick.AddListener(ToRegisterClick);
        if (m_RegisterBtn != null)
            m_RegisterBtn.onClick.AddListener(RegisterClick);
        if (m_RegisterCancelBtn != null)
            m_RegisterCancelBtn.onClick.AddListener(RegisterCancelClick);
        m_QuitULPanelBtn.onClick.AddListener(CloseUserLoginPanel);
        
        m_IdIfd.text = PlayerPrefs.GetString("user_Id", null);
        
        m_InfoTxt.text = "Press any key to start..";
        SetLoginView(false);
        
        m_IdIfd.onValidateInput = ValidateAccountCharacter;
        m_PwIfd.onValidateInput = ValidateAccountCharacter;

        new_IdIfd.onValidateInput = ValidateAccountCharacter;
        new_PwIfd.onValidateInput = ValidateAccountCharacter;
        new_PwCheckIfd.onValidateInput = ValidateAccountCharacter;
    }

    private char ValidateAccountCharacter(
        string text, int charIndex, char addedChar)
    {
        return addedChar >= '!' && addedChar <= '~'
            ? addedChar
            : '\0';
    }
    
    private bool IsTitleBusy => m_IsLoading || SceneChangeMgr.IsTransitioning ||
        NetworkMgr.Inst.IsRequesting || PlayerDataMgr.Inst.IsLoading || PlayerDataMgr.Inst.IsSaving;

    private void Update()
    {
        bool busy = IsTitleBusy;
        if (m_LoginBtn != null) m_LoginBtn.interactable = !busy;
        if (m_ToRegisterBtn != null) m_ToRegisterBtn.interactable = !busy;
        if (m_RegisterBtn != null) m_RegisterBtn.interactable = !busy;
        if (m_RegisterCancelBtn != null) m_RegisterCancelBtn.interactable = !busy;
        if (m_QuitULPanelBtn != null) m_QuitULPanelBtn.interactable = !busy;

        if (!m_WaitingForStart) return;
        Color color = m_InfoTxt.color;
        float phase = (Time.unscaledTime - m_BlinkStarted) / Mathf.Max(0.1f, m_InfoBlinkPeriod);
        color.a = 0.6f + 0.4f * Mathf.Cos(phase * Mathf.PI * 2f);
        m_InfoTxt.color = color;

        if (busy || MessageBoxMgr.IsBlockingInput) return;
        if (m_OpenLoginFrame >= 0)
        {
            if (Time.frameCount > m_OpenLoginFrame) SetLoginView(true);
            return;
        }
        if (Time.frameCount > m_StartScreenFrame && Input.anyKeyDown)
            m_OpenLoginFrame = Time.frameCount;
    }

    // 부모 패널과 안내는 이 메서드에서 함께 전환한다.
    private void SetLoginView(bool showLogin)
    {
        m_OpenLoginFrame = -1;
        m_WaitingForStart = !showLogin;
        m_StartScreenFrame = Time.frameCount;
        m_BlinkStarted = Time.unscaledTime;
        if (m_EventSystem != null) m_EventSystem.SetSelectedGameObject(null);
        BackgroundTR.sizeDelta = new Vector2(375f, 300f);
        m_LoginPanel.SetActive(true);
        m_RegisterPanel.SetActive(false);
        m_InfoTxt.gameObject.SetActive(!showLogin);
        m_UserLoginPanel.SetActive(showLogin);
        Color color = m_InfoTxt.color;
        color.a = 1f;
        m_InfoTxt.color = color;
    }

    private void CloseUserLoginPanel()
    {
        if (IsTitleBusy || MessageBoxMgr.IsBlockingInput) return;
        m_PwIfd.text = "";
        new_IdIfd.text = "";
        new_PwIfd.text = "";
        new_PwCheckIfd.text = "";
        new_Nickname.text = "";
        SetLoginView(false);
    }

    void LoginClick()
    {
        if (IsTitleBusy || m_WaitingForStart ||
            !m_UserLoginPanel.activeInHierarchy)
            return;

        MessageBoxMgr.Inst.ShowLoadingMsg(
            "서버 응답을 기다리는 중",
            "로그인 중"
        );

        NetworkMgr.Inst.Login(m_IdIfd.text, m_PwIfd.text, (success, message) =>
        {
            // 결과 메시지를 표시하거나 씬 전환하기 전에 로딩창 정리
            MessageBoxMgr.Inst.CloseLoadingMsg();

            if (this == null) return;

            if (!success)
            {
                ItemSaleMgr.ShowMessage(message);
                return;
            }

            if (m_IdSvToggle.isOn)
                PlayerPrefs.SetString("user_Id", m_IdIfd.text);
            else
                PlayerPrefs.DeleteKey("user_Id");

            StartCoroutine(EnterGame());
        });
    }

    void ToRegisterClick()
    {
        if (IsTitleBusy || m_WaitingForStart) return;
        BackgroundTR.sizeDelta = new Vector2(410f, 430f);
        
        new_IdIfd.text = "";
        new_PwIfd.text = "";
        new_PwCheckIfd.text = "";
        new_Nickname.text = "";
        
        m_RegisterPanel.SetActive(true);
        m_LoginPanel.SetActive(false);
    }

    void RegisterClick()
    {
        if (IsTitleBusy || m_WaitingForStart || !m_UserLoginPanel.activeInHierarchy) return;
        m_IdIfd.text = "";
        m_PwIfd.text = "";
        
        if (m_RegisterPanel.activeSelf && new_PwIfd.text != new_PwCheckIfd.text)
        {
            PDebug.Log("패스워드가 동일하지 않습니다.");
            return;
        }
        
        NetworkMgr.Inst.Register(
            new_IdIfd.text,
            new_PwIfd.text,
            new_Nickname.text,
            (success, message) =>
            {
                if (this == null) return;
                PDebug.Log(message);

                if (!success)
                {
                    ItemSaleMgr.ShowMessage(message);
                    return;
                }

                // 회원가입과 신규 데이터 생성 완료
                
                // 로그인창으로 이동
                m_IdIfd.text = new_IdIfd.text;
                m_PwIfd.text = new_PwIfd.text;
                m_LoginPanel.SetActive(true);
                m_RegisterPanel.SetActive(false);
                BackgroundTR.sizeDelta = new Vector2(410f, 430f);
                
                // 회원가입창 인풋필드 초기화
                new_IdIfd.text = "";
                new_PwIfd.text = "";
                new_PwCheckIfd.text = "";
                new_Nickname.text = "";
            });
    }

    void RegisterCancelClick()
    {
        if (IsTitleBusy || m_WaitingForStart) return;
        BackgroundTR.sizeDelta = new Vector2(375f, 300f);

        new_IdIfd.text = "";
        new_PwIfd.text = "";
        new_PwCheckIfd.text = "";
        new_Nickname.text = "";
        
        m_LoginPanel.SetActive(true);
        m_RegisterPanel.SetActive(false);
    }
    
    private void OnDestroy()
    {
        if (m_LoginBtn != null) m_LoginBtn.onClick.RemoveListener(LoginClick);
        if (m_ToRegisterBtn != null) m_ToRegisterBtn.onClick.RemoveListener(ToRegisterClick);
        if (m_RegisterBtn != null) m_RegisterBtn.onClick.RemoveListener(RegisterClick);
        if (m_RegisterCancelBtn != null) m_RegisterCancelBtn.onClick.RemoveListener(RegisterCancelClick);
        if (m_QuitULPanelBtn != null) m_QuitULPanelBtn.onClick.RemoveListener(CloseUserLoginPanel);
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
}
