using UnityEngine;

/// <summary>레이드 전용 캐릭터가 좌우 방향과 거리로 선택하는 단일 공격 대상.</summary>
public interface IRaidAttackTarget
{
    Vector3 Position { get; }
    bool CanBeHit { get; }
    void ReceiveHit();
}
