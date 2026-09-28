using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Café counter: ordering a drink hands over a receipt printed with the real Wi-Fi name and password.
// Needs a Collider (for the interaction raycast) and optionally an InteractableLabel.
public class CafeCounter : MonoBehaviour, IInteractable
{
    public const string WifiName = "CafeWifi67";
    public const string WifiPassword = "brew2026";

    void Start() => UpdatePrompt();

    public void Interact()
    {
        // The E press that puts the receipt away must not reopen it (Destroy lands at end of frame, so IsOpen still holds).
        if (CafeReceipt.IsOpen) return;

        var gp = GameProgressManager.Instance;
        if (gp != null && !gp.hasReceipt)
        {
            gp.OrderDrink();
            if (NarrationManager.Instance != null)
                NarrationManager.Instance.Show("One iced latte, please. ...Oh, the Wi-Fi password is on the receipt.", 4f);
        }
        CafeReceipt.Show();
        UpdatePrompt();
    }

    void UpdatePrompt()
    {
        var label = GetComponent<InteractableLabel>();
        bool ordered = GameProgressManager.Instance != null && GameProgressManager.Instance.hasReceipt;
        if (label != null) label.promptText = ordered ? "View Receipt" : "Order a Drink";
    }
}

public class CafeReceipt : MonoBehaviour
{
    public static bool IsOpen => _current != null;
    static CafeReceipt _current;
    int _openedFrame;

    public static void Show()
    {
        if (_current != null) return;
        _current = new GameObject("CafeReceipt").AddComponent<CafeReceipt>();
        _current.Build();
    }

    void Build()
    {
        _openedFrame = Time.frameCount;
        UIBuild.Overlay(gameObject, 420);
        var ink = new Color(0.16f, 0.15f, 0.14f);

        var paper = UIBuild.NewRect("Receipt", transform);
        paper.anchorMin = paper.anchorMax = new Vector2(0.5f, 0.5f);
        paper.sizeDelta = new Vector2(600, 0);
        paper.localRotation = Quaternion.Euler(0, 0, -2);
        paper.gameObject.AddComponent<Image>().color = new Color(0.98f, 0.97f, 0.93f);
        UIBuild.Column(paper, 40, 10);
        paper.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Line(paper, "CAFÉ 67", 40, ink, FontStyles.Bold);
        Line(paper, "Order #067  ·  " + System.DateTime.Now.ToString("h:mm tt"), 20, ink, FontStyles.Normal);
        Line(paper, "- - - - - - - - - - - - - - - - - - -", 20, ink, FontStyles.Normal);
        var item = Line(paper, "1  Iced Latte<pos=80%>4.50", 26, ink, FontStyles.Normal);
        item.alignment = TextAlignmentOptions.Left;
        var total = Line(paper, "<b>TOTAL</b><pos=80%><b>4.50</b>", 26, ink, FontStyles.Normal);
        total.alignment = TextAlignmentOptions.Left;
        Line(paper, "- - - - - - - - - - - - - - - - - - -", 20, ink, FontStyles.Normal);
        Line(paper, $"WiFi: <b>{CafeCounter.WifiName}</b>  |  Password: <b>{CafeCounter.WifiPassword}</b>", 22, ink, FontStyles.Normal);
        Line(paper, "Thank you! Come again.", 22, ink, FontStyles.Italic);
        Line(paper, "[E] Put away", 18, new Color(0.45f, 0.44f, 0.42f), FontStyles.Normal);
    }

    static TextMeshProUGUI Line(RectTransform parent, string text, float size, Color color, FontStyles style)
    {
        var label = UIBuild.Label(parent, text, size, color, style);
        label.alignment = TextAlignmentOptions.Center;
        return label;
    }

    void Update()
    {
        if (Time.frameCount == _openedFrame) return; // ignore the E press that opened it
#if ENABLE_INPUT_SYSTEM
        bool close = Keyboard.current != null && (Keyboard.current.eKey.wasPressedThisFrame || Keyboard.current.escapeKey.wasPressedThisFrame);
#else
        bool close = Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape);
#endif
        if (close) Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (_current == this) _current = null;
    }
}
