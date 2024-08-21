using Sandbox.Player;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;


public partial struct ResourceAppliedSystem : ISystem
{


    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.TempJob);

        foreach (var (totalCurrencyComponent, currencyComponent, e)
            in SystemAPI.Query<RefRW<TotalCurrencyComponent>, RefRW<CurrencyComponent>>().WithEntityAccess())
        {
            if (currencyComponent.ValueRW.enabled == true)
            {
                totalCurrencyComponent.ValueRW.currency += currencyComponent.ValueRW.currencyValue;//modifier component values can be added here too
                currencyComponent.ValueRW.enabled = false;
                ecb.RemoveComponent<CurrencyComponent>(e);
                //Debug.Log("MONEY " + totalCurrencyComponent.currency);
                ecb.DestroyEntity(currencyComponent.ValueRW.psAttached);


            }

        }
        
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}

