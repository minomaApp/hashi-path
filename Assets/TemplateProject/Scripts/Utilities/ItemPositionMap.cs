using System;
using System.Collections.Generic;
using UnityEngine;

namespace TemplateProject.Scripts.Utilities
{
    public interface IPositionMappable
    {
        public Vector2Int GetGridPosition();
    }

    [Serializable]
    public class ItemPositionMap<T>
    {
        private Dictionary<Vector2Int, T> map = new();

        private Vector2Int[] directions = new Vector2Int[]
        {
            new Vector2Int(0, 1), //Up
            new Vector2Int(0, -1), //Down
            new Vector2Int(-1, 0), //Left
            new Vector2Int(1, 0) //Right
        };

        public void Add(T item, Vector2Int position)
        {
            map.Add(position, item);
        }

        public void Remove(Vector2Int position)
        {
            map.Remove(position);
        }

        public void UpdateItemPosition(T item, Vector2Int oldPosition, Vector2Int newPosition)
        {
            if (map.ContainsKey(oldPosition) && map[oldPosition].Equals(item) && !map.ContainsKey(newPosition))
            {
                Remove(oldPosition);
                Add(item, newPosition);
            }

            else
            {
                Debug.LogError("Update Item Position Map Failed. Possible mistakes:" +
                               "1- old position doesn't exist or contain the item. 2- new position is not empty.");
            }
        }

        public bool TryGetItem(Vector2Int position, out T item)
        {
            var customPosition = new Vector2Int(position.x, position.y);
            return map.TryGetValue(customPosition, out item);
        }

        public bool TryGetPosition(T item, out Vector2Int position)
        {
            foreach (var keyValuePair in map)
            {
                if (keyValuePair.Value.Equals(item)) continue;
                position = keyValuePair.Key;
                return true;
            }

            position = Vector2Int.zero;
            return false;
        }

        public bool IsPositionEmpty(Vector2Int position)
        {
            return !map.ContainsKey(position);
        }

        public bool TryGetNeighbourItem(Vector2Int position, Vector2Int direction, out T neighbour)
        {
            var neighbourPosition = position + direction;
            return map.TryGetValue(neighbourPosition, out neighbour);
        }

        public List<T> GetNeighbors(Vector2Int position)
        {
            var neighbours = new List<T>();

            foreach (var direction in directions)
            {
                if (TryGetNeighbourItem(position, direction, out var neighbour)) neighbours.Add(neighbour);
            }

            return neighbours;
        }
    }
}