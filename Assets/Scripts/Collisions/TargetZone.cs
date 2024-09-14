using System;
using UnityEngine;

namespace Collisions
{
    public class TargetZone : MonoBehaviour
    {
        public Transform headZone;

        private void Start()
        {
            var animator = GetComponent<Animator>();
            if (!animator) return;
            if (!headZone)
            {
                headZone = animator.GetBoneTransform(HumanBodyBones.Head);
            }

        }
    }
}