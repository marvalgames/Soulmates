using Unity.Entities;
using Unity.Transforms;


[UpdateInGroup(typeof(TransformSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class SimpleFollowCharacterSystem : SystemBase
{
    protected override void OnUpdate()
    {
        Entities.WithoutBurst().ForEach((SimpleFollowAuthoring simple,   ref LocalTransform localTransform, 
            in SimpleFollowComponent simpleFollowComponent) =>
        {
            var transform = simple.transform;
            localTransform.Position = transform.position;
            localTransform.Rotation = transform.rotation;
            //Debug.Log("sim foll sys");

        }).Run();
    }
}
