using System;
using UnityEngine;

////////////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Hybrid
{
    [Serializable]
    public class WeightedTransform
    {
        public Transform bone;
        [Range(0, 1)]
        public float weight = 1;
    }
}
