using System.Collections.Generic;
using UnityEngine;

namespace Feeder
{
    public static class FMeshUvUtils
    {
        /// <summary>True when any UV of the mesh falls outside the 0..1 square. Also returns
        /// the rect the UVs actually span, so callers can see how far outside they reach.</summary>
        public static bool HasOutOfBoundsUVs(Mesh mesh, ref Rect uvBounds)
        {
            uvBounds = new Rect(0f, 0f, 1f, 1f);
            if (mesh == null || !mesh.isReadable) return false;

            List<Vector2> uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);
            if (uvs.Count == 0) return false;

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < uvs.Count; i++)
            {
                Vector2 uv = uvs[i];
                if (uv.x < minX) minX = uv.x;
                if (uv.y < minY) minY = uv.y;
                if (uv.x > maxX) maxX = uv.x;
                if (uv.y > maxY) maxY = uv.y;
            }

            uvBounds = new Rect(minX, minY, maxX - minX, maxY - minY);
            return minX < 0f || minY < 0f || maxX > 1f || maxY > 1f;
        }
    }
}
