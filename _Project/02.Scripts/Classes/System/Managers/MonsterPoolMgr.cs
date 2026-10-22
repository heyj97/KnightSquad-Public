using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지와 레이드 몬스터의 생성, 재사용 및 웨이브 종료를 관리합니다.
/// </summary>
public class MonsterPoolMgr : MonoBehaviour
{
    private const int MaxMonSpawnCount = 4;
    
    [SerializeField] private Transform m_SpawnpointsRoot;
    [SerializeField] private Transform m_CharacterSpawnpointsRoot;

    private MonsterRootMgr[] m_MonsterRoots;
    /// <summary>
    /// 생성된 몬스터를 배치하는 부모 Transform을 가져옵니다.
    /// </summary>
    public Transform SpawnRoot => m_SpawnpointsRoot;
    /// <summary>
    /// 몬스터가 공격 대상으로 사용할 캐릭터 배치 루트를 가져옵니다.
    /// </summary>
    public Transform CharacterSpawnRoot => m_CharacterSpawnpointsRoot;
    public MonsterBase RaidBoss { get; private set; }

    /// <summary>
    /// 활성 몬스터들의 스테이지 전환 이동 애니메이션을 일괄 전환합니다.
    /// </summary>
    /// <param name="moving">이동 애니메이션을 재생할지 여부입니다.</param>
    public void SetTransitionMoving(bool moving)
    {
        foreach (MonsterBase monster in m_AliveMonsters)
            if (monster != null)
                monster.SetTransitionMoving(moving);
    }
    private readonly HashSet<MonsterBase> m_AliveMonsters = new();
    private bool m_IsWaveActive;
    /// <summary>
    /// 현재 웨이브에 등록된 모든 몬스터가 제거되었을 때 발생합니다.
    /// </summary>
    public event System.Action OnWaveCleared;

    private void Awake()
    {
        if (GetComponent<BattleFlowMgr>() == null)
            gameObject.AddComponent<BattleFlowMgr>();
    }

    /// <summary>
    /// 현재 스테이지 규칙에 맞는 필드 몬스터 웨이브 생성을 시도합니다.
    /// </summary>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TrySpawnCurrentStage()
    {
        if (m_IsWaveActive)
            return false;
        if (m_MonsterRoots == null && !TryGetMonsterRootPrefabs(out m_MonsterRoots))
            return false;
        ClearWave();
        if (!TrySpawnMonster(m_MonsterRoots))
        {
            ClearWave();
            return false;
        }
        m_IsWaveActive = m_AliveMonsters.Count > 0;
        return m_IsWaveActive;
    }

    /// <summary>
    /// 지정된 레이드 보스를 풀에서 준비하여 전투 위치에 생성합니다.
    /// </summary>
    /// <param name="bossData">레이드에서 생성할 보스 마스터 데이터입니다.</param>
    /// <returns>작업이 성공하면 <see langword="true"/>, 사전 조건이 충족되지 않거나 처리에 실패하면 <see langword="false"/>입니다.</returns>
    public bool TrySpawnRaidBoss(MonsterData bossData)
    {
        if (m_IsWaveActive || bossData == null || bossData.Grade != MonsterGrade.RaidBoss)
            return false;
        if (m_MonsterRoots == null && !TryGetMonsterRootPrefabs(out m_MonsterRoots))
            return false;
        ClearWave();
        // 일반 스테이지 보스와 같은 MiddleB_2 슬롯을 사용한다.
        const int bossSpawnIndex = 2;
        if (m_MonsterRoots.Length <= bossSpawnIndex ||
            !TryInitializeMonster(m_MonsterRoots[bossSpawnIndex], bossData))
        {
            ClearWave();
            return false;
        }
        m_IsWaveActive = true;
        RaidBoss = m_MonsterRoots[bossSpawnIndex].Monster;
        return true;
    }

    /// <summary>
    /// 현재 웨이브의 활성 몬스터를 모두 풀로 반환하고 등록 상태를 정리합니다.
    /// </summary>
    public void ClearWave()
    {
        RaidBoss = null;
        m_IsWaveActive = false;
        foreach (MonsterBase monster in m_AliveMonsters)
            if (monster != null)
                monster.OnMonDead -= HandleMonsterDied;
        m_AliveMonsters.Clear();
        if (m_MonsterRoots == null)
            return;
        foreach (MonsterRootMgr root in m_MonsterRoots)
            if (root != null)
                root.ResetForPool();
    }

    private bool TryInitializeMonster(MonsterRootMgr root, MonsterData data, int fieldSlot = -1)
    {
        if (root == null)
            return false;
        root.gameObject.SetActive(true);
        if (!root.TryInitialize(data, m_CharacterSpawnpointsRoot))
            return false;
        root.Monster.BindFieldRewardSlot(fieldSlot);
        m_AliveMonsters.Add(root.Monster);
        root.Monster.OnMonDead += HandleMonsterDied;
        return true;
    }

    private void HandleMonsterDied(MonsterBase monster)
    {
        if (!m_AliveMonsters.Remove(monster))
            return;
        monster.OnMonDead -= HandleMonsterDied;
        if (!m_IsWaveActive || m_AliveMonsters.Count > 0)
            return;
        m_IsWaveActive = false;
        OnWaveCleared?.Invoke();
    }

    private void OnDestroy()
    {
        foreach (MonsterBase monster in m_AliveMonsters)
            if (monster != null)
                monster.OnMonDead -= HandleMonsterDied;
    }

    private bool TryGetMonsterRootPrefabs(out MonsterRootMgr[] monsterRoots)
    {
        monsterRoots = null;
        
        if (m_SpawnpointsRoot == null || m_CharacterSpawnpointsRoot == null)
            return false;
        monsterRoots = m_SpawnpointsRoot.GetComponentsInChildren<MonsterRootMgr>(true);

        if (monsterRoots.Length < 1 || MaxMonSpawnCount < monsterRoots.Length)
        {
            return false;
        } // if (characterRoot.Length < 1 || MaxPartyCount < characterRoot.Length)
        
        return true;
    }

    private bool TrySpawnMonster(MonsterRootMgr[] monsterRoots)
    {
        int stage = StageMgr.Inst.CurStage;
        // 현재 챕터에서 등장 가능한 몬스터 데이터 가져오기
        if (!MasterDataMgr.Inst.TryGetMonsterDataByChapter(
                StageMgr.Inst.CurChapter,
                out List<MonsterData> monDataList))
        {
            PDebug.LogError("현재 챕터의 몬스터 데이터를 가져오지 못했습니다.");

            return false;
        }

        // 등급별 몬스터 분류
        List<MonsterData> normalMons = new();
        List<MonsterData> eliteMons = new();
        List<MonsterData> bossMons = new();

        foreach (MonsterData monData in monDataList)
        {
            switch (monData.Grade)
            {
                case MonsterGrade.Normal:
                    normalMons.Add(monData);
                    break;

                case MonsterGrade.Elite:
                    eliteMons.Add(monData);
                    break;

                case MonsterGrade.StageBoss:
                    bossMons.Add(monData);
                    break;
            }
        }

        // =========================================
        // 1. 최종 스테이지라면 보스 스폰
        // =========================================
        if (stage == StageMgr.Inst.MaxStage)
        {
            const int bossSpawnIndex = 2;

            if (monsterRoots.Length <= bossSpawnIndex ||
                monsterRoots[bossSpawnIndex] == null)
            {
                PDebug.LogError(
                    $"[{gameObject.name} / {GetType().Name}] 보스 스폰 위치가 없습니다.");
                return false;
            }
            if (bossMons.Count == 0)
            {
                PDebug.LogError("현재 챕터에 StageBoss 몬스터가 없습니다.");

                return false;
            }

            // 보스 랜덤 선택
            MonsterData bossData =
                bossMons[Random.Range(0, bossMons.Count)];

            // MiddleB_2 위치에 보스 배치
            if (!TryInitializeMonster(monsterRoots[bossSpawnIndex], bossData, bossSpawnIndex))
            {
                PDebug.LogError("StageBoss 초기화 실패");

                return false;
            }

            return true;
        }

        // =========================================
        // 2. 일반 스테이지 처리
        // =========================================

        // 일반 몬스터가 하나도 없으면 진행 불가
        if (normalMons.Count == 0)
        {
            PDebug.LogError("현재 챕터에 Normal 몬스터가 없습니다.");

            return false;
        }

        // 이번 스테이지에 등장할 엘리트 몬스터 수
        int eliteCount = StageMgr.Inst.GetEliteMonCount();

        // 엘리트 몬스터 데이터가 없으면 엘리트 스폰하지 않음
        if (eliteMons.Count == 0)
        {
            eliteCount = 0;
        }

        // 스폰 위치 Index 생성
        List<int> spawnIndices = new();

        for (int i = 0; i < monsterRoots.Length; i++)
        {
            spawnIndices.Add(i);
        }

        // Fisher-Yates 셔플
        for (int i = spawnIndices.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            (spawnIndices[i], spawnIndices[randomIndex]) =
                (spawnIndices[randomIndex], spawnIndices[i]);
        }

        // 실제 스폰할 몬스터 데이터
        MonsterData[] spawnMonsters =
            new MonsterData[monsterRoots.Length];

        // =========================================
        // 3. 모든 자리를 일반 몬스터로 채움
        // =========================================
        for (int i = 0; i < spawnMonsters.Length; i++)
        {
            int randomNormalIndex =
                Random.Range(0, normalMons.Count);

            spawnMonsters[i] =
                normalMons[randomNormalIndex];
        }

        // =========================================
        // 4. 일부 자리를 엘리트로 교체
        // =========================================
        eliteCount = Mathf.Min(
            eliteCount,
            monsterRoots.Length
        );

        for (int i = 0; i < eliteCount; i++)
        {
            int spawnIndex = spawnIndices[i];

            int randomEliteIndex =
                Random.Range(0, eliteMons.Count);

            spawnMonsters[spawnIndex] =
                eliteMons[randomEliteIndex];
        }

        // =========================================
        // 5. 실제 MonsterRoot 초기화
        // =========================================
        for (int i = 0; i < spawnMonsters.Length; i++)
        {
            if (!TryInitializeMonster(monsterRoots[i], spawnMonsters[i], i))
            {
                PDebug.LogError("MonsterRoot 초기화 실패: index={i}");

                return false;
            }
        }

        return true;
    }
}
