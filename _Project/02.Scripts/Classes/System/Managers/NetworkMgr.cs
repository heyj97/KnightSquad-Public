using System;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine;

/// <summary>
/// PlayFab 인증 요청을 담당하고, 로그인 성공 후 DataMgr의 통합 데이터를 초기화한다.
///
/// 로그인:
/// NetworkMgr.Inst.Login(id, password, (success, message) => { ... });
///
/// 회원가입:
/// NetworkMgr.Inst.Register(id, password, nickname, (success, message) => { ... });
/// </summary>
public class NetworkMgr : G_Singleton<NetworkMgr>
{
    /// <summary>
    /// PlayFab 네트워크 요청을 처리 중이어서 중복 요청을 막아야 하는지 나타냅니다.
    /// </summary>
    public bool IsRequesting { get; private set; }

    /// <summary>
    /// PlayFab 아이디와 비밀번호로 로그인한다.
    /// 로그인 성공 후 서버의 GameSaveData 로드까지 완료되어야 성공을 반환한다.
    /// </summary>
    /// <param name="userName">로그인하거나 등록할 PlayFab 계정 이름입니다.</param>
    /// <param name="password">PlayFab 계정 인증에 사용할 비밀번호입니다.</param>
    /// <param name="onCompleted">요청 완료 시 성공 여부와 결과 메시지를 전달받을 콜백입니다.</param>
    public void Login(string userName, string password, Action<bool, string> onCompleted = null)
    {
        if (!ValidateAccountInput(userName, password, onCompleted))
            return;

        IsRequesting = true;
        var request = new LoginWithPlayFabRequest
        {
            Username = userName.Trim(),
            Password = password.Trim()
        };

        PlayFabClientAPI.LoginWithPlayFab(
            request,
            async result =>
            {
                bool initialized = await PlayerDataMgr.Inst.InitializeAsync(result.PlayFabId);

                IsRequesting = false;
                if (!initialized)
                {
                    onCompleted?.Invoke(false, PlayerDataMgr.Inst.LastMessage);
                    return;
                }

                onCompleted?.Invoke(true, "로그인 성공");
            },
            error =>
            {
                IsRequesting = false;
                string message = error.GenerateErrorReport();
                PDebug.LogError($"PlayFab 로그인 실패\n{message}");
                onCompleted?.Invoke(false, message);
            });
    }

    /// <summary>
    /// PlayFab 계정을 생성하고, 신규 GameSaveData까지 서버에 저장한다.
    /// </summary>
    /// <param name="userName">로그인하거나 등록할 PlayFab 계정 이름입니다.</param>
    /// <param name="password">PlayFab 계정 인증에 사용할 비밀번호입니다.</param>
    /// <param name="nickName">신규 계정에 설정할 게임 닉네임입니다.</param>
    /// <param name="onCompleted">요청 완료 시 성공 여부와 결과 메시지를 전달받을 콜백입니다.</param>
    public void Register(
        string userName,
        string password,
        string nickName,
        Action<bool, string> onCompleted = null)
    {
        if (!ValidateAccountInput(userName, password, onCompleted))
            return;

        if (string.IsNullOrWhiteSpace(nickName))
        {
            onCompleted?.Invoke(false, "닉네임을 입력해주세요.");
            return;
        }

        IsRequesting = true;
        var request = new RegisterPlayFabUserRequest
        {
            Username = userName.Trim(),
            Password = password.Trim(),
            DisplayName = nickName.Trim(),
            RequireBothUsernameAndEmail = false
        };

        PlayFabClientAPI.RegisterPlayFabUser(
            request,
            result => OnRegisterSucceeded(result, nickName.Trim(), onCompleted),
            error =>
            {
                IsRequesting = false;
                string message = error.GenerateErrorReport();
                PDebug.LogError($"PlayFab 회원가입 실패\n{message}");
                onCompleted?.Invoke(false, message);
            });
    }

    private async void OnRegisterSucceeded(
        RegisterPlayFabUserResult result,
        string nickName,
        Action<bool, string> onCompleted)
    {
        bool initialized =
            await PlayerDataMgr.Inst.InitializeAsync(result.PlayFabId);

        if (!initialized)
        {
            IsRequesting = false;
            onCompleted?.Invoke(false, "계정은 생성됐지만 데이터 준비에 실패했습니다.\n" + PlayerDataMgr.Inst.LastMessage);
            return;
        }

        GlobalValue.SetNickName(nickName);
        bool saved = await PlayerDataMgr.Inst.SaveDataAsync(SaveType.All);
        IsRequesting = false;

        if (!saved)
        {
            onCompleted?.Invoke(false, PlayerDataMgr.Inst.LastMessage);
            return;
        }

        PDebug.Log($"PlayFab 회원가입 완료: {GlobalValue.PlayFabId}");
        onCompleted?.Invoke(true, "회원가입 성공");
    }

    /// <summary>
    /// 현재 GlobalValue 전체를 PlayFab에 저장한다.
    /// UI 버튼처럼 await를 직접 사용하기 어려운 곳에서 호출할 수 있는 래퍼다.
    /// </summary>
    /// <param name="onCompleted">요청 완료 시 성공 여부와 결과 메시지를 전달받을 콜백입니다.</param>
    public async void SaveData(Action<bool, string> onCompleted = null)
    {
        if (IsRequesting)
        {
            onCompleted?.Invoke(false, "다른 서버 요청을 처리 중입니다.");
            return;
        }

        IsRequesting = true;
        bool saved = await PlayerDataMgr.Inst.SaveDataAsync(SaveType.All);
        IsRequesting = false;
        onCompleted?.Invoke(saved, saved ? "저장 성공" : PlayerDataMgr.Inst.LastMessage);
    }

    /// <summary>
    /// PlayFab의 최신 GameSaveData를 다시 불러와 GlobalValue에 적용한다.
    /// 저장하지 않은 메모리 변경 사항은 사라질 수 있다.
    /// </summary>
    /// <param name="onCompleted">요청 완료 시 성공 여부와 결과 메시지를 전달받을 콜백입니다.</param>
    public async void ReloadData(Action<bool, string> onCompleted = null)
    {
        if (IsRequesting)
        {
            onCompleted?.Invoke(false, "다른 서버 요청을 처리 중입니다.");
            return;
        }

        IsRequesting = true;
        bool loaded = await PlayerDataMgr.Inst.LoadDataAsync();
        IsRequesting = false;
        onCompleted?.Invoke(loaded, loaded ? "불러오기 성공" : PlayerDataMgr.Inst.LastMessage);
    }

    private bool ValidateAccountInput(
        string userName,
        string password,
        Action<bool, string> onCompleted)
    {
        if (IsRequesting)
        {
            onCompleted?.Invoke(false, "다른 서버 요청을 처리 중입니다.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(userName) ||
            string.IsNullOrWhiteSpace(password))
        {
            onCompleted?.Invoke(false, "아이디와 비밀번호를 입력해주세요.");
            return false;
        }

        return true;
    }
}
