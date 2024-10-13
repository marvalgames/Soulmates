using Unity.Entities;
using UnityEngine;

/////////////////////////////////////////////////////////////////////////////////

namespace Rukhanka.Samples
{
public class BoneMarkerAuthoring: MonoBehaviour
{
    public enum BoneID
    {
        EllenLeftLeg,
        EllenRightLeg,
        EllenLeftFoot,
        EllenRightFoot,
        EllenLeftHand,
        EllenRightHand,
        SnakeStartBone
    }

    public BoneID boneID;
}

/////////////////////////////////////////////////////////////////////////////////

public struct EllenLeftLegTag: IComponentData{}
public struct EllenRightLegTag: IComponentData{}
public struct EllenLeftFootTag: IComponentData{}
public struct EllenRightFootTag: IComponentData{}
public struct EllenRightHandTag: IComponentData{}
public struct EllenLeftHandTag: IComponentData{}
public struct SnakeTag: IComponentData{}

/////////////////////////////////////////////////////////////////////////////////

public class BoneMarkerBaker : Baker<BoneMarkerAuthoring>
{
    public override void Bake(BoneMarkerAuthoring a)
    {
        var e = GetEntity(a, TransformUsageFlags.Dynamic);
        switch (a.boneID)
        {
        case BoneMarkerAuthoring.BoneID.EllenLeftLeg: AddComponent<EllenLeftLegTag>(e); break;
        case BoneMarkerAuthoring.BoneID.EllenRightLeg: AddComponent<EllenRightLegTag>(e); break;
        case BoneMarkerAuthoring.BoneID.EllenLeftFoot: AddComponent<EllenLeftFootTag>(e); break;
        case BoneMarkerAuthoring.BoneID.EllenRightFoot: AddComponent<EllenRightFootTag>(e); break;
        case BoneMarkerAuthoring.BoneID.EllenLeftHand: AddComponent<EllenLeftHandTag>(e); break;
        case BoneMarkerAuthoring.BoneID.EllenRightHand: AddComponent<EllenRightHandTag>(e); break;
        case BoneMarkerAuthoring.BoneID.SnakeStartBone: AddComponent<SnakeTag>(e); break;
        }
    }
}
}
