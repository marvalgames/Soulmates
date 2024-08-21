using UnityEngine;
using UnityEngine.EventSystems;

public class ControlMapperGame : MonoBehaviour
{
    private CanvasGroup canvasGroup;
    [SerializeField]
    private EventSystem eventSystem;
    [SerializeField]
    private GameObject lastSelectedGameObject;

    private void OnEnable()
    {
        GameInterface.HideMenuEvent += HideMenu;
    }

    private void OnDisable()
    {
        GameInterface.HideMenuEvent -= HideMenu;
    }




    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }



    public void ShowMenu()
    {
        canvasGroup.alpha = 1;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;
        eventSystem.SetSelectedGameObject(lastSelectedGameObject);
    }

    public void HideMenu()
    {
        canvasGroup.interactable = false;
        canvasGroup.alpha = 0.0f;
        canvasGroup.blocksRaycasts = false;

    }





}
