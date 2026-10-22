using System;

/// <summary>씬 컴포넌트가 아닌 오프라인 보상 계산기. 지급과 저장은 PlayerDataMgr가 담당한다.</summary>
public static class OfflineRewardMgr
{
    public static ulong CalculateGold(long lastActiveUnixTime, long nowUnixTime, int chapter, int stage,
        out long elapsedSeconds, out int appliedSeconds)
    {
        elapsedSeconds = lastActiveUnixTime <= 0 || nowUnixTime <= lastActiveUnixTime
            ? 0 : nowUnixTime - lastActiveUnixTime;
        appliedSeconds = elapsedSeconds <= GlobalConstants.OfflineMinSeconds
            ? 0 : (int)Math.Min(elapsedSeconds, GlobalConstants.OfflineMaxSeconds);
        return (ulong)decimal.Floor(appliedSeconds * GlobalConstants.GetOfflineGoldPerMinute(chapter, stage) / 60m);
    }

    public static string CreateMessage(long elapsedSeconds, int appliedSeconds, ulong gold)
        => $"미접속 시간: {FormatTime(elapsedSeconds)}\n보상 적용 시간: {FormatTime(appliedSeconds)}\n획득 골드: {gold:N0} G";

    private static string FormatTime(long seconds)
        => $"{seconds / 3600:N0}시간 {seconds % 3600 / 60}분 {seconds % 60}초";
}
