using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class GamePadGroup : MonoBehaviour
{
    private CanvasGroup canvasGroup = null;
    [SerializeField]
    private Button defaultButton;
    [SerializeField] private EventSystem eventSystem;
    private bool show;


    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        //        PauseMenuGroup.OptionsClickedEvent += ShowMenu;
        GameInterface.HideMenuEvent += HideMenu;

    }

    private void OnDisable()
    {
        //PauseMenuGroup.OptionsClickedEvent -= ShowMenu;
        GameInterface.HideMenuEvent -= HideMenu;


    }

    void Update()
    {
        if (show == true)
        {
            eventSystem.sendNavigationEvents = false;
        }
        else
        {
            eventSystem.sendNavigationEvents = true;
        }
    }


    public void ShowMenu()
    {
        show = true;
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
        }

        Debug.Log("        eventSystem");

    }

    public void HideMenu()
    {
        if (GetComponent<CanvasGroup>() == null || canvasGroup == null) return;//gets destroyed sometimes ???
        show = false;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }

}







