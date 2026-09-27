using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The stranger at the table next to the player's laptop. After the upload, when the player stands up,
// their screen flashes the Ghostline logo, the lid closes and they walk out of the café.
// Their laptop is a stripped copy of the café laptop; the logo is drawn in code (no art asset yet).
public class CafeStranger : MonoBehaviour
{
    // Next table over (Table_08 (2)); the screen faces the player's seat so the logo can be glimpsed.
    static readonly Vector3 LaptopSpot = new Vector3(-3.98f, 0.939f, 79.95f);
    static readonly Vector3 StandSpot = new Vector3(-3.35f, 0f, 80.45f);
    // Around the counter and out through the café door.
    static readonly Vector3[] ExitPath =
    {
        new Vector3(-2.6f, 0, 82.6f), new Vector3(-0.9f, 0, 83.3f), new Vector3(-0.61f, 0, 84.8f), new Vector3(-0.61f, 0, 89f)
    };
    const float WalkSpeed = 1.4f;

    static CafeStranger _current;

    Transform laptop, hinge;
    GameObject screen;
    Animator anim;
    bool leaving;

    public static void Spawn(CityLife city)
    {
        var cafeLaptop = FindFirstObjectByType<LaptopInteraction>();
        if (city == null || city.characters == null || city.characters.Length == 0 || cafeLaptop == null) return;
        var person = city.Person("Stranger", StandSpot, 0);
        person.transform.rotation = Quaternion.LookRotation(Flat(LaptopSpot - person.transform.position));
        _current = person.AddComponent<CafeStranger>();
        _current.anim = person.GetComponentInChildren<Animator>();
        _current.BuildLaptop(cafeLaptop.transform);
    }

    public static void LeaveIfPresent()
    {
        if (_current != null && !_current.leaving) _current.StartCoroutine(_current.Leave());
    }

    static Vector3 Flat(Vector3 v) { v.y = 0; return v.normalized; }

    void BuildLaptop(Transform cafeLaptop)
    {
        // Same model, no interaction: the café laptop's screen faces +X at yaw 180, so yaw 90 faces +Z.
        laptop = Instantiate(cafeLaptop.gameObject, LaptopSpot, Quaternion.Euler(0, 90, 0)).transform;
        laptop.name = "StrangerLaptop";
        foreach (var mb in laptop.GetComponentsInChildren<MonoBehaviour>()) DestroyImmediate(mb); // before LaptopInteraction.Start can touch the café canvas
        var viewPoint = laptop.Find("LaptopViewPoint");
        if (viewPoint != null) Destroy(viewPoint.gameObject);
        hinge = laptop.Find("ScreenHinge");

        // Screen canvas: same pose relative to the hinge as the café laptop's LaptopCanvas, so it folds with the lid.
        var cafeHinge = cafeLaptop.Find("ScreenHinge");
        var cafeCanvas = FindFirstObjectByType<LaptopDesktop>(FindObjectsInactive.Include);
        if (hinge == null || cafeHinge == null || cafeCanvas == null) return;
        var source = (RectTransform)cafeCanvas.transform;
        var rect = UIBuild.NewRect("GhostlineScreen", hinge);
        rect.gameObject.layer = 0;
        rect.localPosition = cafeHinge.InverseTransformPoint(source.position);
        rect.localRotation = Quaternion.Inverse(cafeHinge.rotation) * source.rotation;
        rect.localScale = source.lossyScale / cafeHinge.lossyScale.x;
        rect.sizeDelta = source.rect.size;
        rect.gameObject.AddComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        screen = rect.gameObject;
        BuildLogo(rect);
        screen.SetActive(false);
    }

    static void BuildLogo(RectTransform rect)
    {
        var green = new Color(0.35f, 1f, 0.55f);
        UIBuild.Stretch(UIBuild.NewRect("Background", rect)).gameObject.AddComponent<Image>().color = new Color(0.02f, 0.03f, 0.04f);
        var icon = UIBuild.NewRect("Ghost", rect);
        icon.sizeDelta = new Vector2(360, 360);
        icon.anchoredPosition = new Vector2(0, 110);
        icon.gameObject.AddComponent<Image>().sprite = UIBuild.Icon(128, IsGhost, green);
        var label = UIBuild.Label(rect, "GHOSTLINE", 150, green, FontStyles.Bold);
        label.alignment = TextAlignmentOptions.Center;
        label.characterSpacing = 18;
        label.rectTransform.sizeDelta = new Vector2(1800, 200);
        label.rectTransform.anchoredPosition = new Vector2(0, -210);
    }

    // Ghost: round head, straight sides, wavy hem, two eye holes.
    static bool IsGhost(float x, float y)
    {
        bool head = y > 0 && x * x + y * y < 0.6f;
        bool body = y <= 0 && Mathf.Abs(x) < 0.77f && y > -0.75f + 0.12f * Mathf.Cos(x * 12f);
        bool eye = (x + 0.28f) * (x + 0.28f) + (y - 0.1f) * (y - 0.1f) < 0.02f || (x - 0.28f) * (x - 0.28f) + (y - 0.1f) * (y - 0.1f) < 0.02f;
        return (head || body) && !eye;
    }

    IEnumerator Leave()
    {
        leaving = true;
        if (screen != null) screen.SetActive(true);
        yield return new WaitForSeconds(2.5f);

        // Fold the lid down onto the keyboard.
        if (hinge != null)
        {
            var keyboard = laptop.Find("KeyboardFrame");
            Vector3 toKeyboard = keyboard != null ? Flat(keyboard.GetComponent<Renderer>().bounds.center - hinge.position) : laptop.right;
            Quaternion open = hinge.rotation, closed = Quaternion.FromToRotation(Vector3.up, toKeyboard) * open;
            for (float t = 0; t < 1; t += Time.deltaTime / 0.7f)
            {
                hinge.rotation = Quaternion.Slerp(open, closed, t * t);
                yield return null;
            }
            hinge.rotation = closed;
        }
        if (screen != null) screen.SetActive(false);
        yield return new WaitForSeconds(0.6f);

        Destroy(laptop.gameObject); // takes it with them
        anim.SetFloat("Speed", 0.5f); // blend tree: 0.5 = walk
        foreach (var point in ExitPath)
            while (Vector2.Distance(Flat2(transform.position), Flat2(point)) > 0.05f)
            {
                Vector3 here = transform.position;
                Vector3 step = Vector3.MoveTowards(new Vector3(here.x, here.y, here.z), new Vector3(point.x, here.y, point.z), WalkSpeed * Time.deltaTime);
                Vector3 dir = Flat(step - here);
                if (dir != Vector3.zero) transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 8 * Time.deltaTime);
                step.y = CityLife.GroundY(step);
                transform.position = step;
                yield return null;
            }
        Destroy(gameObject);
    }

    static Vector2 Flat2(Vector3 v) => new Vector2(v.x, v.z);

    void OnDestroy()
    {
        CityLife.Movers.Remove(transform);
        if (laptop != null) Destroy(laptop.gameObject);
    }
}
