using UnityEngine;

namespace BoxPuller.Scripts.Data.Enums
{
    public class EnumHolder : MonoBehaviour
    {
        public enum GameColor
        {
            None,
            Blue,
            DarkBlue,
            DarkGreen,
            Green,
            Orange,
            Pink,
            Purple,
            Red,
            Teal,
            Yellow
        }

        public enum MaterialType
        {
            Default,
            Other
        }

        public enum Direction
        {
            None,
            Up,
            Down,
            Left,
            Right
        }

        public enum ObjectType
        {
            None,
            ChainCell,
            Food,
            GridLock,
            Key,
            Lock
        }

        public enum FeatureType
        {
            Ice,
            Box,
            Lock
        }

        public enum WallType
        {
            FourSidesEmpty,
            ThreeSidesEmpty,
            Corner,
            Straight,
            OneSideEmpty,
            NoSidesEmpty
        }

        public enum GridType
        {
            Normal,
            DynamicSpaced,
            Hexagon
        }

        public enum LevelDataDefaultObjectType
        {
            Single,
            Stacked
        }

        public enum IslandBridgeMode
        {
            SingleOnly,
            DoubleAllowed
        }

        public enum HashiEditorMode
        {
            Island,
            FixedBridge,
            TutorialBridge,
            Chain
        }
    }
}
