using System;
using TMPro;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
    public class EventsSampleConf: MonoBehaviour
    {
        public GameObject walkStepLParticle;
        public GameObject walkStepRParticle;
        public TextMeshProUGUI sampleDescription;

        void Start()
        {
        #if !RUKHANKA_SAMPLES_WITH_VFX_GRAPH
            sampleDescription.text += "\n\n<color=red>Please install the <b>Visual Effect Graph</b> package for the proper functioning of this sample!</color>";
        #endif
        }
    }
}
