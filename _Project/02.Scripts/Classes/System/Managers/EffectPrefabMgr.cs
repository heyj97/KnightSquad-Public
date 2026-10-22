using System.Collections;
using UnityEngine;


/// <summary>
/// 재생할 전투 이펙트의 용도를 구분합니다.
/// </summary>
public enum EffectType
{
    /// <summary>
    /// 일반 공격 적중 이펙트를 재생합니다.
    /// </summary>
    Attack,
    /// <summary>
    /// 치명타 공격 적중 이펙트를 재생합니다.
    /// </summary>
    CriticalAttack,
    /// <summary>
    /// 스킬 사용 이펙트를 재생합니다.
    /// </summary>
    Skill
}


/// <summary>
/// 지정된 전투 이펙트 프리팹을 생성하고 정해진 시간 뒤 정리합니다.
/// </summary>
[RequireComponent(typeof(Animator))]
public class EffectPrefabMgr : MonoBehaviour
{
    Animator m_Anim;
    /// <summary>
    /// 생성한 전투 이펙트를 유지한 뒤 제거하기까지의 시간입니다.
    /// </summary>
    [Tooltip("애니메이션 재생시간")]
    public float m_PlayTime;

    void Awake()
    {
        m_Anim = GetComponent<Animator>();
    }

    /// <summary>
    /// 이펙트 종류와 선택적 스킬 이름에 맞는 프리팹을 지정 위치에 생성합니다.
    /// </summary>
    /// <param name="type">조회하거나 처리할 종류입니다.</param>
    /// <param name="SkillName">스킬 이펙트 프리팹을 조회할 리소스 이름입니다.</param>
    public void Play(EffectType type, string SkillName = "")
    {
        string a_AnimName = "";
        switch (type)
        {
            case EffectType.Attack:
                a_AnimName = "AttackNormal";
                break;

            case EffectType.CriticalAttack:
                a_AnimName = "AttackCritical";
                break;

            case EffectType.Skill:
                a_AnimName = SkillName;
                break;

            default:
                break;
        }

        StartCoroutine(PlayFx(m_PlayTime, a_AnimName));
    }

    IEnumerator PlayFx(float t, string animName)
    {
        m_Anim.Play(animName, 0, 0f);
        yield return new WaitForSeconds(t);
        this.gameObject.SetActive(false);
    }
}
