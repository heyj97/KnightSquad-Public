using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// 전투 UI의 공통 입력 보조 기능을 제공합니다.
/// </summary>
public class UIMgr : MonoBehaviour
{
    /// <summary>
    /// 현재 포인터 위치에서 UGUI 그래픽이 입력을 우선 점유하는지 검사합니다.
    /// </summary>
    /// <returns>포인터가 UGUI 그래픽 위에 있으면 <see langword="true"/>, 그렇지 않으면 <see langword="false"/>입니다.</returns>
    public static bool IsPointerOverUIObject() //UGUI의 UI들이 먼저 피킹되는지 확인하는 함수
    {
        if (EventSystem.current == null)
            return false;
            
        PointerEventData a_EDCurPos = new PointerEventData(EventSystem.current);

    #if !UNITY_EDITOR && (UNITY_IPHONE || UNITY_ANDROID)

            List<RaycastResult> results = new List<RaycastResult>();
            for (int i = 0; i < Input.touchCount; ++i)
            {
                a_EDCurPos.position = Input.GetTouch(i).position;  
                results.Clear();
                EventSystem.current.RaycastAll(a_EDCurPos, results);
                if (0 < results.Count)
                    return true;
            }

            return false;
    #else
        a_EDCurPos.position = Input.mousePosition;
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(a_EDCurPos, results);
        return (0 < results.Count);
    #endif
    }//public bool IsPointerOverUIObject() 
}
