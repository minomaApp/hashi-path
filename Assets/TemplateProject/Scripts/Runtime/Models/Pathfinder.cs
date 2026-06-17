using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data.Enums;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
	public interface IGridProvider<out T>
	{
		int Width { get; }
		int Height { get; }
		T GetCell(Vector2Int position);
		bool IsCellValid(Vector2Int position);
		bool IsCellEmpty(Vector2Int position);
	
		bool IsCellAvailableToMove(Vector2Int position,EnumHolder.GameColor color );
		Vector3 GetCellPosition(Vector2Int position); // Assuming this method is required for the pathfinding
	}


	public class Pathfinder<T>
	{
		private readonly IGridProvider<T> gridProvider;

		public Pathfinder(IGridProvider<T> gridProvider) { this.gridProvider = gridProvider; }


		public List<Vector3> FindPath(Vector2Int start, Vector2Int target)
		{
			Vector2Int startCell = start;
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
			queue.Enqueue(startCell);
			cameFrom[startCell] = null;

			while (queue.Count > 0)
			{
				Vector2Int current = queue.Dequeue();

				//Debug.Log($"Current: {current}, Target: {target}");
				if (current == target)
				{
					return ReconstructPath(cameFrom, current);
				}

				foreach (Vector2Int neighbor in GetNeighbors(current))
				{
					string debug = "Neighbor: " + neighbor + " Valid: " + gridProvider.IsCellValid(neighbor);
					if (gridProvider.IsCellValid(neighbor))
					{
						debug += " Empty: " + gridProvider.IsCellEmpty(neighbor);
					}

					if (!cameFrom.ContainsKey(neighbor) && gridProvider.IsCellValid(neighbor) &&
					    gridProvider.IsCellEmpty(neighbor))
					{
						queue.Enqueue(neighbor);
						cameFrom[neighbor] = current;
					}
				}
			}

			return new List<Vector3>();
		}


		public List<GridBase> FindPathGridBase(GridBase start, GridBase target,EnumHolder.GameColor color)
		{
			Vector2Int startCell = start.Position;
			Vector2Int targetCell = target.Position;
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
			queue.Enqueue(startCell);
			cameFrom[startCell] = null;

			while (queue.Count > 0)
			{
				Vector2Int current = queue.Dequeue();

				if (current == targetCell)
				{
					return ReconstructPathForGridBase(cameFrom, current);
				}

				foreach (Vector2Int neighbor in GetNeighbors(current))
				{
					if (!cameFrom.ContainsKey(neighbor) && gridProvider.IsCellValid(neighbor) &&
					    (gridProvider.IsCellAvailableToMove(neighbor,color) || neighbor == targetCell))
					{
						queue.Enqueue(neighbor);
						cameFrom[neighbor] = current;
					}
					
				}
			}

			return null;
		}
	
		public List<Vector3> FindPath(Vector2Int start, Func<Vector2Int, bool> stopCondition)
		{
			Vector2Int startCell = start;
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();


			if (gridProvider.IsCellValid(startCell) && !gridProvider.IsCellEmpty(startCell) && stopCondition(startCell))
			{
				return new List<Vector3> { gridProvider.GetCellPosition(startCell) };
			}

			if (!gridProvider.IsCellValid(startCell) || !gridProvider.IsCellEmpty(startCell))
			{
				return null;
			}

			queue.Enqueue(startCell);
			cameFrom[startCell] = null;


			while (queue.Count > 0)
			{
				Vector2Int current = queue.Dequeue();

				foreach (Vector2Int neighbor in GetNeighbors(current))
				{
					if (gridProvider.IsCellValid(neighbor) && !gridProvider.IsCellEmpty(neighbor) &&
					    stopCondition(neighbor))
					{
						cameFrom[neighbor] = current;
						return ReconstructPath(cameFrom, neighbor);
					}
				}

				//Add New Neighbors To Queue
				foreach (Vector2Int neighbor in GetNeighbors(current))
				{
					if (!cameFrom.ContainsKey(neighbor) && gridProvider.IsCellValid(neighbor) &&
					    gridProvider.IsCellEmpty(neighbor))
					{
						queue.Enqueue(neighbor);
						cameFrom[neighbor] = current;
					}
				}
			}

			return null;
		}


		public List<Vector3> FindPathOrClosest(Vector2Int start, Vector2Int target)
		{
			Queue<Vector2Int> queue = new Queue<Vector2Int>();
			Dictionary<Vector2Int, Vector2Int?> cameFrom = new Dictionary<Vector2Int, Vector2Int?>();
			queue.Enqueue(start);
			cameFrom[start] = null;

			Vector2Int closest = start;
			float closestDistance = Vector2Int.Distance(start, target);

			while (queue.Count > 0)
			{
				Vector2Int current = queue.Dequeue();

				float distanceToTarget = Vector2Int.Distance(current, target);
				if (distanceToTarget < closestDistance)
				{
					closest = current;
					closestDistance = distanceToTarget;
				}

				if (current == target)
				{
					return ReconstructPath(cameFrom, current);
				}

				foreach (Vector2Int neighbor in GetNeighbors(current))
				{
					if (!cameFrom.ContainsKey(neighbor) &&
					    gridProvider.IsCellValid(neighbor) &&
					    !gridProvider.IsCellEmpty(neighbor))
					{
						queue.Enqueue(neighbor);
						cameFrom[neighbor] = current;
					}
				}
			}

			return ReconstructPath(cameFrom, closest);
		}


		private List<Vector3> ReconstructPath(Dictionary<Vector2Int, Vector2Int?> cameFrom, Vector2Int end)
		{
			List<Vector3> path = new List<Vector3>();
			Vector2Int? current = end;
			while (current != null)
			{
				path.Add(gridProvider.GetCellPosition(current.Value));
				current = cameFrom[current.Value];
			}

			path.Reverse();
			return path;
		}

		private List<GridBase> ReconstructPathForGridBase(Dictionary<Vector2Int, Vector2Int?> cameFrom, Vector2Int end)
		{
			List<GridBase> path = new List<GridBase>();
			Vector2Int? current = end;
			while (current != null)
			{
				path.Add(gridProvider.GetCell(current.Value) as GridBase);
				current = cameFrom[current.Value];
			}

			path.Reverse();
			return path;
		}

		private List<Vector2Int> GetNeighbors(Vector2Int cell)
		{
			return new List<Vector2Int>
			{
				new Vector2Int(cell.x + 1, cell.y),
				new Vector2Int(cell.x - 1, cell.y),
				new Vector2Int(cell.x, cell.y + 1),
				new Vector2Int(cell.x, cell.y - 1)
			};
		}
	}
}