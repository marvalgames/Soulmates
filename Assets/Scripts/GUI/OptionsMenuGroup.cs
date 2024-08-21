using Rewired;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.EventSystems;
using UnityEngine.UI;



public class OptionsMenuGroup : MonoBehaviour
{

    public Rewired.Player player;
    public int playerId = 0; // The Rewired player id of this character
    public static event Action OptionsExitBackClickedEvent;
    private List<Button> buttons;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField]
    private CanvasGroup optionsCanvasGroup = null;
    [SerializeField]
    private Button exitButton;
    [SerializeField]
    private Button defaultButton;


    [Header("Audio")]
    [SerializeField]
    AudioSource audioSource;
    public AudioClip clickSound;
    public AudioMixer audioMixer;
    [SerializeField]
    private Slider musicSlider = null;
    [SerializeField]
    private Slider soundSlider = null;



    private void OnEnable()
    {
        GameInterface.SelectClickedEvent += Back;
        PickupMenuGroup.HideSubscriberMenu += HideSubscriberMenu;
    }
    private void OnDisable()
    {
        GameInterface.SelectClickedEvent -= Back;
        PickupMenuGroup.HideSubscriberMenu -= HideSubscriberMenu;
    }

    private void HideSubscriberMenu(bool resume)
    {
        HideMenu();//event has parameter but this has HideMenu method with no parameter so just call from here
    }


    void Start()
    {

        if (!ReInput.isReady) return;
        player = ReInput.players.GetPlayer(playerId);
        optionsCanvasGroup = GetComponent<CanvasGroup>();
        var sw = SaveManager.instance.saveWorld;
        soundSlider.value = sw.soundVolume;
        musicSlider.value = sw.musicVolume;
        //int currentQualityIndex = sw.graphicsQuality - 1;
        //if (currentQualityIndex < 0) currentQualityIndex = 2;//default high
        buttons = GetComponentsInChildren<Button>().ToList();
        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
        PlayButtonClickSound(clickSound)));


    }



    private void PlayButtonClickSound(AudioClip clip)
    {
        if (!audioSource) return;
        audioSource.clip = clip;
        audioSource.Play();
    }

    private void Back()
    {
        if (optionsCanvasGroup == null || eventSystem == null) return;
        if (eventSystem.currentSelectedGameObject == null) return;
        OnExitButtonClicked();
        HideMenu();

    }

    private void Update()
    {



        audioMixer.SetFloat("musicVolume", musicSlider.value * .8f - 80);//0 to 100 slider -80 to 0 db
        audioMixer.SetFloat("soundVolume", soundSlider.value * .8f - 80);//0 to 100 slider -80 to 0 db



    }

    public void ShowMenu()
    {
        if (optionsCanvasGroup == null) return;//gets destroyed sometimes ???
        optionsCanvasGroup.interactable = true;
        optionsCanvasGroup.alpha = 1;
        optionsCanvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
        }

    }

    public void HideMenu()
    {
        if (optionsCanvasGroup == null) return;//gets destroyed sometimes ???
 
        //GameInterface.Paused = false;
       // GameInterface.StateChange = true;
        
        eventSystem.sendNavigationEvents = true;
        //optionsCanvasGroup = GetComponent<CanvasGroup>();
        optionsCanvasGroup.interactable = false;
        optionsCanvasGroup.alpha = 0.0f;
        optionsCanvasGroup.blocksRaycasts = false;

    }


    public void OnExitButtonClicked()//saved in memory
    {
        if (optionsCanvasGroup == null) return;//gets destroyed sometimes ???

        OptionsExitBackClickedEvent?.Invoke();


        SaveManager.instance.saveWorld.musicVolume = musicSlider.value;
        SaveManager.instance.saveWorld.soundVolume = soundSlider.value;

    }

    public void OnMusicSliderValueChanged(float musicVolume)
    {
        var value = musicVolume * .8f - 80f;
        audioMixer.SetFloat("musicVolume", value);//0 to 100 slider -80 to 0 db

    }

    public void OnSoundSliderValueChanged(float soundVolume)
    {
        var value = soundVolume * .8f - 80f;
        audioMixer.SetFloat("soundVolume", value);
        Debug.Log("so " + value);
    }

}







