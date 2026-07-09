using UnityEngine;
using UnityEngine.EventSystems;

public class FireButton : MonoBehaviour, IPointerDownHandler
{
    private ElementSelectionManager manager;
    void Awake() { manager = FindObjectOfType<ElementSelectionManager>(); }

    public void OnPointerDown(PointerEventData eventData)
    {
        manager.ToggleElement(ElementSelectionManager.ElementType.Fire);
    }
}