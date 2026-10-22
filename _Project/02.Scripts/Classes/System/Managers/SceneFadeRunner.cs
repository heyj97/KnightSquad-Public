using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>TitleScene의 독립된 최상위 오브젝트에 배치하는 영구 페이드 실행자.</summary>
public class SceneFadeRunner : MonoBehaviour
{
    public static SceneFadeRunner Instance { get; private set; }
    public bool IsTransitioning { get; private set; }
    [SerializeField] private CanvasGroup m_FadeGroup;
    [SerializeField, Min(0f)] private float m_FadeOutDuration = 0.3f;
    [SerializeField, Min(0f)] private float m_FadeInDuration = 0.3f;
    private EventSystem m_LockedEventSystem;
    private bool m_PreviousNavigation;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        if (transform.parent != null || m_FadeGroup == null)
        {
            Debug.LogError("SceneFadeRunner는 최상위 오브젝트에 배치하고 Fade Group을 연결해야 합니다.");
            enabled = false;
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        ClearFade();
    }

    public bool Run(IEnumerator transition, Action<bool> onCompleted)
    {
        if (!isActiveAndEnabled || IsTransitioning || transition == null || m_FadeGroup == null)
            return false;
        IsTransitioning = true;
        m_FadeGroup.blocksRaycasts = true;
        LockNavigation();
        StartCoroutine(Execute(transition, onCompleted));
        return true;
    }

    private void Update()
    {
        if (IsTransitioning) LockNavigation();
    }

    private void LockNavigation()
    {
        if (m_LockedEventSystem != EventSystem.current)
        {
            RestoreNavigation();
            m_LockedEventSystem = EventSystem.current;
            if (m_LockedEventSystem != null)
                m_PreviousNavigation = m_LockedEventSystem.sendNavigationEvents;
        }
        if (m_LockedEventSystem != null)
        {
            m_LockedEventSystem.sendNavigationEvents = false;
            m_LockedEventSystem.SetSelectedGameObject(null);
        }
    }

    private void RestoreNavigation()
    {
        if (m_LockedEventSystem != null)
            m_LockedEventSystem.sendNavigationEvents = m_PreviousNavigation;
        m_LockedEventSystem = null;
    }

    private IEnumerator Execute(IEnumerator transition, Action<bool> onCompleted)
    {
        bool succeeded = true;
        var stack = new Stack<IEnumerator>();
        try
        {
            yield return Fade(1f, m_FadeOutDuration);
            stack.Push(transition);
            // 중첩 IEnumerator의 예외도 잡아 화면과 입력 잠금을 복구한다.
            while (stack.Count > 0)
            {
                IEnumerator current = stack.Peek();
                object next = null;
                bool moved = false;
                try
                {
                    moved = current.MoveNext();
                    if (moved) next = current.Current;
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    succeeded = false;
                    break;
                }
                if (!moved)
                {
                    stack.Pop();
                    (current as IDisposable)?.Dispose();
                }
                else if (next is IEnumerator nested)
                    stack.Push(nested);
                else
                    yield return next;
            }
            // 새 씬의 Start와 첫 UI 갱신을 화면 복원 전에 수행한다.
            yield return null;
            yield return Fade(0f, m_FadeInDuration);
        }
        finally
        {
            while (stack.Count > 0)
            {
                try { (stack.Pop() as IDisposable)?.Dispose(); }
                catch (Exception exception) { Debug.LogException(exception); }
            }
            ClearFade();
        }
        onCompleted?.Invoke(succeeded);
    }

    private IEnumerator Fade(float target, float duration)
    {
        float start = m_FadeGroup.alpha;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            m_FadeGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        m_FadeGroup.alpha = target;
    }

    private void ClearFade()
    {
        if (m_FadeGroup != null)
        {
            m_FadeGroup.alpha = 0f;
            m_FadeGroup.blocksRaycasts = false;
        }
        RestoreNavigation();
        IsTransitioning = false;
    }

    private void OnDisable()
    {
        if (Instance != this) return;
        StopAllCoroutines();
        ClearFade();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
