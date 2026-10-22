// 공통 전투 기준. 기능별 설정은 같은 폴더의 partial 파일에서 관리합니다.
/// <summary>게임 전반에서 공유하는 밸런스 상수와 데이터 조회 규칙을 제공합니다.</summary>
public static partial class GlobalConstants
{
    // 콘텐츠 추가 시 마지막 챕터를 변경하고 해당 몬스터·드롭 데이터를 함께 등록한다.
    public const int FinalFieldChapter = 3;
    public const int FieldStagesPerChapter = 10;
    public const int FinalFieldStage = FieldStagesPerChapter;
    /// <summary>
    /// 캐릭터 방어력의 절대기준 이정도 수치는 꽤 높은 방어력이라는 뜻
    /// </summary>
    public const float CharDefenseConst = 100f;

    /// <summary>
    /// 몬스터 방어력의 절대기준 이정도 수치는 꽤 높은 방어력이라는 뜻
    /// </summary>
    public const float MonDefenseConst = 20f;

    // BalanceGuide 7-3: 필드 몬스터 챕터 성장 배율
    /// <summary>
    /// 챕터 증가에 따라 필드 몬스터 최대 체력에 누적 적용하는 성장 계수입니다.
    /// </summary>
    public const float FieldMonsterHpChapterFactor = 1.045f;
    /// <summary>
    /// 챕터 증가에 따라 필드 몬스터 공격력에 누적 적용하는 성장 계수입니다.
    /// </summary>
    public const float FieldMonsterAttackChapterFactor = 1.025f;
    /// <summary>
    /// 챕터 증가에 따라 필드 몬스터 방어력에 누적 적용하는 성장 계수입니다.
    /// </summary>
    public const float FieldMonsterDefenseChapterFactor = 1.015f;
}
