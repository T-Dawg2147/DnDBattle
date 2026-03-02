namespace DnDBattle.GameLogic.Movement;

public sealed class PathfindingService
{
    /// <summary>A* pathfinding on a grid.</summary>
    public IReadOnlyList<(int Col, int Row)> FindPath(
        (int Col, int Row) start,
        (int Col, int Row) goal,
        Func<int, int, bool> isWalkable,
        int gridWidth,
        int gridHeight,
        bool allowDiagonals = true)
    {
        var openSet = new PriorityQueue<(int, int), double>();
        var cameFrom = new Dictionary<(int, int), (int, int)>();
        var gScore = new Dictionary<(int, int), double>();
        var fScore = new Dictionary<(int, int), double>();

        gScore[start] = 0;
        fScore[start] = Heuristic(start, goal);
        openSet.Enqueue(start, fScore[start]);

        while (openSet.Count > 0)
        {
            var current = openSet.Dequeue();
            if (current == goal) return ReconstructPath(cameFrom, current);

            foreach (var neighbor in GetNeighbors(current, gridWidth, gridHeight, allowDiagonals))
            {
                if (!isWalkable(neighbor.Item1, neighbor.Item2)) continue;

                double tentativeG = gScore.GetValueOrDefault(current, double.MaxValue) +
                    MoveCost(current, neighbor);

                if (tentativeG < gScore.GetValueOrDefault(neighbor, double.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, goal);
                    openSet.Enqueue(neighbor, fScore[neighbor]);
                }
            }
        }

        return Array.Empty<(int, int)>();
    }

    private static double Heuristic((int Col, int Row) a, (int Col, int Row) b) =>
        Math.Sqrt(Math.Pow(a.Col - b.Col, 2) + Math.Pow(a.Row - b.Row, 2));

    private static double MoveCost((int Col, int Row) from, (int Col, int Row) to) =>
        from.Col != to.Col && from.Row != to.Row ? Math.Sqrt(2) : 1.0;

    private static IEnumerable<(int, int)> GetNeighbors(
        (int Col, int Row) cell, int w, int h, bool diagonals)
    {
        var (col, row) = cell;
        var dirs = new List<(int, int)>
        {
            (col - 1, row), (col + 1, row), (col, row - 1), (col, row + 1)
        };
        if (diagonals)
        {
            dirs.Add((col - 1, row - 1)); dirs.Add((col + 1, row - 1));
            dirs.Add((col - 1, row + 1)); dirs.Add((col + 1, row + 1));
        }
        return dirs.Where(d => d.Item1 >= 0 && d.Item1 < w && d.Item2 >= 0 && d.Item2 < h);
    }

    private static IReadOnlyList<(int, int)> ReconstructPath(
        Dictionary<(int, int), (int, int)> cameFrom, (int, int) current)
    {
        var path = new List<(int, int)> { current };
        while (cameFrom.TryGetValue(current, out var prev))
        {
            path.Insert(0, prev);
            current = prev;
        }
        return path;
    }
}
