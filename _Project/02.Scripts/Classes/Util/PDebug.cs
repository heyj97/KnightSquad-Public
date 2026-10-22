using System.IO;
using System.Runtime.CompilerServices;
using UnityEngine;

/// <summary>
/// 호출 위치 정보를 덧붙여 Unity 콘솔 로그를 기록하는 기능을 제공합니다.
/// </summary>
public static class PDebug
{
    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 일반 Unity 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void Log(
        object message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.Log(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}"
        );
    }

    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 일반 Unity 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="context">로그 항목과 함께 표시할 Unity 오브젝트입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void Log(
        object message,
        Object context,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.Log(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}",
            context
        );
    }

    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 Unity 경고 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void LogWarning(
        object message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.LogWarning(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}"
        );
    }

    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 Unity 경고 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="context">로그 항목과 함께 표시할 Unity 오브젝트입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void LogWarning(
        object message,
        Object context,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.LogWarning(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}",
            context
        );
    }

    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 Unity 오류 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void LogError(
        object message,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.LogError(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}"
        );
    }

    /// <summary>
    /// 호출 파일, 멤버와 줄 번호를 덧붙여 Unity 오류 로그를 기록합니다.
    /// </summary>
    /// <param name="message">Unity 콘솔에 기록할 내용입니다.</param>
    /// <param name="context">로그 항목과 함께 표시할 Unity 오브젝트입니다.</param>
    /// <param name="memberName">호출 멤버 이름이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="filePath">호출 소스 파일 경로이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    /// <param name="lineNumber">호출 소스 줄 번호이며 생략하면 컴파일러가 자동으로 채웁니다.</param>
    [HideInCallstack]
    public static void LogError(
        object message,
        Object context,
        [CallerMemberName] string memberName = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        string fileName = Path.GetFileNameWithoutExtension(filePath);

        Debug.LogError(
            $"[{fileName}.{memberName} L:{lineNumber}]\n{message}",
            context
        );
    }
}
