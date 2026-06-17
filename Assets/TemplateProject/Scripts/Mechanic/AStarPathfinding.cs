using System.Collections.Generic;
using System.Linq;
using TemplateProject.Scripts.Runtime.Models;
using UnityEngine;

namespace TemplateProject.Scripts.Mechanic
{
    public class AStarPathfinding
    {
        [Header("Parameters")]
        private readonly GridBase[,] gridBases;
        private readonly int gridWidth;
        private readonly int gridHeight;

        public AStarPathfinding(GridBase[,] grid)
        {
            gridBases = grid;
            gridWidth = grid.GetLength(0);
            gridHeight = grid.GetLength(1);
        }

        public List<GridBase> FindPath(Vector2Int startPos)
        {
            var openSet = new List<GridBase>();
            var closedSet = new HashSet<GridBase>();

            var startNode = gridBases[startPos.x, startPos.y];
            openSet.Add(startNode);

            while (openSet.Count > 0)
            {
                var currentNode = GetLowestFCostNode(openSet);
                openSet.Remove(currentNode);
                closedSet.Add(currentNode);
                currentNode.SetVisited(true);

                if (currentNode.GetYAxis() == 0)
                    return RetracePath(startNode, currentNode);

                foreach (var neighbor in GetNeighbors(currentNode))
                {
                    if (neighbor.GetVisited() || neighbor.GetIsClosed() || closedSet.Contains(neighbor))
                        continue;

                    var newGCost = currentNode.GetGCost() + GetDistance(currentNode, neighbor);
                    if (!(newGCost < neighbor.GetGCost()) && openSet.Contains(neighbor)) continue;
                    neighbor.SetGCost(newGCost);
                    neighbor.SetHCost(GetHeuristic(neighbor));
                    neighbor.SetBaseParent(currentNode);

                    if (!openSet.Contains(neighbor))
                        openSet.Add(neighbor);
                }
            }

            return null;
        }

        private GridBase GetLowestFCostNode(List<GridBase> openSet)
        {
            var bestNode = openSet[0];
            foreach (var node in openSet.Where(node => node.GetFCost() < bestNode.GetFCost()))
                bestNode = node;
            return bestNode;
        }

        private List<GridBase> GetNeighbors(GridBase node)
        {
            var neighbors = new List<GridBase>();
            Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var dir in directions)
            {
                var newX = node.GetXAxis() + dir.x;
                var newY = node.GetYAxis() + dir.y;

                if (newX >= 0 && newX < gridWidth && newY >= 0 && newY < gridHeight)
                    neighbors.Add(gridBases[newX, newY]);
            }

            return neighbors;
        }

        private float GetDistance(GridBase a, GridBase b)
        {
            return Vector2Int.Distance(new Vector2Int(a.GetXAxis(), a.GetYAxis()), new Vector2Int(b.GetXAxis(), b.GetYAxis()));
        }

        private float GetHeuristic(GridBase node)
        {
            return node.GetYAxis(); 
        }

        private List<GridBase> RetracePath(GridBase startNode, GridBase endNode)
        {
            var path = new List<GridBase>();
            var currentNode = endNode;

            while (currentNode != startNode)
            {
                path.Add(currentNode);
                currentNode = currentNode.GetBaseParent();
            }

            path.Reverse();
            return path;
        }
    
        public void ResetVisitedStates()
        {
            foreach (var gridBase in gridBases)
            {
                gridBase.ResetVisited();
            }
        }
    }
}
