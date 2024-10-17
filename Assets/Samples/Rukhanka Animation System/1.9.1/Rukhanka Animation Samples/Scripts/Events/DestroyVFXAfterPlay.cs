using UnityEngine;
using UnityEngine.VFX;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
    [RequireComponent(typeof(VisualEffect))]
    public class DestroyVFXAfterPlay: MonoBehaviour
    {
        VisualEffect vfx;
        
        void OnEnable()
        {
            vfx = GetComponent<VisualEffect>();
        }

        void Update()
        {
            if (vfx.aliveParticleCount == 0)
                Destroy(gameObject, 1);
        }
    }
}
