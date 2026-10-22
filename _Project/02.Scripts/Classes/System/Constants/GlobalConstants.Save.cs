// 변경 감지 및 서버 저장 주기.
public static partial class GlobalConstants
{
    /// <summary>메모리 변경 영역을 확인하는 주기. 서버 요청은 발생하지 않습니다.</summary>
    public const float SaveChangeProbeIntervalSeconds = 3f;
    /// <summary>변경 데이터와 활동 시각을 서버에 저장하는 일반 주기입니다.</summary>
    public const float AutoSaveIntervalSeconds = 30f;
    /// <summary>연속된 명시적 저장 요청을 합치는 대기 시간입니다.</summary>
    public const float ExplicitSaveCoalesceSeconds = 0.25f;
}
