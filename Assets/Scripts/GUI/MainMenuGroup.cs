using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Linq;
using System.Collections.Generic;

public class MainMenuGroup : MonoBehaviour
{

    // Use this for initialization
    AudioSource audioSource;
    private List<Button> buttons;
    public AudioClip clickSound;
    public EventSystem eventSystem;
    private CanvasGroup canvasGroup;
    private GameObject lastSelectedGameObject;

    private void OnEnable()
    {
        OptionsMenuGroup.OptionsExitBackClickedEvent += ShowMenu;
        HowMenuGroup.HowExitBackClickedEvent += ShowMenu;
    }

    private void OnDisable()
    {
        OptionsMenuGroup.OptionsExitBackClickedEvent -= ShowMenu;
        HowMenuGroup.HowExitBackClickedEvent -= ShowMenu;
    }


    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        canvasGroup = GetComponent<CanvasGroup>();
        buttons = gameObject.GetComponentsInChildren<Button>().ToList();
        buttons.ForEach((btn) => btn.onClick.AddListener(() =>
            PlayMenuClickSound(clickSound)));//shortcut instead of using inspector to add to each button
        AddMenuButtonHandlers();
        buttons[0].Select();

    }


    public void Quit()
    {
        SaveManager.instance.SaveWorldSettings();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif

    }

    private void AddMenuButtonHandlers()//reference only
    {
        var obj = gameObject;
        buttons = obj.GetComponentsInChildren<Button>().ToList();//linq using
        buttons.ForEach((btn) => btn.gameObject.AddComponent<EventTrigger>());

        for (var i = 0; i < buttons.Count; i++)
        {
            var trig = buttons[i].gameObject.GetComponent<EventTrigger>();
            trig.triggers = new List<EventTrigger.Entry>();
            var entry = new EventTrigger.Entry();

            //This event will respond to a drop event
            entry.eventID = EventTriggerType.PointerEnter;

            //Create a new trigger to hold our callback methods
            entry.callback = new EventTrigger.TriggerEvent();

            var index = i;

            var callback =
                new UnityAction<BaseEventData>(d => OnButtonEnter(d, index, buttons[index].gameObject));

            //Add our callback to the listeners
            entry.callback.AddListener(callback);
            trig.triggers.Add(entry);
        }
    }


    public void ShowMenu()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        buttons[0].Select();
        //eventSystem.SetSelectedGameObject(lastSelectedGameObject);
    }

    public void HideMenu()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }


    public void OnButtonEnter(BaseEventData bed, int id, GameObject go)
    {

        var ped = (PointerEventData)bed;
        eventSystem.SetSelectedGameObject(go, new BaseEventData(eventSystem));
        lastSelectedGameObject = go;


    }


    void PlayMenuClickSound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
        //Debug.Log("clip " + clip);


    }
}




