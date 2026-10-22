using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>장비 편집 결과를 일괄 확정하고 공통 온라인 저장 대기로 전달합니다.</summary>
public static class EquipmentMgr
{
    public static event Action OnEquipmentChanged;
    private static bool m_IsApplying;
    internal static bool IsApplying => m_IsApplying;

    public static bool TryGetEquippedCharacter(string itemUid, out string charId)
    {
        charId = "";
        if (string.IsNullOrWhiteSpace(itemUid)) return false;
        foreach (CharData character in GlobalValue.UserCharDataList)
            if (character != null)
                for (int i = 0; i < (int)EquipType.Count; i++)
                    if (character.GetEquipmentUid((EquipType)i) == itemUid)
                    { charId = character.CharId; return true; }
        return false;
    }

    public static bool TryBeginEdit(out EquipmentEditSession session, out string message)
    {
        session = null;
        message = "";
        ItemSaleMgr.InitializePrices(GlobalValue.UserItemDataList);
        var originals = new Dictionary<string, string[]>();
        foreach (CharData character in GlobalValue.UserCharDataList)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharId) ||
                originals.ContainsKey(character.CharId))
            {
                message = "보유 캐릭터 데이터가 유효하지 않습니다.";
                return false;
            }
            originals.Add(character.CharId, ReadSlots(character));
        }
        session = new EquipmentEditSession(originals);
        return true;
    }

    internal static string[] ReadSlots(CharData character)
    {
        return new[] { character.GetEquipmentUid(EquipType.Weapon),
            character.GetEquipmentUid(EquipType.Armor), character.GetEquipmentUid(EquipType.Accessory) };
    }

    internal static bool SameSlots(string[] first, string[] second)
    {
        for (int i = 0; i < (int)EquipType.Count; i++)
            if (first[i] != second[i]) return false;
        return true;
    }

    public static bool TryResolveEquipment(string itemUid, EquipType type,
        out EquipmentData equipment, out string message)
    {
        equipment = null;
        message = "";
        ItemData item = null;
        foreach (ItemData candidate in GlobalValue.UserItemDataList)
        {
            if (candidate == null || candidate.ItemUid != itemUid) continue;
            if (item != null)
            {
                message = "중복된 아이템 UID가 있습니다.";
                return false;
            }
            item = candidate;
        }
        if (string.IsNullOrWhiteSpace(itemUid) || item == null ||
            item.Type != ItemType.Equipment || string.IsNullOrWhiteSpace(item.ItemId) ||
            item.ItemCount != 1)
        {
            message = $"소유 장비를 찾을 수 없습니다: {itemUid}";
            return false;
        }
        // Inst 접근으로 준비되지 않은 매니저를 자동 생성하지 않는다.
        MasterDataMgr master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
        if (master == null || !master.TryGetEquipment(item.ItemId, out equipment) ||
            equipment == null || equipment.EquipType != type)
        {
            message = $"장비 마스터 또는 슬롯 종류가 유효하지 않습니다: {item.ItemId}";
            return false;
        }
        return true;
    }

    /// <summary>서버 저장 없이 편집된 장비 슬롯만 확정합니다. 실패 시 편집본을 유지합니다.</summary>
    public static bool TryApply(EquipmentEditSession session, out string message)
    {
        message = "";
        if (PlayerDataMgr.IsProgressBlocked)
        { message = "온라인 저장 문제를 해결한 뒤 다시 시도해주세요."; return false; }
        if (m_IsApplying || ItemSaleMgr.IsSelling || session == null || session.IsClosed)
        {
            message = "장비 편집이 종료되었거나 적용 중입니다.";
            return false;
        }
        m_IsApplying = true;
        try { return Apply(session, out message); }
        finally { m_IsApplying = false; }
    }

    private static bool Apply(EquipmentEditSession session, out string message)
    {
        message = "";
        var characters = new Dictionary<string, CharData>();
        foreach (CharData character in GlobalValue.UserCharDataList)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharId) ||
                characters.ContainsKey(character.CharId))
            {
                message = "보유 캐릭터 데이터가 유효하지 않습니다.";
                return false;
            }
            characters.Add(character.CharId, character);
        }
        if (characters.Count != session.Originals.Count)
        {
            message = "편집 중 보유 캐릭터가 변경되었습니다. 다시 열어주세요.";
            return false;
        }
        foreach (var entry in session.Originals)
        {
            if (!characters.TryGetValue(entry.Key, out CharData character) ||
                !SameSlots(ReadSlots(character), entry.Value))
            {
                message = "편집 중 실제 장착 정보가 변경되었습니다. 다시 열어주세요.";
                return false;
            }
        }

        var usedUids = new HashSet<string>();
        var resolved = new Dictionary<string, Dictionary<EquipType, EquipmentData>>();
        var changed = new List<string>();
        foreach (var entry in session.Edits)
        {
            var slots = new Dictionary<EquipType, EquipmentData>();
            for (int i = 0; i < (int)EquipType.Count; i++)
            {
                string uid = entry.Value[i];
                EquipmentData equipment = null;
                if (!string.IsNullOrEmpty(uid))
                {
                    if (!usedUids.Add(uid))
                    {
                        message = "동일 아이템을 여러 슬롯에 장착할 수 없습니다.";
                        return false;
                    }
                    if (!TryResolveEquipment(uid, (EquipType)i, out equipment, out message))
                        return false;
                }
                slots.Add((EquipType)i, equipment);
            }
            resolved.Add(entry.Key, slots);
            if (!SameSlots(session.Originals[entry.Key], entry.Value)) changed.Add(entry.Key);
        }
        var soldItems = new List<ItemData>();
        ulong saleGold = 0;
        foreach (var sale in session.Sales)
        {
            if (usedUids.Contains(sale.Key)) { message = ItemSaleMgr.EquippedMessage; return false; }
            if (!ItemSaleMgr.TryGetSaleInfo(sale.Key, session, out ItemData item, out _, out message, true))
                return false;
            if (
                item.Price != sale.Value.Price || item.ItemId != sale.Value.ItemId ||
                item.Price <= 0 || item.Type != ItemType.Equipment || item.ItemCount != 1)
            { message = "판매 예약한 아이템 또는 가격이 변경되었습니다."; return false; }
            if (ulong.MaxValue - saleGold < (ulong)item.Price)
            { message = "판매금액 한도를 초과합니다."; return false; }
            saleGold += (ulong)item.Price;
            soldItems.Add(item);
        }
        if (ulong.MaxValue - GlobalValue.UserGold < saleGold)
        { message = "보유 골드 한도를 초과합니다."; return false; }
        if (changed.Count == 0 && soldItems.Count == 0)
        {
            session.Close();
            return true;
        }

        PartyMgr party = UnityEngine.Object.FindFirstObjectByType<PartyMgr>(FindObjectsInactive.Include);
        if (party != null && !party.CanApplyEquipment)
        {
            message = "파티가 준비되지 않았거나 다른 변경을 처리 중입니다.";
            return false;
        }
        var runtime = new Dictionary<string, CharacterBase>();
        var snapshots = new Dictionary<string, CharacterBase.PartySnapshot>();
        foreach (string id in changed)
        {
            bool isPartyMember = false;
            foreach (string partyId in GlobalValue.PartyCharIdList)
                if (partyId == id) isPartyMember = true;
            if (party == null || !isPartyMember) continue;
            if (!party.TryGetPartyCharacter(id, out CharacterBase character) ||
                character.UserCharData != characters[id])
            {
                message = "파티 캐릭터와 메모리 데이터가 일치하지 않습니다.";
                return false;
            }
            runtime.Add(id, character);
            snapshots.Add(id, character.CapturePartySnapshot());
        }

        party?.BeginEquipmentBatch();
        var originalItems = new List<ItemData>(GlobalValue.UserItemDataList);
        ulong originalGold = GlobalValue.UserGold;
        bool applied = false;
        try
        {
            foreach (string id in changed)
                for (int i = 0; i < (int)EquipType.Count; i++)
                    characters[id].TrySetEquipmentUid((EquipType)i, session.Edits[id][i]);
            foreach (var entry in runtime)
                entry.Value.ApplyEquipmentBatch(resolved[entry.Key]);
            if (soldItems.Count > 0)
            {
                var remaining = new List<ItemData>(originalItems);
                foreach (ItemData item in soldItems) remaining.Remove(item);
                GlobalValue.ApplySaleState(remaining, originalGold + saleGold);
            }
            applied = true;
            session.Close();
        }
        catch (Exception exception)
        {
            GlobalValue.ApplySaleState(originalItems, originalGold);
            foreach (string id in changed)
                for (int i = 0; i < (int)EquipType.Count; i++)
                    characters[id].TrySetEquipmentUid((EquipType)i, session.Originals[id][i]);
            foreach (var entry in runtime)
                entry.Value.RestoreEquipmentBatchSnapshot(snapshots[entry.Key]);
            PDebug.LogError(exception);
            message = "장비 적용에 실패하여 기존 상태를 복원했습니다.";
        }
        finally
        {
            // 모든 데이터 적용 후에만 구독자에게 확정 상태를 알린다.
            if (applied)
                foreach (CharacterBase character in runtime.Values)
                    NotifySafely(character.NotifyEquipmentBatchApplied);
            if (party != null) NotifySafely(party.EndEquipmentBatch);
        }
        if (!applied) return false;
        if (soldItems.Count > 0) GlobalValue.NotifySaleApplied();
        if (OnEquipmentChanged != null)
            foreach (Action handler in OnEquipmentChanged.GetInvocationList()) NotifySafely(handler);
        SaveType saveType = changed.Count > 0 ? SaveType.CharacterList : SaveType.None;
        if (soldItems.Count > 0) saveType |= SaveType.Inventory | SaveType.PlayerInfo;
        PlayerDataMgr.RequestSave(saveType);
        PlayerDataMgr.RequestImmediateSave();
        return true;
    }

    private static void NotifySafely(Action handler)
    {
        try { handler(); }
        catch (Exception exception) { PDebug.LogError(exception); }
    }
}

/// <summary>전체 보유 캐릭터의 장비 UID만 복사한 편집본입니다. 실제 CharData를 노출하지 않습니다.</summary>
public sealed class EquipmentEditSession
{
    internal readonly Dictionary<string, string[]> Originals;
    internal readonly Dictionary<string, string[]> Edits = new();
    internal readonly Dictionary<string, (string ItemId, int Price)> Sales = new();
    public bool IsClosed { get; private set; }

    public bool HasChanges
    {
        get
        {
            if (IsClosed) return false;
            if (Sales.Count > 0) return true;
            foreach (var entry in Edits)
                if (!EquipmentMgr.SameSlots(Originals[entry.Key], entry.Value)) return true;
            return false;
        }
    }

    public bool IsSalePending(string uid) => uid != null && Sales.ContainsKey(uid);

    public bool TryReserveSale(string uid, int confirmedPrice, out string message)
    {
        if (!ItemSaleMgr.TryGetSaleInfo(uid, this, out ItemData item, out _, out message)) return false;
        if (item.Price != confirmedPrice) { message = "판매가격이 변경되었습니다."; return false; }
        Sales.Add(uid, (item.ItemId, item.Price));
        return true;
    }

    internal EquipmentEditSession(Dictionary<string, string[]> originals)
    {
        Originals = originals;
        foreach (var entry in originals) Edits.Add(entry.Key, (string[])entry.Value.Clone());
    }

    public bool ContainsCharacter(string charId) => !IsClosed && charId != null && Edits.ContainsKey(charId);

    public string GetEquipmentUid(string charId, EquipType type)
    {
        if (!ContainsCharacter(charId) || type < EquipType.Weapon || type >= EquipType.Count) return "";
        return Edits[charId][(int)type];
    }

    public bool TryGetEquippedCharacter(string itemUid, out string charId)
    {
        charId = "";
        if (IsClosed || string.IsNullOrWhiteSpace(itemUid)) return false;
        foreach (var entry in Edits)
            foreach (string uid in entry.Value)
                if (uid == itemUid) { charId = entry.Key; return true; }
        return false;
    }

    public bool TryUnequipItem(string charId, EquipType type, string expectedItemUid, out string message)
    {
        message = "";
        if (!ContainsCharacter(charId) || string.IsNullOrEmpty(expectedItemUid) ||
            GetEquipmentUid(charId, type) != expectedItemUid)
        {
            message = "표시된 장비와 현재 편집 슬롯이 일치하지 않습니다.";
            return false;
        }
        Edits[charId][(int)type] = "";
        return true;
    }

    /// <summary>다른 캐릭터 장비 이동은 UI 확인 후 allowTransfer=true로 요청합니다.</summary>
    public bool TryEquipItem(string charId, EquipType type, string itemUid,
        bool allowTransfer, out string message)
    {
        message = "";
        if (!ContainsCharacter(charId) || type < EquipType.Weapon || type >= EquipType.Count)
        {
            message = "편집 대상 캐릭터 또는 슬롯이 유효하지 않습니다.";
            return false;
        }
        if (IsSalePending(itemUid)) { message = "판매 예약한 아이템은 장착할 수 없습니다."; return false; }
        if (!EquipmentMgr.TryResolveEquipment(itemUid, type, out _, out message)) return false;
        if (TryGetEquippedCharacter(itemUid, out string owner) && (owner == charId || !allowTransfer))
        {
            message = "이미 착용 중인 아이템입니다";
            return false;
        }
        // 한 UID를 참조하던 편집 슬롯을 모두 비워 중복 장착을 막는다.
        foreach (string[] slots in Edits.Values)
            for (int i = 0; i < slots.Length; i++)
                if (slots[i] == itemUid) slots[i] = "";
        Edits[charId][(int)type] = itemUid;
        return true;
    }

    public void Cancel() => Close();
    internal void Close() => IsClosed = true;
}
