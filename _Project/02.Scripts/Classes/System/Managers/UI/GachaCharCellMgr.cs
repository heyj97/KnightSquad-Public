using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public enum GachaCharCellMode { Catalog, PityTarget, Result }

/// <summary>세 가챠 목록의 공용 셀. 보관한 CharacterData/Result를 추후 호버 표시에도 사용할 수 있다.</summary>
public class GachaCharCellMgr : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image m_FrameImg;
    [SerializeField] private Outline m_FrameOutline;
    [SerializeField] private Image m_ProfileImg;
    private Action<string> m_OnSelected;

    public CharacterData CharacterData { get; private set; }
    public string CharacterId => CharacterData != null ? CharacterData.Id : "";
    public CharacterGachaResult Result { get; private set; }
    public GachaCharCellMode Mode { get; private set; }
    public bool IsSelected { get; private set; }

    // 프리팹 자산에서도 읽기만 수행한다. 참조가 비어 있어도 기존 구조에서 찾을 수 있으면 사용한다.
    public bool HasRequiredGraphics => (m_FrameImg != null || GetComponent<Image>() != null) &&
        (m_FrameOutline != null || GetComponent<Outline>() != null) &&
        (m_ProfileImg != null || transform.Find("Profile")?.GetComponent<Image>() != null);

    private void Awake() => ResolveGraphics();

    public void Initialize(CharacterData data, GachaCharCellMode mode,
        CharacterGachaResult result = null, Action<string> onSelected = null)
    {
        ResolveGraphics();
        CharacterData = data;
        Mode = mode;
        Result = result;
        m_OnSelected = mode == GachaCharCellMode.PityTarget ? onSelected : null;
        if (m_ProfileImg != null) m_ProfileImg.sprite = data != null ? data.Icon : null;
        if (m_FrameImg != null) m_FrameImg.raycastTarget = true;
        SetSelected(false);
    }

    public void SetSelected(bool selected)
    {
        IsSelected = Mode == GachaCharCellMode.PityTarget && selected;
        if (m_FrameOutline != null && CharacterData != null)
        {
            m_FrameOutline.enabled = true;
            m_FrameOutline.effectColor = IsSelected ? new Color32(255, 0, 0, 255)
                : GlobalConstants.GetRarityColor32(CharacterData.Rarity);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left || MessageBoxMgr.IsBlockingInput ||
            CharacterGachaMgr.IsBusy || Mode != GachaCharCellMode.PityTarget || CharacterData == null) return;
        m_OnSelected?.Invoke(CharacterId);
    }

    private void ResolveGraphics()
    {
        if (m_FrameImg == null) m_FrameImg = GetComponent<Image>();
        if (m_FrameOutline == null) m_FrameOutline = GetComponent<Outline>();
        if (m_ProfileImg == null) m_ProfileImg = transform.Find("Profile")?.GetComponent<Image>();
    }

    private void OnDestroy() => m_OnSelected = null;
}
