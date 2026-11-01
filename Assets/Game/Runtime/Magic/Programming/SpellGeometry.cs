using UnityEngine;
namespace FusionAnyMonster.Spells
{
    public static class SpellGeometry
    {
        public static Vector2 Rotate(Vector2 direction,float degrees) {return Quaternion.Euler(0,0,degrees)*direction.normalized;}
        public static bool SegmentHitsPoint(Vector2 start, Vector2 end, Vector2 point, float radius)
        {
            float fraction;
            return TrySegmentHit(start,end,point,radius,out fraction);
        }
        public static bool TrySegmentHit(Vector2 start, Vector2 end, Vector2 point, float radius, out float fraction)
        {
            fraction = 0f;
            Vector2 offset = start - point;
            float c = offset.sqrMagnitude - Mathf.Max(0f,radius) * Mathf.Max(0f,radius);
            if (c <= 0f) return true;
            Vector2 segment = end - start;
            float lengthSquared = segment.sqrMagnitude;
            if (lengthSquared < .000001f) return false;
            float b = Vector2.Dot(offset,segment);
            float discriminant = b * b - lengthSquared * c;
            if (discriminant < 0f) return false;
            fraction = (-b - Mathf.Sqrt(discriminant)) / lengthSquared;
            return fraction >= 0f && fraction <= 1f;
        }
    }
}
