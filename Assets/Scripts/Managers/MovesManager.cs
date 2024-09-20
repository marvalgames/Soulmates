using System.Collections.Generic;
using Enemy;
using UnityEngine;

public class MovesManager : MonoBehaviour
{
    public List<Moves> Moves = new List<Moves>();

    private void Start()
    {
        for (var i = 0; i < Moves.Count; i++)
        {
            var move = Moves[i];
        }
    }



}
