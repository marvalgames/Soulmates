using Sandbox.Player;
using Unity.Entities;

[RequireMatchingQueriesForUpdate]
public partial class LevelUpMechanicSystem : SystemBase
{

    protected override void OnCreate()
    {
        base.OnCreate();
    }

    protected override void OnUpdate()
    {

        Entities.ForEach(
            (
                Entity e,
                //ref ControlBarComponent controlBar,
                ref LevelUpMechanicComponent levelUpMechanic,
                ref RatingsComponent ratings,
                in HealthComponent health,
                in SkillTreeComponent skillTreeComponent

            ) =>
            {
                var pointsNeeded = skillTreeComponent.PointsNextLevel * skillTreeComponent.CurrentLevel;


                var pct = skillTreeComponent.CurrentLevelXp / (float)pointsNeeded;
                //controlBar.value = pct;


            }


        ).ScheduleParallel();



    }
}

