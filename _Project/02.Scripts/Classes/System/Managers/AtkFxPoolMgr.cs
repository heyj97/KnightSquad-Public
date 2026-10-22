using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 일반 공격과 치명타 공격 이펙트를 풀링하여 재생합니다.
/// </summary>
public class AtkFxPoolMgr : G_Singleton<AtkFxPoolMgr>
{
    Dictionary<EffectType, List<GameObject>> m_EffectPool = new();

    /// <summary>
    /// 시작 시 미리 생성할 공격 이펙트의 종류별 개수입니다.
    /// </summary>
    [Header("Pool Setting")]
    public int m_PoolCount = 20;
    /// <summary>
    /// 공격 이펙트 풀을 확장할 때 한 번에 추가하는 비율입니다.
    /// </summary>
    public int m_ExpandScalePerOneTime = 5;

    /// <summary>
    /// 일반 공격 적중 시 풀에서 재생할 이펙트 프리팹입니다.
    /// </summary>
    [Header("Effects Prefab")]
    public GameObject m_NormalAttackPrefab;
    /// <summary>
    /// 치명타 공격 적중 시 풀에서 재생할 이펙트 프리팹입니다.
    /// </summary>
    public GameObject m_CriticalAttackPrefab;


    protected override void Awake()
    {
        base.Awake();

        if (Inst != this)
            return;

        // 풀 초기화
        for (int i = 0; i < m_PoolCount; i++)
        {
            CreateEffect(EffectType.Attack, m_NormalAttackPrefab);
            CreateEffect(EffectType.CriticalAttack, m_CriticalAttackPrefab);
        }
    }

    private GameObject GetEffect(EffectType type)
    {
        // dictionary에서 EffectType에 맞는 리스트를 반환
        if (!m_EffectPool.TryGetValue(type, out List<GameObject> effectList))
            return null;

        // 리스트를 돌며 반환가능한 이펙트가 있는지 확인
        foreach (GameObject effect in effectList)
        {
            if (effect != null && !effect.activeSelf)
                return effect;
        }

        // 위 foreach문에서 반환되지 않으면(반환가능한 이펙트가 없다면)
        switch (type)
        {
            case EffectType.Attack:
                for(int i = 0; i < m_ExpandScalePerOneTime; i ++)
                    CreateEffect(EffectType.Attack, m_NormalAttackPrefab);

                break;

            case EffectType.CriticalAttack:
                for(int i = 0; i < m_ExpandScalePerOneTime; i ++)
                CreateEffect(EffectType.CriticalAttack, m_CriticalAttackPrefab);

                break;

            default:
                PDebug.LogError("지원하지 않는 이펙트 타입입니다.");
                break;
        }
        
        // 리필 후 리스트를 재순회
        foreach (GameObject effect in effectList)
        {
            if (effect != null && !effect.activeSelf)
                return effect;
        }
        return null;
    }

    private GameObject CreateEffect(EffectType type, GameObject prefab)
    {
        if (prefab == null)
            return null;

        GameObject effect = Instantiate(prefab, transform);
        effect.SetActive(false);

        if (!m_EffectPool.TryGetValue(type, out List<GameObject> effectList))
        {
            effectList = new List<GameObject>();
            m_EffectPool.Add(type, effectList);
        }

        effectList.Add(effect);
        return effect;
    }

    /// <summary>
    /// 일반 또는 치명타 공격 이펙트를 풀에서 가져와 지정 위치에 재생합니다.
    /// </summary>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <param name="pos">공격 이펙트를 재생할 월드 위치입니다.</param>
    public void Play(EffectType type, Vector3 pos)
    {
        GameObject a_Effect = GetEffect(type);

        if (a_Effect == null)
            return;

        if (!a_Effect.TryGetComponent(out EffectPrefabMgr effectMgr))
        {
            PDebug.LogError($"{a_Effect.name}에 EffectPrefabMgr이 없습니다.");
            return;
        }
        
        float _ranRotZ = Random.Range(0f, 360f);
        float _ranPosX = pos.x - Random.Range(-0.3f, 0.3f);
        float _ranPosY = pos.y - Random.Range(-0.1f, 0.1f);
        a_Effect.transform.position = new Vector3(_ranPosX, _ranPosY, 0);
        a_Effect.transform.Rotate(new Vector3(0, 0, _ranRotZ));
        a_Effect.SetActive(true);
        effectMgr.Play(type);
    }
}
