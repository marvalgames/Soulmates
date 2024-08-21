using System;
using System.Collections.Generic;
using System.Linq;
using Rewired;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HowMenuGroup : MonoBehaviour
{
    // Start is called before the first frame update
    AudioSource audioSource;
    private List<Button> buttons;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    [SerializeField]
    private Button defaultButton;


    public Rewired.Player player;
    public static event Action HowExitBackClickedEvent;



    void Start()
    {

        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(0);


        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        buttons = gameObject.GetComponentsInChildren<Button>().ToList();
        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
            PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button

    }

    // Update is called once per frame
    void Update()
    {
        var buttonSelect_Pressed = player.GetButtonDown("Select");
        if (buttonSelect_Pressed)
        {
            HideMenu();
            HowExitBackClickedEvent?.Invoke();
        }




    }



    public void ShowMenu()
    {
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

    void PlayMenuClickSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }










}
