using System;

/// <summary>공격·스킬·피격이 공유하는 순수 피해 계산. 상태 변경과 난수 생성은 호출자가 담당한다.</summary>
public static class DamageCalculator
{
    /// <summary>roll은 0~1 범위의 난수. 확률 0은 항상 실패, 1은 항상 성공한다.</summary>
    /// <param name="chance">0부터 1 사이로 표현한 치명타 발생 확률입니다.</param>
    /// <param name="roll">확률과 비교할 0부터 1 사이의 난수 값입니다.</param>
    /// <returns>조건을 충족하면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool IsCritical(float chance, float roll)
    {
        if (!IsFinite(chance) || !IsFinite(roll) || roll < 0f || roll > 1f)
            return false;

        return chance > 0f && (chance >= 1f || roll < chance);
    }

    /// <summary>현재 공격력에 공격/스킬 배율과 치명타 배율을 적용한 방어 적용 전 피해.</summary>
    /// <param name="attackPower">피해 계산의 기준이 되는 공격력입니다.</param>
    /// <param name="multiplier">공격 또는 스킬에 적용할 피해 계수입니다.</param>
    /// <param name="isCritical">치명타로 처리된 피해인지 여부입니다.</param>
    /// <param name="criticalMultiplier">치명타일 때 피해에 적용할 배율입니다.</param>
    /// <returns>방어 감소를 적용하기 전의 기본 또는 치명타 피해량입니다.</returns>
    public static float CalculateRawDamage(float attackPower, float multiplier,
        bool isCritical, float criticalMultiplier)
    {
        if (!IsFinite(attackPower) || !IsFinite(multiplier) || attackPower <= 0f || multiplier <= 0f)
            return 0f;

        if (isCritical && (!IsFinite(criticalMultiplier) || criticalMultiplier <= 0f))
            return 0f;

        double damage = (double)attackPower * multiplier;
        
        if (isCritical)
            damage *= criticalMultiplier;

        return (float)Math.Min(float.MaxValue, damage);
    }

    /// <summary>
    /// 양수 피해에만 방어 감소와 최소 피해 1을 적용한다.
    /// 음수 방어력은 0으로 취급하고, 유효하지 않은 값/방어 상수는 피해 0을 반환한다.
    /// </summary>
    /// <param name="rawDamage">방어 감소를 적용하기 전의 피해량입니다.</param>
    /// <param name="defense">적용할 방어력 값 또는 성장 레벨입니다.</param>
    /// <param name="defenseConstant">방어 감소율 곡선을 조절하는 기준 상수입니다.</param>
    /// <returns>방어 감소를 적용하고 최소 피해를 보장한 최종 피해량입니다.</returns>
    public static float CalculateFinalDamage(float rawDamage, float defense, float defenseConstant)
    {
        if (!IsFinite(rawDamage) || !IsFinite(defense) || !IsFinite(defenseConstant)
            || rawDamage <= 0f || defenseConstant <= 0f)
            return 0f;

        double damage = rawDamage * ((double)defenseConstant
            / (defenseConstant + (double)Math.Max(0f, defense)));
        return (float)Math.Max(1.0, damage);
    }

    private static bool IsFinite(float value)
    {
        return !float.IsNaN(value) && !float.IsInfinity(value);
    }
}
