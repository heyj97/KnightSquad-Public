using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;

/// <summary>
/// 캐릭터의 기본 데이터 Scriptable Object
/// </summary>
/// <remarks>
///
/// <para><b>Identity</b></para>
/// <list type="bullet">
/// <item><description><c>string Id</c> 시스템에서 사용하는 캐릭터 ID</description></item>
/// <item><description><c>string DisplayName</c> UI 또는 게임에서 표시하는 캐릭터 이름</description></item>
/// <item><description><c>CharacterRarity Rarity</c> 캐릭터의 희귀도</description></item>
/// <item><description><c>CharacterRole Role</c> 캐릭터의 역할군</description></item>
/// </list>
///
/// <para><b>Resources</b></para>
/// <list type="bullet">
/// <item><description><c>Sprite Illustration</c> 캐릭터 일러스트</description></item>
/// <item><description><c>Sprite Icon</c> 캐릭터 아이콘</description></item>
/// <item><description><c>GameObject Prefab</c> 캐릭터 프리팹</description></item>
/// </list>
///
/// <para><b>Base Stats</b></para>
/// <list type="bullet">
/// <item><description><c>float BaseMaxHp</c> 캐릭터의 기본 최대 체력</description></item>
/// <item><description><c>float BaseAttack</c> 캐릭터의 기본 공격력</description></item>
/// <item><description><c>float BaseAttackSpeed</c> 캐릭터의 기본 공격속도</description></item>
/// <item><description><c>float BaseDefense</c> 캐릭터의 기본 방어력</description></item>
/// <item><description><c>float BaseCriticalChance</c> 캐릭터의 기본 치명타 확률</description></item>
/// <item><description><c>float BaseCriticalDamageMultiplier</c> 캐릭터의 기본 치명타 데미지 배율</description></item>
/// </list>
///
/// </remarks>
[CreateAssetMenu(
    fileName = "CharacterData",
    menuName = "Project:Mini/CharacterData"
)]

public class CharacterData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string characterId;
    [SerializeField] private string characterName;
    [SerializeField] private SystemRarity rarity;
    [SerializeField] private CharacterRole role;


    [Header("Resources")]
    [SerializeField] private Sprite characterIllust;
    [SerializeField] private Sprite characterIcon;
    [SerializeField] private GameObject characterPrefab;

    [Header("Base Stats")]
    [SerializeField, Min(1.0f)] private float baseMaxHp = 100f;
    
    [FormerlySerializedAs("baseAttack")] [SerializeField, Min(1.0f)] private float baseAttackPower = 10f;
    [SerializeField, Min(0.01f)] private float baseAttackSpeed = 1f;

    [SerializeField, Min(1.0f)] private float baseDefense = 10f;

    [SerializeField, Range(0f, 1f)] private float baseCriticalChance = 0f;
    [SerializeField, Range(1f, 3f)] private float baseCriticalDamageMultiplier = 1.5f;

    [FormerlySerializedAs("skills")]
    [Header("Skills")]
    [SerializeField] private List<SkillData> skillsList = new();
    /// <summary>
    /// 이 캐릭터가 보유하도록 설정된 스킬 마스터 목록을 가져옵니다.
    /// </summary>
    public IReadOnlyList<SkillData> SkillsList => skillsList;
    
    // [Header("Equipped Items")]
    // private


    // Identity
    /// <summary>시스템에서 사용하는 캐릭터 ID</summary>
    public string Id => characterId;

    /// <summary>UI 또는 게임에서 표시하는 캐릭터 이름</summary>
    public string DisplayName => characterName;

    /// <summary>캐릭터의 희귀도</summary>
    public SystemRarity Rarity => rarity;

    /// <summary>캐릭터의 역할군</summary>
    public CharacterRole Role => role;

    // Resources
    /// <summary>캐릭터 일러스트</summary>
    public Sprite Illustration => characterIllust;

    /// <summary>캐릭터 아이콘</summary>
    public Sprite Icon => characterIcon;

    /// <summary>캐릭터 프리팹</summary>
    public GameObject Prefab => characterPrefab;

    // Base Stats
    /// <summary>캐릭터의 기본 최대 체력</summary>
    public float BaseMaxHp => baseMaxHp;

    /// <summary>캐릭터의 기본 공격력</summary>
    public float BaseAttackPower => baseAttackPower;

    /// <summary>캐릭터의 기본 공격속도</summary>
    public float BaseAttackSpeed => baseAttackSpeed;

    /// <summary>캐릭터의 기본 방어력</summary>
    public float BaseDefense => baseDefense;

    /// <summary>캐릭터의 기본 치명타 확률</summary>
    public float BaseCriticalChance => baseCriticalChance;

    /// <summary>캐릭터의 기본 치명타 데미지 배율</summary>
    public float BaseCriticalDamageMultiplier => baseCriticalDamageMultiplier;

    /// <summary>
    /// 이 마스터 데이터의 프리팹으로 캐릭터 모델을 생성하고 SPUM 구성 요소를 반환합니다.
    /// </summary>
    /// <param name="charId">생성할 캐릭터를 식별하는 마스터 ID입니다.</param>
    /// <param name="charDisplayName">게임 UI에 표시할 캐릭터 이름입니다.</param>
    /// <param name="charRarity">캐릭터의 성장 한도와 UI 표현에 사용할 등급입니다.</param>
    /// <param name="charRole">캐릭터의 전투 역할과 UI 분류입니다.</param>
    /// <param name="charIllust">도감 상세 화면에 표시할 캐릭터 일러스트입니다.</param>
    /// <param name="charIcon">목록과 파티 UI에 표시할 캐릭터 아이콘입니다.</param>
    /// <param name="charPrefab">런타임 캐릭터 모델을 생성할 SPUM 프리팹입니다.</param>
    /// <param name="charBaseMaxHp">장비와 성장 보정 전의 기본 최대 체력입니다.</param>
    /// <param name="charBaseAttackPower">장비와 성장 보정 전의 기본 공격력입니다.</param>
    /// <param name="charBaseAttackSpeed">장비와 성장 보정 전의 기본 공격속도입니다.</param>
    /// <param name="charBaseDefense">장비와 성장 보정 전의 기본 방어력입니다.</param>
    /// <param name="charBaseCriticalChance">장비와 성장 보정 전의 기본 치명타 확률입니다.</param>
    /// <param name="charBaseCriticalDamageMultiplier">장비와 성장 보정 전의 기본 치명타 피해 배율입니다.</param>
    /// <param name="charSkillList">캐릭터가 보유할 스킬 마스터 목록입니다.</param>
    public void CharMaker(
        string charId,
        string charDisplayName,
        SystemRarity charRarity,
        CharacterRole charRole,
        Sprite charIllust,
        Sprite charIcon,
        GameObject charPrefab,
        float charBaseMaxHp,
        float charBaseAttackPower,
        float charBaseAttackSpeed,
        float charBaseDefense,
        float charBaseCriticalChance,
        float charBaseCriticalDamageMultiplier,
        List<SkillData> charSkillList
    )
    {
        characterId = charId;
        characterName = charDisplayName;
        rarity = charRarity;
        role = charRole;
        characterIllust = charIllust;
        characterIcon = charIcon;
        characterPrefab = charPrefab;
        baseMaxHp = charBaseMaxHp;
        baseAttackPower = charBaseAttackPower;
        baseAttackSpeed = charBaseAttackSpeed;
        baseDefense = charBaseDefense;
        baseCriticalChance = charBaseCriticalChance;
        baseCriticalDamageMultiplier = charBaseCriticalDamageMultiplier;
        skillsList = charSkillList;
    }
}