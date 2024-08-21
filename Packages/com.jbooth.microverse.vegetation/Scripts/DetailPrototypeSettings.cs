using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace JBooth.MicroVerseCore
{
    [CreateAssetMenu(fileName = "DetailPrototypeSettings", menuName = "MicroVerse/Detail Prototype Settings")]
    public class DetailPrototypeSettings : ScriptableObject
    {
        public DetailPrototypeSerializable prototype;
    }
}
