using System;
using System.Collections.Generic;
using System.Linq;

namespace DnDBattle.Services
{
    public static class MovementService
    {
        private static readonly (int dx, int dy)[] CardinalDirs = { (1, 0), (-1, 0), (0, 1), (0, -1) };
        private static readonly (int dx, int dy)[] DiagonalDirs = { (1, 1), (1, -1), (-1, 1), (-1, -1) };

        private static readonly (int dx, int dy, double baseCost)[] _cardinalDirsWithCost =
            { (1, 0, 1.0), (-1, 0, 1.0), (0, 1, 1.0), (0, -1, 1.0) };

        private static readonly (int dx, int dy, double baseCost)[] _allDirsWithCost =
            { (1, 0, 1.0), (-1, 0, 1.0), (0, 1, 1.0), (0, -1, 1.0), (1, 1, 1.5), (1, -1, 1.5), (-1, 1, 1.5), (-1, -1, 1.5) };

        public static HashSet<(int x, int y)> GetReachableSquares(
            int startX, int startY, int maxSquares,
            int gridWidth, int gridHeight,
            Func<int, int, bool> isCellBlocked,
            Func<int, int, int>? getCellCost = null)
        {
            var visited = new HashSet<(int, int)>();
            // Use double for fractional diagonal costs
            var costMap = new Dictionary<(int, int), double>();
            var q = new PriorityQueue<(int x, int y), double>();

            q.Enqueue((startX, startY), 0);
            costMap[(startX, startY)] = 0;
            visited.Add((startX, startY));

            var dirs = Options.AllowDiagonalMovement ? _allDirsWithCost : _cardinalDirsWithCost;

            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                double currentCost = costMap[(x, y)];

                foreach (var (dx, dy, baseCost) in dirs)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                    if (isCellBlocked(nx, ny)) continue;

                    int cellCost = getCellCost?.Invoke(nx, ny) ?? 1;
                    double moveCost = baseCost * cellCost;
                    double newCost = currentCost + moveCost;

                    if (newCost > maxSquares) continue;

                    if (!costMap.ContainsKey((nx, ny)) || newCost < costMap[(nx, ny)])
                    {
                        costMap[(nx, ny)] = newCost;
                        visited.Add((nx, ny));
                        q.Enqueue((nx, ny), newCost);
                    }
                }
            }
            return visited;
        }

        /// <summary>
        /// Computes the movement cost from start to each reachable cell.
        /// Returns a dictionary mapping cell position to movement cost.
        /// </summary>
        public static Dictionary<(int x, int y), double> GetReachableCosts(
            int startX, int startY, int maxSquares,
            int gridWidth, int gridHeight,
            Func<int, int, bool> isCellBlocked,
            Func<int, int, int>? getCellCost = null)
        {
            var costMap = new Dictionary<(int, int), double>();
            var q = new PriorityQueue<(int x, int y), double>();

            q.Enqueue((startX, startY), 0);
            costMap[(startX, startY)] = 0;

            var dirs = Options.AllowDiagonalMovement ? _allDirsWithCost : _cardinalDirsWithCost;

            while (q.Count > 0)
            {
                var (x, y) = q.Dequeue();
                double currentCost = costMap[(x, y)];

                foreach (var (dx, dy, baseCost) in dirs)
                {
                    int nx = x + dx, ny = y + dy;
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                    if (isCellBlocked(nx, ny)) continue;

                    int cellCost = getCellCost?.Invoke(nx, ny) ?? 1;
                    double moveCost = baseCost * cellCost;
                    double newCost = currentCost + moveCost;

                    if (newCost > maxSquares) continue;

                    if (!costMap.ContainsKey((nx, ny)) || newCost < costMap[(nx, ny)])
                    {
                        costMap[(nx, ny)] = newCost;
                        q.Enqueue((nx, ny), newCost);
                    }
                }
            }
            return costMap;
        }

        public static List<(int x, int y)> FindPathAStar(
            (int x, int y) start, (int x, int y) goal,
            int gridWidth, int gridHeight,
            Func<int, int, bool> isWalkable,
            Func<int, int, int>? getCellCost = null)
        {
            var open = new PriorityQueue<(int x, int y), double>();
            var gScore = new Dictionary<(int x, int y), double>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();
            int nodesVisited = 0;

            var dirs = Options.AllowDiagonalMovement ? _allDirsWithCost : _cardinalDirsWithCost;

            gScore[start] = 0;
            open.Enqueue(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var current = open.Dequeue();
                nodesVisited++;

                if (nodesVisited > Options.MaxAStarNodes) break;

                if (current == goal)
                {
                    var path = new List<(int x, int y)>();
                    var cur = current;
                    path.Add(cur);
                    while (cameFrom.ContainsKey(cur))
                    {
                        cur = cameFrom[cur];
                        path.Add(cur);
                    }
                    path.Reverse();
                    return path;
                }

                double currentG = gScore.ContainsKey(current) ? gScore[current] : double.MaxValue;

                foreach (var (dx, dy, baseCost) in dirs)
                {
                    int nx = current.x + dx, ny = current.y + dy;
                    var neighbor = (nx, ny);
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                    if (!isWalkable(nx, ny) && neighbor != goal) continue;

                    int cellCost = getCellCost?.Invoke(nx, ny) ?? 1;
                    double tentativeG = currentG + baseCost * cellCost;

                    if (!gScore.ContainsKey(neighbor) || tentativeG < gScore[neighbor])
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        double priority = tentativeG + Heuristic(neighbor, goal);
                        open.Enqueue(neighbor, priority);
                    }
                }
            }
            return new List<(int, int)>();
        }

        private static double Heuristic((int x, int y) a, (int x, int y) b)
        {
            // Octile heuristic when diagonals allowed (diagonal cost 1.5 = 1 + 0.5),
            // Manhattan otherwise.
            if (Options.AllowDiagonalMovement)
            {
                int dx = Math.Abs(a.x - b.x);
                int dy = Math.Abs(a.y - b.y);
                return Math.Max(dx, dy) + 0.5 * Math.Min(dx, dy);
            }
            return Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);
        }

        public static HashSet<int> ComputeAOOIndices(List<(int x, int y)> path, IEnumerable<(int x, int y)> enemyPositions)
        {
            var aooIndices = new HashSet<int>();
            if (path == null || path.Count < 2) return aooIndices;

            var enemyAdjacency = new Dictionary<(int x, int y), HashSet<(int x, int y)>>();
            foreach (var e in enemyPositions)
            {
                var adj = new HashSet<(int x, int y)>();
                for (int dx = -1; dx <= 1; dx++)
                    for (int dy = -1; dy <= 1; dy++)
                        adj.Add((e.x + dx, e.y + dy));
                enemyAdjacency[e] = adj;
            }

            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var cur = path[i];
                foreach (var kv in enemyAdjacency)
                {
                    bool prevAdj = kv.Value.Contains(prev);
                    bool curAdj = kv.Value.Contains(cur);
                    if (prevAdj && !curAdj)
                        aooIndices.Add(i);
                }
            }
            return aooIndices;
        }

        /// <summary>
        /// Detects which enemy tokens would get an Attack of Opportunity along the path.
        /// Returns the enemy positions that trigger AOO.
        /// </summary>
        public static List<(int x, int y)> DetectAOOEnemies(
            List<(int x, int y)> path,
            IEnumerable<(int x, int y)> enemyPositions)
        {
            var aooEnemies = new List<(int x, int y)>();
            if (path == null || path.Count < 2) return aooEnemies;

            foreach (var enemy in enemyPositions)
            {
                for (int i = 1; i < path.Count; i++)
                {
                    var prev = path[i - 1];
                    var cur = path[i];
                    bool prevAdj = IsAdjacent(prev, enemy);
                    bool curAdj = IsAdjacent(cur, enemy);
                    if (prevAdj && !curAdj)
                    {
                        if (!aooEnemies.Contains(enemy))
                            aooEnemies.Add(enemy);
                        break;
                    }
                }
            }
            return aooEnemies;
        }

        /// <summary>
        /// Checks if two positions are adjacent (including diagonals).
        /// </summary>
        public static bool IsAdjacent((int x, int y) a, (int x, int y) b)
        {
            int dx = Math.Abs(a.x - b.x);
            int dy = Math.Abs(a.y - b.y);
            return dx <= 1 && dy <= 1 && !(dx == 0 && dy == 0);
        }

        /// <summary>
        /// Determines if an attacker is flanking a target with help from allies.
        /// </summary>
        public static bool IsFlanking(
            (int x, int y) attackerPos,
            (int x, int y) targetPos,
            IEnumerable<(int x, int y)> allyPositions)
        {
            double attackerAngle = Math.Atan2(
                attackerPos.y - targetPos.y,
                attackerPos.x - targetPos.x) * 180.0 / Math.PI;

            foreach (var ally in allyPositions)
            {
                if (!IsAdjacent(ally, targetPos)) continue;

                double allyAngle = Math.Atan2(
                    ally.y - targetPos.y,
                    ally.x - targetPos.x) * 180.0 / Math.PI;

                double diff = Math.Abs(attackerAngle - allyAngle);
                if (diff > 180) diff = 360 - diff;

                if (diff >= 135)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Returns the 3D distance accounting for elevation difference (in grid squares).
        /// </summary>
        public static double GetElevationAdjustedDistance(
            (int x, int y) a, (int x, int y) b,
            int elevationA, int elevationB)
        {
            double dx = a.x - b.x;
            double dy = a.y - b.y;
            double dz = (elevationA - elevationB) / 5.0; // Scale feet to grid squares (1 square = 5 ft)
            return Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }
    }
}
