﻿using System;
using Unity.Entities;
using UnityEngine;

[Serializable]
public struct SaveComponent : IComponentData
{
    public bool value;
    public bool saveGame;
    public bool saveScore;
}




    
public class SaveAuthoring : MonoBehaviour    
{
        public bool saveGame;
        public bool saveScore;

    
}
