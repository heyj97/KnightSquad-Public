/// <summary>
/// 생성할 UID의 종류
/// </summary>
/// <remarks>
///<para><b>Type</b></para>
/// <list type="bullet">
/// <item><description><c>UidType.None</c> 접두사가 필요 없는 일반 UID</description></item>
/// <item><description><c>UidType.User</c> 유저 인스턴스 UID</description></item>
/// <item><description><c>UidType.Item</c> 아이템 인스턴스 UID</description></item>
/// </list>
/// </remarks>
public enum UidType {
    /// <summary>접두사가 필요 없는 일반 UID</summary>
    None,
    /// <summary>유저 인스턴스 UID</summary>
    User,
    /// <summary>아이템 인스턴스 UID</summary>
    Item,
    /// <summary>캐릭터 인스턴스 UID</summary>
    Char
}