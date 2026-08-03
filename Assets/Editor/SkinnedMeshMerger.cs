using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Logging;
using UnityEditor;
using UnityEngine;

namespace Shooter.Editing
{
    public static class SkinnedMeshMerger
    {
        private static readonly Journal Log = Logs.Here();

        [MenuItem("Tools/Merge Skinned Meshes")]
        private static void Merge()
        {
            GameObject chosen = Selection.activeGameObject;
            if (chosen == null)
            {
                Log.Error("Select a model in the scene to merge its skinned meshes");
                return;
            }

            SkinnedMeshRenderer[] sources = chosen.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            if (sources.Length == 0)
            {
                Log.Error("Object {} has no skinned meshes", chosen.name);
                return;
            }

            string folder = Folder(sources[0]);
            if (folder == null) return;

            Log.Info("Merging {} skinned meshes of {} into materials", sources.Length, chosen.name);

            foreach (IGrouping<Material, SkinnedMeshRenderer> group in sources.GroupBy(source => source.sharedMaterial))
            {
                Build(chosen.transform, group.Key, group.ToArray(), folder);
            }

            foreach (SkinnedMeshRenderer source in sources)
            {
                if (source.transform.childCount == 0) Object.DestroyImmediate(source.gameObject);
                else Object.DestroyImmediate(source);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Log.Info("Object {} now wears {} skinned meshes", chosen.name,
                chosen.GetComponentsInChildren<SkinnedMeshRenderer>(true).Length);
        }

        private static string Folder(SkinnedMeshRenderer source)
        {
            string model = AssetDatabase.GetAssetPath(source.sharedMesh);
            if (string.IsNullOrEmpty(model))
            {
                Log.Error("Mesh {} does not come from an asset, nowhere to put the merged one", source.sharedMesh.name);
                return null;
            }

            string beside = Path.GetDirectoryName(model).Replace('\\', '/');
            string folder = beside + "/Merged";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(beside, "Merged");

            return folder;
        }

        private static Matrix4x4 Blend(Matrix4x4[] posing, BoneWeight weight)
        {
            Matrix4x4 blended = new Matrix4x4();

            Add(ref blended, posing[weight.boneIndex0], weight.weight0);
            Add(ref blended, posing[weight.boneIndex1], weight.weight1);
            Add(ref blended, posing[weight.boneIndex2], weight.weight2);
            Add(ref blended, posing[weight.boneIndex3], weight.weight3);

            return blended;
        }

        private static void Add(ref Matrix4x4 blended, Matrix4x4 bone, float weight)
        {
            if (weight == 0f) return;

            for (int i = 0; i < 16; i++) blended[i] += bone[i] * weight;
        }

        private static void Build(Transform owner, Material material, SkinnedMeshRenderer[] group, string folder)
        {
            var bones = new List<Transform>();
            var bindposes = new List<Matrix4x4>();
            var known = new Dictionary<(Transform, Matrix4x4), int>();

            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var tangents = new List<Vector4>();
            var uv = new List<Vector2>();
            var weights = new List<BoneWeight>();
            var triangles = new List<int>();

            foreach (SkinnedMeshRenderer source in group)
            {
                Mesh mesh = source.sharedMesh;
                Transform[] own = source.bones;
                Matrix4x4[] poses = mesh.bindposes;

                var posing = new Matrix4x4[own.Length];
                var remap = new int[own.Length];

                for (int i = 0; i < own.Length; i++)
                {
                    posing[i] = owner.worldToLocalMatrix * own[i].localToWorldMatrix * poses[i];

                    if (!known.TryGetValue((own[i], Matrix4x4.identity), out int at))
                    {
                        at = bones.Count;
                        known.Add((own[i], Matrix4x4.identity), at);
                        bones.Add(own[i]);
                        bindposes.Add((owner.worldToLocalMatrix * own[i].localToWorldMatrix).inverse);
                    }

                    remap[i] = at;
                }

                int shift = vertices.Count;
                Vector3[] own_vertices = mesh.vertices;
                Vector3[] own_normals = mesh.normals;
                Vector4[] own_tangents = mesh.tangents;
                BoneWeight[] own_weights = mesh.boneWeights;

                for (int v = 0; v < own_vertices.Length; v++)
                {
                    BoneWeight weight = own_weights[v];
                    Matrix4x4 skinning = Blend(posing, weight);

                    vertices.Add(skinning.MultiplyPoint3x4(own_vertices[v]));
                    if (own_normals.Length == own_vertices.Length)
                        normals.Add(skinning.MultiplyVector(own_normals[v]).normalized);
                    if (own_tangents.Length == own_vertices.Length)
                    {
                        Vector4 tangent = own_tangents[v];
                        Vector3 turned = skinning.MultiplyVector(new Vector3(tangent.x, tangent.y, tangent.z)).normalized;
                        tangents.Add(new Vector4(turned.x, turned.y, turned.z, tangent.w));
                    }

                    weights.Add(new BoneWeight
                    {
                        boneIndex0 = remap[weight.boneIndex0],
                        boneIndex1 = remap[weight.boneIndex1],
                        boneIndex2 = remap[weight.boneIndex2],
                        boneIndex3 = remap[weight.boneIndex3],
                        weight0 = weight.weight0,
                        weight1 = weight.weight1,
                        weight2 = weight.weight2,
                        weight3 = weight.weight3
                    });
                }

                uv.AddRange(mesh.uv);

                for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
                {
                    foreach (int index in mesh.GetTriangles(submesh)) triangles.Add(index + shift);
                }
            }

            var merged = new Mesh
            {
                name = material == null ? "Merged" : material.name,
                indexFormat = vertices.Count > 65000
                    ? UnityEngine.Rendering.IndexFormat.UInt32
                    : UnityEngine.Rendering.IndexFormat.UInt16
            };

            merged.SetVertices(vertices);
            if (normals.Count == vertices.Count) merged.SetNormals(normals);
            if (tangents.Count == vertices.Count) merged.SetTangents(tangents);
            if (uv.Count == vertices.Count) merged.SetUVs(0, uv);
            merged.boneWeights = weights.ToArray();
            merged.bindposes = bindposes.ToArray();
            merged.SetTriangles(triangles, 0);
            merged.RecalculateBounds();

            AssetDatabase.CreateAsset(merged, folder + "/" + merged.name + ".asset");

            var carrier = new GameObject(merged.name);
            carrier.transform.SetParent(owner, false);

            SkinnedMeshRenderer renderer = carrier.AddComponent<SkinnedMeshRenderer>();
            renderer.sharedMesh = merged;
            renderer.bones = bones.ToArray();
            renderer.rootBone = owner;
            renderer.sharedMaterial = material;
            renderer.quality = group[0].quality;
            renderer.updateWhenOffscreen = false;

            Bounds room = merged.bounds;
            room.Expand(new Vector3(room.size.x * 0.5f, 0f, room.size.z * 0.5f));
            renderer.localBounds = room;

            Log.Info("Material {} collected {} meshes into one of {} vertices on {} bone slots over {} bones, {} m tall",
                merged.name, group.Length, vertices.Count, bones.Count, bones.Distinct().Count(), merged.bounds.size.y);
        }
    }
}
