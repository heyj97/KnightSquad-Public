using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>Button의 좌클릭은 유지하고 우클릭만 강화 편집 매니저에 전달합니다.</summary>
[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public class PartyLvUpCellInput : MonoBehaviour, IPointerClickHandler
{
    private Button m_Button;
    private Action m_OnRightClick;
    private void Awake() => m_Button = GetComponent<Button>();
    public void BindRightClick(Action callback) => m_OnRightClick = callback;
    public void ClearBinding() => m_OnRightClick = null;
    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Right || !isActiveAndEnabled ||
            m_Button == null || !m_Button.IsInteractable() || MessageBoxMgr.IsBlockingInput || PlayerDataMgr.IsProgressBlocked)
            return;
        m_OnRightClick?.Invoke();
    }
}
