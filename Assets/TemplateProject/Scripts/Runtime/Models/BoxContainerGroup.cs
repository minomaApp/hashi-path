using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class BoxContainerGroup : MonoBehaviour
    {
        [SerializeField] private List<BoxContainer> ownedContainers = new List<BoxContainer>();
        [SerializeField] private BoxContainerMovement groupMovement;

        public void AssignGroupMovement(BoxContainerMovement movement)
        {
            groupMovement = movement;
        }

        public void AddToGroup(BoxContainer container)
        {
            if (ownedContainers.Contains(container)) return;
            ownedContainers.Add(container);
        }

        public void RemoveFromGroup(BoxContainer container)
        {
            if (!ownedContainers.Contains(container)) return;
            ownedContainers.Remove(container);
        }

        public BoxContainer GetLastContainer()
        {
            if (ownedContainers.Count <= 0) return null;
            return ownedContainers[^1];
        }

#if UNITY_EDITOR
        public void HandleSortAndSpawnConnection(GameObject connectionPrefab)
        {
            if (ownedContainers == null) return;
            if (ownedContainers.Count <= 1) return;

            var adjMap = new Dictionary<BoxContainer, List<BoxContainer>>();
            foreach (var container in ownedContainers)
            {
                var gb = container.GetBelongedGrid();
                var x = gb.GetXAxis();
                var y = gb.GetYAxis();

                var neighbors = ownedContainers
                    .Where(other =>
                    {
                        if (other == container) return false;
                        var gb2 = other.GetBelongedGrid();
                        var dx = Mathf.Abs(gb2.GetXAxis() - x);
                        var dy = Mathf.Abs(gb2.GetYAxis() - y);
                        return dx + dy == 1;
                    })
                    .ToList();

                adjMap[container] = neighbors;
            }

            var endpoints = adjMap
                .Where(kv => kv.Value.Count == 1)
                .Select(kv => kv.Key)
                .ToList();

            BoxContainer start;
            if (endpoints.Count > 0)
            {
                start = endpoints
                    .OrderBy(c =>
                    {
                        var gb = c.GetBelongedGrid();
                        return gb.GetXAxis() + gb.GetYAxis();
                    })
                    .First();
            }
            else
            {
                start = ownedContainers[0];
            }

            var ordered = new List<BoxContainer>();
            var visited = new HashSet<BoxContainer>();
            var current = start;
            while (current && !visited.Contains(current))
            {
                ordered.Add(current);
                visited.Add(current);
                current = adjMap[current].FirstOrDefault(n => !visited.Contains(n));
            }

            ownedContainers.Clear();
            ownedContainers.AddRange(ordered);

            for (var i = 0; i < ownedContainers.Count - 1; i++)
            {
                var container = ownedContainers[i];
                var nextContainer = ownedContainers[i + 1];
                var dir = (nextContainer.transform.position - container.transform.position).normalized;
                dir.y = 0;
                nextContainer.transform.rotation = Quaternion.LookRotation(-1 * dir);
                if (i == 0)
                {
                    container.transform.rotation = Quaternion.LookRotation(-1 * dir);
                }

                var midPoint = (container.transform.position + nextContainer.transform.position) / 2;

                var connection = PrefabUtility.InstantiatePrefab(connectionPrefab) as GameObject;
                connection.transform.position = midPoint + new Vector3(0f, 0.25f, 0f);
                connection.transform.localEulerAngles = new Vector3(0f, Quaternion.LookRotation(dir).eulerAngles.y, 0f);
                connection.transform.SetParent(container.transform);
            }
        }
#endif
        public List<BoxContainer> GetContainers()
        {
            return ownedContainers;
        }

        public BoxContainer GetFirstContainer()
        {
            if (ownedContainers.Count <= 0) return null;
            return ownedContainers[0];
        }

        public BoxContainerMovement GetGroupMovement()
        {
            return groupMovement;
        }

        public void DisableOutlines()
        {
            foreach (var container in ownedContainers)
            {
                container.DisableOutline();
            }
        }
    }
}