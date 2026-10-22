using UnityEngine;

/// <summary>길찾기 3회, 비석 5회 등 패턴별 남은 실패 허용 횟수를 표시한다.</summary>
public class RaidPatternHeartsUI : MonoBehaviour
{
    [Tooltip("왼쪽부터 순서대로 연결한다. 비어 있으면 자식 Image를 Hierarchy 순서로 찾는다.")]
    [SerializeField] private UnityEngine.UI.Image[] m_Hearts;
    private int m_Maximum;
    private bool m_HasBeenShown;

    private void Start()
    {
        if (!m_HasBeenShown) Hide();
    }

    public bool Show(int maximum)
    {
        if (m_Hearts == null || m_Hearts.Length == 0)
            m_Hearts = GetComponentsInChildren<UnityEngine.UI.Image>(true);
        if (maximum <= 0 || m_Hearts.Length < maximum)
        {
            Debug.LogError("패턴 하트: 필요한 개수 이상의 하트 Image를 연결해주세요.", this);
            return false;
        }
        foreach (UnityEngine.UI.Image heart in m_Hearts)
            if (heart == null || heart.gameObject == gameObject)
            {
                Debug.LogError("패턴 하트: 그룹 자체가 아닌 자식 하트 Image를 연결해주세요.", this);
                return false;
            }
        m_Maximum = maximum;
        m_HasBeenShown = true;
        SetRemaining(maximum);
        gameObject.SetActive(true);
        return true;
    }

    public void SetRemaining(int remaining)
    {
        if (m_Hearts == null) return;
        int visible = Mathf.Clamp(remaining, 0, m_Maximum);
        for (int i = 0; i < m_Hearts.Length; i++)
            if (m_Hearts[i] != null) m_Hearts[i].gameObject.SetActive(i < visible);
    }

    public void Hide() => gameObject.SetActive(false);
}
