using Sandbox.Player;
using Unity.Entities;

namespace Player
{
    [RequireMatchingQueriesForUpdate]
    public partial class PlayerTurboSystem : SystemBase
    {

        protected override void OnUpdate()
        {
            Entities.ForEach
            (
                (
                    ref RatingsComponent playerRatings,
                    in PlayerMoveComponent playerMoveComponent,
                    in InputControllerComponent inputController,
                    in PlayerTurboComponent playerTurbo
                ) =>
                {
                    var button = inputController.buttonY_Press;
                    var buttonReleased = inputController.buttonY_Released;
                    var currentSpeed = playerRatings.speed;
                    if (button)
                    {
                        playerRatings.gameSpeed = playerRatings.speed * playerTurbo.multiplier;

                    }
                    else if (buttonReleased)
                    {
                        playerRatings.gameSpeed = playerRatings.speed;
                    }


                }
            ).Schedule();

        }
    }
}

