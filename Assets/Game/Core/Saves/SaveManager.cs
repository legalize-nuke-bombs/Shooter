using UnityEngine;

namespace Shooter.Game.Core.Saves
{
    public class SaveManager : MonoBehaviour
    {
        private Register<SaveableObject> saveables;

        private void Awake()
        {
            saveables = Registers.Current.Of<SaveableObject>();
        }

        public void Save(string filename)
        {

        }

        public void Load(string filename)
        {

        }
    }
}
