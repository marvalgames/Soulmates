using Sandbox.Player;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;


[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PlayerDashSystem))]
[RequireMatchingQueriesForUpdate]
public partial class VelocityControlSystem : SystemBase
{
    protected override void OnUpdate()
    {
      
        
        
        
        Entities.ForEach((in Entity e, in DamageComponent damageComponent) => {
            
            if (damageComponent.DamageReceived > 0 && SystemAPI.HasComponent<PhysicsVelocity>(e))
            {
                var pv = SystemAPI.GetComponent<PhysicsVelocity>(e);
                pv.Linear = new float3(0, 0, 0);
                SystemAPI.SetComponent(e, pv);
                //Debug.Log("freeze");
            }
            
        }).Schedule();
    }
}
