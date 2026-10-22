using UnityEngine;

/// <summary>
/// 레이드 메뉴에서 전투와 분리된 보스 모델 미리보기를 생성하고 정리합니다.
/// </summary>
public class RaidBossPreview : MonoBehaviour
{
    [Header("Preview Setting")]
    [SerializeField] private Transform m_BossSpawnPoint;

    private GameObject m_CurrentBoss;

    /// <summary>
    /// 보스 프리팹의 모델 루트만 복제하여 메뉴 일시정지와 무관하게 움직이는 미리보기를 표시합니다.
    /// </summary>
    /// <param name="bossModelPrefab">미리보기에서 UnitRoot를 복제할 보스 프리팹입니다.</param>
    public void ShowBoss(GameObject bossModelPrefab)
    {
        ClearBoss();

        if (bossModelPrefab == null)
        {
            PDebug.LogError("레이드 보스 모델 Prefab이 없습니다.");
            return;
        }

        if (m_BossSpawnPoint == null)
        {
            PDebug.LogError("레이드 보스 생성 위치가 없습니다.", this);
            return;
        }

        int previewLayer = LayerMask.NameToLayer("Raid Preview");

        if (previewLayer < 0)
        {
            PDebug.LogError("Raid Preview 레이어가 없습니다.", this);
            return;
        }

        // 몬스터 프리팹 내부의 순수 모델 Root 검색
        Transform a_UnitRoot = bossModelPrefab.transform.Find("UnitRoot");

        if (a_UnitRoot == null)
        {
            PDebug.LogError(
                $"{bossModelPrefab.name} Prefab에 UnitRoot가 없습니다.",
                this
            );
            return;
        }

        // UnitRoot만 복제하므로 MonsterRoot의 전투 로직은 생성되지 않음
        m_CurrentBoss = Instantiate(
            a_UnitRoot.gameObject,
            m_BossSpawnPoint
        );

        m_CurrentBoss.transform.localPosition = Vector3.zero;
        // 미리보기 모델만 메뉴 일시정지와 무관하게 애니메이션 재생
        foreach (Animator a_Animator in m_CurrentBoss.GetComponentsInChildren<Animator>(true))
            a_Animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        m_CurrentBoss.transform.localRotation = Quaternion.identity;

        // 프리뷰 카메라에서만 보이도록 Layer 변경
        SetLayerRecursive(
            m_CurrentBoss,
            previewLayer
        );
    }

    private void SetLayerRecursive(GameObject target, int layer)
    {
        target.layer = layer;

        foreach (Transform child in target.transform)
        {
            SetLayerRecursive(
                child.gameObject,
                layer
            );
        }
    }

    /// <summary>
    /// 현재 생성된 보스 미리보기 모델을 제거합니다.
    /// </summary>
    public void ClearBoss()
    {
        if (m_CurrentBoss == null)
            return;

        Destroy(m_CurrentBoss);
        m_CurrentBoss = null;
    }
}
