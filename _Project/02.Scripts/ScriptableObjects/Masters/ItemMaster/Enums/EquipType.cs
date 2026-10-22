/// <summary>
/// 장비 아이템의 장착 타입.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item><description>(0) Weapon - 무기</description></item>
/// <item><description>(1) Armor - 방어구</description></item>
/// <item><description>(2) Accessory - 장신구</description></item>
/// </list>
/// </remarks>
public enum EquipType
{
    /// <summary>(0) Weapon - 무기</summary>
    Weapon,

    /// <summary>(1) Armor - 방어구</summary>
    Armor,

    /// <summary>(2) Accessory - 장신구</summary>
    Accessory,

    /// <summary>Length 대신 사용하기 위한 Count 변수</summary>
    Count
}
