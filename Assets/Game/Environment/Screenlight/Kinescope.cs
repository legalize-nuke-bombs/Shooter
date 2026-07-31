using System.Collections.Generic;
using UnityEngine;

namespace Shooter.Game
{
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class Kinescope : MonoBehaviour
    {
        [SerializeField] private float roundness = 3.5f;

        [SerializeField] private int sides = 48;

        [SerializeField] private int rings = 10;

        private void OnEnable()
        {
            Build();
        }

        private void OnValidate()
        {
            if (isActiveAndEnabled) Build();
        }

        private void Build()
        {
            var filter = GetComponent<MeshFilter>();
            var glass = new Mesh { name = "Kinescope", hideFlags = HideFlags.HideAndDontSave };

            int edges = Mathf.Max(3, sides);
            int layers = Mathf.Max(1, rings);

            var points = new List<Vector3>();
            var coats = new List<Vector2>();
            var faces = new List<int>();

            points.Add(new Vector3(0f, 0f, 0.5f));
            coats.Add(new Vector2(0.5f, 0.5f));

            for (int ring = 1; ring <= layers; ring++)
            {
                float reach = ring / (float)layers;

                for (int side = 0; side < edges; side++)
                {
                    float turn = side / (float)edges * Mathf.PI * 2f;
                    Vector2 rim = Rim(turn) * (reach * 0.5f);

                    points.Add(new Vector3(rim.x, rim.y, Dome(reach)));
                    coats.Add(new Vector2(rim.x + 0.5f, rim.y + 0.5f));
                }
            }

            for (int side = 0; side < edges; side++)
            {
                faces.Add(0);
                faces.Add(1 + (side + 1) % edges);
                faces.Add(1 + side);
            }

            for (int ring = 1; ring < layers; ring++)
            {
                int inner = 1 + (ring - 1) * edges;
                int outer = 1 + ring * edges;

                for (int side = 0; side < edges; side++)
                {
                    int next = (side + 1) % edges;

                    faces.Add(inner + side);
                    faces.Add(inner + next);
                    faces.Add(outer + side);

                    faces.Add(inner + next);
                    faces.Add(outer + next);
                    faces.Add(outer + side);
                }
            }

            glass.SetVertices(points);
            glass.SetUVs(0, coats);
            glass.SetTriangles(faces, 0);
            glass.RecalculateNormals();
            glass.RecalculateBounds();

            filter.sharedMesh = glass;
        }

        private Vector2 Rim(float turn)
        {
            float cosine = Mathf.Cos(turn);
            float sine = Mathf.Sin(turn);
            float power = 2f / Mathf.Max(roundness, 2f);

            return new Vector2(
                Mathf.Sign(cosine) * Mathf.Pow(Mathf.Abs(cosine), power),
                Mathf.Sign(sine) * Mathf.Pow(Mathf.Abs(sine), power));
        }

        private static float Dome(float reach)
        {
            return 0.5f * (1f - reach * reach);
        }
    }
}
