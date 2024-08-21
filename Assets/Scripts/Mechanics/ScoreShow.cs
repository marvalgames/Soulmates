using TMPro;
using Unity.Entities;
using UnityEngine;


public class ScoreShow : MonoBehaviour

{
    [SerializeField]
    private TextMeshProUGUI labelScore;
    [SerializeField]
    private TextMeshProUGUI labelStreak;
    [SerializeField]
    private TextMeshProUGUI labelCombo;
    [SerializeField]
    private TextMeshProUGUI labelLevel;

    public Entity entity;
    public EntityManager manager;

    public void Start()
    {
        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (manager == default)
            {
                manager = GetComponent<CharacterEntityTracker>().entityManager;
            }

            //manager.AddComponentObject(entity, this);
            if(entity != Entity.Null) manager.AddComponentObject(entity, this);

        }

    }


    public void ShowLabelStreak(int streak)
    {
        if (labelStreak == null) return;
        labelStreak.text = streak.ToString();
    }


    public void ShowLabelScore(int score)
    {

        if (labelScore == null) return;

        labelScore.text = score.ToString();
        
    }

    public void ShowLabelCombo(int combo)
    {
        if (labelCombo == null) return;
        labelCombo.text = combo.ToString();
    }

    public void ShowLabelLevel(int level)
    {
        if (labelLevel == null) return;
        labelLevel.text= level.ToString();
    }


    

  
}
