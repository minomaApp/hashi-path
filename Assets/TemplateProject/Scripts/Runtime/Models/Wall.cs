using System;
using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data.Enums;
using BoxPuller.Scripts.Data.SO;
using TemplateProject.Scripts.Utilities;
using TemplateProject.Scripts.Utilities.EditorUtilities.InspectorLogger;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{


	public class Wall : MonoBehaviour, IPositionMappable
	{
		[Header("References")]
		[SerializeField] private GamePrefabs gamePrefabs;

		[Header("Info")]
		[SerializeField] private Transform visualTransform;
		[SerializeField] private EnumHolder.WallType wallType;
		[SerializeField] private Vector2Int gridPosition;
		[SerializeField] private int variantType;

		public Vector2Int GetGridPosition() => gridPosition;

		public void Init(Vector2Int gridPosition) { this.gridPosition = gridPosition; }

		public void SecondInit(ItemPositionMap<Wall> wallPositionMap) { DecideWallPrefab(wallPositionMap); }


		public void LastInit(ItemPositionMap<Wall> wallPositionMap)
		{
			if (wallType is EnumHolder.WallType.Corner) HandleCornerVariant(wallPositionMap);
			if (wallType is EnumHolder.WallType.ThreeSidesEmpty) HandleThreeEmptyVariant(wallPositionMap);
			if (wallType is EnumHolder.WallType.NoSidesEmpty) HandleNoEmptyVariant(wallPositionMap);

		}


		private void HandleCornerVariant(ItemPositionMap<Wall> wallPositionMap)
		{
			variantType = 0;

			var checkDirection = Vector2Int.zero;
			const float epsilon = 0.2f;

			if (visualTransform.eulerAngles.y == 0f) checkDirection = Vector2Int.down + Vector2Int.left;
			else if (Math.Abs(visualTransform.eulerAngles.y - 90f) < epsilon) checkDirection = Vector2Int.up + Vector2Int.left;
			else if (Math.Abs(visualTransform.eulerAngles.y - 180f) < epsilon) checkDirection = Vector2Int.up + Vector2Int.right;
			else if (Math.Abs(visualTransform.eulerAngles.y - 270f) < epsilon) checkDirection = Vector2Int.down + Vector2Int.right;

			this.Log("Check Direction: " + checkDirection, LogStyles.Positive);
			this.Log("Variant Type: " + variantType, LogStyles.Positive);

			if (!wallPositionMap.IsPositionEmpty(gridPosition + checkDirection)) return;

			var wallRotation = visualTransform.rotation;
			SpawnVariant(wallRotation);
		}


		private void HandleThreeEmptyVariant(ItemPositionMap<Wall> wallPositionMap)
		{
			var firstCheckDirection = Vector2Int.zero;
			var secondCheckDirection = Vector2Int.zero;
			const float epsilon = 0.2f;

			if (Math.Abs(visualTransform.eulerAngles.y) < epsilon)
			{
				firstCheckDirection = Vector2Int.up + Vector2Int.right;
				secondCheckDirection = Vector2Int.down + Vector2Int.right;
			}

			else if (Math.Abs(visualTransform.eulerAngles.y - 90f) < epsilon)
			{
				firstCheckDirection = Vector2Int.down + Vector2Int.right;
				secondCheckDirection = Vector2Int.down + Vector2Int.left;
			}

			else if (Math.Abs(visualTransform.eulerAngles.y - 180f) < epsilon)
			{
				firstCheckDirection = Vector2Int.down + Vector2Int.left;
				secondCheckDirection = Vector2Int.up + Vector2Int.left;
			}

			else if (Math.Abs(visualTransform.eulerAngles.y - 270f) < epsilon)
			{
				firstCheckDirection = Vector2Int.up + Vector2Int.left;
				secondCheckDirection = Vector2Int.up + Vector2Int.right;
			}

			this.Log("First Check Direction: " + firstCheckDirection, LogStyles.Positive);
			this.Log("Second Check Direction: " + secondCheckDirection, LogStyles.Positive);

			var isFirstPositionEmpty = wallPositionMap.IsPositionEmpty(gridPosition + firstCheckDirection);
			var isSecondPositionEmpty = wallPositionMap.IsPositionEmpty(gridPosition + secondCheckDirection);
			if (!isFirstPositionEmpty && !isSecondPositionEmpty) return;

			if (isFirstPositionEmpty && isSecondPositionEmpty) variantType = 0;
			else if (isFirstPositionEmpty) variantType = 1;
			else variantType = 2;

			this.Log("Variant Type: " + variantType, LogStyles.Positive);

			var wallRotation = visualTransform.rotation;
			SpawnVariant(wallRotation);
		}


		private void HandleNoEmptyVariant(ItemPositionMap<Wall> wallPositionMap)
		{
			var upRight = gridPosition + Vector2Int.up + Vector2Int.right;
			var upLeft = gridPosition + Vector2Int.up + Vector2Int.left;
			var downRight = gridPosition + Vector2Int.down + Vector2Int.right;
			var downLeft = gridPosition + Vector2Int.down + Vector2Int.left;

			var checkPositions = new[]
			{
				upRight,
				upLeft,
				downRight,
				downLeft
			};

			var wallRotation = visualTransform.rotation;

			var isFourCornersEmpty = checkPositions.All(wallPositionMap.IsPositionEmpty);
			if (isFourCornersEmpty)
			{
				this.Log("Four Sides Empty, Variant Type 0", LogStyles.Positive);

				variantType = 0;
				SpawnVariant(wallRotation);
				return;
			}

			var isSingleCornerEmpty = checkPositions.Count(wallPositionMap.IsPositionEmpty) == 1;
			if (isSingleCornerEmpty)
			{
				this.Log("Single Side Empty, Variant Type 1", LogStyles.Positive);

				variantType = 1;
				if (wallPositionMap.IsPositionEmpty(upRight)) wallRotation = Quaternion.Euler(0, 180, 0);
				if (wallPositionMap.IsPositionEmpty(upLeft)) wallRotation = Quaternion.Euler(0, 90, 0);
				if (wallPositionMap.IsPositionEmpty(downRight)) wallRotation = Quaternion.Euler(0, 270, 0);
				if (wallPositionMap.IsPositionEmpty(downLeft)) wallRotation = Quaternion.Euler(0, 0, 0);

				SpawnVariant(wallRotation);
				return;
			}

			var isTwoCornerEmpty = checkPositions.Count(wallPositionMap.IsPositionEmpty) == 2;
			if (isTwoCornerEmpty)
			{
				var isUpEmpty = wallPositionMap.IsPositionEmpty(upLeft) && wallPositionMap.IsPositionEmpty(upRight);
				var isDownEmpty = wallPositionMap.IsPositionEmpty(downLeft) && wallPositionMap.IsPositionEmpty(downRight);
				var isRightEmpty = wallPositionMap.IsPositionEmpty(upRight) && wallPositionMap.IsPositionEmpty(downRight);
				var isLeftEmpty = wallPositionMap.IsPositionEmpty(upLeft) && wallPositionMap.IsPositionEmpty(downLeft);

				//Single side: up, down, right, left
				if (isUpEmpty || isDownEmpty || isRightEmpty || isLeftEmpty)
				{
					variantType = 2;
					this.Log("Two Same Side Corners Empty, Variant Type 2", LogStyles.Positive);

					if (isUpEmpty) wallRotation = Quaternion.Euler(0, 180, 0);
					if (isDownEmpty) wallRotation = Quaternion.Euler(0, 0, 0);
					if (isRightEmpty) wallRotation = Quaternion.Euler(0, 270, 0);
					if (isLeftEmpty) wallRotation = Quaternion.Euler(0, 90, 0);
				}

				else
				{
					variantType = 3;
					this.Log("Two Opposite Side Corners Empty, Variant Type 3", LogStyles.Positive);

					if (wallPositionMap.IsPositionEmpty(upRight) && wallPositionMap.IsPositionEmpty(downLeft)) wallRotation = Quaternion.Euler(0, 180, 0);
					if (wallPositionMap.IsPositionEmpty(upLeft) && wallPositionMap.IsPositionEmpty(downRight)) wallRotation = Quaternion.Euler(0, 90, 0);
				}

				SpawnVariant(wallRotation);
				return;
			}

			var isThreeCornerEmpty = checkPositions.Count(wallPositionMap.IsPositionEmpty) == 3;
			if (isThreeCornerEmpty)
			{
				variantType = 4;
				this.Log("Three Corners Empty, Variant Type 4", LogStyles.Positive);

				if (!wallPositionMap.IsPositionEmpty(upRight)) wallRotation = Quaternion.Euler(0, 180, 0);
				if (!wallPositionMap.IsPositionEmpty(upLeft)) wallRotation = Quaternion.Euler(0, 90, 0);
				if (!wallPositionMap.IsPositionEmpty(downRight)) wallRotation = Quaternion.Euler(0, 270, 0);
				if (!wallPositionMap.IsPositionEmpty(downLeft)) wallRotation = Quaternion.Euler(0, 0, 0);

				SpawnVariant(wallRotation);
				return;
			}
		}


		private void SpawnVariant(Quaternion wallRotation)
		{
#if UNITY_EDITOR
			DestroyImmediate(visualTransform.gameObject);
			var visual = PrefabUtility.InstantiatePrefab(gamePrefabs.GetWallVariantPrefab(wallType, variantType)) as GameObject;
			visualTransform = visual.transform;
			visualTransform.SetParent(transform);
			visualTransform.localPosition = Vector3.zero;
			visualTransform.rotation = wallRotation;
#endif
		}


		private void DecideWallPrefab(ItemPositionMap<Wall> wallPositionMap)
		{
			var occupiedNeighborWalls = wallPositionMap.GetNeighbors(gridPosition);
			var occupiedNeighbors = occupiedNeighborWalls.Select(wall => wall.GetGridPosition()).ToList();

			var occupiedNeighborsString = "";
			foreach (var neighbor in occupiedNeighbors) occupiedNeighborsString += neighbor + " ";
			this.Log($"Occupied Neighbors: " + occupiedNeighborsString, LogStyles.Positive);

			wallType = DetermineWallType(occupiedNeighbors.Count, occupiedNeighbors);
			this.Log("Wall Type: " + wallType, LogStyles.Positive);

			var wallRotation = DetermineWallRotation(occupiedNeighbors);
			this.Log("Wall Rotation: " + wallRotation.eulerAngles, LogStyles.Positive);

#if UNITY_EDITOR
			var visual = PrefabUtility.InstantiatePrefab(gamePrefabs.GetWallPrefab(wallType)) as GameObject;
			visualTransform = visual.transform;
			visualTransform.SetParent(transform);
			visualTransform.localPosition = Vector3.zero;
			visualTransform.rotation = wallRotation;
#endif
		}


		private EnumHolder.WallType DetermineWallType(int occupiedCount, List<Vector2Int> occupiedNeighbors)
		{
			if (occupiedCount == 0) return EnumHolder.WallType.FourSidesEmpty;
			if (occupiedCount == 1) return EnumHolder.WallType.OneSideEmpty;
			if (occupiedCount == 2) return IsStraightLine(occupiedNeighbors) ? EnumHolder.WallType.Straight : EnumHolder.WallType.Corner;
			if (occupiedCount == 3) return EnumHolder.WallType.ThreeSidesEmpty;
			if (occupiedCount == 4) return EnumHolder.WallType.NoSidesEmpty;

			return EnumHolder.WallType.FourSidesEmpty; //Fallback
		}


		private bool IsStraightLine(List<Vector2Int> occupiedNeighbors) { return (occupiedNeighbors[0].x == occupiedNeighbors[1].x) || (occupiedNeighbors[0].y == occupiedNeighbors[1].y); }


		private Quaternion DetermineWallRotation(List<Vector2Int> occupiedNeighbors)
		{
			if (wallType is EnumHolder.WallType.FourSidesEmpty or EnumHolder.WallType.NoSidesEmpty)
			{
				return Quaternion.identity;
			}

			if (wallType is EnumHolder.WallType.OneSideEmpty or EnumHolder.WallType.ThreeSidesEmpty)
			{
				var targetDirection = wallType is EnumHolder.WallType.ThreeSidesEmpty ? FindFirstEmptyDirection(occupiedNeighbors) : FindFirstFullDirection(occupiedNeighbors);
				this.Log("Target Direction: " + targetDirection, LogStyles.Positive);

				var directionRotateAmount = GetDirectionRotateAmount(targetDirection);
				this.Log("Direction Index: " + directionRotateAmount, LogStyles.Positive);

				return Quaternion.Euler(0, directionRotateAmount * 90, 0); // Rotate by multiples of 90 degrees
			}

			if (wallType == EnumHolder.WallType.Straight)
			{
				//Horizontal or vertical
				return (occupiedNeighbors[0].x == occupiedNeighbors[1].x) ? Quaternion.identity : Quaternion.Euler(0, 90, 0);
			}

			if (wallType == EnumHolder.WallType.Corner) return DetermineCornerRotation(occupiedNeighbors);

			return Quaternion.identity; //Fallback
		}


		private Vector2Int FindFirstEmptyDirection(List<Vector2Int> occupiedNeighbors)
		{
			Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

			foreach (var direction in directions)
			{
				if (!occupiedNeighbors.Contains(gridPosition + direction))
				{
					this.Log("First Empty Direction: " + direction, LogStyles.Positive);
					return direction;
				}
			}

			return Vector2Int.zero; //Fallback
		}


		private Vector2Int FindFirstFullDirection(List<Vector2Int> occupiedNeighbors)
		{
			Vector2Int[] directions = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

			foreach (var direction in directions)
			{
				if (occupiedNeighbors.Contains(gridPosition + direction))
				{
					this.Log("First Full Direction: " + direction, LogStyles.Positive);
					return direction;
				}
			}

			return Vector2Int.zero; //Fallback
		}


		private int GetDirectionRotateAmount(Vector2Int direction)
		{
			if (wallType is EnumHolder.WallType.OneSideEmpty)
			{
				if (direction == Vector2Int.up) return 2;
				if (direction == Vector2Int.down) return 0;
				if (direction == Vector2Int.right) return 3;
				if (direction == Vector2Int.left) return 1;
			}

			else if (wallType is EnumHolder.WallType.ThreeSidesEmpty)
			{
				if (direction == Vector2Int.up) return 1;
				if (direction == Vector2Int.down) return 3;
				if (direction == Vector2Int.right) return 2;
				if (direction == Vector2Int.left) return 0;
			}

			return 0; //Fallback
		}


		private Quaternion DetermineCornerRotation(List<Vector2Int> occupiedNeighbors)
		{
			if (!occupiedNeighbors.Contains(gridPosition + Vector2Int.up) && !occupiedNeighbors.Contains(gridPosition + Vector2Int.right))
				return Quaternion.identity; //Top-right corner (0, 0, 0)

			if (!occupiedNeighbors.Contains(gridPosition + Vector2Int.up) && !occupiedNeighbors.Contains(gridPosition + Vector2Int.left))
				return Quaternion.Euler(0, 270, 0); // Top-left corner (rotated 270 degrees)

			if (!occupiedNeighbors.Contains(gridPosition + Vector2Int.down) && !occupiedNeighbors.Contains(gridPosition + Vector2Int.right))
				return Quaternion.Euler(0, 90, 0); //Bottom-right corner (rotated 90 degrees)

			if (!occupiedNeighbors.Contains(gridPosition + Vector2Int.down) && !occupiedNeighbors.Contains(gridPosition + Vector2Int.left))
				return Quaternion.Euler(0, 180, 0); //Bottom-left corner (rotated 180 degrees)

			return Quaternion.identity; //Fallback
		}
	}


}