using UnityEngine;
using UnityEngine.UI;

// Gives a vertical Scrollbar a fixed-pixel-size handle, for art that shouldn't stretch.
[RequireComponent(typeof(Scrollbar))]
[DefaultExecutionOrder(100)] // runs after the Scroll Rect, so our size wins
public class FixedHandleScrollbar : MonoBehaviour
{
    public float knobHeight = 48f;       // knob height in canvas pixels
    public bool insetSlidingArea = true; // keeps the knob inside the track at both ends

    private Scrollbar bar;
    private RectTransform handle;

    void Awake()
    {
        bar = GetComponent<Scrollbar>();
        handle = bar.handleRect;

        if (handle == null)
        {
            Debug.LogError("FixedHandleScrollbar: the Scrollbar's Handle Rect is not assigned.");
            enabled = false;
            return;
        }

        handle.pivot = new Vector2(0.5f, 0.5f); // knob is centered on its position

        if (insetSlidingArea && handle.parent is RectTransform area)
        {
            float half = knobHeight * 0.5f;
            area.offsetMin = new Vector2(area.offsetMin.x, area.offsetMin.y + half);
            area.offsetMax = new Vector2(area.offsetMax.x, area.offsetMax.y - half);
        }
    }

    void LateUpdate()
    {
        bar.size = 0.001f; // Unity's handle collapses to a point on the track...
        handle.sizeDelta = new Vector2(handle.sizeDelta.x, knobHeight); // ...and we give it a fixed height
    }
}