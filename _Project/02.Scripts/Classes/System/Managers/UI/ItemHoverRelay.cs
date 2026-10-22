using System;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>실제 Raycast 대상에서 셀의 호버를 공통 정보창으로 전달합니다.</summary>
public class ItemHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Component m_Owner;
    private Func<ItemData> m_GetItem;
    private Func<bool> m_CanShow;
    private ItemDetailPanelMgr m_Panel;

    public void Bind(Component owner, Func<ItemData> getItem, Func<bool> canShow)
    {
        Hide();
        m_Owner = owner;
        m_GetItem = getItem;
        m_CanShow = canShow;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (m_Owner == null || m_GetItem == null || (m_CanShow != null && !m_CanShow())) return;
        if (m_Panel == null)
        {
            m_Panel = FindFirstObjectByType<ItemDetailPanelMgr>();
            // 씬 YAML을 수정하지 않고 사용자가 준비한 활성 정보창에 런타임으로 연결한다.
            if (m_Panel == null)
            {
                GameObject root = GameObject.Find("ItemDetailPanel");
                if (root != null) m_Panel = root.GetComponent<ItemDetailPanelMgr>() ?? root.AddComponent<ItemDetailPanelMgr>();
            }
        }
        if (m_Panel != null) m_Panel.Show(m_Owner, m_GetItem());
    }

    public void OnPointerExit(PointerEventData eventData) => Hide();
    private void OnDisable() => Hide();
    private void OnDestroy() => Hide();

    public void Hide()
    {
        if (m_Panel != null && m_Owner != null) m_Panel.Hide(m_Owner);
    }
}
