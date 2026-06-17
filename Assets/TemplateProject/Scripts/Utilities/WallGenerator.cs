
#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Runtime.LevelCreation;
using TemplateProject.Scripts.Runtime.Models;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{


	public class WallGenerator : MonoBehaviour
	{
		private LevelData _levelData;
		private LevelCreator _creator;
		private GameObject wallParent;
		// private GridManager _gridManager;

		private ItemPositionMap<Wall> wallPositionMap;
		private List<Wall> wallsToPostInit = new List<Wall>();


		public void Init(LevelData levelData, LevelCreator creator, int levelIndex)
		{
			_levelData = levelData;
			_creator = creator;

			wallParent = new GameObject("WallParent");
			wallParent.transform.SetParent(GameObject.Find($"Level_{levelIndex}").transform);

			// _gridManager = GameObject.Find("GridManager").GetComponent<GridManager>();
			wallPositionMap = new ItemPositionMap<Wall>();
			wallsToPostInit = new List<Wall>();
		}


		public void GenerateWalls()
		{
			int width = _levelData.Width;
			int height = _levelData.Height;
			OutsideInGridScan();
			GeneratePlaneWalls(width, height);
		}


		public void OutsideInGridScan()
		{
			int width = _levelData.Width;
			int height = _levelData.Height;
			int maxLayer = Mathf.Max(width + 1, height + 1) / 2;

			for (int layer = -1; layer <= maxLayer; layer++)
			{
				for (int x = layer; x < width - layer; x++)
				{
					int y = layer;
					ProcessCell(x, y);
				}

				for (int y = layer + 1; y < height - layer; y++)
				{
					int x = width - 1 - layer;
					ProcessCell(x, y);
				}

				for (int x = width - 2 - layer; x >= layer; x--)
				{
					int y = height - 1 - layer;
					ProcessCell(x, y);
				}

				for (int y = height - 2 - layer; y > layer; y--)
				{
					int x = layer;
					ProcessCell(x, y);
				}
			}

			wallsToPostInit.ForEach(wall => { wall.SecondInit(wallPositionMap); });
			wallsToPostInit.ForEach(wall => { wall.LastInit(wallPositionMap); });
		}


		private void ProcessCell(int x, int y)
		{
			Vector2Int current = new(x, y);
			if (IsInsideGrid(current) && IsActive(current))
			{
				return;
			}

			if (!wallPositionMap.IsPositionEmpty(current))
			{
				return;
			}

			Vector3 spawnPosition = _creator.GridSpaceToWorldSpace(x, y);


			var wallObject = PrefabUtility.InstantiatePrefab(_creator.prefabs.wallPrefab) as GameObject;
			wallObject.transform.SetParent(wallParent.transform);
			wallObject.transform.position = spawnPosition;
			wallObject.name = wallObject.name + " (" + x + "x" + y + ") ";

			var wallScript = wallObject.GetComponent<Wall>();
			wallScript.Init(current);

			try
			{
				wallPositionMap.Add(wallScript, new Vector2Int(x, y));
			}
			catch (Exception e)
			{
				Debug.Log("Cannot Processing cell: " + current);

				Console.WriteLine(e);
				throw;
			}

			wallsToPostInit.Add(wallScript);
		}


		private void GetActiveNeighbors(Vector2Int pos, out List<Vector2Int> activeNeighbors)
		{
			activeNeighbors = new List<Vector2Int>();

			Vector2Int[] directions =
			{
				Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
			};
			foreach (var dir in directions)
			{
				Vector2Int neighborPos = pos + dir;

				if (!IsInsideGrid(neighborPos))
				{
					continue;
				}

				if (_levelData.GridData[neighborPos.x, neighborPos.y].isActive)
				{
					activeNeighbors.Add(neighborPos);
				}
			}
		}


		private void GeneratePlaneWalls(int width, int height)
		{
			GameObject planeWallPrefab = _creator.prefabs.planeWallPrefab;

			foreach (var direction in Enum.GetValues(typeof(EnumHolder.Direction)))
			{
				Vector3 spawnPos;
				Vector3 spawnRot;

				switch (direction)
				{
					case EnumHolder.Direction.Up:
						spawnPos = _creator.GridSpaceToWorldSpace(width / 2, height);
						spawnRot = new Vector3(0, 180, 0);
						break;

					case EnumHolder.Direction.Down:
						spawnPos = _creator.GridSpaceToWorldSpace(width / 2, -1);
						spawnRot = new Vector3(0, 0, 0);
						break;

					case EnumHolder.Direction.Left:
						spawnPos = _creator.GridSpaceToWorldSpace(-1, height / 2f);
						spawnRot = new Vector3(0, 90, 0);
						break;

					case EnumHolder.Direction.Right:
						spawnPos = _creator.GridSpaceToWorldSpace(width, height / 2f);
						spawnRot = new Vector3(0, -90, 0);
						break;

					default:
						spawnPos = _creator.GridSpaceToWorldSpace(width / 2, height);
						spawnRot = new Vector3(0, 0, 0);
						continue;
				}

				spawnPos.y = -2.15f;
				var planeWall = PrefabUtility.InstantiatePrefab(planeWallPrefab) as GameObject;
				planeWall.transform.position = spawnPos;
				planeWall.transform.eulerAngles = spawnRot;
				planeWall.transform.SetParent(wallParent.transform);
			}
		}


		private void GetOuterNeighbors(Vector2Int pos, out List<Vector2Int> outerNeighbors)
		{
			outerNeighbors = new List<Vector2Int>();

			Vector2Int[] directions =
			{
				Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
			};
			foreach (var dir in directions)
			{
				Vector2Int neighborPos = pos + dir;

				if (!IsInsideGrid(neighborPos))
				{
					outerNeighbors.Add(neighborPos);
				}
			}
		}


		private void GetInactiveNeighbors(Vector2Int pos, out List<Vector2Int> inactiveNeighbors)
		{
			inactiveNeighbors = new List<Vector2Int>();

			Vector2Int[] directions =
			{
				Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
			};
			foreach (var dir in directions)
			{
				Vector2Int neighborPos = pos + dir;

				if (!IsInsideGrid(neighborPos))
				{
					continue;
				}

				if (!_levelData.GridData[neighborPos.x, neighborPos.y].isActive)
				{
					inactiveNeighbors.Add(neighborPos);
				}
			}
		}


		private bool IsInsideGrid(Vector2Int pos) { return pos.x >= 0 && pos.x < _levelData.Width && pos.y >= 0 && pos.y < _levelData.Height; }

		private bool IsInactive(Vector2Int pos) { return _levelData.GridData[pos.x, pos.y].isActive == false; }
		private bool IsActive(Vector2Int pos) { return _levelData.GridData[pos.x, pos.y].isActive == true; }

		private bool IsOutOfBounds(Vector2Int pos) { return pos.x < 0 || pos.x >= _levelData.Width || pos.y < 0 || pos.y >= _levelData.Height; }


		private Vector2Int DirectionToVector2Int(EnumHolder.Direction direction)
		{
			return direction switch
			{
				EnumHolder.Direction.Up => Vector2Int.up,
				EnumHolder.Direction.Down => Vector2Int.down,
				EnumHolder.Direction.Left => Vector2Int.left,
				EnumHolder.Direction.Right => Vector2Int.right,
				_ => Vector2Int.zero
			};
		}


		private bool AreDiagonallyAdjacent(Vector2Int a, Vector2Int b)
		{
			int dx = Mathf.Abs(a.x - b.x);
			int dy = Mathf.Abs(a.y - b.y);

			return dx == 1 && dy == 1;
		}


		private bool AreAdjacent(Vector2Int a, Vector2Int b)
		{
			int dx = Mathf.Abs(a.x - b.x);
			int dy = Mathf.Abs(a.y - b.y);

			return (dx == 1 && dy == 0) || (dx == 0 && dy == 1);
		}
	}


}
#endif