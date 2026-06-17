using System;
using System.Collections.Generic;
using System.Linq;
using BoxPuller.Scripts.Data.Enums;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

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
        [Header("New Shooter Box Game Prefabs")]
        public GameObject shooterPrefab;
        public GameObject boxPrefab;
        public GameObject bottomSlotNodePrefab;
        public GameObject middleSlotNodePrefab;
        public GameObject flowerPetalPrefab;

        [Header("Chain Prefabs")] public GameObject chainPrefab; 
        public GameObject chainNodePrefab;
        public GameObject nodePrefab; 
        public GameObject midNodePrefab;
        public GameObject objectSpawnerPrefab;
        public GameObject objectSpawnerPlatform;
        public GameObject matchingObjectPrefab;
        public GameObject completeConfettiPrefab;

        [Header("Ice Prefabs")] public ParticleSystem iceParticle;
        public ParticleSystem iceFinalParticle;
        public TextMeshPro iceTextPrefab;

        [Space(15)] [Header("Wall Prefabs")] public GameObject wallPrefab;
        public GameObject planeWallPrefab;
        public List<WallPrefabData> wallPrefabList;
        public List<WallPrefabData> wallVariantList;


        public GameObject GetWallPrefab(EnumHolder.WallType wallType)
        {
            return (from prefabData in wallPrefabList where prefabData.wallType == wallType select prefabData.prefab)
                .FirstOrDefault();
        }


        public GameObject GetWallVariantPrefab(EnumHolder.WallType wallType, int variantType)
        {
            var matchingVariants = wallVariantList.Where(prefabData => prefabData.wallType == wallType).ToList();
            return matchingVariants.Count > variantType ? matchingVariants[variantType].prefab : null;
        }
    }
}