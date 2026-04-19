using UnityEngine;

namespace SystemBreachOverdrive
{
    public enum Direction
    {
        Up = 0,
        Right = 1,
        Down = 2,
        Left = 3
    }

    public static class DirectionExtensions
    {
        public static Direction Opposite(this Direction direction)
        {
            return (Direction)(((int)direction + 2) % 4);
        }

        public static Direction RotateClockwise(this Direction direction, int steps)
        {
            var normalized = ((steps % 4) + 4) % 4;
            return (Direction)(((int)direction + normalized) % 4);
        }

        public static Vector2Int ToVector2Int(this Direction direction)
        {
            switch (direction)
            {
                case Direction.Up:
                    return Vector2Int.up;
                case Direction.Right:
                    return Vector2Int.right;
                case Direction.Down:
                    return Vector2Int.down;
                case Direction.Left:
                    return Vector2Int.left;
                default:
                    return Vector2Int.zero;
            }
        }
    }
}
