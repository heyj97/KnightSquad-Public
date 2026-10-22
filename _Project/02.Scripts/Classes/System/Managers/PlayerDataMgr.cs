using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlayFab;
using PlayFab.DataModels;
using PlayFab.Json;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>UserData 3개 영역의 저장 예약, 미저장 스냅샷 및 재시도를 관리합니다.</summary>
public class PlayerDataMgr : G_Singleton<PlayerDataMgr>
{
    public GameSaveData CurrentData { get; private set; }
    public bool IsInitialized { get; private set; }
    public bool IsLoading { get; private set; }
    public bool IsSaving => m_SaveTask != null && !m_SaveTask.IsCompleted;
    public bool HasPendingSave => m_Pending != null;

    /// <summary>로그인 때 확인한 서버 UTC에 실제 경과 시간을 더한다. PC 시계 변경은 반영하지 않는다.</summary>
    public bool TryGetServerUnixTime(out long unixTime)
    {
        unixTime = m_HasServerTime ? m_ServerUnixTime + (long)Math.Max(0f, Time.realtimeSinceStartup - m_ServerTimeAnchor) : 0;
        return IsInitialized && m_HasServerTime;
    }
    public bool HasUnsavedChanges => IsInitialized && (m_Pending != null || CaptureJson() != m_SavedJson);
    public string LastMessage { get; private set; } = "";
    /// <summary>마지막 응답의 UserData 버전입니다. 조건부 저장에는 사용하지 않습니다.</summary>
    public uint UserDataVersion { get; private set; }
    public static bool IsProgressBlocked { get; private set; }
    public static float ResumeTimeScale { get; private set; } = 1f;

    private string m_SavedJson;
    private SaveGameRequest m_Pending;
    private SaveType m_RequestedTypes;
    private SaveType m_SendTypes;
    private bool m_DrainOnCompletion;
    private const string InventoryKey = "InventoryInfo", CharacterKey = "CharacterInfo", PlayerKey = "PlayerInfo";
    private Task<bool> m_SaveTask;
    private float m_NextSave, m_NextChangeProbe, m_NextAutoSave, m_FailureSince = -1f, m_PreviousTimeScale;
    private float m_RetryNotBefore;
    private int m_Failures, m_Epoch;
    private bool m_Terminal, m_Returning, m_OwnsPause, m_Notice;
    private bool m_QuitRequested, m_AllowQuit;
    private long m_LastActiveUnixTime, m_ServerUnixTime;
    private float m_ServerTimeAnchor;
    private bool m_HasServerTime;
    private string m_OfflineNotice;

    protected override void Awake()
    {
        base.Awake();
        if (!Application.isEditor) Application.wantsToQuit += OnWantsToQuit;
    }
    private void OnDestroy()
    {
        Application.wantsToQuit -= OnWantsToQuit;
    }
    private bool OnWantsToQuit()
    {
        if (m_AllowQuit || !IsInitialized) return true;
        SaveAndQuit();
        return false;
    }

    /// <summary>영역을 합산해 저장을 예약한다. 로그인 전/개발 진입에서는 매니저를 생성하지 않고 무시한다.</summary>
    public static void RequestSave(SaveType type = SaveType.All)
    {
        var manager = FindFirstObjectByType<PlayerDataMgr>();
        if (manager != null && manager.IsInitialized && !manager.m_Terminal && IsValidSaveType(type))
        {
            manager.m_RequestedTypes |= type;
        }
    }

    /// <summary>저장 버튼·챕터 종료 등에서 현재 변경 전체를 합산해 서버 저장을 예약한다.</summary>
    public static void RequestImmediateSave()
    {
        var manager = FindFirstObjectByType<PlayerDataMgr>();
        if (manager == null || !manager.IsInitialized || manager.m_Terminal) return;
        manager.m_SendTypes |= SaveType.All;
        manager.m_NextSave = Mathf.Max(Mathf.Min(manager.m_NextSave, Time.realtimeSinceStartup + GlobalConstants.ExplicitSaveCoalesceSeconds),
            manager.m_RetryNotBefore);
    }

    private void Update()
    {
        if (m_OwnsPause) Time.timeScale = 0f;
        if (m_Notice && !MessageBoxMgr.IsBlockingInput)
        {
            var box = FindFirstObjectByType<MessageBoxMgr>();
            if (box != null)
            {
                m_Notice = false;
                box.ShowMsg(LastMessage, "온라인 저장", MessageBoxType.Info, m_Terminal ? ReturnToTitle : (Action)null);
            }
        }
        // 로그인 UI의 로딩/성공 메시지와 겹치지 않도록 게임 진입 후 표시한다.
        if (IsInitialized && !IsLoading && m_OfflineNotice != null && HasRuntimeParty() && !MessageBoxMgr.IsBlockingInput)
        {
            var box = FindFirstObjectByType<MessageBoxMgr>();
            if (box != null)
            {
                box.ShowMsg(m_OfflineNotice, "오프라인 보상", MessageBoxType.Info, null);
                m_OfflineNotice = null;
            }
        }
        if (!IsInitialized || IsLoading || m_Returning || m_Terminal) return;
        float now = Time.realtimeSinceStartup;
        if (m_FailureSince >= 0f && now - m_FailureSince >= 120f && !IsProgressBlocked)
        {
            BlockProgress();
            LastMessage = "서버 저장이 지연되어 진행을 일시정지했습니다. 연결이 복구되면 자동으로 재개합니다.";
            m_Notice = true;
        }
        // 3초 확인은 전송하지 않는다. 전송 중 새 변경도 예약 목록에만 남긴다.
        if (now >= m_NextChangeProbe)
        {
            m_NextChangeProbe = now + GlobalConstants.SaveChangeProbeIntervalSeconds;
            m_RequestedTypes |= GetChangedTypes();
        }
        if (IsSaving) return;
        if (m_Pending != null)
        {
            if (now >= m_RetryNotBefore) _ = StartSaveLoop();
            return;
        }
        if (m_SendTypes != SaveType.None)
        {
            if (now >= m_NextSave) _ = StartSaveLoop();
            return;
        }
        if (now >= m_NextAutoSave)
        {
            m_NextAutoSave = now + GlobalConstants.AutoSaveIntervalSeconds;
            StampActivityTime(); // 변화 없는 접속 시간도 오프라인 보상에서 제외한다.
            m_SendTypes |= m_RequestedTypes | GetChangedTypes();
            if (m_SendTypes != SaveType.None) _ = StartSaveLoop();
        }
    }

    public async Task<bool> InitializeAsync(string playFabId)
    {
        if (IsLoading || IsSaving || HasRuntimeParty() || string.IsNullOrWhiteSpace(playFabId))
            return Fail("게임을 종료한 뒤 다시 로그인해주세요.");
        IsInitialized = false;
        m_Pending = null;
        m_RequestedTypes = SaveType.None;
        m_SendTypes = SaveType.None;
        m_DrainOnCompletion = false;
        m_HasServerTime = false;
        m_OfflineNotice = null;
        m_Epoch++;
        return await LoadCoreAsync(playFabId);
    }

    /// <summary>실행 중인 파티에는 부분 적용하지 않습니다. 타이틀에서만 사용합니다.</summary>
    public Task<bool> LoadDataAsync()
    {
        if (!IsInitialized || IsLoading || IsSaving || m_Pending != null || HasRuntimeParty())
            return Task.FromResult(Fail("타이틀로 돌아간 뒤 최신 데이터를 불러와주세요."));
        if (HasUnsavedChanges) return Task.FromResult(Fail("미저장 변경사항이 있습니다. 먼저 저장하거나 타이틀로 돌아가주세요."));
        return LoadCoreAsync(GlobalValue.PlayFabId);
    }

    /// <summary>개발용 데이터가 이전 로그인 계정에 자동 저장되지 않게 분리합니다.</summary>
    public void EnterDevelopmentMode()
    {
        m_HasServerTime = false;
        m_LastActiveUnixTime = 0;
        m_OfflineNotice = null;
        m_Epoch++;
        IsInitialized = false;
        m_Pending = null;
        m_RequestedTypes = SaveType.None;
        m_SendTypes = SaveType.None;
        m_DrainOnCompletion = false;
        CurrentData = null;
        m_SavedJson = null;
        m_Terminal = false;
        m_Notice = false;
        m_FailureSince = -1f;
        ReleasePause();
        PlayFabClientAPI.ForgetAllCredentials();
    }

    private async Task<bool> LoadCoreAsync(string playFabId)
    {
        IsLoading = true;
        try
        {
            var reply = await LoadOnlineAsync();
            if (reply.Status != "Success") return Fail(reply.Message);
            var data = JsonUtility.FromJson<GameSaveData>(reply.Json);
            NormalizeEmptyCheckpoint(data);
            if (!Validate(data, out string reason))
            {
                PDebug.LogError("[로드 검증 실패: 메모리 적용 전] " + reason);
                return Fail("서버 저장 데이터를 확인해주세요. " + reason + " 기존 데이터는 변경하지 않았습니다.");
            }
            ApplyToMemory(Clone(data), playFabId);
            CurrentData = Clone(data);
            m_LastActiveUnixTime = data.Progress.LastActiveUnixTime;
            m_HasServerTime = false;
            m_OfflineNotice = null;
            UserDataVersion = reply.UserDataVersion;
            m_SavedJson = CaptureJson();
            m_RequestedTypes = SaveType.None;
            m_SendTypes = SaveType.None;
            m_RetryNotBefore = 0f;
            m_Terminal = false;
            m_FailureSince = -1f;
            m_Failures = 0;
            m_Notice = false;
            ReleasePause();
            m_NextSave = float.PositiveInfinity;
            m_NextChangeProbe = Time.realtimeSinceStartup + GlobalConstants.SaveChangeProbeIntervalSeconds;
            m_NextAutoSave = Time.realtimeSinceStartup + GlobalConstants.AutoSaveIntervalSeconds;
            // 골드와 기준 시각은 같은 PlayerInfo 요청으로 확정한다.
            // 실패하면 게임 진입을 막고 다음 로그인에서 서버 저장본을 다시 읽는다.
            if (!await PrepareOfflineRewardAsync(data))
            {
                IsInitialized = false;
                m_Notice = false;
                return false;
            }
            IsInitialized = true;
            LastMessage = "";
            return true;
        }
        catch (Exception exception)
        {
            PDebug.LogError("온라인 데이터 로드 실패: " + exception.GetType().Name);
            return Fail("서버 데이터를 불러오지 못했습니다. PlayFab 연결과 저장 데이터를 확인해주세요.");
        }
        finally { IsLoading = false; }
    }

    private async Task<bool> PrepareOfflineRewardAsync(GameSaveData loaded)
    {
        DateTime serverTime;
        try
        {
            var source = new TaskCompletionSource<PlayFab.ClientModels.GetTimeResult>();
            PlayFabClientAPI.GetTime(new PlayFab.ClientModels.GetTimeRequest(),
                result => source.TrySetResult(result), error =>
                {
                    LogPlayFabFailure("Client/GetTime", error);
                    source.TrySetException(new InvalidOperationException(error.Error.ToString()));
                });
            serverTime = (await WaitRequest(source)).Time;
        }
        catch (Exception exception)
        {
            PDebug.LogWarning("오프라인 보상 시간 확인 보류: " + exception.GetType().Name);
            m_OfflineNotice = "서버 시간을 확인하지 못해 오프라인 보상을 보류했습니다. 다음 로그인 시 다시 확인합니다.";
            return true; // 기존 시각을 유지하고 일반 플레이는 허용한다.
        }
        m_ServerUnixTime = new DateTimeOffset(DateTime.SpecifyKind(serverTime, DateTimeKind.Utc)).ToUnixTimeSeconds();
        m_ServerTimeAnchor = Time.realtimeSinceStartup;
        m_HasServerTime = true;
        var stage = loaded.Progress.StageData;
        ulong gold = OfflineRewardMgr.CalculateGold(m_LastActiveUnixTime, m_ServerUnixTime,
            stage.CurChapter, stage.CurStage, out long elapsed, out int applied);
        gold = Math.Min(gold, ulong.MaxValue - GlobalValue.UserGold);
        GlobalValue.AddUserGold(gold);
        StampActivityTime();
        m_SendTypes = SaveType.PlayerInfo;
        m_NextSave = Time.realtimeSinceStartup;
        if (!await StartSaveLoop())
            return Fail("오프라인 보상 및 활동 시각을 저장하지 못했습니다. 다시 로그인해주세요.\n" + LastMessage);
        if (gold > 0) m_OfflineNotice = OfflineRewardMgr.CreateMessage(elapsed, applied, gold);
        return true;
    }

    private void StampActivityTime()
    {
        if (!m_HasServerTime) return;
        long now = m_ServerUnixTime + (long)Math.Max(0f, Time.realtimeSinceStartup - m_ServerTimeAnchor);
        m_LastActiveUnixTime = Math.Max(m_LastActiveUnixTime, now);
    }

    /// <summary>중복 호출은 한 루프로 합산한다. 반환 true는 예약한 영역의 저장 완료를 뜻한다.</summary>
    public Task<bool> SaveDataAsync(SaveType type = SaveType.All)
    {
        if (!IsInitialized || IsLoading || m_Terminal || m_Returning || !IsValidSaveType(type))
            return Task.FromResult(Fail("온라인 저장을 진행할 수 없습니다."));
        m_RequestedTypes |= type;
        m_SendTypes |= type;
        m_NextSave = Time.realtimeSinceStartup;
        return StartSaveLoop();
    }

    private Task<bool> StartSaveLoop()
    {
        if (IsSaving) return m_SaveTask;
        m_SaveTask = SaveLoopAsync(m_Epoch);
        return m_SaveTask;
    }

    private static bool IsValidSaveType(SaveType type) => type != SaveType.None && (type & ~SaveType.All) == SaveType.None;

    private async Task<bool> SaveLoopAsync(int epoch)
    {
        try
        {
            while (epoch == m_Epoch && !m_Returning)
            {
                if (m_Pending == null && m_SendTypes != SaveType.None)
                {
                    StampActivityTime();
                    // 부분 저장도 골드/시각이 있는 PlayerInfo를 함께 기록한다.
                    if (m_HasServerTime) m_SendTypes |= SaveType.PlayerInfo;
                }
                // 변경이 없으면 재시도 대기를 기다리거나 서버를 호출하지 않는다.
                if (m_Pending == null && (m_SendTypes & GetChangedTypes()) == SaveType.None)
                { m_SendTypes = SaveType.None; m_NextSave = float.PositiveInfinity; return true; }
                while (Time.realtimeSinceStartup < m_RetryNotBefore ||
                    (m_Pending == null && m_NextSave != float.PositiveInfinity && Time.realtimeSinceStartup < m_NextSave))
                {
                    await Task.Delay(100);
                    if (epoch != m_Epoch || m_Returning) return false;
                }
                if (m_Pending == null)
                {
                    StampActivityTime();
                    string json = CaptureJson();
                    var data = JsonUtility.FromJson<GameSaveData>(json);
                    if (!Validate(data, out string invalidReason))
                    {
                        PDebug.LogError("[저장 검증 실패] " + invalidReason);
                        return Terminal("저장 데이터가 유효하지 않습니다. " + invalidReason);
                    }
                    SaveType changed = GetChangedTypes(json);
                    SaveType types = m_SendTypes == SaveType.All && changed != SaveType.None
                        ? SaveType.All : m_SendTypes & changed;
                    m_SendTypes = SaveType.None;
                    m_NextSave = float.PositiveInfinity;
                    if (types == SaveType.None) return true;
                    var baseline = JsonUtility.FromJson<GameSaveData>(m_SavedJson);
                    MergeAreas(baseline, data, types);
                    if (!Validate(baseline, out string areaReason))
                    {
                        // 예: 장비 장착 전에 획득한 인벤토리가 아직 서버에 없는 경우.
                        SaveType originalTypes = types;
                        types |= changed;
                        baseline = JsonUtility.FromJson<GameSaveData>(m_SavedJson);
                        MergeAreas(baseline, data, types);
                        if (!Validate(baseline, out string expandedReason))
                        {
                            PDebug.LogError($"[저장 영역 검증 실패] {types}: {expandedReason}");
                            return Terminal("저장 데이터 연결을 확인해주세요. " + expandedReason);
                        }
                        PDebug.LogWarning($"[저장 영역 추가] {originalTypes} → {types}: {areaReason}");
                    }
                    m_Pending = new SaveGameRequest { Json = json, Types = types,
                        Values = PackAreas(data, types), RequestId = Guid.NewGuid().ToString("N") };
                }
                // 실패 시 이 스냅샷/전송값을 그대로 재시도한다. 저장 중 변경은 다음 요청으로 남긴다.
                var sent = m_Pending;
                uint version = await WriteUserDataAsync(sent.Values);
                if (epoch != m_Epoch || m_Returning) return false;
                var baselineData = JsonUtility.FromJson<GameSaveData>(m_SavedJson);
                MergeAreas(baselineData, JsonUtility.FromJson<GameSaveData>(sent.Json), sent.Types);
                m_SavedJson = JsonUtility.ToJson(baselineData);
                CurrentData = Clone(baselineData);
                UserDataVersion = version;
                m_Pending = null;
                m_RequestedTypes &= ~sent.Types;
                // 일반 진행은 다음 30초 저장으로 남긴다. 새 명시 요청/정상 종료만 후속 전송한다.
                if (m_DrainOnCompletion) m_SendTypes |= GetChangedTypes();
                m_FailureSince = -1f;
                m_Failures = 0;
                m_RetryNotBefore = 0f;
                m_Notice = false;
                if (!m_QuitRequested) ReleasePause();
                LastMessage = "저장 성공";
                m_NextAutoSave = Time.realtimeSinceStartup + GlobalConstants.AutoSaveIntervalSeconds;
            }
            return false;
        }
        catch (Exception exception)
        {
            if (epoch != m_Epoch || m_Returning) return false;
            bool rateLimited = exception.Message.Contains("RateLimitExceeded") || exception.Message.Contains("RateExceeded") ||
                exception.Message.Contains("RequestLimitExceeded");
            if (!rateLimited && (exception is InvalidDataException || exception.Message.Contains("Limit") || exception.Message.Contains("TooLarge") ||
                exception.Message.Contains("NotAuthorized") || exception.Message.Contains("TotalDataSizeExceeded") ||
                exception.Message.Contains("DataLengthExceeded")))
                return Terminal("PlayFab 저장 한도 또는 권한 설정을 확인해주세요.\n" + exception.Message);
            PDebug.LogWarning("온라인 저장 재시도 대기: " + exception.GetType().Name);
            if (m_FailureSince < 0f) m_FailureSince = Time.realtimeSinceStartup;
            m_Failures++;
            float delay = Mathf.Min(60f, 5f * Mathf.Pow(2, Mathf.Min(m_Failures - 1, 4)));
            if (exception is PlayFabStorageException apiError && apiError.RetryAfterSeconds.HasValue)
                delay = Mathf.Max(delay, apiError.RetryAfterSeconds.Value);
            m_RetryNotBefore = Time.realtimeSinceStartup + delay;
            m_NextSave = m_RetryNotBefore;
            LastMessage = "저장하지 못했습니다. 변경사항은 메모리에 유지되며 자동으로 다시 시도합니다.";
            if (m_Failures == 1) m_Notice = true;
            return false;
        }
    }

    private SaveType GetChangedTypes(string json = null)
    {
        if (m_SavedJson == null) return SaveType.All;
        var current = AreaJson(JsonUtility.FromJson<GameSaveData>(json ?? CaptureJson()));
        var saved = AreaJson(JsonUtility.FromJson<GameSaveData>(m_SavedJson));
        SaveType types = SaveType.None;
        if (current[InventoryKey] != saved[InventoryKey]) types |= SaveType.Inventory;
        if (current[CharacterKey] != saved[CharacterKey]) types |= SaveType.CharacterList;
        if (current[PlayerKey] != saved[PlayerKey]) types |= SaveType.PlayerInfo;
        return types;
    }

    private static Dictionary<string, string> AreaJson(GameSaveData data) => new Dictionary<string, string>
    {
        [InventoryKey] = JsonUtility.ToJson(data.Inventory),
        [CharacterKey] = JsonUtility.ToJson(new CharacterListSaveData { Uid = data.User.Uid,
            UserCharacters = data.Character.UserCharacters }),
        [PlayerKey] = JsonUtility.ToJson(new PlayerInfoSaveData { Uid = data.User.Uid, User = data.User,
            Progress = data.Progress, PartyCharacterIds = data.Character.PartyCharacterIds })
    };

    private static Dictionary<string, string> PackAreas(GameSaveData data, SaveType types)
    {
        var values = AreaJson(data);
        if ((types & SaveType.Inventory) == 0) values.Remove(InventoryKey);
        if ((types & SaveType.CharacterList) == 0) values.Remove(CharacterKey);
        if ((types & SaveType.PlayerInfo) == 0) values.Remove(PlayerKey);
        foreach (string key in values.Keys.ToArray()) values[key] = PackJson(values[key]);
        return values;
    }

    private static void MergeAreas(GameSaveData target, GameSaveData source, SaveType types)
    {
        if ((types & SaveType.Inventory) != 0) target.Inventory = source.Inventory;
        if ((types & SaveType.CharacterList) != 0) target.Character.UserCharacters = source.Character.UserCharacters;
        if ((types & SaveType.PlayerInfo) != 0)
        {
            target.User = source.User;
            target.Progress = source.Progress;
            target.Character.PartyCharacterIds = source.Character.PartyCharacterIds;
        }
    }

    private static EntityKey PlayerEntity()
    {
        var auth = PlayFabSettings.staticPlayer;
        if (string.IsNullOrEmpty(auth.EntityId) || auth.EntityType != "title_player_account")
            throw new InvalidOperationException("로그인 Entity 인증 정보가 없습니다.");
        return new EntityKey { Id = auth.EntityId, Type = auth.EntityType };
    }

    private static async Task<T> WaitRequest<T>(TaskCompletionSource<T> source)
    {
        if (await Task.WhenAny(source.Task, Task.Delay(15000)) != source.Task)
        { source.TrySetCanceled(); throw new TimeoutException(); }
        return await source.Task;
    }

    private static Task<GetObjectsResponse> ReadObjectsAsync()
    {
        var source = new TaskCompletionSource<GetObjectsResponse>();
        PlayFabDataAPI.GetObjects(new GetObjectsRequest { Entity = PlayerEntity(), EscapeObject = true },
            result => source.TrySetResult(result), error =>
            {
                LogPlayFabFailure("Object/GetObjects", error);
                source.TrySetException(new InvalidOperationException(error.Error.ToString()));
            });
        return WaitRequest(source);
    }

    // 요청 본문과 인증 토큰은 출력하지 않는다. 한도 원인 확인용 응답 정보만 남긴다.
    private static void LogPlayFabFailure(string api, PlayFabError error)
    {
        var log = new StringBuilder("[PlayFab 저장/로드 오류] ").Append(api);
        log.Append("\nHTTP: ").Append(error.HttpCode).Append(" / Error: ").Append(error.Error);
        log.Append("\nErrorMessage: ").Append(error.ErrorMessage);
        log.Append("\nErrorDetails: ").Append(error.ErrorDetails == null || error.ErrorDetails.Count == 0
            ? "(제공되지 않음)" : PlayFabSimpleJson.SerializeObject(error.ErrorDetails));
        log.Append("\nRetryAfterSeconds: ").Append(error.RetryAfterSeconds.HasValue
            ? error.RetryAfterSeconds.Value.ToString() : "(제공되지 않음)");
        PDebug.LogWarning(log.ToString());
    }

    private static string StoredJson(GetObjectsResponse result)
    {
        if (result.Objects == null || !result.Objects.TryGetValue("GameSaveData", out var item)) return null;
        string json = item.EscapedDataObject ?? PlayFabSimpleJson.SerializeObject(item.DataObject);
        return UnpackJson(json);
    }

    private static string UnpackJson(string json)
    {
        var envelope = JsonUtility.FromJson<SaveStorageObject>(json);
        if (string.IsNullOrEmpty(envelope.Encoding)) return json; // 이전 일반 JSON 객체도 지원
        if (envelope.Encoding != "gzip-base64-v1") throw new InvalidDataException("Unknown save encoding.");
        using var input = new MemoryStream(Convert.FromBase64String(envelope.Payload));
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        byte[] buffer = new byte[4096];
        int count;
        while ((count = gzip.Read(buffer, 0, buffer.Length)) > 0)
        {
            if (output.Length + count > 60000) throw new InvalidDataException("Save too large.");
            output.Write(buffer, 0, count);
        }
        return Encoding.UTF8.GetString(output.ToArray());
    }

    private static string PackJson(string json)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        if (bytes.Length > 60000) throw new InvalidDataException("Save too large.");
        using var output = new MemoryStream();
        using (var gzip = new GZipStream(output, System.IO.Compression.CompressionLevel.Optimal, true)) gzip.Write(bytes, 0, bytes.Length);
        return JsonUtility.ToJson(new SaveStorageObject { Encoding = "gzip-base64-v1", Payload = Convert.ToBase64String(output.ToArray()) });
    }

    private sealed class PlayFabStorageException : Exception
    {
        public readonly uint? RetryAfterSeconds;
        public PlayFabStorageException(PlayFabError error) : base(error.Error.ToString())
            => RetryAfterSeconds = error.RetryAfterSeconds;
    }

    private static Task<PlayFab.ClientModels.GetUserDataResult> ReadUserDataAsync()
    {
        var source = new TaskCompletionSource<PlayFab.ClientModels.GetUserDataResult>();
        PlayFabClientAPI.GetUserData(new PlayFab.ClientModels.GetUserDataRequest
            { Keys = new List<string> { PlayerKey, InventoryKey, CharacterKey, "GameSaveData" } },
            result => source.TrySetResult(result), error =>
            {
                LogPlayFabFailure("Client/GetUserData", error);
                source.TrySetException(new PlayFabStorageException(error));
            });
        return WaitRequest(source);
    }

    private static Task<uint> WriteUserDataAsync(Dictionary<string, string> values)
    {
        var source = new TaskCompletionSource<uint>();
        PlayFabClientAPI.UpdateUserData(new PlayFab.ClientModels.UpdateUserDataRequest
            { Data = values, Permission = PlayFab.ClientModels.UserDataPermission.Private },
            result => source.TrySetResult(result.DataVersion), error =>
            {
                LogPlayFabFailure("Client/UpdateUserData", error);
                foreach (var entry in values)
                    PDebug.LogWarning($"[PlayFab 저장 크기] {entry.Key}: {Encoding.UTF8.GetByteCount(entry.Value)} bytes (UTF-8)");
                source.TrySetException(new PlayFabStorageException(error));
            });
        return WaitRequest(source);
    }

    private async Task<OnlineSaveReply> LoadOnlineAsync()
    {
        var result = await ReadUserDataAsync();
        var records = result.Data;
        int count = new[] { PlayerKey, InventoryKey, CharacterKey }.Count(key => records != null && records.ContainsKey(key));
        GameSaveData data;
        if (count == 3)
        {
            var player = JsonUtility.FromJson<PlayerInfoSaveData>(UnpackJson(records[PlayerKey].Value));
            var characters = JsonUtility.FromJson<CharacterListSaveData>(UnpackJson(records[CharacterKey].Value));
            var inventory = JsonUtility.FromJson<InventorySaveData>(UnpackJson(records[InventoryKey].Value));
            if (player == null || characters == null || inventory == null || player.SaveDataVersion != 1 ||
                characters.SaveDataVersion != 1 || inventory.SaveDataVersion != 1 || player.User == null ||
                player.Uid != player.User.Uid || characters.Uid != player.Uid || inventory.Uid != player.Uid)
                return InvalidLoadReply("영역의 형식 버전 또는 사용자 UID가 일치하지 않습니다.");
            data = new GameSaveData { User = player.User, Progress = player.Progress, Inventory = inventory,
                Character = new CharacterSaveData { Uid = characters.Uid, UserCharacters = characters.UserCharacters,
                    PartyCharacterIds = player.PartyCharacterIds } };
            NormalizeEmptyCheckpoint(data);
            if (!Validate(data, out string reason)) return InvalidLoadReply(reason);
            return Success(data, result.DataVersion);
        }
        // 일부 키만 있는 저장본은 과거 데이터와 임의로 섞거나 신규 데이터로 덮어쓰지 않는다.
        if (count != 0) return InvalidLoadReply($"새 저장 영역 3개 중 {count}개만 존재합니다.");
        string legacy = StoredJson(await ReadObjectsAsync());
        bool fromLegacyUserData = false;
        if (legacy == null && records != null && records.TryGetValue("GameSaveData", out var old))
        {
            legacy = UnpackJson(old.Value);
            fromLegacyUserData = true;
        }
        data = legacy == null ? CreateInitialData() : JsonUtility.FromJson<GameSaveData>(legacy);
        if (fromLegacyUserData) RepairLegacy(data);
        NormalizeEmptyCheckpoint(data);
        if (!Validate(data, out string legacyReason)) return InvalidLoadReply(legacyReason);
        uint version = await WriteUserDataAsync(PackAreas(data, SaveType.All));
        // 기존 키는 삭제하지 않는다.
        return Success(data, version);
    }

    private static OnlineSaveReply Success(GameSaveData data, uint version)
        => new OnlineSaveReply { Status = "Success", Json = JsonUtility.ToJson(data), UserDataVersion = version };

    private static OnlineSaveReply InvalidLoadReply(string reason)
    {
        PDebug.LogError("[로드 검증 실패] " + reason);
        return new OnlineSaveReply { Status = "InvalidData",
            Message = "저장 데이터를 확인해주세요. " + reason + " 기존 데이터를 변경하지 않았습니다." };
    }

    // Unity의 인라인 직렬화는 null 커스텀 클래스를 빈 객체로 표현할 수 있다.
    // 기록이 전혀 없는 기본 객체만 초기 상태로 처리한다. 일부 손상된 실제 기록은 건드리지 않는다.
    private static void NormalizeEmptyCheckpoint(GameSaveData data)
    {
        if (data?.User != null && data.User.Gacha == null) data.User.Gacha = new CharacterGachaSaveData();
        var stage = data?.Progress?.StageData;
        if (stage == null) return;
        var run = stage.Run;
        if (run != null && !((run.Version == 0 || run.Version == 1) && string.IsNullOrEmpty(run.AttemptId) &&
            run.Chapter == 0 && run.CheckpointStage == 0 && run.State == ChapterRunState.InProgress && run.EarnedGold == 0 &&
            (run.EntryHp == null || run.EntryHp.Count == 0) && (run.Receipts == null || run.Receipts.Count == 0) &&
            (run.EarnedItems == null || run.EarnedItems.Count == 0))) return;
        stage.Run = new ChapterRunSaveData { AttemptId = Guid.NewGuid().ToString("N"), Chapter = stage.CurChapter,
            CheckpointStage = stage.CurStage };
    }

    private GameSaveData CreateInitialData()
    {
        var dev = FindFirstObjectByType<GameStartBtn_Dev>(FindObjectsInactive.Include);
        if (dev == null || dev.cDatas == null || dev.cDatas.Length < 4 || dev.cDatas.Any(item => item == null) ||
            dev.idata1 == null || dev.idata2 == null || dev.idata3 == null)
            throw new InvalidOperationException("개발 초기 데이터 참조를 확인해주세요.");
        string uid = CreateUid(UidType.User);
        var data = new GameSaveData
        {
            Revision = 1,
            User = new UserSaveData { Uid = uid, NickName = "테스트", UserGold = 10000, RealMoney = 1000 },
            Progress = new ProgressSaveData { Uid = uid, AtkPowerRankScore = 100, DummyRankScore = 1000,
                PartyHpLv = 1, PartyAtkLv = 1, PartyAtkSpdLv = 1, PartyDefLv = 1, PartyCritChanceLv = 1, PartyCritDmgLv = 1 },
            Character = new CharacterSaveData { Uid = uid, UserCharacters = dev.cDatas.Select(item => new CharData(item)).ToList(),
                PartyCharacterIds = new List<string> { dev.cDatas[1].Id, dev.cDatas[2].Id, dev.cDatas[0].Id, dev.cDatas[3].Id } },
            Inventory = new InventorySaveData { Uid = uid, UserItems = new List<ItemData>
                { new ItemData(dev.idata1), new ItemData(dev.idata2), new ItemData(dev.idata3) } }
        };
        NormalizeEmptyCheckpoint(data);
        return data;
    }

    private static void RepairLegacy(GameSaveData data)
    {
        if (data == null || data.DataVersion > 1 || data.User == null || data.Character == null || data.Inventory == null || data.Progress == null) return;
        data.DataVersion = 1;
        data.Progress.Uid = data.Character.Uid = data.Inventory.Uid = data.User.Uid;
        if (data.Character.UserCharacters != null)
            foreach (var character in data.Character.UserCharacters)
                if (character != null) character.CharDuplicateCount = Math.Max(0, character.CharDuplicateCount);
        var dev = FindFirstObjectByType<GameStartBtn_Dev>(FindObjectsInactive.Include);
        if (data.Inventory.UserItems == null) return;
        foreach (var item in data.Inventory.UserItems)
        {
            if (item == null || item.Price != 0 || dev == null) continue;
            var rule = new[] { dev.idata1, dev.idata2, dev.idata3 }.OfType<EquipmentData>().FirstOrDefault(value => value.Id == item.ItemId);
            if (rule != null) item.Price = GlobalConstants.CreateEquipmentSalePrice(rule.Rarity);
        }
    }

    private bool Terminal(string message)
    {
        m_Terminal = true;
        BlockProgress();
        LastMessage = message;
        m_Notice = true;
        return false;
    }
    private bool Fail(string message) { LastMessage = message; return false; }
    private void BlockProgress()
    {
        IsProgressBlocked = true;
        if (!m_OwnsPause)
        {
            m_PreviousTimeScale = InformationPanelMgr.IsMenuPaused
                ? InformationPanelMgr.TimeScaleBeforeMenu : Time.timeScale;
            ResumeTimeScale = m_PreviousTimeScale;
            m_OwnsPause = true;
        }
        Time.timeScale = 0f;
        FindFirstObjectByType<TargetSelMgr>()?.ClearTargets();
        foreach (var character in FindObjectsByType<CharacterBase>(FindObjectsSortMode.None))
            character.CancelSkillSelectionForMenu();
    }
    private void ReleasePause()
    {
        IsProgressBlocked = false;
        if (!m_OwnsPause) return;
        m_OwnsPause = false;
        Time.timeScale = InformationPanelMgr.IsMenuPaused ? 0f : m_PreviousTimeScale;
    }

    public void ReturnToTitle()
    {
        if (!m_Returning) StartCoroutine(ReturnToTitleRoutine());
    }
    private IEnumerator ReturnToTitleRoutine()
    {
        m_Returning = true;
        // 저장 오류에 의한 복귀는 현재 전환을 마친 뒤 실행한다.
        while (SceneChangeMgr.IsTransitioning) yield return null;
        if (!SceneChangeMgr.SceneFade(ReturnToTitleScenes(), succeeded =>
        {
            m_Returning = false;
            if (!succeeded) PDebug.LogError("TitleScene 복귀에 실패했습니다.");
        }))
            m_Returning = false;
    }

    private IEnumerator ReturnToTitleScenes()
    {
        m_HasServerTime = false;
        m_OfflineNotice = null;
        m_Returning = true;
        m_Epoch++;
        IsInitialized = false;
        m_Pending = null;
        m_RequestedTypes = SaveType.None;
        m_SendTypes = SaveType.None;
        m_DrainOnCompletion = false;
        // Detached gameplay singleton roots must be rebuilt; preserve authentication and master registries.
        foreach (var behaviour in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (behaviour == null || behaviour.gameObject.scene.name != "DontDestroyOnLoad" ||
                behaviour is PlayerDataMgr || behaviour is NetworkMgr || behaviour is MasterDataMgr) continue;
            for (Type type = behaviour.GetType(); type != null; type = type.BaseType)
                if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(G_Singleton<>))
                { Destroy(behaviour.gameObject); break; }
        }
        yield return null;
        GlobalValue.ClearVariable();
        PlayFabClientAPI.ForgetAllCredentials();
        CurrentData = null;
        m_SavedJson = null;
        UserDataVersion = 0;
        m_Terminal = false;
        m_Notice = false;
        yield return SceneManager.LoadSceneAsync("TitleScene", LoadSceneMode.Single);
        ReleasePause();
        Time.timeScale = 1f;
    }

    /// <summary>종료 버튼 연결용. 저장 완료 전에는 종료하지 않습니다.</summary>
    public async void SaveAndQuit()
    {
        if (m_QuitRequested) return;
        m_QuitRequested = true;
        m_DrainOnCompletion = true;
        BlockProgress();
        if (IsInitialized && !await SaveDataAsync(SaveType.All))
        {
            m_QuitRequested = false;
            m_DrainOnCompletion = false;
            if (!m_Terminal) ReleasePause();
            ItemSaleMgr.ShowMessage(LastMessage);
            return;
        }
        m_AllowQuit = true;
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private static bool HasRuntimeParty() => FindFirstObjectByType<PartyMgr>(FindObjectsInactive.Include) != null;
    private static GameSaveData Clone(GameSaveData data) => JsonUtility.FromJson<GameSaveData>(JsonUtility.ToJson(data));
    private string CaptureJson()
    {
        var data = new GameSaveData
        {
            User = new UserSaveData { Uid = GlobalValue.Uid, NickName = GlobalValue.NickName,
                UserGold = GlobalValue.UserGold, RealMoney = GlobalValue.RealMoney,
                StarFragments = GlobalValue.UserStarFragments, Gacha = GlobalValue.GachaData.Copy() },
            Progress = new ProgressSaveData { Uid = GlobalValue.Uid, LastActiveUnixTime = m_LastActiveUnixTime,
                AtkPowerRankScore = GlobalValue.AttackPowerRankScore,
                DummyRankScore = GlobalValue.DummyDamageRankScore, PartyAtkLv = GlobalValue.PartyAttackPowerLv,
                PartyHpLv = GlobalValue.PartyMaxHpLv, PartyDefLv = GlobalValue.PartyDefenseLv,
                PartyAtkSpdLv = GlobalValue.PartyAttackSpeedLv, PartyCritChanceLv = GlobalValue.PartyCriticalChanceLv,
                PartyCritDmgLv = GlobalValue.PartyCriticalDamageMultiplierLv, StageData = GlobalValue.CurStageData },
            Character = new CharacterSaveData { Uid = GlobalValue.Uid,
                UserCharacters = new List<CharData>(GlobalValue.UserCharDataList), PartyCharacterIds = new List<string>(GlobalValue.PartyCharIdList) },
            Inventory = new InventorySaveData { Uid = GlobalValue.Uid, UserItems = new List<ItemData>(GlobalValue.UserItemDataList) }
        };
        // Serialize synchronously before await: pending payload contains no shared mutable references.
        return JsonUtility.ToJson(data);
    }
    private static void ApplyToMemory(GameSaveData data, string playFabId)
    {
        GlobalValue.ClearVariable();
        GlobalValue.SetPlayFabId(playFabId);
        GlobalValue.SetUid(data.User.Uid);
        GlobalValue.SetNickName(data.User.NickName);
        GlobalValue.SetUserGold(data.User.UserGold);
        GlobalValue.SetUserStarFragments(data.User.StarFragments);
        GlobalValue.SetRealMoney(data.User.RealMoney);
        GlobalValue.SetGachaData(data.User.Gacha);
        GlobalValue.SetAtkPowerRankScore(data.Progress.AtkPowerRankScore);
        GlobalValue.SetDummyRankScore(data.Progress.DummyRankScore);
        GlobalValue.SetUserCharDataList(data.Character.UserCharacters);
        GlobalValue.SetUserItemDataList(data.Inventory.UserItems);
        GlobalValue.CommitPartyComposition(data.Character.PartyCharacterIds, false);
        GlobalValue.SetPartyGrowthLevels(data.Progress.PartyHpLv, data.Progress.PartyAtkLv,
            data.Progress.PartyAtkSpdLv, data.Progress.PartyDefLv, data.Progress.PartyCritChanceLv, data.Progress.PartyCritDmgLv);
        GlobalValue.SetCurStageData(data.Progress.StageData);
    }
    private static bool Validate(GameSaveData data) => Validate(data, out _);

    private static bool Invalid(string reason, out string message) { message = reason; return false; }

    private static bool Validate(GameSaveData data, out string message)
    {
        message = "";
        if (data?.User != null && !CharacterGachaEngine.IsValidState(data.User.Gacha))
            return Invalid("뽑기 천장 또는 일일 횟수가 유효하지 않습니다.", out message);
        if (data == null || data.DataVersion != 1 || data.Revision < 0 || data.User == null ||
            string.IsNullOrWhiteSpace(data.User.Uid) || data.Progress?.StageData == null ||
            data.Character?.UserCharacters == null || data.Character.PartyCharacterIds?.Count != 4 || data.Inventory?.UserItems == null)
            return Invalid("필수 저장 필드 또는 형식 버전이 잘못되었습니다.", out message);
        if (data.Progress.Uid != data.User.Uid || data.Character.Uid != data.User.Uid || data.Inventory.Uid != data.User.Uid ||
            data.Progress.StageData.CurChapter < 1 || data.Progress.StageData.CurStage < 1 ||
            data.Progress.StageData.CurStage > GlobalConstants.FieldStagesPerChapter ||
            data.Progress.LastActiveUnixTime < 0)
            return Invalid("영역 UID 또는 챕터/스테이지 번호가 잘못되었습니다.", out message);
        int[] growth = { data.Progress.PartyHpLv, data.Progress.PartyAtkLv, data.Progress.PartyAtkSpdLv,
            data.Progress.PartyDefLv, data.Progress.PartyCritChanceLv, data.Progress.PartyCritDmgLv };
        if (growth.Any(value => value < 0 || value > GlobalConstants.MaxPartyGrowthLevel))
            return Invalid("파티 강화 레벨 범위를 초과했습니다.", out message);
        var master = FindFirstObjectByType<MasterDataMgr>();
        var items = new Dictionary<string, ItemData>();
        foreach (var item in data.Inventory.UserItems)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.ItemUid) || !items.TryAdd(item.ItemUid, item) ||
                string.IsNullOrWhiteSpace(item.ItemId) || item.Type != ItemType.Equipment || item.ItemLv < 1 || item.ItemCount != 1 || item.Price <= 0)
                return Invalid("인벤토리 아이템의 UID/수량/가격이 잘못되었습니다.", out message);
            if (master != null && (!master.TryGetEquipment(item.ItemId, out var equipment) || equipment == null ||
                !GlobalConstants.TryGetEquipmentSalePriceRange(equipment.Rarity, out int min, out int max) || item.Price < min || item.Price > max))
                return Invalid($"아이템 {item.ItemId}의 마스터 또는 판매가격이 유효하지 않습니다.", out message);
        }
        var owned = new HashSet<string>();
        var equipped = new HashSet<string>();
        foreach (var character in data.Character.UserCharacters)
        {
            if (character == null || string.IsNullOrWhiteSpace(character.CharId) || !owned.Add(character.CharId) ||
                character.CharLv < 1 || character.CharDuplicateCount < 0)
                return Invalid("캐릭터 ID/레벨/중복 수가 잘못되었습니다.", out message);
            if (master != null && (!master.TryGetCharacter(character.CharId, out var rule) || rule == null ||
                character.CharLv > GlobalConstants.GetCharacterMaxLevel(rule.Rarity)))
                return Invalid($"캐릭터 {character.CharId}의 마스터 또는 최대 레벨이 유효하지 않습니다.", out message);
            for (int slot = 0; slot < (int)EquipType.Count; slot++)
            {
                string uid = character.GetEquipmentUid((EquipType)slot);
                if (uid == "") continue;
                if (!items.TryGetValue(uid, out var item) || !equipped.Add(uid))
                    return Invalid($"캐릭터 {character.CharId}의 장비 {uid}가 인벤토리에 없거나 중복 장착되었습니다.", out message);
                if (master != null && (!master.TryGetEquipment(item.ItemId, out var ruleItem) || ruleItem == null ||
                    (int)ruleItem.EquipType != slot))
                    return Invalid($"캐릭터 {character.CharId}의 장비 슬롯이 잘못되었습니다.", out message);
            }
        }
        var party = new HashSet<string>();
        foreach (string id in data.Character.PartyCharacterIds)
            if (!string.IsNullOrEmpty(id) && (!owned.Contains(id) || !party.Add(id)))
                return Invalid($"편성 캐릭터 {id}가 보유 목록에 없거나 중복되었습니다.", out message);
        if (party.Count == 0) return Invalid("파티가 비어 있습니다.", out message);
        return ValidateRun(data.Progress.StageData, owned, out message);
    }

    private static bool ValidateRun(StageSaveData stage, HashSet<string> owned, out string message)
    {
        message = "";
        if (!ValidClearPosition(stage.ClearedChapter, stage.ClearedStage) ||
            !ValidClearPosition(stage.SettledChapter, stage.SettledStage) ||
            stage.SettledChapter > stage.ClearedChapter ||
            stage.SettledChapter == stage.ClearedChapter && stage.SettledStage > stage.ClearedStage)
            return Invalid("최종 클리어/정산 기록이 잘못되었습니다.", out message);
        var run = stage.Run;
        if (run == null) return true; // 이전 저장 형식은 최초 전투 준비 시 초기화한다.
        if (run.Version != 1 || string.IsNullOrWhiteSpace(run.AttemptId) || run.Chapter != stage.CurChapter ||
            run.CheckpointStage != stage.CurStage || !Enum.IsDefined(typeof(ChapterRunState), run.State) ||
            run.EntryHp == null || run.EntryHp.Count > 4 || run.Receipts == null ||
            run.Receipts.Count > GlobalConstants.FieldStagesPerChapter * 4 || run.EarnedItems == null ||
            run.State == ChapterRunState.Cleared && stage.CurStage != GlobalConstants.FieldStagesPerChapter)
            return Invalid("챕터 체크포인트의 번호/상태/필수 필드가 잘못되었습니다.", out message);
        var ids = new HashSet<string>();
        foreach (var hp in run.EntryHp)
            if (hp == null || string.IsNullOrWhiteSpace(hp.CharId) || !owned.Contains(hp.CharId) || !ids.Add(hp.CharId) ||
                float.IsNaN(hp.Hp) || float.IsInfinity(hp.Hp) || hp.Hp < 0f)
                return Invalid("체크포인트의 캐릭터/체력이 잘못되었습니다.", out message);
        var receipts = new HashSet<int>();
        foreach (var receipt in run.Receipts)
            if (receipt == null || receipt.Stage < 1 || receipt.Stage > stage.CurStage || receipt.Slot < 0 || receipt.Slot >= 4 ||
                !receipts.Add(receipt.Stage * 4 + receipt.Slot))
                return Invalid("필드 보상 지급 기록이 잘못되었습니다.", out message);
        ids.Clear();
        foreach (var item in run.EarnedItems)
            if (item == null || string.IsNullOrWhiteSpace(item.ItemId) || string.IsNullOrWhiteSpace(item.ItemUid) ||
                !ids.Add(item.ItemUid) || item.Type != ItemType.Equipment || item.ItemCount != 1 || item.ItemLv < 1 || item.Price <= 0)
                return Invalid("챕터 획득 아이템 표시 기록이 잘못되었습니다.", out message);
        return true;
    }
    private static bool ValidClearPosition(int chapter, int stage)
        => chapter == 0 && stage == 0 || chapter > 0 && stage == GlobalConstants.FieldStagesPerChapter;
    public string CreateUid(UidType type = UidType.None)
        => (type == UidType.User ? "USR_" : type == UidType.Item ? "ITEM_" : "") + Guid.NewGuid().ToString("N");
}
