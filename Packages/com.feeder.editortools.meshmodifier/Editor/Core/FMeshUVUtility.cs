#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEngine;

namespace Feeder
{
    public static class FMeshUVUtility
    {
        private const float Epsilon = 1e-4f;

        public static bool HasOutOfBoundsUVs(Mesh mesh)
        {
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            foreach (Vector2 uv in uvs)
                if (uv.x < -Epsilon || uv.x > 1f + Epsilon || uv.y < -Epsilon || uv.y > 1f + Epsilon)
                    return true;
            return false;
        }

        public static Rect GetBounds(List<Vector2> uvs, int[] indices)
        {
            if (uvs.Count == 0 || indices.Length == 0)
                return new Rect(0f, 0f, 1f, 1f);

            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);
            foreach (int index in indices)
            {
                min = Vector2.Min(min, uvs[index]);
                max = Vector2.Max(max, uvs[index]);
            }
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        public static Rect Union(Rect a, Rect b)
        {
            return Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin),
                Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
        }
    }
}
#endif
