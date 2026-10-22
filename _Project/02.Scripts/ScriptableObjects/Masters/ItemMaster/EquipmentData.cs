using UnityEngine;

using System;
using System.Collections.Generic;

/// <summary>
/// 장비 아이템의 기본 데이터 Scriptable Object
/// </summary>
/// <remarks>
///
/// <para><b>Identity</b></para>
/// <list type="bullet">
/// <item><description><c>string Id</c> 시스템에서 사용하는 장비 아이템 ID</description></item>
/// <item><description><c>string DisplayName</c> UI 또는 게임에서 표시하는 장비 아이템 이름</description></item>
/// <item><description><c>SystemRarity Rarity</c> 장비 아이템의 희귀도</description></item>
/// <item><description><c>EquipType EquipType</c> 장비 아이템의 장착 타입</description></item>
/// </list>
///
/// <para><b>Resources</b></para>
/// <list type="bullet">
/// <item><description><c>Sprite Icon</c> 장비 아이템 아이콘</description></item>
/// </list>
///
/// <para><b>Item Stats</b></para>
/// <list type="bullet">
/// <item><description><c>float BonusHp</c> 추가 체력</description></item>
/// <item><description><c>float BonusAttack</c> 추가 공격력</description></item>
/// <item><description><c>float BonusAttackSpeed</c> 추가 공격속도</description></item>
/// <item><description><c>float BonusDefense</c> 추가 방어력</description></item>
/// <item><description><c>float BonusCriticalChance</c> 추가 치명타 확률</description></item>
/// <item><description><c>float BonusCriticalDamageMultiplier</c> 추가 치명타 데미지 배율</description></item>
/// </list>
///
/// <para><b>Item Special Effects</b></para>
/// <list type="bullet">
/// <item><description><c>IReadOnlyList&lt;ItemEffectData&gt; SpecialEffects</c> 아이템의 특수효과 목록</description></item>
/// </list>
///
/// </remarks>
[CreateAssetMenu(
    fileName = "EquipmentData",
    menuName = "Project:Mini/EquipmentData"
)]
public class EquipmentData : ItemParentSO
{
    [Header("Identity")]
    [SerializeField] private SystemRarity rarity;
    [SerializeField] private EquipType type;
    
    [Header("Resources")]
    [SerializeField] private Sprite itemIcon;
    
    [Header("Item Stats")]
    [SerializeField] private float bonusHp;
    [SerializeField] private float bonusAttack;
    [SerializeField] private float bonusAttackSpeed;
    [SerializeField] private float bonusDefense;
    [SerializeField] private float bonusCriticalChance;
    [SerializeField] private float bonusCriticalDamageMultiplier;
    
    [Header("Item Special Effects")]
    [SerializeField] private List<ItemEffectData> specialEffects = new();


    // Identity
    /// <summary>장비 아이템의 희귀도</summary>
    public SystemRarity Rarity => rarity;

    /// <summary>장비 아이템의 장착 타입</summary>
    public EquipType EquipType => type;


    // Resources
    /// <summary>장비 아이템 아이콘</summary>
    public Sprite Icon => itemIcon;


    // Item Stats
    /// <summary>추가 체력</summary>
    public float BonusHp => bonusHp;

    /// <summary>추가 공격력</summary>
    public float BonusAttack => bonusAttack;

    /// <summary>추가 공격속도</summary>
    public float BonusAttackSpeed => bonusAttackSpeed;

    /// <summary>추가 방어력</summary>
    public float BonusDefense => bonusDefense;

    /// <summary>추가 치명타 확률</summary>
    public float BonusCriticalChance => bonusCriticalChance;

    /// <summary>추가 치명타 데미지 배율</summary>
    public float BonusCriticalDamageMultiplier => bonusCriticalDamageMultiplier;


    // Item Special Effects
    /// <summary>아이템의 특수효과 목록(일정 등급 이상의 아이템에서 추가)</summary>
    public IReadOnlyList<ItemEffectData> SpecialEffects => specialEffects;
}

/// <summary>
/// 장비 아이템에 부여되는 특수효과 데이터
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description><c>string Description</c> 아이템 특수효과 설명</description></item>
/// <item><description><c>ItemEffectTrigger Trigger</c> 아이템 특수효과의 발동 조건</description></item>
/// <item><description><c>ItemEffectType Type</c> 아이템 특수효과의 기능 타입</description></item>
/// <item><description><c>float TriggerChance</c> 아이템 특수효과의 발동 확률</description></item>
/// <item><description><c>float EffectMultiplier</c> 아이템 특수효과의 효과 계수</description></item>
/// <item><description><c>bool IsSkill</c> 아이템 특수효과가 스킬 타입인지 여부</description></item>
/// <item><description><c>GameObject EffectPrefab</c> 아이템 특수효과 발동 시 생성할 프리팹</description></item>
/// </list>
/// </remarks>
[Serializable]
public class ItemEffectData
{
    [SerializeField] private ItemEffectTrigger trigger;
    [SerializeField] private ItemEffectType type;
    [SerializeField, TextArea] private string effectDescription;

    [SerializeField, Range(0f, 1f)]
    private float triggerChance = 1f;

    [SerializeField, Min(0f)]
    private float effectMultiplier = 1f;

    [SerializeField] private GameObject effectPrefab;


    /// <summary>아이템 특수효과 설명</summary>
    public string Description => effectDescription;

    /// <summary>아이템 특수효과의 발동 조건</summary>
    public ItemEffectTrigger Trigger => trigger;

    /// <summary>아이템 특수효과의 기능 타입</summary>
    public ItemEffectType Type => type;

    /// <summary>아이템 특수효과의 발동 확률</summary>
    public float TriggerChance => triggerChance;

    /// <summary>아이템 특수효과의 효과 계수</summary>
    public float EffectMultiplier => effectMultiplier;

    /// <summary>아이템 특수효과가 스킬 타입인지 여부</summary>
    public bool IsSkill => type == ItemEffectType.Skill;

    /// <summary>아이템 특수효과 발동 시 생성할 프리팹</summary>
    public GameObject EffectPrefab => effectPrefab;
}
