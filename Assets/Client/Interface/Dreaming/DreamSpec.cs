using UnityEngine;
using UnityEngine.UIElements;

namespace Shooter.Client.Dreaming
{
    public abstract class DreamSpec : ScriptableObject
    {
        [SerializeField] private float weight = 1f;

        public float Weight => Mathf.Max(weight, 0f);

        public abstract Dream Begin(VisualElement screen);
    }
}
