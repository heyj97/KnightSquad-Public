using UnityEngine;

/// <summary>
/// 스킬 이펙트 애니메이션 종료 이벤트에서 이펙트 오브젝트를 제거합니다.
/// </summary>
public class SkillEffectLifetime : MonoBehaviour
{
    /// <summary>
    /// 애니메이션 이벤트에서 호출하여 재생이 끝난 스킬 이펙트 오브젝트를 제거합니다.
    /// </summary>
    public void OnAnimationFinished()
    {
        Destroy(gameObject);
    }
}