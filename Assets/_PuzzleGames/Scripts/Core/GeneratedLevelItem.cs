using BoxPuller.Scripts.Data.Enums;
using UnityEngine;

namespace BoxPuller.Scripts.Runtime.LevelCreation
{
    public enum GeneratedLevelItemType
    {
        None = 0,
        Box = 1,
        Shooter = 2,
        BottomSlotNode = 3,
        FlowerPetal = 4
    }

    public class GeneratedLevelItem : MonoBehaviour
    {
        [Header("Generated Type")]
        public GeneratedLevelItemType itemType;

        [Header("Grid Data")]
        public int x;
        public int y;
        public int moldGroupId = -1;

        [Header("Shooter Data")]
        public int laneIndex = -1;
        public int orderIndex = -1;
        public int bulletCount;
        public int linkGroupId = -1;
        public bool isHidden;


        [Header("Common")]
        public EnumHolder.GameColor color = EnumHolder.GameColor.None;

        public void SetupBox(int gridX, int gridY, EnumHolder.GameColor boxColor, int boxMoldGroupId)
        {
            itemType = GeneratedLevelItemType.Box;
            x = gridX;
            y = gridY;
            color = boxColor;
            moldGroupId = boxMoldGroupId;
        }

        public void SetupShooter(
           int shooterLaneIndex,
           int shooterOrderIndex,
           EnumHolder.GameColor shooterColor,
           int shooterBulletCount,
           int shooterLinkGroupId,
           bool shooterIsHidden)
        {
            itemType = GeneratedLevelItemType.Shooter;
            laneIndex = shooterLaneIndex;
            orderIndex = shooterOrderIndex;
            color = shooterColor;
            bulletCount = shooterBulletCount;
            linkGroupId = shooterLinkGroupId;
            isHidden = shooterIsHidden;
        }

        public void SetupBottomSlotNode(int nodeLaneIndex, int nodeOrderIndex)
        {
            itemType = GeneratedLevelItemType.BottomSlotNode;
            laneIndex = nodeLaneIndex;
            orderIndex = nodeOrderIndex;
        }

        public void SetupFlowerPetal(int petalIndex)
        {
            itemType = GeneratedLevelItemType.FlowerPetal;
            orderIndex = petalIndex;
        }
    }
}