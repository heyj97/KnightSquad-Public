using System;
using UnityEngine;

/// <summary>셀 크기를 1로 정규화한 이동 선분의 모든 통과 칸을 검사한다.</summary>
internal static class RaidTilePathUtility
{
    // touchOnly는 대각선으로 정확히 모서리를 통과할 때 닿는 두 옆 칸이다.
    internal static bool VisitCells(Vector2 from, Vector2 to, Func<Vector2Int, bool, bool> visit)
    {
        Vector2Int cell = new Vector2Int(Mathf.FloorToInt(from.x), Mathf.FloorToInt(from.y));
        Vector2Int destination = new Vector2Int(Mathf.FloorToInt(to.x), Mathf.FloorToInt(to.y));
        if (!visit(cell, false)) return false;
        Vector2 delta = to - from;
        int stepX = Math.Sign(delta.x), stepY = Math.Sign(delta.y);
        float strideX = stepX == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.x);
        float strideY = stepY == 0 ? float.PositiveInfinity : 1f / Mathf.Abs(delta.y);
        float nextX = stepX == 0 ? float.PositiveInfinity :
            ((stepX > 0 ? cell.x + 1f : cell.x) - from.x) / delta.x;
        float nextY = stepY == 0 ? float.PositiveInfinity :
            ((stepY > 0 ? cell.y + 1f : cell.y) - from.y) / delta.y;
        int limit = Math.Abs(destination.x - cell.x) + Math.Abs(destination.y - cell.y) + 1;
        for (int i = 0; cell != destination && i < limit; i++)
        {
            if (cell.x == destination.x)
            {
                cell.y += stepY;
                nextY += strideY;
            }
            else if (cell.y == destination.y)
            {
                cell.x += stepX;
                nextX += strideX;
            }
            else if (Mathf.Abs(nextX - nextY) <= 0.000001f)
            {
                if (!visit(new Vector2Int(cell.x + stepX, cell.y), true) ||
                    !visit(new Vector2Int(cell.x, cell.y + stepY), true)) return false;
                cell += new Vector2Int(stepX, stepY);
                nextX += strideX;
                nextY += strideY;
            }
            else if (nextX < nextY)
            {
                cell.x += stepX;
                nextX += strideX;
            }
            else
            {
                cell.y += stepY;
                nextY += strideY;
            }
            if (!visit(cell, false)) return false;
        }
        return cell == destination;
    }
}
