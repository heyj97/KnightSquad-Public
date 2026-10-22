using UnityEngine;

/// <summary>
/// 같은 배경 두 장을 사용해 왼쪽으로 무한 스크롤한다.
/// </summary>
public class MapInteractionMgr : MonoBehaviour
{
    [Header("배경 참조")]
    [SerializeField] private SpriteRenderer m_Map1;
    [SerializeField] private SpriteRenderer m_Map2;

    [Header("스크롤 설정")]
    // 초당 이동하는 월드 거리
    [SerializeField, Min(0f)] private float m_ScrollSpeed = 1f;
    [SerializeField] private bool m_IsScrolling = true;


    private float m_MapWidth;


    private Vector3 m_StartPosition;
    private float m_ScrollOffset;

    private void Start()
    {
        // 필수 참조 확인
        if (m_Map1 == null || m_Map2 == null ||
            m_Map1.sprite == null || m_Map2.sprite == null)
        {
            PDebug.LogError(
                "Map1과 Map2의 SpriteRenderer 및 Sprite를 지정해주세요.",
                this);

            enabled = false;
            return;
        }

        // Transform의 Scale까지 반영된 실제 너비를 사용한다.
        m_MapWidth = m_Map1.bounds.size.x;

        if (m_MapWidth <= 0f)
        {
            PDebug.LogError("배경 너비가 0입니다.", this);
            enabled = false;
            return;
        }

        // Map1 위치를 기준으로 Map2를 바로 오른쪽에 배치한다.
        m_StartPosition = m_Map1.transform.position;
        ApplyPositions();
    }

    private void Update()
    {
        if (!m_IsScrolling || !BattleFlowMgr.IsStageMoving)
            return;

        // deltaTime을 곱해 프레임 속도와 관계없이 일정하게 이동한다.
        // 이동 거리를 한 장 너비 안에서 반복해 무한히 커지지 않게 한다.
        m_ScrollOffset = Mathf.Repeat(
            m_ScrollOffset + m_ScrollSpeed * Time.deltaTime,
            m_MapWidth);

        ApplyPositions();
    }

    private void ApplyPositions()
    {
        Vector3 position = m_StartPosition;
        position.x -= m_ScrollOffset;

        m_Map1.transform.position = position;

        // 두 번째 배경은 항상 첫 번째 배경 바로 오른쪽에 둔다.
        position.x += m_MapWidth;
        m_Map2.transform.position = position;
    }

    /// <summary>
    /// 전투 상태 등에 따라 배경 이동을 켜거나 끈다.
    /// </summary>
    /// <param name="isScrolling">맵 드래그 스크롤이 진행 중인지 여부입니다.</param>
    public void SetScrolling(bool isScrolling)
    {
        m_IsScrolling = isScrolling;
    }
}