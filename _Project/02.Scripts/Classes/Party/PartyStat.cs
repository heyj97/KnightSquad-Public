using System;
using UnityEngine;

/// <summary>
/// 파티 전체에 적용되는 강화 스탯을 관리하는 클래스
/// </summary>
public class PartyStat
{
    /// <summary>파티 스탯 값이 변경되었을 때 호출된다.</summary>
    public event Action OnPartyStatsChanged;

    // 기본값 0 = 추가 강화 없음
    /// <summary>
    /// 기본 최대 체력에 더해지는 파티 성장 비율을 가져옵니다.
    /// </summary>
    public float HpMultiplier { get; private set; } = 0f;
    /// <summary>
    /// 기본 공격력에 더해지는 파티 성장 비율을 가져옵니다.
    /// </summary>
    public float AttackMultiplier { get; private set; } = 0f;
    /// <summary>
    /// 기본 공격속도에 더해지는 파티 성장 비율을 가져옵니다.
    /// </summary>
    public float AttackSpeedMultiplier { get; private set; } = 0f;
    /// <summary>
    /// 기본 방어력에 더해지는 파티 성장 비율을 가져옵니다.
    /// </summary>
    public float DefenseMultiplier { get; private set; } = 0f;

    // 기본값 0 = 추가 보정 없음
    /// <summary>
    /// 치명타 확률에 더해지는 파티 성장 보정값을 가져옵니다.
    /// </summary>
    public float CriticalChanceBonus { get; private set; } = 0f;
    /// <summary>
    /// 치명타 피해 배율에 더해지는 파티 성장 보정값을 가져옵니다.
    /// </summary>
    public float CriticalDamageBonus { get; private set; } = 0f;

    /// <summary>모든 성장값을 확정한 뒤 변경 알림을 한 번만 보낸다.</summary>
    /// <param name="hp">적용할 최대 체력 성장 비율 또는 레벨입니다.</param>
    /// <param name="attack">적용할 공격력 성장 비율 또는 레벨입니다.</param>
    /// <param name="attackSpeed">적용할 공격속도 성장 비율 또는 레벨입니다.</param>
    /// <param name="defense">적용할 방어력 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalChance">적용할 치명타 확률 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalDamage">적용할 치명타 피해 배율 또는 성장 레벨입니다.</param>
    public void SetAll(float hp, float attack, float attackSpeed, float defense,
        float criticalChance, float criticalDamage)
    {
        hp = Mathf.Max(0f, hp);
        attack = Mathf.Max(0f, attack);
        attackSpeed = Mathf.Max(0f, attackSpeed);
        defense = Mathf.Max(0f, defense);
        criticalChance = Mathf.Max(0f, criticalChance);
        criticalDamage = Mathf.Max(0f, criticalDamage);
        if (Mathf.Approximately(HpMultiplier, hp) &&
            Mathf.Approximately(AttackMultiplier, attack) &&
            Mathf.Approximately(AttackSpeedMultiplier, attackSpeed) &&
            Mathf.Approximately(DefenseMultiplier, defense) &&
            Mathf.Approximately(CriticalChanceBonus, criticalChance) &&
            Mathf.Approximately(CriticalDamageBonus, criticalDamage))
            return;

        HpMultiplier = hp;
        AttackMultiplier = attack;
        AttackSpeedMultiplier = attackSpeed;
        DefenseMultiplier = defense;
        CriticalChanceBonus = criticalChance;
        CriticalDamageBonus = criticalDamage;
        OnPartyStatsChanged?.Invoke();
    }


    /// <summary>
    /// 공격력 강화 수치를 설정
    /// 1.2 = 기존 공격력의 120%
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetAttackMultiplier(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(AttackMultiplier, newValue))
            return;

        AttackMultiplier = newValue;
        OnPartyStatsChanged?.Invoke();
    }

    /// <summary>
    /// 최대 체력 강화 수치를 설정
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetHpMultiplier(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(HpMultiplier, newValue))
            return;

        HpMultiplier = newValue;
        OnPartyStatsChanged?.Invoke();
    }

    /// <summary>
    /// 방어력 강화 수치를 설정
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetDefenseMultiplier(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(DefenseMultiplier, newValue))
            return;

        DefenseMultiplier = newValue;
        OnPartyStatsChanged?.Invoke();
    }

    /// <summary>
    /// 공격속도 강화 수치를 설정
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetAttackSpeedMultiplier(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(AttackSpeedMultiplier, newValue))
            return;

        AttackSpeedMultiplier = newValue;
        OnPartyStatsChanged?.Invoke();
    }

    /// <summary>
    /// 치명타 확률 추가 보정값을 설정
    /// 0.1 = +10%p
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetCriticalChanceBonus(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(CriticalChanceBonus, newValue))
            return;

        CriticalChanceBonus = newValue;
        OnPartyStatsChanged?.Invoke();
    }

    /// <summary>
    /// 치명타 데미지 추가 보정값을 설정
    /// 0.2 = 치명타 배율 +0.2
    /// </summary>
    /// <param name="value">새로 적용할 값입니다.</param>
    public void SetCriticalDamageBonus(float value)
    {
        float newValue = Mathf.Max(0f, value);

        if (Mathf.Approximately(CriticalDamageBonus, newValue))
            return;

        CriticalDamageBonus = newValue;
        OnPartyStatsChanged?.Invoke();
    }
}
