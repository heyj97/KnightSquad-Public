using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>소유 장비의 판매를 메모리에 확정하고 일반 서버 저장을 예약합니다.</summary>
public static class ItemSaleMgr
{
    public const string EquippedMessage = "착용 해제 후 아이템판매가 가능합니다";
    private static bool m_IsSelling;
    internal static bool IsSelling => m_IsSelling;

    public static void InitializePrices(IEnumerable<ItemData> items)
    {
        MasterDataMgr master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
        if (master == null || items == null) return;
        foreach (ItemData item in items)
            if (item != null && item.Type == ItemType.Equipment && item.Price == 0 &&
                !string.IsNullOrWhiteSpace(item.ItemId) &&
                master.TryGetEquipment(item.ItemId, out EquipmentData equipment) && equipment != null)
                item.Price = GlobalConstants.CreateEquipmentSalePrice(equipment.Rarity);
    }

    public static bool TryFindItem(string uid, out ItemData item)
    {
        item = null;
        if (string.IsNullOrWhiteSpace(uid)) return false;
        foreach (ItemData candidate in GlobalValue.UserItemDataList)
        {
            if (candidate == null || candidate.ItemUid != uid) continue;
            if (item != null) { item = null; return false; }
            item = candidate;
        }
        return item != null;
    }

    public static bool TryGetSaleInfo(string uid, EquipmentEditSession edit,
        out ItemData item, out EquipmentData equipment, out string message, bool allowPendingSale = false)
    {
        equipment = null;
        message = "";
        if (!TryFindItem(uid, out item) || item.Type != ItemType.Equipment ||
            item.ItemCount != 1 || string.IsNullOrWhiteSpace(item.ItemId))
        {
            message = "판매할 아이템 또는 가격이 유효하지 않습니다.";
            return false;
        }
        if (edit != null && (edit.IsClosed || (!allowPendingSale && edit.IsSalePending(uid))))
        {
            message = "편집이 종료되었거나 이미 판매 예약한 아이템입니다.";
            return false;
        }
        bool equipped = edit != null ? edit.TryGetEquippedCharacter(uid, out _) :
            EquipmentMgr.TryGetEquippedCharacter(uid, out _);
        if (equipped) { message = EquippedMessage; return false; }
        if (item.Price <= 0) { message = "판매가격이 유효하지 않습니다."; return false; }
        MasterDataMgr master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
        if (master == null || !master.TryGetEquipment(item.ItemId, out equipment) || equipment == null)
        {
            message = "아이템 마스터 정보를 찾을 수 없습니다.";
            return false;
        }
        return true;
    }

    public static bool TrySell(string uid, int confirmedPrice, out string message)
    {
        message = "";
        if (PlayerDataMgr.IsProgressBlocked)
        { message = "온라인 저장 문제를 해결한 뒤 다시 시도해주세요."; return false; }
        if (m_IsSelling || EquipmentMgr.IsApplying)
        {
            message = "아이템 변경을 처리 중입니다.";
            return false;
        }
        if (!TryGetSaleInfo(uid, null, out ItemData item, out _, out message)) return false;
        if (item.Price != confirmedPrice || ulong.MaxValue - GlobalValue.UserGold < (ulong)item.Price)
        {
            message = "판매가격이 변경되었거나 보유 골드 한도를 초과합니다.";
            return false;
        }
        m_IsSelling = true;
        try
        {
            var items = new List<ItemData>(GlobalValue.UserItemDataList);
            items.Remove(item);
            GlobalValue.ApplySaleState(items, GlobalValue.UserGold + (ulong)item.Price);
            GlobalValue.NotifySaleApplied();
            // 연속 판매는 일반 자동저장 또는 다음 명시적 저장에 합산한다.
            PlayerDataMgr.RequestSave(SaveType.Inventory | SaveType.PlayerInfo);
            return true;
        }
        finally { m_IsSelling = false; }
    }

    // 새 매니저 자동 생성 없이 기존 메시지박스를 사용한다.
    public static void ShowMessage(string message)
        => ShowDialog(message, MessageBoxType.Info);

    public static void ShowDialog(string message, MessageBoxType type, Action onSubmit = null)
    {
        MessageBoxMgr box = UnityEngine.Object.FindFirstObjectByType<MessageBoxMgr>();
        if (box == null) { PDebug.LogError("MessageBoxMgr를 찾을 수 없습니다."); return; }
        box.ShowMsg(message, type: type, onSubmit: onSubmit);
    }
}
