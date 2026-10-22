using UnityEngine;
/// <summary>
/// 저장하거나 불러올 데이터의 종류
/// </summary>
/// <remarks>
///<para><b>Type</b></para>
/// <list type="bullet">
/// <item><description><c>DataType.User</c> 유저 기본정보 및 재화 데이터</description></item>
/// <item><description><c>DataType.Progress</c> 스테이지, 랭킹, 파티 강화 등의 진행 데이터</description></item>
/// <item><description><c>DataType.Character</c> 보유 캐릭터 및 파티 배치 데이터</description></item>
/// <item><description><c>DataType.Inventory</c> 보유 아이템 및 인벤토리 데이터</description></item>
/// <item><description><c>DataType.All</c> 모든 데이터</description></item>
/// </list>
/// </remarks>
public enum DataType {
    /// <summary>유저 기본정보 및 재화 데이터</summary>
    User,
    
    /// <summary>스테이지, 랭킹, 파티 강화 등의 진행 데이터</summary>
    Progress,
    
    /// <summary>보유 캐릭터 및 파티 배치 데이터</summary>
    Character,
    
    ///<summary>보유 아이템 및 인벤토리 데이터</summary>
    Inventory,
    
    /// <summary>모든 데이터</summary>
    All
}