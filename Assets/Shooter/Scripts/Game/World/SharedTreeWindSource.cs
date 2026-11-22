using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Shooter.Game
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshRenderer))]
    [RequireComponent(typeof(Tree))]
    public class SharedTreeWindSource : MonoBehaviour
    {
        private const string TemplatesName = "Templates";
        private static readonly int SourceProperty = Shader.PropertyToID("_SpeedTreeWindSource");

        private readonly List<Material> materials = new();
        private int source = -1;

        public static GameObject CreateTemplate(GameObject prefab, Transform parent)
        {
            Tree[] prefabTrees = prefab.GetComponentsInChildren<Tree>(true);
            if (!Application.isPlaying || prefabTrees.Length == 0)
                return prefab;

            Tree prefabTree = prefabTrees[0];
            GameObject holder = new(prefab.name + " Wind");
            holder.transform.SetParent(parent, false);
            holder.AddComponent<MeshFilter>().sharedMesh = prefabTree.GetComponent<MeshFilter>().sharedMesh;
            MeshRenderer renderer = holder.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = prefabTree.GetComponent<MeshRenderer>().sharedMaterials;
            renderer.forceRenderingOff = true;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            holder.AddComponent<Tree>().windAsset = prefabTree.windAsset;
            SharedTreeWindSource windSource = holder.AddComponent<SharedTreeWindSource>();

            Transform templates = new GameObject(TemplatesName).transform;
            templates.SetParent(holder.transform, false);
            templates.gameObject.SetActive(false);
            GameObject template = Instantiate(prefab, templates);
            template.name = prefab.name;
            foreach (Tree tree in template.GetComponentsInChildren<Tree>(true))
                DestroyImmediate(tree);
            windSource.ShareWind(template);
            return template;
        }

        private void ShareWind(GameObject template)
        {
            Dictionary<Material, Material> copies = new();
            foreach (Renderer renderer in template.GetComponentsInChildren<Renderer>(true))
            {
                Material[] shared = renderer.sharedMaterials;
                for (int i = 0; i < shared.Length; i++)
                {
                    Material original = shared[i];
                    if (original == null || !original.HasFloat(SourceProperty))
                        continue;
                    if (!copies.TryGetValue(original, out Material copy))
                    {
                        copy = new Material(original) { name = original.name };
                        copy.SetFloat(SourceProperty, source);
                        copies.Add(original, copy);
                        materials.Add(copy);
                    }
                    shared[i] = copy;
                }
                renderer.sharedMaterials = shared;
            }
        }

        private void OnEnable()
        {
            source = SpeedTreeSharedWind.Register(GetComponent<MeshRenderer>());
            foreach (Material material in materials)
                material.SetFloat(SourceProperty, source);
        }

        private void OnDisable()
        {
            SpeedTreeSharedWind.Unregister(source);
            source = -1;
            foreach (Material material in materials)
                material.SetFloat(SourceProperty, source);
        }

        private void OnDestroy()
        {
            foreach (Material material in materials)
                Destroy(material);
        }
    }
}
