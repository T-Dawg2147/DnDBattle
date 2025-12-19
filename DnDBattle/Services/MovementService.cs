using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace DnDBattle.Services
{
    public static class MovementService
    {
        public static HashSet<(int x, int y)> GetReachableSquares(int startX, int startY, int maxSquares,
            int gridWidth, int gridHeight, Func<int, int, bool> isCellBlocked)
        {
            var visited = new HashSet<(int, int)>();
            var q = new Queue<((int x, int y) pos, int cost)>();
            q.Enqueue(((startX, startY), 0));
            visited.Add((startX, startY));

            while (q.Count > 0)
            {
                var cur = q.Dequeue();
                var (x, y) = cur.pos;
                int cost = cur.cost;
                if (cost >= maxSquares) continue;

                foreach (var d in new (int dx, int dy)[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
                {
                    int nx = x + d.dx, ny = y + d.dy;
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                    if (visited.Contains((nx, ny))) continue;
                    if (isCellBlocked(nx, ny)) continue;
                    visited.Add((nx, ny));
                    q.Enqueue(((nx, ny), cost + 1));
                }
            }
            return visited;
        }

        public static List<(int x, int y)> FindPathAStar((int x, int y) start, (int x, int y) goal, int gridWidth, int gridHeight, Func<int, int, bool> isWalkable)
        {
            var open = new PriorityQueue<(int x, int y), double>();
            var gScore = new Dictionary<(int x, int y), int>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();

            (int x, int y)[] neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

            gScore[start] = 0;
            open.Enqueue(start, Heuristic(start, goal));

            while (open.Count > 0)
            {
                var current = open.Dequeue();
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

                int currentG = gScore.ContainsKey(current) ? gScore[current] : int.MaxValue;
                foreach (var d in neighbors)
                {
                    var nx = current.x + d.x;
                    var ny = current.y + d.y;
                    var neighbor = (nx, ny);
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight) continue;
                    if (!isWalkable(nx, ny) && neighbor != goal) continue;

                    int tentativeG = currentG + 1;
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

        private static int Heuristic((int x, int y) a, (int x, int y) b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        public static HashSet<int> ComputeAOOIndices(List<(int x, int y)> path, IEnumerable<(int x, int y)> enemyPositions)
        {
            var aooIndices = new HashSet<int>();
            if (path == null || path.Count < 2) return aooIndices;

            var enemyAdjacency = new Dictionary<(int x, int y), HashSet<(int x, int y)>>();
            foreach (var e in enemyPositions)
            {
                var adj = new HashSet<(int x, int y)>();
                adj.Add(e);
                adj.Add((e.x + 1, e.y));
                adj.Add((e.x - 1, e.y));
                adj.Add((e.x, e.y + 1));
                adj.Add((e.x, e.y - 1));
                enemyAdjacency[e] = adj;
            }

            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var cur = path[i];
                foreach (var kv in enemyAdjacency)
                {
                    var enemy = kv.Key;
                    var adj = kv.Value;
                    bool prevAdj = adj.Contains(prev);
                    bool curAdj = adj.Contains(cur);
                    if (prevAdj && !curAdj)
                        aooIndices.Add(i);
                }
            }
            return aooIndices;
        }
    }
}
