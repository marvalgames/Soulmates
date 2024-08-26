using Unity.Collections;
using Unity.Entities;

public partial class TalentAppliedSystem : SystemBase
{

    //EndSimulationEntityCommandBufferSystem m_EndSimulationEcbSystem;


    protected override void OnCreate()
    {
        base.OnCreate();
    }



    protected override void OnUpdate()
    {


        var ecb = new EntityCommandBuffer(Allocator.TempJob);


        Entities.ForEach(
            (
                Entity e, ref TalentItemComponent talentItemComponent

            ) =>
            {
                if (talentItemComponent.enabled)
                {
                    //Debug.Log("Talent Description  " + talentItemComponent.description);
                }

            }
        ).Schedule();





        ecb.Playback(EntityManager);
        ecb.Dispose();




    }




}