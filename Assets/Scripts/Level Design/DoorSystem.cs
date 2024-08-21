using Collisions;
using Unity.Entities;

[RequireMatchingQueriesForUpdate]
public partial class DoorOpenSystem : SystemBase
{
    protected override void OnUpdate()
    {

            Entities.WithoutBurst().WithStructuralChanges().ForEach((ref Entity e, ref DoorComponent doorComponent) =>
                {
                   if(LevelManager.instance.currentLevelCompleted == doorComponent.area)
                   {
                       EntityManager.DestroyEntity(e);
                   }
                }
            ).Run();
    }
}


public partial class LevelRemoveItemSystem : SystemBase
{
    protected override void OnUpdate()
    {

        Entities.WithoutBurst().WithAll<TriggerComponent>().WithStructuralChanges().ForEach(( ref Entity e, ref LevelCompleteRemove levelCompleteRemove ) =>
            {
                if(LevelManager.instance.currentLevelCompleted == levelCompleteRemove.levelCompleteIndex)
                {
                    EntityManager.DestroyEntity(e);
                }
            }
        ).Run();
    }
}
