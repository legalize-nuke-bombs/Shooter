using System.Collections;
using UnityEngine;

namespace Shooter.Bootstrapping
{
    internal class Starter : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return Bootstrap.Begin();

            Destroy(gameObject);
        }
    }
}
