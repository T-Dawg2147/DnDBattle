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

        /// <summary>
        /// Finds the shortest path using A* algorithm with a node limit to prevent freezes.
        /// </summary>
        /// <param name="start">Starting position</param>
        /// <param name="goal">Target position</param>
        /// <param name="gridWidth">Grid width in cells</param>
        /// <param name="gridHeight">Grid height in cells</param>
        /// <param name="isWalkable">Function to check if a cell is walkable</param>
        /// <returns>List of positions forming the path, or empty list if no path found or limit exceeded</returns>
        public static List<(int x, int y)> FindPathAStar(
            (int x, int y) start,
            (int x, int y) goal,
            int gridWidth,
            int gridHeight,
            Func<int, int, bool> isWalkable)
        {
            int distance = Heuristic(start, goal);
            int dynamicLimit = Math.Min(Options.MaxAStarNodes, distance * distance * 4);

            // Early exit: if start equals goal, return single-point path
            if (start == goal)
                return new List<(int x, int y)> { start };

            // Early exit: if goal is out of bounds, no path possible
            if (goal.x < 0 || goal.y < 0 || goal.x >= gridWidth || goal.y >= gridHeight)
                return new List<(int, int)>();

            var open = new PriorityQueue<(int x, int y), double>();
            var gScore = new Dictionary<(int x, int y), int>();
            var cameFrom = new Dictionary<(int x, int y), (int x, int y)>();
            var closedSet = new HashSet<(int x, int y)>();  // Track processed nodes

            (int x, int y)[] neighbors = { (1, 0), (-1, 0), (0, 1), (0, -1) };

            gScore[start] = 0;
            open.Enqueue(start, Heuristic(start, goal));

            int nodesProcessed = 0;
            int maxNodes = Options.MaxAStarNodes;  // Use the configured limit!

            while (open.Count > 0)
            {
                // SAFETY CHECK: Enforce node limit to prevent UI freezes
                nodesProcessed++;
                if (nodesProcessed > maxNodes)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"A* pathfinding hit node limit ({maxNodes}) - path from ({start.x},{start.y}) to ({goal.x},{goal.y}) aborted");

                    // Return partial path to the closest point we found, or empty if none
                    return GetBestPartialPath(cameFrom, gScore, goal);
                }

                var current = open.Dequeue();

                // Skip if we've already processed this node (can happen with priority queue)
                if (closedSet.Contains(current))
                    continue;

                closedSet.Add(current);

                // SUCCESS: Found the goal!
                if (current == goal)
                {
                    return ReconstructPath(cameFrom, current);
                }

                int currentG = gScore.TryGetValue(current, out var g) ? g : int.MaxValue;

                foreach (var d in neighbors)
                {
                    var nx = current.x + d.x;
                    var ny = current.y + d.y;
                    var neighbor = (nx, ny);

                    // Bounds check
                    if (nx < 0 || ny < 0 || nx >= gridWidth || ny >= gridHeight)
                        continue;

                    // Skip already processed nodes
                    if (closedSet.Contains(neighbor))
                        continue;

                    // Walkability check (goal is always considered walkable for pathing purposes)
                    if (!isWalkable(nx, ny) && neighbor != goal)
                        continue;

                    int tentativeG = currentG + 1;

                    if (!gScore.TryGetValue(neighbor, out var neighborG) || tentativeG < neighborG)
                    {
                        cameFrom[neighbor] = current;
                        gScore[neighbor] = tentativeG;
                        double priority = tentativeG + Heuristic(neighbor, goal);
                        open.Enqueue(neighbor, priority);
                    }
                }
            }

            // No path found
            return new List<(int, int)>();
        }

        /// <summary>
        /// Reconstructs the path from the cameFrom dictionary
        /// </summary>
        private static List<(int x, int y)> ReconstructPath(
            Dictionary<(int x, int y), (int x, int y)> cameFrom,
            (int x, int y) current)
        {
            var path = new List<(int x, int y)> { current };

            while (cameFrom.ContainsKey(current))
            {
                current = cameFrom[current];
                path.Add(current);
            }

            path.Reverse();
            return path;
        }

        /// <summary>
        /// When we hit the node limit, return the best partial path we found
        /// (path to the node closest to the goal)
        /// Optimized to avoid LINQ OrderBy allocation.
        /// </summary>
        private static List<(int x, int y)> GetBestPartialPath(
            Dictionary<(int x, int y), (int x, int y)> cameFrom,
            Dictionary<(int x, int y), int> gScore,
            (int x, int y) goal)
        {
            if (cameFrom.Count == 0)
                return new List<(int, int)>();

            // Find the explored node closest to the goal - manual loop instead of LINQ OrderBy
            (int x, int y) closestNode = default;
            int minDist = int.MaxValue;
            
            foreach (var node in cameFrom.Keys)
            {
                int dist = Heuristic(node, goal);
                if (dist < minDist)
                {
                    minDist = dist;
                    closestNode = node;
                }
            }

            if (closestNode == default)
                return new List<(int, int)>();

            return ReconstructPath(cameFrom, closestNode);
        }

        private static int Heuristic((int x, int y) a, (int x, int y) b) =>
            Math.Abs(a.x - b.x) + Math.Abs(a.y - b.y);

        /// <summary>
        /// Computes the indices of path steps where an Attack of Opportunity (AOO) would trigger.
        /// Optimized to pre-compute adjacency only once per enemy and use efficient set lookups.
        /// </summary>
        public static HashSet<int> ComputeAOOIndices(List<(int x, int y)> path, IEnumerable<(int x, int y)> enemyPositions)
        {
            var aooIndices = new HashSet<int>();
            if (path == null || path.Count < 2) return aooIndices;

            // Pre-compute adjacency sets for all enemies once
            // This is O(enemies) instead of O(enemies * path_length)
            // Check for common collection types to avoid unnecessary ToList() allocation
            var enemyList = enemyPositions as IList<(int x, int y)> 
                         ?? enemyPositions as IReadOnlyList<(int x, int y)> as IList<(int x, int y)>
                         ?? enemyPositions.ToList();
            if (enemyList.Count == 0) return aooIndices;

            var enemyAdjacency = new ((int x, int y) enemy, HashSet<(int x, int y)> adj)[enemyList.Count];
            for (int i = 0; i < enemyList.Count; i++)
            {
                var e = enemyList[i];
                enemyAdjacency[i] = (e, new HashSet<(int x, int y)>
                {
                    e,
                    (e.x + 1, e.y),
                    (e.x - 1, e.y),
                    (e.x, e.y + 1),
                    (e.x, e.y - 1)
                });
            }

            // Check each path step
            for (int i = 1; i < path.Count; i++)
            {
                var prev = path[i - 1];
                var cur = path[i];

                // Use array iteration instead of dictionary foreach for better performance
                for (int j = 0; j < enemyAdjacency.Length; j++)
                {
                    var adj = enemyAdjacency[j].adj;

                    // AOO triggers when leaving an enemy's reach
                    if (adj.Contains(prev) && !adj.Contains(cur))
                    {
                        aooIndices.Add(i);
                        break; // Only need to mark this index once
                    }
                }
            }

            return aooIndices;
        }
    }
}