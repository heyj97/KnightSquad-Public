using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 버프를 제외한 캐릭터 능력치로 고정 전투력을 산출하는 계산 기능을 제공합니다.
/// </summary>
public class PartyAtkCalc : MonoBehaviour
{
    // Project Mini DataSheet: ValuePerOne!B4:B10 및 Calculators!E4:E10 기준.
    // 기준 캐릭터의 전투력은 1000. 시트 변경 시 기준값을 함께 갱신한다.
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 공격력 기준값입니다.
    /// </summary>
    public const double ReferenceAttack = 10.0;
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 공격속도 기준값입니다.
    /// </summary>
    public const double ReferenceAttackSpeed = 1.0;
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 최대 체력 기준값입니다.
    /// </summary>
    public const double ReferenceMaxHp = 100.0;
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 방어력 기준값입니다.
    /// </summary>
    public const double ReferenceDefense = 10.0;
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 치명타 확률 기준값입니다.
    /// </summary>
    public const double ReferenceCriticalChance = 0.1;
    /// <summary>
    /// 기준 전투력 1000을 정의할 때 사용하는 치명타 피해 배율 기준값입니다.
    /// </summary>
    public const double ReferenceCriticalDamageMultiplier = 1.7;
    /// <summary>
    /// 방어력을 유효 체력으로 환산하는 식의 분모 상수입니다.
    /// </summary>
    public const double DefenseConstant = 100.0;
    /// <summary>
    /// 기준 능력치 조합에 부여하는 캐릭터 전투력 점수입니다.
    /// </summary>
    public const double ReferenceCombatPower = 1000.0;

    /// <summary>
    /// 기본 능력치 + 장비 + 파티 강화로 계산한다.
    /// 임시 버프, 현재 체력, 사망 여부, 장비 발동 효과는 포함하지 않는다.
    /// CharLv는 HP/ATK/DEF 기본 능력치에 적용한다. ItemLv 성장식은 아직 없다.
    /// 컴포넌트를 생성하거나 부착하지 않고 호출할 수 있다.
    /// </summary>
    /// <param name="data">초기화하거나 계산에 사용할 마스터 데이터입니다.</param>
    /// <param name="equipments">고정 전투력에 포함할 장비 슬롯별 마스터 데이터입니다.</param>
    /// <param name="partyStat">캐릭터 능력치에 적용할 파티 성장 보정값입니다.</param>
    /// <returns>버프를 제외한 능력치로 계산한 정수 전투력입니다.</returns>
    public static long Calculate(CharacterData data,
        IReadOnlyDictionary<EquipType, EquipmentData> equipments, PartyStat partyStat, int characterLevel = 1)
    {
        if (data == null)
            return 0;

        double scale = CharacterGrowthCalc.GetMultiplier(data.Rarity, characterLevel);
        double hp = data.BaseMaxHp * scale;
        double attack = data.BaseAttackPower * scale;
        double attackSpeed = data.BaseAttackSpeed;
        double defense = data.BaseDefense * scale;
        double criticalChance = data.BaseCriticalChance;
        double criticalDamage = data.BaseCriticalDamageMultiplier;

        if (equipments != null)
        {
            foreach (EquipmentData equipment in equipments.Values)
            {
                if (equipment == null)
                    continue;
                hp += equipment.BonusHp;
                attack += equipment.BonusAttack;
                attackSpeed += equipment.BonusAttackSpeed;
                defense += equipment.BonusDefense;
                criticalChance += equipment.BonusCriticalChance;
                criticalDamage += equipment.BonusCriticalDamageMultiplier;
            }
        }

        if (partyStat != null)
        {
            hp *= 1.0 + partyStat.HpMultiplier;
            attack *= 1.0 + partyStat.AttackMultiplier;
            attackSpeed *= 1.0 + partyStat.AttackSpeedMultiplier;
            defense *= 1.0 + partyStat.DefenseMultiplier;
            criticalChance += partyStat.CriticalChanceBonus;
            criticalDamage += partyStat.CriticalDamageBonus;
        }

        return CalculateFromStats(attack, attackSpeed, hp, defense, criticalChance, criticalDamage);
    }

    /// <summary>버프를 제외한 능력치를 받아 시트의 DPS/EHP 비율의 기하평균을 정수화한다.</summary>
    /// <param name="attack">적용할 공격력 성장 비율 또는 레벨입니다.</param>
    /// <param name="attackSpeed">적용할 공격속도 성장 비율 또는 레벨입니다.</param>
    /// <param name="maxHp">전투력 계산에 사용할 버프 제외 최대 체력입니다.</param>
    /// <param name="defense">적용할 방어력 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalChance">적용할 치명타 확률 값 또는 성장 레벨입니다.</param>
    /// <param name="criticalDamage">적용할 치명타 피해 배율 또는 성장 레벨입니다.</param>
    /// <returns>DPS와 유효 체력의 기하평균을 기준 점수로 환산한 정수 전투력입니다.</returns>
    public static long CalculateFromStats(double attack, double attackSpeed, double maxHp,
        double defense, double criticalChance, double criticalDamage)
    {
        criticalChance = Math.Max(0.0, Math.Min(1.0, criticalChance));
        double expectedDps = Math.Max(0.0, attack) * Math.Max(0.0, attackSpeed)
            * (1.0 + criticalChance * (Math.Max(0.0, criticalDamage) - 1.0));
        double effectiveHp = Math.Max(0.0, maxHp)
            * Math.Max(0.0, 1.0 + defense / DefenseConstant);
        double referenceDps = ReferenceAttack * ReferenceAttackSpeed
            * (1.0 + ReferenceCriticalChance * (ReferenceCriticalDamageMultiplier - 1.0));
        double referenceEhp = ReferenceMaxHp * (1.0 + ReferenceDefense / DefenseConstant);
        double power = Math.Sqrt((expectedDps / referenceDps) * (effectiveHp / referenceEhp))
            * ReferenceCombatPower;

        if (double.IsNaN(power) || power <= 0.0)
            return 0;
        if (power >= long.MaxValue)
            return long.MaxValue;
        return (long)Math.Round(power, MidpointRounding.AwayFromZero);
    }
}
