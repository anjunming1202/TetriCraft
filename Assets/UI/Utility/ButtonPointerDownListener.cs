using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ButtonPointerDownListener : MonoBehaviour, IPointerDownHandler
{
    public UnityEvent onDown = new UnityEvent();

    public void OnPointerDown(PointerEventData eventData)
    {
        onDown?.Invoke();
    }
}