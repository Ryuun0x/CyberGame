using TMPro;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// The receipt on screen: a placed overlay (CafeReceipt), hidden until the player orders or views it.
// E or Esc puts it away.
public class CafeReceipt : MonoBehaviour
{
    [Tooltip("The receipt paper (hidden until shown).")]
    public GameObject paper;
    [Tooltip("The \"Order #067 · time\" line; the time is filled in when shown.")]
    public TextMeshProUGUI orderLine;

    public static bool IsOpen { get; private set; }

    int openedFrame;

    void Start()
    {
        paper.SetActive(false);
        IsOpen = false;
    }

    public void Show()
    {
        orderLine.text = "Order #067  ·  " + System.DateTime.Now.ToString("h:mm tt");
        paper.SetActive(true);
        IsOpen = true;
        openedFrame = Time.frameCount;
    }

    void Update()
    {
        if (!IsOpen || Time.frameCount == openedFrame) return; // ignore the E press that opened it
#if ENABLE_INPUT_SYSTEM
        bool close = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);
#else
        bool close = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape);
#endif
        if (!close) return;
        paper.SetActive(false);
        IsOpen = false;
    }
}
