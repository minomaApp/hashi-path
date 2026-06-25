using System;
using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data.Enums;
using TMPro;
using UnityEngine;

namespace BoxPuller.Scripts.Data.SO
{
    [Serializable]
    public struct WallPrefabData
    {
        public EnumHolder.WallType wallType;
        public GameObject prefab;
    }

    [CreateAssetMenu(fileName = "GamePrefabs", menuName = "ScriptableObjects/GamePrefabs")]
    public class GamePrefabs : ScriptableObject
    {
        [Header("Default Prefabs")]
        public GameObject gridPrefab;
        public GameObject gridBlockPrefab;

        [Space(15)]
        [Header("Hashi Game Prefabs")]
        public GameObject islandPrefab;
        public GameObject oneBridgeIslandPrefab;
        public GameObject twoBridgeIslandPrefab;
        public GameObject bridgePrefab;
        public GameObject chainBarrierPrefab;

        public GameObject GetIslandPrefab(EnumHolder.IslandBridgeMode bridgeMode)
        {
            if (bridgeMode == EnumHolder.IslandBridgeMode.DoubleAllowed)
            {
                return twoBridgeIslandPrefab != null
                    ? twoBridgeIslandPrefab
                    : islandPrefab;
            }

            return oneBridgeIslandPrefab != null
                ? oneBridgeIslandPrefab
                : islandPrefab;
        }

        [Space(15)]
        [Header("Legacy Shooter Box Game Prefabs")]
        public GameObject shooterPrefab;
        public GameObject boxPrefab;
        public GameObject bottomSlotNodePrefab;
        public GameObject middleSlotNodePrefab;
        public GameObject flowerPetalPrefab;

        [Header("Legacy Chain Prefabs")]
        public GameObject chainPrefab;
        public GameObject chainNodePrefab;
        public GameObject nodePrefab;
        public GameObject midNodePrefab;
        public GameObject objectSpawnerPrefab;
        public GameObject objectSpawnerPlatform;
        public GameObject matchingObjectPrefab;
        public GameObject completeConfettiPrefab;

        [Header("Legacy Ice Prefabs")]
        public ParticleSystem iceParticle;
        public ParticleSystem iceFinalParticle;
        public TextMeshPro iceTextPrefab;

        [Space(15)]
        [Header("Legacy Wall Prefabs")]
        public GameObject wallPrefab;
        public GameObject planeWallPrefab;
        public List<WallPrefabData> wallPrefabList;
        public List<WallPrefabData> wallVariantList;

        public GameObject GetWallPrefab(EnumHolder.WallType wallType)
        {
            if (wallPrefabList == null)
            {
                return null;
            }

            return wallPrefabList
                .Where(prefabData => prefabData.wallType == wallType)
                .Select(prefabData => prefabData.prefab)
                .FirstOrDefault();
        }

        public GameObject GetWallVariantPrefab(EnumHolder.WallType wallType, int variantType)
        {
            if (wallVariantList == null)
            {
                return null;
            }

            List<WallPrefabData> matchingVariants = wallVariantList
                .Where(prefabData => prefabData.wallType == wallType)
                .ToList();

            return variantType >= 0 && variantType < matchingVariants.Count
                ? matchingVariants[variantType].prefab
                : null;
        }
    }
}