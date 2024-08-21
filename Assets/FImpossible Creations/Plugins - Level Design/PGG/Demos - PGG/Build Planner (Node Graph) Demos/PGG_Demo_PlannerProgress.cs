using FIMSpace.Generating;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace FIMSpace.Generating
{
    public class PGG_Demo_PlannerProgress : MonoBehaviour
    {
        public BuildPlannerExecutor Executor;
        public Image ProgressBar;

        void Start()
        {
            if (Executor == null) return;
            if (ProgressBar == null) return;
        }

        void Update()
        {
            ProgressBar.transform.localScale = new Vector3( Executor.GeneratingProgressSmooth, 1f, 1f);
            if (Executor.GeneratingProgress >= 1f)
            {
                ProgressBar.color = Color.green;
            }
        }
    }
}