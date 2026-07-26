using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Interface.Dreaming
{
    [CreateAssetMenu(menuName = "Shooter/Dreams/Starry Sky", fileName = "StarrySky")]
    public sealed class StarrySkyDreamSpec : DreamSpec
    {
        [SerializeField] private Color sky = new Color(0.02f, 0.03f, 0.08f);

        [SerializeField] private int stars = 140;

        [SerializeField] private float smallestStar = 1f;

        [SerializeField] private float largestStar = 3f;

        [SerializeField] private float slowestTwinkle = 0.4f;

        [SerializeField] private float fastestTwinkle = 2.5f;

        [SerializeField] [Range(0f, 1f)] private float dimmest = 0.1f;

        public Color Sky => sky;

        public int Stars => Mathf.Max(stars, 0);

        public float SmallestStar => smallestStar;

        public float LargestStar => largestStar;

        public float SlowestTwinkle => slowestTwinkle;

        public float FastestTwinkle => fastestTwinkle;

        public float Dimmest => dimmest;

        public override Dream Begin(VisualElement screen)
        {
            return new StarrySkyDream(screen, this);
        }
    }
}
