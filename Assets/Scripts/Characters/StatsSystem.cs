using Sandbox.Player;
using Unity.Entities;
using UnityEngine;

[RequireMatchingQueriesForUpdate]
public partial class StatsSystem : SystemBase
{
    protected override void OnUpdate()
    {

        var showMessage = false;
        Entities.WithoutBurst().ForEach
        (
            (
                Entity e,
                ref StatsComponent statsComponent,
                ref SkillTreeComponent skillTree,
                ref RatingsComponent ratingsComponent,
                ref HealthComponent healthComponent
                 
            ) =>
            {
                //Debug.Log("pts " + skillTree.PointsNextLevel + " lvl " + skillTree.CurrentLevel);

                var pointsNeeded = skillTree.PointsNextLevel * skillTree.CurrentLevel;


                if (skillTree.CurrentLevelXp >= pointsNeeded)
                {
                    skillTree.CurrentLevelXp = 0;
                    skillTree.CurrentLevel += 1;
                    skillTree.availablePoints += 1;

                    if (SystemAPI.HasComponent<LevelUpMechanicComponent>(e) == true && skillTree.CurrentLevel <= 8)
                    {
                        ratingsComponent.gameSpeed = ratingsComponent.gameSpeed * 1.1f;
                        ratingsComponent.gameWeaponPower = ratingsComponent.gameWeaponPower * 1.1f;
                        ratingsComponent.maxHealth = ratingsComponent.maxHealth * 1.1f;
                        showMessage = true;
                        Debug.Log("speed boost");
                    }

                }
            }
        ).Run();

        Entities.WithoutBurst().WithStructuralChanges().ForEach(
            (in ShowMessageMenuComponent messageMenuComponent, in ShowMessageMenuGroup messageMenu) =>
            {
                if (showMessage == true)
                {

                    messageMenu.messageString = "Level Up ...  Max Health Up ... HP Up ... Durability Up";
                    messageMenu.ShowMenu();
                }
            }
        ).Run();










    }






}


