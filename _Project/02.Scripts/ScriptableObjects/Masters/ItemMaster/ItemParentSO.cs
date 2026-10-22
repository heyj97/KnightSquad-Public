using UnityEngine;

/// <summary>
/// 저장 데이터와 마스터 데이터가 공유하는 아이템 식별 정보를 정의합니다.
/// </summary>
[CreateAssetMenu(fileName = "ItemParentSO", menuName = "Scriptable Objects/ItemParentSO")]
public class ItemParentSO : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private ItemType itemType;
    
    /// <summary>시스템에서 사용하는 장비 아이템 ID</summary>
    public string Id => itemId;
    /// <summary>
    /// 저장 및 인벤토리 처리에 사용하는 아이템 종류를 가져옵니다.
    /// </summary>
    public ItemType Type => itemType;

    /// <summary>UI 또는 게임에서 표시하는 장비 아이템 이름</summary>
    public string DisplayName => itemName;
}
