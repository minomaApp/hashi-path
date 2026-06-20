using UnityEngine;

namespace HashiGame.Scripts.Runtime
{
    public static class BridgeGeometryUtility
    {
        private const float Epsilon = 0.0001f;

        public static Vector2 ToXZ(Vector3 point)
        {
            return new Vector2(point.x, point.z);
        }

        public static bool SegmentsIntersect(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            return SegmentsIntersect(
                ToXZ(firstStart),
                ToXZ(firstEnd),
                ToXZ(secondStart),
                ToXZ(secondEnd));
        }

        public static bool SegmentsIntersect(
            Vector2 firstStart,
            Vector2 firstEnd,
            Vector2 secondStart,
            Vector2 secondEnd)
        {
            float o1 = Orientation(firstStart, firstEnd, secondStart);
            float o2 = Orientation(firstStart, firstEnd, secondEnd);
            float o3 = Orientation(secondStart, secondEnd, firstStart);
            float o4 = Orientation(secondStart, secondEnd, firstEnd);

            bool properIntersection =
                ((o1 > Epsilon && o2 < -Epsilon) || (o1 < -Epsilon && o2 > Epsilon)) &&
                ((o3 > Epsilon && o4 < -Epsilon) || (o3 < -Epsilon && o4 > Epsilon));

            if (properIntersection)
            {
                return true;
            }

            if (Mathf.Abs(o1) <= Epsilon && IsPointOnSegment(secondStart, firstStart, firstEnd))
            {
                return true;
            }

            if (Mathf.Abs(o2) <= Epsilon && IsPointOnSegment(secondEnd, firstStart, firstEnd))
            {
                return true;
            }

            if (Mathf.Abs(o3) <= Epsilon && IsPointOnSegment(firstStart, secondStart, secondEnd))
            {
                return true;
            }

            if (Mathf.Abs(o4) <= Epsilon && IsPointOnSegment(firstEnd, secondStart, secondEnd))
            {
                return true;
            }

            return false;
        }

        public static float DistancePointToSegment(
            Vector3 point,
            Vector3 segmentStart,
            Vector3 segmentEnd)
        {
            return DistancePointToSegment(
                ToXZ(point),
                ToXZ(segmentStart),
                ToXZ(segmentEnd));
        }

        public static float DistancePointToSegment(
            Vector2 point,
            Vector2 segmentStart,
            Vector2 segmentEnd)
        {
            Vector2 segment = segmentEnd - segmentStart;
            float lengthSquared = segment.sqrMagnitude;

            if (lengthSquared <= Epsilon)
            {
                return Vector2.Distance(point, segmentStart);
            }

            float t = Vector2.Dot(point - segmentStart, segment) / lengthSquared;
            t = Mathf.Clamp01(t);
            Vector2 closestPoint = segmentStart + segment * t;
            return Vector2.Distance(point, closestPoint);
        }

        public static float DistanceSegmentToSegment(
            Vector3 firstStart,
            Vector3 firstEnd,
            Vector3 secondStart,
            Vector3 secondEnd)
        {
            Vector2 a = ToXZ(firstStart);
            Vector2 b = ToXZ(firstEnd);
            Vector2 c = ToXZ(secondStart);
            Vector2 d = ToXZ(secondEnd);

            if (SegmentsIntersect(a, b, c, d))
            {
                return 0f;
            }

            float distanceA = DistancePointToSegment(a, c, d);
            float distanceB = DistancePointToSegment(b, c, d);
            float distanceC = DistancePointToSegment(c, a, b);
            float distanceD = DistancePointToSegment(d, a, b);

            return Mathf.Min(distanceA, distanceB, distanceC, distanceD);
        }

        public static bool ApproximatelySamePoint(Vector3 first, Vector3 second)
        {
            return Vector2.SqrMagnitude(ToXZ(first) - ToXZ(second)) <= Epsilon * Epsilon;
        }

        private static float Orientation(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) -
                   (b.y - a.y) * (c.x - a.x);
        }

        private static bool IsPointOnSegment(Vector2 point, Vector2 start, Vector2 end)
        {
            return point.x >= Mathf.Min(start.x, end.x) - Epsilon &&
                   point.x <= Mathf.Max(start.x, end.x) + Epsilon &&
                   point.y >= Mathf.Min(start.y, end.y) - Epsilon &&
                   point.y <= Mathf.Max(start.y, end.y) + Epsilon;
        }
    }
}
