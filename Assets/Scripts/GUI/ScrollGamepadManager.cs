using Rewired;
using UnityEngine;
using UnityEngine.UI;

public class ScrollGamepadManager : MonoBehaviour
{
    [Header("SLIDER")]
    public Scrollbar scrollbarObject;
    public float changeValue = 0.05f;

    Rewired.Player player;
    public int playerId = 0; // The Rewired player id of this character



    void Start()
    {

        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(playerId);

    }



    void Update()
    {

        if (!ReInput.isReady) return;


        var h = player.GetAxis("Move Horizontal");


        if (h == 1)
            scrollbarObject.value += changeValue;
        else if (h == -1)
            scrollbarObject.value -= changeValue;
    }
}