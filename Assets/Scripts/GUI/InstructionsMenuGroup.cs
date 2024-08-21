using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;
using TMPro;
using Unity.Entities;


public struct InstructionsMenuComponent : IComponentData
{
    public bool hide;
    public int index;
}

[System.Serializable]
public class InstructionsView
{
    public TextMeshProUGUI instruction;
    public string text;
    public float fontSize = 25;
    public float showTimeLength = 3;
    public float hideTimeLength = 1;
    public bool playAudio = false;
    public bool isSetup = false;

}

public class InstructionsMenuGroup : MonoBehaviour
{
    private EntityManager manager;
    public Entity entity;
    AudioSource audioSource;

    private List<Button> buttons;

    //public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    [SerializeField] private Button defaultButton;
    private float showTimer = 0f;
    private bool startShowTimer;

    //[SerializeField] private bool playAudioSource = false;

    [SerializeField]
    private float hideTime = 1f;
    private float hideTimer;
    private bool startHideTimer;
    private float showTimeLength = 3;

    [SerializeField] private InstructionsView startInstructions;
    private bool startInstructionsCompleted;

    public InstructionsView[] instructionList;
    private TextMeshProUGUI currentInstruction;



    private int currentInstructionCount = 1;
    private int totalInstructions = 0;



    void Start()
    {
        manager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var entity = World.DefaultGameObjectInjectionWorld.EntityManager.CreateEntity();
        manager.AddComponentData(entity, new InstructionsMenuComponent()
        {
            
        });
        manager.AddComponentObject(entity, this);
        
        totalInstructions = instructionList.Length;
        hideTimer = hideTime;
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        buttons = gameObject.GetComponentsInChildren<Button>().ToList();
        startShowTimer = false;
        startHideTimer = true;


        currentInstruction = startInstructions.instruction;
        currentInstruction.text = startInstructions.text;
        currentInstruction.fontSize = startInstructions.fontSize;


    }

    void SetupCurrentInstruction(int index)
    {
        if (instructionList.Length < index + 1) return;
        if (instructionList[index].isSetup) return;

        if (instructionList[index].playAudio == true)
        {
            audioSource.Play();

        }


        currentInstruction = instructionList[index].instruction;
        currentInstruction.text = instructionList[index].text;
        currentInstruction.fontSize = instructionList[index].fontSize;
        showTimeLength = instructionList[index].showTimeLength;
        hideTime = instructionList[index].hideTimeLength;
        instructionList[index].isSetup = true;

    }

    void Update()
    {
        if (manager == default) return;

        if (!GameInterface.instance.Paused)
        {
            startInstructionsCompleted = true;
        }
        

        if (currentInstructionCount > totalInstructions || !startInstructionsCompleted) return;




        if (startShowTimer)
        {
            //bool mobile = LevelManager.instance.mobile;
            showTimer += Time.deltaTime;

            if (showTimer > showTimeLength)
            {
                currentInstructionCount += 1;
                showTimer = 0;
                startShowTimer = false;
                startHideTimer = true;
                HideMenu();
            }
        }
        else if (startHideTimer)
        {

            hideTimer -= Time.deltaTime;
            if (hideTimer < 0)
            {
                hideTimer = hideTime;
                startShowTimer = true;
                startHideTimer = false;
                SetupCurrentInstruction(currentInstructionCount - 1);
                ShowMenu();
            }
        }




    }



    public void ShowMenu()
    {
        startShowTimer = true;
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
        }
    }

    public void HideMenu()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }


    
}

