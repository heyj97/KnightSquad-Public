using UnityEngine;

/// <summary>UI 팝업의 공통 재생 요청과 크기 곡선을 제공한다.</summary>
public static class GlobalUIMgr
{
    public static void PlayPopup(UIPopupEffect effect)
    {
        if (effect != null) effect.Play();
    }

    /// <summary>처음 70% 동안 확대하고, 나머지 시간에 원래 크기로 돌아온다.</summary>
    public static float EvaluatePopupScale(float progress, float startScale, float peakScale)
    {
        progress = Mathf.Clamp01(progress);
        return progress < 0.7f
            ? Mathf.Lerp(startScale, peakScale, Mathf.SmoothStep(0f, 1f, progress / 0.7f))
            : Mathf.Lerp(peakScale, 1f, Mathf.SmoothStep(0f, 1f, (progress - 0.7f) / 0.3f));
    }
}
