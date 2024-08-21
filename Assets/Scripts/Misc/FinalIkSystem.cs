using Sandbox.Player;
using Unity.Entities;

[UpdateInGroup(typeof(PresentationSystemGroup))]
[RequireMatchingQueriesForUpdate]
public partial class FinalIkSystem : SystemBase
{
    protected override void OnUpdate()
    {


        Entities.WithoutBurst().WithAny<DeadComponent>().ForEach((PlayerCombat playerCombat) =>
        {

            playerCombat.LateUpdateSystem();


        }).Run();

        Entities.WithoutBurst().WithAny<DeadComponent>().ForEach((EnemyMelee  enemyMelee) =>
        {
            enemyMelee.LateUpdateSystem();


        }).Run();


        Entities.WithoutBurst().WithAny<DeadComponent>().ForEach((BossIKScript bossIK) =>
        {

            bossIK.LateUpdateSystem();


        }).Run();
    }
}
