using System;
using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 메시지 상자의 표시 형태와 제공할 응답 버튼 구성을 구분합니다.
/// </summary>
public enum MessageBoxType {
    /// <summary>
    /// 사용자 응답 버튼 없이 진행 상태를 표시합니다.
    /// </summary>
    Loading,
    /// <summary>
    /// 확인 버튼만 제공하는 일반 안내 메시지입니다.
    /// </summary>
    Info,
    /// <summary>
    /// 확인 버튼만 제공하는 오류 안내 메시지입니다.
    /// </summary>
    Error,
    /// <summary>
    /// 확인과 취소 버튼으로 작업 승인을 요청합니다.
    /// </summary>
    Submit,
    /// <summary>
    /// 확인과 취소 버튼으로 주의가 필요한 작업을 요청합니다.
    /// </summary>
    Warning
}

/// <summary>
/// 안내·확인·로딩 메시지를 표시하고 키보드 응답 및 등록된 콜백의 실행 수명을 관리합니다.
/// </summary>
public class MessageBoxMgr : G_Singleton<MessageBoxMgr>
{
    
    /// <summary>
    /// 메시지 상자에 사용하는 게임 오브젝트 참조입니다.
    /// </summary>
    [Header("MessageBox root")]
    public GameObject m_MessageBox;
    [SerializeField] private UIPopupEffect m_PopupEffect;

    /// <summary>
    /// 현재 메시지 상자의 제목을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    [Header("MessageBox Text")]
    public Text m_TitleTxt;
    /// <summary>
    /// 현재 메시지 상자의 본문을 표시하는 텍스트 UI 참조입니다.
    /// </summary>
    public Text m_MsgDescTxt;
    
    /// <summary>
    /// 확인 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    [Header("MessageBox Buttons")]
    public Button m_SubmitBtn;
    /// <summary>
    /// 취소 동작을 요청하는 버튼 UI 참조입니다.
    /// </summary>
    public Button m_CancelBtn;

    private Action m_OnSubmit;
    private Action m_OnCancel;
    private Coroutine m_LoadingMsgCoroutine;
    private int m_MessageOpenedFrame = -1;
    private EventSystem m_KeyboardEventSystem;
    private bool m_PreviousNavigationEvents;
    private static MessageBoxMgr s_ActiveMessageBox;
    private static int s_LastResponseFrame = -1;

    private bool IsMessageBoxOpen => m_MessageBox != null && m_MessageBox.activeInHierarchy;

    // 닫힌 프레임까지 유지하여 다른 Update가 같은 ESC를 배치 취소로 처리하지 않게 한다.
    /// <summary>
    /// 메시지 상자가 열려 있거나 응답 직후 프레임이라 다른 UI가 입력을 처리하면 안 되는지 나타냅니다.
    /// </summary>
    public static bool IsBlockingInput =>
        (s_ActiveMessageBox != null && s_ActiveMessageBox.IsMessageBoxOpen) ||
        s_LastResponseFrame == Time.frameCount;
    
    protected override void Awake()
    {
        base.Awake();

        // UI 참조가 없는 상태라면 자동 생성된 싱글톤일 가능성이 큼
        if (m_MessageBox == null)
        {
            PDebug.LogError(
                "MessageBoxMgr의 UI가 연결되지 않았습니다. " +
                "MessageBoxMgr를 시작 Scene에 미리 배치해주세요.",
                this
            );
        }
    }
    

    void Start()
    {
        if (m_SubmitBtn != null)
            m_SubmitBtn.onClick.AddListener(OnSubmitClick);
        
        if (m_CancelBtn != null)
            m_CancelBtn.onClick.AddListener(OnCancelClick);
        
        if (m_MessageBox != null)
            m_MessageBox.SetActive(false);
    }
    
    void LateUpdate()
    {
        if (SceneChangeMgr.IsTransitioning) return;
        if (!IsMessageBoxOpen)
        {
            RestoreMessageKeyboardControl();
            return;
        }

        // Additive 씬 전환으로 현재 EventSystem이 바뀌어도 키 입력을 전담한다.
        if (m_KeyboardEventSystem != EventSystem.current)
            BeginMessageKeyboardControl();

        if (m_MessageOpenedFrame == Time.frameCount || s_LastResponseFrame == Time.frameCount)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CanClickMessageButton(m_CancelBtn))
                m_CancelBtn.onClick.Invoke();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (CanClickMessageButton(m_SubmitBtn))
                m_SubmitBtn.onClick.Invoke();
        }
    }

    private bool CanClickMessageButton(Button button)
    {
        return isActiveAndEnabled && IsMessageBoxOpen &&
               s_LastResponseFrame != Time.frameCount &&
               button != null && button.isActiveAndEnabled && button.IsInteractable();
    }

    private void BeginMessageKeyboardControl()
    {
        m_MessageOpenedFrame = Time.frameCount;
        EventSystem current = EventSystem.current;
        if (m_KeyboardEventSystem != current)
        {
            RestoreMessageKeyboardControl();
            m_KeyboardEventSystem = current;
            if (current != null)
                m_PreviousNavigationEvents = current.sendNavigationEvents;
        }

        s_ActiveMessageBox = this;
        if (m_KeyboardEventSystem != null)
        {
            m_KeyboardEventSystem.sendNavigationEvents = false;
            m_KeyboardEventSystem.SetSelectedGameObject(null);
        }
    }

    private void RestoreMessageKeyboardControl()
    {
        if (m_KeyboardEventSystem != null)
        {
            m_KeyboardEventSystem.SetSelectedGameObject(null);
            m_KeyboardEventSystem.sendNavigationEvents = m_PreviousNavigationEvents;
        }
        m_KeyboardEventSystem = null;
        if (s_ActiveMessageBox == this)
        {
            s_ActiveMessageBox = null;
            s_LastResponseFrame = Time.frameCount;
        }
    }

    private void StopLoadingMessage()
    {
        if (m_LoadingMsgCoroutine == null)
            return;
        StopCoroutine(m_LoadingMsgCoroutine);
        m_LoadingMsgCoroutine = null;
    }

    void OnDisable()
    {
        StopLoadingMessage();
        RestoreMessageKeyboardControl();
    }

    /// <summary>
    /// 메시지 종류에 맞는 버튼과 콜백을 구성하여 사용자 응답을 받는 메시지 상자를 표시합니다.
    /// </summary>
    /// <param name="msg">사용자에게 표시할 메시지 본문입니다.</param>
    /// <param name="title">메시지 상자에 표시할 제목입니다.</param>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <param name="onSubmit">사용자가 확인했을 때 한 번 실행할 콜백입니다.</param>
    /// <param name="onCancel">사용자가 취소했을 때 한 번 실행할 콜백입니다.</param>
    public void ShowMsg(string msg, string title = "알림", MessageBoxType type = MessageBoxType.Info, Action onSubmit = null, Action onCancel = null)
    {
        if (type == MessageBoxType.Loading)
        {
            PDebug.LogError("로딩 메세지는 ShowLoadingMsg(), CloseLoadingMsg() 메서드를 사용해주세요.");
            return;
        }
        
        if (m_MessageBox == null || m_TitleTxt == null || m_MsgDescTxt == null || m_SubmitBtn == null ||
            m_CancelBtn == null)
        {
            PDebug.LogError("MessageBoxMgr is not initialized");
            return;
        }
        
        StopLoadingMessage();
        m_TitleTxt.text = title;
        m_MsgDescTxt.text = msg;
        
        m_SubmitBtn.gameObject.SetActive(true);
        m_CancelBtn.gameObject.SetActive(type >= MessageBoxType.Submit);
        
        m_OnSubmit = onSubmit;
        m_OnCancel = onCancel;
        
        BeginMessageKeyboardControl();
        m_MessageBox.SetActive(true);
        GlobalUIMgr.PlayPopup(m_PopupEffect);
    }

    /// <summary>
    /// 현재 메시지의 확인 응답을 한 번 처리하고 등록된 확인 콜백을 실행합니다.
    /// </summary>
    public void OnSubmitClick()
    {
        if (!CanClickMessageButton(m_SubmitBtn))
            return;
        s_LastResponseFrame = Time.frameCount;
        Action a_Callback = m_OnSubmit;
        
        ClearCallback();
        m_MessageBox.SetActive(false);
        ClearTextUI();
        RestoreMessageKeyboardControl();
        
        // 콜백함수 안에서 메세지박스를 다시 실행했을 때를 대비하여 메세지박스를 끄고나서 콜백을 실행
        a_Callback?.Invoke();
    }
    
    /// <summary>
    /// 현재 메시지의 취소 응답을 한 번 처리하고 등록된 취소 콜백을 실행합니다.
    /// </summary>
    public void OnCancelClick()
    {
        if (!CanClickMessageButton(m_CancelBtn))
            return;
        s_LastResponseFrame = Time.frameCount;
        Action a_Callback = m_OnCancel;
        
        ClearCallback();
        m_MessageBox.SetActive(false);
        ClearTextUI();
        RestoreMessageKeyboardControl();
        
        // 콜백함수 안에서 메세지박스를 다시 실행했을 때를 대비하여 메세지박스를 끄고나서 콜백을 실행
        a_Callback?.Invoke();
    }

    /// <summary>
    /// 현재 메시지 상자의 콜백과 표시 텍스트를 초기화합니다.
    /// </summary>
    public void ClearCallback()
    {
        m_OnSubmit = null;
        m_OnCancel = null;
    }

    /// <summary>지정한 확인 콜백의 메시지만 콜백 실행 없이 닫는다.</summary>
    public bool TryCloseMessage(Action expectedSubmit)
    {
        if (expectedSubmit == null || m_OnSubmit != expectedSubmit || !IsMessageBoxOpen)
            return false;

        s_LastResponseFrame = Time.frameCount;
        StopLoadingMessage();
        ClearCallback();
        ClearTextUI();
        m_MessageBox.SetActive(false);
        RestoreMessageKeyboardControl();
        return true;
    }

    private void ClearTextUI()
    {
        m_TitleTxt.text = "";
        m_MsgDescTxt.text = "";
    }

    /// <summary>
    /// 키보드로 닫을 수 없는 로딩 메시지와 진행 애니메이션을 표시합니다.
    /// </summary>
    /// <param name="msg">사용자에게 표시할 메시지 본문입니다.</param>
    /// <param name="title">메시지 상자에 표시할 제목입니다.</param>
    public void ShowLoadingMsg(string msg, string title = "로딩 중")
    {
        if (m_MessageBox == null || m_TitleTxt == null || m_MsgDescTxt == null ||
            m_SubmitBtn == null || m_CancelBtn == null)
        {
            PDebug.LogError("MessageBoxMgr is not initialized");
            return;
        }
        m_TitleTxt.text = title;

        StopLoadingMessage();
        ClearCallback();

        m_SubmitBtn.gameObject.SetActive(false);
        m_CancelBtn.gameObject.SetActive(false);

        BeginMessageKeyboardControl();
        m_MessageBox.SetActive(true);

        m_LoadingMsgCoroutine =
            StartCoroutine(LoadingMsgCoroutine(msg));
        GlobalUIMgr.PlayPopup(m_PopupEffect);
    }

    /// <summary>
    /// 진행 중인 로딩 메시지 애니메이션을 중지하고 메시지 상자를 닫습니다.
    /// </summary>
    public void CloseLoadingMsg()
    {
        StopLoadingMessage();

        ClearTextUI();
        m_MessageBox.SetActive(false);
        RestoreMessageKeyboardControl();
    }

    private IEnumerator LoadingMsgCoroutine(string msg)
    {
        int a_PointCount = 1;

        while (true)
        {
            // 로딩 중. → 로딩 중.. → 로딩 중...
            m_MsgDescTxt.text =
                $"{msg}{new string('.', a_PointCount)}";

            a_PointCount++;

            // 점 3개 다음에는 다시 1개
            if (a_PointCount > 3)
                a_PointCount = 1;

            // Time.timeScale과 관계없이 UI 애니메이션 실행
            yield return new WaitForSecondsRealtime(0.5f);
            
        }
    }
}
