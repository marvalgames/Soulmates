// using RootMotion.FinalIK;
// using UnityEngine;
//
//
//
// public enum BodyBones
// {
//     LeftHand,
//     RightHand,
//     LeftFoot,
//     RightFoot,
//     //Hips,
//     //Spine,
//     //Chest,
//     //Neck,
//     //Head,
//     //LeftShoulder,
//     //RightShoulder,
//     //LeftUpperArm,
//     //RightUpperArm,
//     //LeftLowerArm,
//     //RightLowerArm,
// }
//
//
// public class Bones : MonoBehaviour
// {
//
//     private AimIK aimIK;
//     //[Range(0.0f, 1.0f)] [SerializeField] private float aimWeight = 1.0f;
//     private Animator animator;
//     [SerializeField] private BodyBones rootBone;
//
//     // Start is called before the first frame update
//     void Start()
//     {
//         animator = GetComponentInParent<Animator>();
//         aimIK = GetComponent<AimIK>();
//
//         if(animator && aimIK)
//         {
//             SetupBoneHeirarchy();
//         }
//     }
//
//     private void SetupBoneHeirarchy()
//     {
//
//         if (rootBone == BodyBones.LeftHand)
//         {
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftShoulder));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftUpperArm));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftLowerArm));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftHand));
//         }
//         else if (rootBone == BodyBones.RightHand)
//         {
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightShoulder));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightUpperArm));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightLowerArm));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightHand));
//         }
//         else if (rootBone == BodyBones.LeftFoot)
//         {
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftUpperLeg));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftLowerLeg));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.LeftFoot));
//         }
//         else if (rootBone == BodyBones.RightFoot)
//         {
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightUpperLeg));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightLowerLeg));
//             aimIK.solver.AddBone(animator.GetBoneTransform(HumanBodyBones.RightFoot));
//         }
//
//     }
//
//     // Update is called once per frame
//     void Update()
//     {
//         
//     }
// }
