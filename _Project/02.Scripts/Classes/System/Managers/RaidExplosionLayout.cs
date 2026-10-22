using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>같은 반지름의 원들을 경계 안에 서로 겹치지 않게 배치한다.</summary>
internal static class RaidExplosionLayout
{
    internal static void Generate(Rect area, float radius, int count, int attempts,
        IReadOnlyList<Vector2> occupied, List<Vector2> result, Func<float, float, float> random)
    {
        result.Clear();
        if (radius <= 0f || float.IsNaN(radius) || float.IsInfinity(radius) ||
            area.width < radius * 2f || area.height < radius * 2f) return;
        float diameterSquared = radius * radius * 4f;
        for (int attempt = 0; attempt < attempts && result.Count < count; attempt++)
        {
            Vector2 candidate = new Vector2(random(area.xMin + radius, area.xMax - radius),
                random(area.yMin + radius, area.yMax - radius));
            bool overlaps = false;
            if (occupied != null)
                foreach (Vector2 other in occupied)
                    if ((candidate - other).sqrMagnitude < diameterSquared) { overlaps = true; break; }
            if (overlaps) continue;
            foreach (Vector2 other in result)
                if ((candidate - other).sqrMagnitude < diameterSquared) { overlaps = true; break; }
            if (!overlaps) result.Add(candidate);
        }
    }
}
