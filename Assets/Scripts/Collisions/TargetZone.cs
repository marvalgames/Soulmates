using System;
using UnityEngine;

namespace Collisions
{
    public class TargetZone : MonoBehaviour
    {
        public Transform headZone;
        public Transform leftHandZone;
        public Transform rightHandZone;
        public Transform leftFootZone;
        public Transform rightFootZone;
        public Transform bodyZone;
        private void Start()
        {
            var animator = GetComponent<Animator>();
            if (!animator) return;
            if (!headZone)
            {
                headZone = animator.GetBoneTransform(HumanBodyBones.Head);
            }
            if (!leftHandZone)
            {
                leftHandZone = animator.GetBoneTransform(HumanBodyBones.LeftHand);
            }
            if (!rightHandZone)
            {
                rightHandZone = animator.GetBoneTransform(HumanBodyBones.RightHand);
            }
            if (!leftFootZone)
            {
                leftFootZone = animator.GetBoneTransform(HumanBodyBones.LeftFoot);
            }
            if (!rightFootZone)
            {
                rightFootZone = animator.GetBoneTransform(HumanBodyBones.RightFoot);
            }
            if (!bodyZone)
            {
                bodyZone = animator.GetBoneTransform(HumanBodyBones.Chest);
            }

        }
    }
}