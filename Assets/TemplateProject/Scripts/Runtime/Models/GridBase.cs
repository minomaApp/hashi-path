using System.Collections.Generic;
using BoxPuller.Scripts.Data;
using BoxPuller.Scripts.Data.Enums;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TemplateProject.Scripts.Runtime.Managers;
using UnityEngine;
using UnityEngine.Serialization;

namespace TemplateProject.Scripts.Runtime.Models
{
    public class GridBase : MonoBehaviour
    {
        [FormerlySerializedAs("stickman")] [Header("Cached References")] [SerializeField]
        protected BoxContainer boxContainer;

        [SerializeField] private Renderer gridRenderer;
        [SerializeField] private GameObject wallObject;
        [SerializeField] private GridBase parent;
        [SerializeField] private List<GridBase> adjacentCells;
        [SerializeField] private List<GridBase> closestPath;
        [SerializeField] private GameColors gameColors;
        public Placeable ownedPlaceable;
        public Collider gridBaseCollider;
        public EnumHolder.GameColor currentColor;

        [Header("Parameters")] [SerializeField]
        private int x, y;

        [SerializeField] private float gCost, hCost;
        private float fCost => gCost + hCost;
        public Material defaultMaterial;
        public Material hoverMaterial;
        public Vector2Int Position => new Vector2Int(x, y);
        private Vector3 _oldScale = Vector3.zero;


        [Header("Flags")] [SerializeField] private bool isClosed;
        [SerializeField] private bool visited;
        public bool isActive;

        private void Update()
        {
            HandleColorMatch();
        }

        public void HandlePath()
        {
            if (!boxContainer) return;

            UniTask.SwitchToTaskPool();
            closestPath = GridManager.instance.GetPathfinder().FindPath(new Vector2Int(x, y));
            UniTask.SwitchToMainThread();

            GridManager.instance.GetPathfinder().ResetVisitedStates();

            if (closestPath == null)
            {
                boxContainer.DisableOutline();
            }
            else
            {
                if (boxContainer.GetIsSecret())
                {
                    boxContainer.ResetColor();
                }

                boxContainer.EnableOutline();
            }
        }

        public void Init(int xAxis, int yAxis)
        {
            x = xAxis;
            y = yAxis;
        }

        private void HandleColorMatch()
        {
            if (!ownedPlaceable) return;
            if (ownedPlaceable.color != currentColor) return;
            var containerChainIsFrozen = (ownedPlaceable as BoxContainerChainNode)?.ContainerChain.IsFrozen;
            if (containerChainIsFrozen != null && (bool)containerChainIsFrozen)
            {
                return;
            }

            (ownedPlaceable as BoxContainerChainNode)?.CheckForColorMatch(this);
        }


        public void AddToAdjacent(GridBase adjacentGridCell)
        {
            if (!adjacentCells.Contains(adjacentGridCell))
            {
                adjacentCells.Add(adjacentGridCell);
            }
        }

        public List<GridBase> GetNeighbors()
        {
            return adjacentCells;
        }

        private void EnableWall()
        {
            gridRenderer.enabled = false;
            wallObject.SetActive(true);
        }

        public void ResetVisited()
        {
            visited = false;
        }

        public void DissociateContainer()
        {
            boxContainer = null;
        }

        public List<GridBase> GetClosestPath()
        {
            return closestPath;
        }

        public int GetYAxis()
        {
            return y;
        }

        public int GetXAxis()
        {
            return x;
        }

        public GridBase GetBaseParent()
        {
            return parent;
        }

        public void SetBaseParent(GridBase newParent)
        {
            parent = newParent;
        }

        public float GetFCost()
        {
            return fCost;
        }

        public float GetHCost()
        {
            return hCost;
        }

        public float GetGCost()
        {
            return gCost;
        }

        public void SetHCost(float newHCost)
        {
            hCost = newHCost;
        }

        public void SetGCost(float newGCost)
        {
            gCost = newGCost;
        }

        public bool GetVisited()
        {
            return visited;
        }

        public bool GetIsClosed()
        {
            return isClosed;
        }

        public BoxContainer GetContainer()
        {
            return boxContainer;
        }

        public void SetVisited(bool flag)
        {
            visited = flag;
        }

        public bool IsEmpty()
        {
            return !ownedPlaceable && isActive;
        }

        public void SetEmpty()
        {
            ownedPlaceable = null;
            boxContainer = null;
        }

        public void SetOccupied(Placeable placeable)
        {
            ownedPlaceable = placeable;
            gridBaseCollider.enabled = true;
            //gameObject.layer = LayerMask.NameToLayer("Chain");
        }

        public void SetActive(bool flag)
        {
            isActive = flag;
            if (gridBaseCollider) gridBaseCollider.enabled = isActive;
            if (gridRenderer) gridRenderer.enabled = isActive;
        }

        public void SetHover(bool isHover)
        {
            if (_oldScale == Vector3.zero)
            {
                _oldScale = gridRenderer.transform.localScale;
            }

            if (isHover)
            {
                if (ownedPlaceable)
                {
                    if (ownedPlaceable is BoxContainerChainNode containerNode)
                    {
                        containerNode.BoxContainer.GetContainerColor();
                    }
                }

                gridRenderer.transform.DOScale(_oldScale * 0.85f, 0.15f);
            }
            else
            {
                // gridRenderer.material.color = new Color(gridRenderer.material.color.r, gridRenderer.material.color.g,
                //     gridRenderer.material.color.b, gridRenderer.material.color.a * 2f);

                gridRenderer.transform.DOScale(_oldScale, 0.15f);
            }
        }

        public void SetColor(EnumHolder.GameColor matchingObjectColor)
        {
            if (ABManager.IsHighlight)
            {
                gridRenderer.material = gameColors.gridMatchColors[(int)matchingObjectColor];
            }

            currentColor = matchingObjectColor;
        }
    }
}