using UnityEngine;

namespace Sandbox.Player
{
    [CreateAssetMenu(fileName = "Resource", menuName = "Create Resource")]
    public class ResourceScriptableObject : ScriptableObject
    {
        public ResourceType resourceType = ResourceType.Currency;
        public CurrencyType currencyType = CurrencyType.Pouds;
        public string resourceItemDescription;
        public string resourceItemLongDescription;

        public float resourceValue;

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



