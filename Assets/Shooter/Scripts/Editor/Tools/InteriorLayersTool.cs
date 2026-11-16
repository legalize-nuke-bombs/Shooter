using System.Collections.Generic;
using System.IO;
using System.Linq;
using Shooter.Infrastructure;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Shooter.Editor
{
    public class InteriorLayersTool : EditorWindow
    {
        public const string InteriorLayerName = "Interior";
        public const string ExteriorLayerName = "Exterior";
        private const string GeneratedFolderName = "Generated";
        private const int RaysPerTriangle = 32;
        private const float RayLength = 50f;
        private const float RayLift = 0.01f;
        private const float EnclosedShare = 0.6f;
        private const float MixedAreaShare = 0.05f;
        private static readonly Journal Log = Logs.Here();
        private static readonly Vector3[] HemisphereRays = BuildHemisphereRays();

        [MenuItem("Tools/Shooter/Prefabs/Interior Layers")]
        private static void OpenWindow()
        {
            GetWindow<InteriorLayersTool>("Interior Layers");
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Gives every part of the selected building prefabs that contributes GI the Interior or Exterior rendering layer, " +
                                       "so a surface inside samples only the probes inside. A triangle is inside when most rays from its face hit the building itself; " +
                                       "a mesh with both kinds is split in two, the inside part goes to a child object", EditorStyles.wordWrappedMiniLabel);

            string[] prefabPaths = Selection.objects.Select(AssetDatabase.GetAssetPath).Where(path => path.EndsWith(".prefab")).ToArray();
            using (new EditorGUI.DisabledScope(prefabPaths.Length == 0 || EditorApplication.isPlaying))
            {
                if (GUILayout.Button($"Assign layers in {prefabPaths.Length} selected prefabs"))
                {
                    foreach (string path in prefabPaths)
                    {
                        AssignLayers(path);
                    }
                }
            }
        }

        public static void AssignLayers(string prefabPath)
        {
            int interiorIndex = RenderingLayerMask.NameToRenderingLayer(InteriorLayerName);
            int exteriorIndex = RenderingLayerMask.NameToRenderingLayer(ExteriorLayerName);
            if (interiorIndex < 0 || exteriorIndex < 0)
            {
                Log.Error($"Rendering layers {InteriorLayerName} and {ExteriorLayerName} are not defined in Tags and Layers");
                return;
            }

            var layers = new SurfaceLayers(1u << interiorIndex, 1u << exteriorIndex);
            string generatedFolder = EnsureGeneratedFolder(Path.GetDirectoryName(prefabPath).Replace('\\', '/'));
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            Scene colliderScene = EditorSceneManager.NewPreviewScene();
            try
            {
                MeshRenderer[] renderers = root.GetComponentsInChildren<MeshRenderer>(true).Where(ContributesGlobalIllumination).ToArray();
                PhysicsScene physics = BuildColliders(renderers, colliderScene);
                var summary = new List<string>();
                foreach (MeshRenderer renderer in renderers)
                {
                    summary.Add(AssignLayer(renderer, physics, layers, generatedFolder, root.name));
                }

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                Log.Info($"{prefabPath}: {string.Join("; ", summary)}");
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(colliderScene);
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static bool ContributesGlobalIllumination(MeshRenderer renderer)
        {
            return GameObjectUtility.GetStaticEditorFlags(renderer.gameObject).HasFlag(StaticEditorFlags.ContributeGI) &&
                   renderer.TryGetComponent(out MeshFilter filter) && filter.sharedMesh != null;
        }

        private static PhysicsScene BuildColliders(MeshRenderer[] renderers, Scene colliderScene)
        {
            foreach (MeshRenderer renderer in renderers)
            {
                var collider = new GameObject(renderer.name);
                SceneManager.MoveGameObjectToScene(collider, colliderScene);
                collider.transform.SetPositionAndRotation(renderer.transform.position, renderer.transform.rotation);
                collider.transform.localScale = renderer.transform.lossyScale;
                collider.AddComponent<MeshCollider>().sharedMesh = renderer.GetComponent<MeshFilter>().sharedMesh;
            }

            return colliderScene.GetPhysicsScene();
        }

        private static string AssignLayer(MeshRenderer renderer, PhysicsScene physics, SurfaceLayers layers, string generatedFolder, string prefabName)
        {
            var filter = renderer.GetComponent<MeshFilter>();
            Mesh mesh = filter.sharedMesh;
            if (AssetDatabase.GetAssetPath(mesh).Contains($"/{GeneratedFolderName}/")) return $"{renderer.name} already split";

            Matrix4x4 toWorld = renderer.transform.localToWorldMatrix;
            bool mirrored = toWorld.determinant < 0f;
            Vector3[] vertices = mesh.vertices;
            var inside = new List<int>[mesh.subMeshCount];
            var outside = new List<int>[mesh.subMeshCount];
            float insideArea = 0f;
            float outsideArea = 0f;

            for (int subMesh = 0; subMesh < mesh.subMeshCount; subMesh++)
            {
                inside[subMesh] = new List<int>();
                outside[subMesh] = new List<int>();
                int[] triangles = mesh.GetTriangles(subMesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    Vector3 a = toWorld.MultiplyPoint3x4(vertices[triangles[i]]);
                    Vector3 b = toWorld.MultiplyPoint3x4(vertices[triangles[i + 1]]);
                    Vector3 c = toWorld.MultiplyPoint3x4(vertices[triangles[i + 2]]);
                    Vector3 cross = Vector3.Cross(b - a, c - a);
                    float area = cross.magnitude * 0.5f;
                    bool enclosed = area > 1e-8f && IsEnclosed(physics, (a + b + c) / 3f, (mirrored ? -cross : cross).normalized);

                    List<int> side = enclosed ? inside[subMesh] : outside[subMesh];
                    side.Add(triangles[i]);
                    side.Add(triangles[i + 1]);
                    side.Add(triangles[i + 2]);
                    if (enclosed) insideArea += area;
                    else outsideArea += area;
                }
            }

            float insideShare = insideArea / Mathf.Max(insideArea + outsideArea, 1e-8f);
            uint otherLayers = renderer.renderingLayerMask & ~(layers.Interior | layers.Exterior);
            if (insideShare <= MixedAreaShare)
            {
                renderer.renderingLayerMask = otherLayers | layers.Exterior;
                return $"{renderer.name} exterior ({insideShare:P0} inside)";
            }

            if (insideShare >= 1f - MixedAreaShare)
            {
                renderer.renderingLayerMask = otherLayers | layers.Interior;
                return $"{renderer.name} interior ({insideShare:P0} inside)";
            }

            string baseName = $"{prefabName} {renderer.name}";
            filter.sharedMesh = SaveMesh(ExtractTriangles(mesh, outside, $"{mesh.name} Exterior"), generatedFolder, $"{baseName} Exterior");
            renderer.renderingLayerMask = otherLayers | layers.Exterior;

            var interiorPart = new GameObject($"{renderer.name} Interior");
            interiorPart.transform.SetParent(renderer.transform, false);
            interiorPart.layer = renderer.gameObject.layer;
            interiorPart.tag = renderer.gameObject.tag;
            GameObjectUtility.SetStaticEditorFlags(interiorPart, GameObjectUtility.GetStaticEditorFlags(renderer.gameObject));
            interiorPart.AddComponent<MeshFilter>().sharedMesh = SaveMesh(ExtractTriangles(mesh, inside, $"{mesh.name} Interior"), generatedFolder, $"{baseName} Interior");
            var interiorRenderer = interiorPart.AddComponent<MeshRenderer>();
            ComponentUtility.CopyComponent(renderer);
            ComponentUtility.PasteComponentValues(interiorRenderer);
            interiorRenderer.renderingLayerMask = otherLayers | layers.Interior;
            if (renderer.TryGetComponent(out ShooterTransformObserver observer))
            {
                interiorPart.AddComponent<ShooterEntity>();
                ComponentUtility.CopyComponent(observer);
                ComponentUtility.PasteComponentValues(interiorPart.GetComponent<ShooterTransformObserver>());
            }

            return $"{renderer.name} split ({insideShare:P0} inside)";
        }

        private static bool IsEnclosed(PhysicsScene physics, Vector3 center, Vector3 normal)
        {
            Quaternion toNormal = Quaternion.FromToRotation(Vector3.forward, normal);
            Vector3 origin = center + normal * RayLift;
            int hits = 0;
            for (int i = 0; i < HemisphereRays.Length; i++)
            {
                if (physics.Raycast(origin, toNormal * HemisphereRays[i], out _, RayLength)) hits++;

                int left = HemisphereRays.Length - i - 1;
                if (hits >= EnclosedShare * HemisphereRays.Length) return true;
                if (hits + left < EnclosedShare * HemisphereRays.Length) return false;
            }

            return false;
        }

        private static Vector3[] BuildHemisphereRays()
        {
            var rays = new Vector3[RaysPerTriangle];
            float goldenAngle = Mathf.PI * (3f - Mathf.Sqrt(5f));
            for (int i = 0; i < RaysPerTriangle; i++)
            {
                float u = (i + 0.5f) / RaysPerTriangle;
                float radius = Mathf.Sqrt(u);
                float angle = i * goldenAngle;
                rays[i] = new Vector3(radius * Mathf.Cos(angle), radius * Mathf.Sin(angle), Mathf.Sqrt(1f - u));
            }

            return rays;
        }

        private static Mesh ExtractTriangles(Mesh source, List<int>[] trianglesBySubMesh, string name)
        {
            var newIndexByOld = new Dictionary<int, int>();
            var oldIndices = new List<int>();
            var subMeshes = new int[trianglesBySubMesh.Length][];
            for (int subMesh = 0; subMesh < trianglesBySubMesh.Length; subMesh++)
            {
                subMeshes[subMesh] = trianglesBySubMesh[subMesh].Select(old =>
                {
                    if (newIndexByOld.TryGetValue(old, out int index)) return index;

                    index = oldIndices.Count;
                    newIndexByOld[old] = index;
                    oldIndices.Add(old);
                    return index;
                }).ToArray();
            }

            var mesh = new Mesh { name = name, indexFormat = oldIndices.Count > ushort.MaxValue ? IndexFormat.UInt32 : IndexFormat.UInt16 };
            mesh.SetVertices(Pick(source.vertices, oldIndices));
            if (source.HasVertexAttribute(VertexAttribute.Normal)) mesh.SetNormals(Pick(source.normals, oldIndices));
            if (source.HasVertexAttribute(VertexAttribute.Tangent)) mesh.SetTangents(Pick(source.tangents, oldIndices));
            if (source.HasVertexAttribute(VertexAttribute.Color)) mesh.SetColors(Pick(source.colors32, oldIndices));
            for (int channel = 0; channel < 8; channel++)
            {
                var attribute = (VertexAttribute)((int)VertexAttribute.TexCoord0 + channel);
                if (!source.HasVertexAttribute(attribute)) continue;

                var uvs = new List<Vector4>();
                source.GetUVs(channel, uvs);
                switch (source.GetVertexAttributeDimension(attribute))
                {
                    case 2: mesh.SetUVs(channel, Pick(uvs, oldIndices).Select(uv => (Vector2)uv).ToList()); break;
                    case 3: mesh.SetUVs(channel, Pick(uvs, oldIndices).Select(uv => (Vector3)uv).ToList()); break;
                    default: mesh.SetUVs(channel, Pick(uvs, oldIndices)); break;
                }
            }

            mesh.subMeshCount = subMeshes.Length;
            for (int subMesh = 0; subMesh < subMeshes.Length; subMesh++)
            {
                mesh.SetTriangles(subMeshes[subMesh], subMesh, false);
            }

            mesh.RecalculateBounds();
            return mesh;
        }

        private static List<T> Pick<T>(IList<T> values, List<int> indices)
        {
            return indices.Select(index => values[index]).ToList();
        }

        private static Mesh SaveMesh(Mesh mesh, string folder, string fileName)
        {
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{fileName}.asset");
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static string EnsureGeneratedFolder(string prefabFolder)
        {
            string folder = $"{prefabFolder}/{GeneratedFolderName}";
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(prefabFolder, GeneratedFolderName);
            return folder;
        }

        private readonly struct SurfaceLayers
        {
            public readonly uint Interior;
            public readonly uint Exterior;

            public SurfaceLayers(uint interior, uint exterior)
            {
                Interior = interior;
                Exterior = exterior;
            }
        }
    }
}
