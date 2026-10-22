using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterGachaOperation
{
    // Applied=true면 저장 실패여도 재추첨·재지급하지 않는다. 공통 저장 매니저가 재시도한다.
    public bool Applied, Saved;
    public string Message;
    public IReadOnlyList<CharacterGachaResult> Results;
}

/// <summary>뽑기 검증·메모리 확정·저장. 컴포넌트를 추가할 필요가 없는 공통 진입점.</summary>
public static class CharacterGachaMgr
{
    public static bool IsBusy { get; private set; }
    public static IReadOnlyList<CharacterData> GetPityTargets()
    {
        var master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
        return master == null ? new List<CharacterData>() : CharacterGachaEngine.GetTargets(master.GetCharacters());
    }

    public static string SelectedTargetId => string.IsNullOrEmpty(GlobalValue.GachaData.TargetCharacterId)
        ? GetPityTargets().FirstOrDefault()?.Id ?? "" : GlobalValue.GachaData.TargetCharacterId;

    public static int GetRemainingGoldDraws()
    {
        if (!TryGetClock(out _, out long now, out _)) return 0;
        var state = GlobalValue.GachaData;
        int used = state.GoldDrawDay == CharacterGachaEngine.GetGoldDrawDay(now) ? state.GoldDrawCount : 0;
        return Math.Max(0, GlobalConstants.CharacterGachaDailyGoldLimit - used);
    }

    public static bool TrySelectTarget(string id, out string message)
    {
        message = "";
        if (IsBusy || PlayerDataMgr.IsProgressBlocked) { message = "뽑기 또는 저장 처리를 기다려주세요."; return false; }
        if (!TryGetClock(out var player, out _, out message)) return false;
        if (player != null && (player.IsSaving || player.HasPendingSave))
        { message = "서버 저장 완료 후 다시 시도해주세요."; return false; }
        var master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
        if (master == null) { message = "캐릭터 마스터가 준비되지 않았습니다."; return false; }
        var state = GlobalValue.GachaData.Copy();
        // 자동 선택되어 보이는 기본 대상도 변경 페널티의 기준으로 사용한다.
        if (string.IsNullOrEmpty(state.TargetCharacterId)) state.TargetCharacterId = SelectedTargetId;
        if (!CharacterGachaEngine.TrySelectTarget(state, id, master.GetCharacters(), out var updated, out message)) return false;
        if (state.TargetCharacterId == id) return true;
        GlobalValue.ApplyGachaTarget(updated);
        PlayerDataMgr.RequestSave(SaveType.PlayerInfo);
        PlayerDataMgr.RequestImmediateSave();
        return true;
    }

    public static async Task<CharacterGachaOperation> DrawAsync(GachaCurrency currency, int count)
    {
        var operation = new CharacterGachaOperation();
        if (IsBusy || PlayerDataMgr.IsProgressBlocked)
        { operation.Message = "뽑기 또는 저장 처리를 기다려주세요."; return operation; }
        IsBusy = true;
        try
        {
            if (!TryGetClock(out var player, out long now, out string message))
            { operation.Message = message; return operation; }
            if (player != null && (player.IsSaving || player.HasPendingSave))
            { operation.Message = "서버 저장 완료 후 다시 시도해주세요."; return operation; }
            var master = UnityEngine.Object.FindFirstObjectByType<MasterDataMgr>();
            if (master == null) { operation.Message = "캐릭터 마스터가 준비되지 않았습니다."; return operation; }
            if (!CharacterGachaEngine.TryCreatePlan(master.GetCharacters(), GlobalValue.UserCharDataList,
                GlobalValue.GachaData, GlobalValue.UserGold, GlobalValue.RealMoney, currency, count, now,
                () => Math.Min((double)UnityEngine.Random.value, 0.9999999999999999), out var plan, out message,
                starFragments: GlobalValue.UserStarFragments))
            { operation.Message = message; return operation; }
            GlobalValue.ApplyGachaState(plan);
            operation.Applied = true;
            operation.Results = plan.Results.AsReadOnly();
            foreach (var result in plan.Results)
                if (result.StarFragments > 0)
                {
                    // 실제 재화는 ApplyGachaState에서 캐릭터·비용·천장과 함께 한 번만 확정된다.
                    PDebug.Log($"별의 조각 {result.StarFragments}개 획득");
                }
            operation.Saved = player == null || await player.SaveDataAsync(SaveType.CharacterList | SaveType.PlayerInfo);
            operation.Message = operation.Saved ? "뽑기 완료" : player.LastMessage;
            return operation;
        }
        catch (Exception exception)
        {
            PDebug.LogError(exception);
            operation.Message = operation.Applied
                ? "뽑기 결과는 적용되었습니다. 저장 상태를 확인해주세요."
                : "뽑기 데이터 또는 설정을 확인해주세요.";
            return operation;
        }
        finally { IsBusy = false; }
    }

    private static bool TryGetClock(out PlayerDataMgr player, out long now, out string message)
    {
        player = UnityEngine.Object.FindFirstObjectByType<PlayerDataMgr>();
        message = ""; now = 0;
        if (player != null && player.TryGetServerUnixTime(out now)) return true;
#if UNITY_EDITOR
        // 개발 진입은 실제 로그인 계정의 저장/일일 횟수와 분리한다.
        if (string.IsNullOrEmpty(GlobalValue.PlayFabId))
        { player = null; now = DateTimeOffset.UtcNow.ToUnixTimeSeconds(); return true; }
#endif
        message = "서버 시간을 확인할 수 없습니다. 다시 로그인해주세요.";
        return false;
    }
}
