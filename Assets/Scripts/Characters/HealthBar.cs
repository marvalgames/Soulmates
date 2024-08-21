using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;




public class HealthBar : MonoBehaviour
{

    public Image _healthBar = null;
    bool showHealth = true;
    [SerializeField] float showTime = 3;
    float alphaTime = 0;
    public TextMeshPro score3dText;
    private TextMeshPro score3dTextInstance;

    public Entity entity;
    private EntityManager entityManager;
    //Animator animator;




    void Start()
    {
        //animator = GetComponent<Animator>();
        if (entity == Entity.Null)
        {
            entity = GetComponent<CharacterEntityTracker>().linkedEntity;
            if (entityManager == default)
            {
                entityManager = GetComponent<CharacterEntityTracker>().entityManager;
            }
            //Debug.Log("LE1 " + entity);
            //entityManager.AddComponentObject(entity, this);
            if(entity != Entity.Null) entityManager.AddComponentObject(entity, this);

            //entityManager.AddComponentObject(entity, animator);

        }

        if (score3dText)
        {
            var ps = Instantiate(score3dText);
            ps.transform.SetParent( transform, false);
            ps.transform.localPosition = new Vector3(0, 5, 0);
            score3dTextInstance = ps;
            //renderer = score3dTextInstance.GetComponent<MeshRenderer>();
            //material = renderer.sharedMaterial;
            SetAlpha(0);
        }
  

    }


    void SetAlpha(float alphaValue)
    {
        var color = score3dTextInstance.color;
        color.a = alphaValue;
        score3dTextInstance.color = color;
    }


    void Update()
    {
        if (showHealth == true && entity != Entity.Null)
        {
            showHealth = false;
            HealthChange();
        }


        if (alphaTime > 0)
        {
            alphaTime += Time.deltaTime;
            if(alphaTime > showTime)
            {
                alphaTime = 0;
                SetAlpha(0);
            }
            else
            {
                SetAlpha((showTime - alphaTime) / showTime);
            }
        }
    }


    public void ShowText3dValue(int value)
    {
        score3dTextInstance.text = value.ToString();
        //Debug.Log("val " + value);
        SetAlpha(1);
        alphaTime += Time.deltaTime; 
        //StartCoroutine(Wait(showTime));
        //SetAlpha(1);


    }



    public void HealthChange()
    {

        if (_healthBar == null || !entityManager.HasComponent<HealthComponent>(entity))
        {
            return;
        }

        var maxHealth = 100f;

        if (entityManager.HasComponent<RatingsComponent>(entity))
        {
            maxHealth = entityManager.GetComponentData<RatingsComponent>(entity).maxHealth;
        }

        var damage = entityManager.GetComponentData<HealthComponent>(entity).totalDamageReceived;

        var pct = 1.00f - (damage / maxHealth);
        if (pct < 0)
        {
            pct = 0;
        }

        var en = (entityManager.HasComponent<RatingsComponent>(entity));
        //Debug.Log("td " + damage + " en " + en + " max " + maxHealth);
        _healthBar.gameObject.transform.localScale = new Vector3(pct, 1f, 1f);

    }



   
}
