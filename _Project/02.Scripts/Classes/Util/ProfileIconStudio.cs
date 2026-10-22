using UnityEngine;

/// <summary>
/// 프로필 아이콘 촬영용 스튜디오 씬 설정 컴포넌트
/// </summary>
public class ProfileIconStudio : MonoBehaviour
{
    [Header("촬영용 카메라")]
    [SerializeField] private Camera m_IconCamera;

    [Header("프리팹 스폰 위치")]
    [SerializeField] private Transform m_SpawnPoint;

    [Header("출력 해상도")]
    [SerializeField] private int m_Width = 256;
    [SerializeField] private int m_Height = 256;

    /// <summary>
    /// 프로필 아이콘을 렌더링하는 전용 카메라를 가져옵니다.
    /// </summary>
    public Camera IconCamera => m_IconCamera;
    /// <summary>
    /// 촬영할 캐릭터 모델을 배치할 기준 지점을 가져옵니다.
    /// </summary>
    public Transform SpawnPoint => m_SpawnPoint;
    /// <summary>
    /// 프로필 아이콘 출력 이미지의 가로 픽셀 수를 가져옵니다.
    /// </summary>
    public int Width => m_Width;
    /// <summary>
    /// 프로필 아이콘 출력 이미지의 세로 픽셀 수를 가져옵니다.
    /// </summary>
    public int Height => m_Height;
}