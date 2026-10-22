using System;
using System.Collections;

/// <summary>씬 이름이나 로드 방식에 종속되지 않는 공통 전환 호출 창구.</summary>
public static class SceneChangeMgr
{
    public static bool IsTransitioning => SceneFadeRunner.Instance != null && SceneFadeRunner.Instance.IsTransitioning;

    /// <summary>암전 후 작업을 실행하고 화면을 복원한다. 반환값은 요청 수락 여부다.</summary>
    public static bool SceneFade(IEnumerator transition, Action<bool> onCompleted = null)
    {
        if (SceneFadeRunner.Instance == null)
        {
            UnityEngine.Debug.LogError("TitleScene의 SceneFadeRoot에 SceneFadeRunner와 CanvasGroup을 연결해주세요.");
            return false;
        }
        return SceneFadeRunner.Instance.Run(transition, onCompleted);
    }
}
