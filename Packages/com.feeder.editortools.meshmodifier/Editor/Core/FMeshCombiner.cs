#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace Feeder
{
    public sealed class FMeshCombineSource
    {
        public Transform Transform;
        public Mesh Mesh;
        public Material[] SubMeshMaterials;
    }

    public static class FMeshCombiner
    {
        public static Vector3 ComputePivot(IReadOnlyList<FMeshCombineSource> sources, FMeshPivotLocation location, Vector3 custom)
        {
            switch (location)
            {
                case FMeshPivotLocation.WorldOrigin:
                    return Vector3.zero;
                case FMeshPivotLocation.CustomLocation:
                    return custom;
                case FMeshPivotLocation.BoundsCenter:
                    return WorldBounds(sources).center;
                default:
                    throw new ArgumentOutOfRangeException(nameof(location), location, null);
            }
        }

        public static Mesh Combine(IReadOnlyList<FMeshCombineSource> sources, Func<Material, Vector2, Vector2> remapUV,
            Vector3 pivot, bool withTangents, bool generateLightmapUV)
        {
            var positions = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uvs = new List<Vector2>();
            var triangles = new List<int>();

            foreach (FMeshCombineSource source in sources)
                Append(source, remapUV, pivot, withTangents, positions, normals, tangents, uvs, triangles);

            var mesh = new Mesh
            {
                indexFormat = positions.Count > 65535 ? IndexFormat.UInt32 : IndexFormat.UInt16
            };
            mesh.SetVertices(positions);
            mesh.SetNormals(normals);
            if (withTangents)
                mesh.SetTangents(tangents);
            mesh.SetUVs(0, uvs);
            mesh.SetTriangles(triangles, 0, true);

            if (generateLightmapUV)
                Unwrapping.GenerateSecondaryUVSet(mesh);
            return mesh;
        }

        private static void Append(FMeshCombineSource source, Func<Material, Vector2, Vector2> remapUV, Vector3 pivot,
            bool withTangents, List<Vector3> positions, List<Vector3> normals, List<Vector4> tangents, List<Vector2> uvs,
            List<int> triangles)
        {
            Mesh mesh = source.Mesh;
            Mesh scratch = null;
            try
            {
                bool needNormals = mesh.normals.Length != mesh.vertexCount;
                bool needTangents = withTangents && !mesh.HasVertexAttribute(VertexAttribute.Tangent);
                if (needNormals || needTangents)
                {
                    scratch = Object.Instantiate(mesh);
                    if (needNormals)
                        scratch.RecalculateNormals();
                    if (needTangents)
                        scratch.RecalculateTangents();
                    mesh = scratch;
                }

                Vector3[] srcPositions = mesh.vertices;
                Vector3[] srcNormals = mesh.normals;
                Vector4[] srcTangents = withTangents ? mesh.tangents : null;
                var srcUVs = new List<Vector2>();
                mesh.GetUVs(0, srcUVs);
                bool hasUVs = srcUVs.Count == srcPositions.Length;

                Matrix4x4 toWorld = source.Transform.localToWorldMatrix;
                Matrix4x4 normalMatrix = toWorld.inverse.transpose;
                bool mirrored = toWorld.determinant < 0f;
                var remap = new int[srcPositions.Length];
                var corners = new int[3];

                for (int sub = 0; sub < mesh.subMeshCount; sub++)
                {
                    if (mesh.GetTopology(sub) != MeshTopology.Triangles)
                    {
                        Debug.LogWarning($"[Feeder Mesh Modifier] Submesh {sub} of '{mesh.name}' is not a triangle list and was skipped.");
                        continue;
                    }

                    Material material = source.SubMeshMaterials[Mathf.Min(sub, source.SubMeshMaterials.Length - 1)];
                    for (int i = 0; i < remap.Length; i++)
                        remap[i] = -1;

                    int[] indices = mesh.GetTriangles(sub);
                    for (int t = 0; t < indices.Length; t += 3)
                    {
                        corners[0] = indices[t];
                        corners[1] = mirrored ? indices[t + 2] : indices[t + 1];
                        corners[2] = mirrored ? indices[t + 1] : indices[t + 2];

                        foreach (int v in corners)
                        {
                            if (remap[v] < 0)
                            {
                                remap[v] = positions.Count;
                                positions.Add(toWorld.MultiplyPoint3x4(srcPositions[v]) - pivot);
                                normals.Add(normalMatrix.MultiplyVector(srcNormals[v]).normalized);
                                if (withTangents)
                                {
                                    Vector4 tan = srcTangents[v];
                                    Vector3 dir = toWorld.MultiplyVector(tan).normalized;
                                    tangents.Add(new Vector4(dir.x, dir.y, dir.z, mirrored ? -tan.w : tan.w));
                                }
                                uvs.Add(remapUV(material, hasUVs ? srcUVs[v] : Vector2.zero));
                            }
                            triangles.Add(remap[v]);
                        }
                    }
                }
            }
            finally
            {
                if (scratch != null)
                    Object.DestroyImmediate(scratch);
            }
        }

        private static Bounds WorldBounds(IReadOnlyList<FMeshCombineSource> sources)
        {
            bool any = false;
            var bounds = new Bounds();
            foreach (FMeshCombineSource source in sources)
            {
                Matrix4x4 toWorld = source.Transform.localToWorldMatrix;
                foreach (Vector3 v in source.Mesh.vertices)
                {
                    Vector3 w = toWorld.MultiplyPoint3x4(v);
                    if (!any)
                    {
                        bounds = new Bounds(w, Vector3.zero);
                        any = true;
                    }
                    else
                    {
                        bounds.Encapsulate(w);
                    }
                }
            }
            return bounds;
        }
    }
}
#endif
