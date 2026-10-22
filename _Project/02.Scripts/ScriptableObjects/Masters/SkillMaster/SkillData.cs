using UnityEngine;

/// <summary>
/// 스킬의 기본 데이터 Scriptable Object
/// </summary>
/// <remarks>
///
/// <para><b>Identity</b></para>
/// <list type="bullet">
/// <item><description><c>string Id</c> 시스템에서 사용하는 스킬 ID</description></item>
/// <item><description><c>string DisplayName</c> UI 또는 게임에서 표시하는 스킬 이름</description></item>
/// <item><description><c>string Description</c> UI 또는 게임에서 표시하는 스킬 설명</description></item>
/// </list>
///
/// <para><b>Resources</b></para>
/// <list type="bullet">
/// <item><description><c>Sprite Icon</c> 스킬 아이콘 이미지</description></item>
/// </list>
///
/// <para><b>Skill Settings</b></para>
/// <list type="bullet">
/// <item><description><c>SkillType Type</c> 스킬의 기능 타입(공격형/버프형)</description></item>
/// <item><description><c>SkillBuffStat BuffStat</c> 버프 스킬이 적용하는 스텟 종류</description></item>
/// <item><description><c>SkillTargetType TargetType</c> 스킬의 타겟 지정 방식(싱글/랜덤/글로벌)</description></item>
/// <item><description><c>float EffectMultiplier</c> 스킬 효과에 적용하는 계수 공격은 n.n, 버프는 0.n 으로 적용</description></item>
/// <item><description><c>float BuffTime</c> 스킬 효과가 지속되는 시간(버프용)</description></item>
/// <item><description><c>float Cooldown</c> 스킬의 재사용 대기시간</description></item>
/// </list>
///
/// </remarks>
[CreateAssetMenu(
    fileName = "SkillData",
    menuName = "Project:Mini/SkillData"
)]
public class SkillData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string skillId;
    [SerializeField] private string skillName;
    [TextArea, SerializeField] private string description;


    [Header("Resources")]
    [SerializeField] private Sprite skillIcon;
    [SerializeField] private GameObject skillEffectPrefab;


    [Header("Skill Settings")]
    [SerializeField] private SkillType skillType;


    [SerializeField] private SkillBuffStat skillBuffStat;

    [SerializeField] private SkillTargetType skillTargetType;


    [SerializeField, Min(0f)]
    private float effectMultiplier = 1f;

    [SerializeField]
    private float buffTime = 1f;
    
    [SerializeField, Min(0f)]
    private float cooldown = 1f;


    // Identity
    /// <summary>시스템에서 사용하는 스킬 ID</summary>
    public string Id => skillId;

    /// <summary>UI 또는 게임에서 표시하는 스킬 이름</summary>
    public string DisplayName => skillName;

    /// <summary>UI 또는 게임에서 표시하는 스킬 설명</summary>
    public string Description => description;


    // Resources
    /// <summary>스킬 아이콘 이미지</summary>
    public Sprite Icon => skillIcon;

    /// <summary>
    /// 스킬 사용 시 대상의 이펙트 지점에 생성할 프리팹을 가져옵니다.
    /// </summary>
    public GameObject EffectPrefab => skillEffectPrefab;


    // Skill Settings
    /// <summary>스킬의 기능 타입(공격형/버프형)</summary>
    public SkillType Type => skillType;

    /// <summary>버프 스킬이 적용하는 스텟 종류</summary>
    public SkillBuffStat BuffStat => skillBuffStat;

    /// <summary>스킬의 타겟 지정 방식(싱글/랜덤/글로벌)</summary>
    public SkillTargetType TargetType => skillTargetType;

    /// <summary>스킬 효과에 적용하는 계수 공격은 n.n, 버프는 0.n 으로 적용</summary>
    public float EffectMultiplier => effectMultiplier;

    /// <summary>스킬 효과가 지속되는 시간(버프용)</summary>
    public float BuffTime => buffTime;
    
    /// <summary>스킬의 재사용 대기시간</summary>
    public float Cooldown => cooldown;
}