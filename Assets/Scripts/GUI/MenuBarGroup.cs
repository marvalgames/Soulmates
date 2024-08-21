using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class MenuBarGroup : MonoBehaviour
{

    private CanvasGroup canvasGroup = null;
    [SerializeField]
    private Button defaultButton;

    [SerializeField] private EventSystem eventSystem;


    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();

        defaultButton.Select();

    }

    private void OnEnable()
    {
        //PauseMenuGroup.OptionsClickedEvent += ShowMenu;
        GameInterface.HideMenuEvent += HideMenu;

    }

    private void OnDisable()
    {
        //PauseMenuGroup.OptionsClickedEvent -= ShowMenu;
        GameInterface.HideMenuEvent -= HideMenu;


    }





    public void ShowMenu()
    {
        canvasGroup.interactable = true;
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        if (defaultButton)
        {
            defaultButton.Select();
        }

    }

    public void HideMenu()
    {
        if (GetComponent<CanvasGroup>() == null || canvasGroup == null) return;//gets destroyed sometimes ???
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }

}







