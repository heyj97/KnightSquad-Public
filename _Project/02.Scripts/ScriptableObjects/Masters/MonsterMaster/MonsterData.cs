using System;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 몬스터 생성과 능력치 초기화에 사용하는 마스터 데이터를 보관합니다.
/// </summary>
[CreateAssetMenu(
    fileName = "MonsterData",
    menuName = "Project:Mini/MonsterData"
)]
public class MonsterData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string monsterId;
    [SerializeField] private string monsterName;
    [FormerlySerializedAs("rarity")] [SerializeField] private MonsterGrade m_Grade;


    [Header("Resources")]
    [SerializeField] private Sprite monsterIcon;
    [SerializeField] private GameObject monsterPrefab;


    [Header("Base Stats")]
    [SerializeField, Min(1.0f)] private float baseMaxHp = 100f;

    [SerializeField, Min(1.0f)] private float baseAttack = 10f;

    [SerializeField, Min(0.01f)] private float baseAttackSpeed = 1f;

    [SerializeField, Min(0.0f)] private float baseDefense = 10f;

    [Header("Spawn Chapter")]
    [SerializeField, Min(1)] private int spawnChapter = 1;

    
    // Identity
    /// <summary>시스템에서 사용하는 몬스터 ID</summary>
    public string Id => monsterId;

    /// <summary>UI 또는 게임에서 표시하는 몬스터 이름</summary>
    public string DisplayName => monsterName;

    /// <summary>몬스터의 등급</summary>
    public MonsterGrade Grade => m_Grade;


    // Resources
    /// <summary>몬스터 아이콘</summary>
    public Sprite Icon => monsterIcon;
    
    /// <summary>몬스터 프리팹 </summary>
    public GameObject Prefab => monsterPrefab;


    // Base Stats
    /// <summary>몬스터의 기본 최대 체력</summary>
    public float BaseMaxHp => baseMaxHp;

    /// <summary>몬스터의 기본 공격력</summary>
    public float BaseAttack => baseAttack;

    /// <summary>몬스터의 기본 공격속도</summary>
    public float BaseAttackSpeed => baseAttackSpeed;

    /// <summary>몬스터의 기본 방어력</summary>
    public float BaseDefense => baseDefense;

    // Spawn Chapter
    /// <summary>
    /// 필드 진행에서 이 몬스터가 등장하기 시작하는 챕터를 가져옵니다.
    /// </summary>
    public int SpawnChapter => spawnChapter;
}
