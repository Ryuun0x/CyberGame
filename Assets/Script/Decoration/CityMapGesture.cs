using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CityMapGesture : MonoBehaviour, IScrollHandler, IDragHandler, IPointerDownHandler, IPointerUpHandler
{
    public CityMapController map;
    readonly Dictionary<int, Vector2> pointers = new Dictionary<int, Vector2>();

    public void OnScroll(PointerEventData eventData)
    {
        if (eventData.scrollDelta.y != 0) map.Zoom(eventData.scrollDelta.y > 0 ? 0.8f : 1.25f);
    }

    public void OnPointerDown(PointerEventData eventData) => pointers[eventData.pointerId] = eventData.position;
    public void OnPointerUp(PointerEventData eventData) => pointers.Remove(eventData.pointerId);

    public void OnDrag(PointerEventData eventData)
    {
        float before = PinchDistance();
        pointers[eventData.pointerId] = eventData.position;
        float after = PinchDistance();
        if (pointers.Count >= 2)
        {
            if (before > 1 && after > 1) map.Zoom(before / after);
            return;
        }
        var rect = (RectTransform)transform;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position, eventData.pressEventCamera, out var now) &&
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, eventData.position - eventData.delta, eventData.pressEventCamera, out var previous))
            map.Pan(now - previous);
    }

    float PinchDistance()
    {
        var positions = pointers.Values.GetEnumerator();
        if (!positions.MoveNext()) return 0;
        var first = positions.Current;
        return positions.MoveNext() ? Vector2.Distance(first, positions.Current) : 0;
    }

    void OnDisable() => pointers.Clear();
}
