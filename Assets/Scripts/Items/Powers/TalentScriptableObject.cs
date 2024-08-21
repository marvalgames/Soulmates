using UnityEngine;

namespace Sandbox.Player
{
    [CreateAssetMenu(fileName = "Talent", menuName = "Create Talent")]
    public class TalentScriptableObject : ScriptableObject
    {
        public TalentType talentType = TalentType.None;
        public string talentItemDescription;
        public string talentItemLongDescription;


        [Header ("Stats")]
        public string statDescription1;
        public float statRating1;
        public string statLongDescription1;
        public string statDescription2;
        public float statRating2;
        public string statLongDescription2;
        public string statDescription3;
        public float statRating3;
        public string statLongDescription3;



    }
}



