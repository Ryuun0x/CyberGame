using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasRenderer))]
public class CityMapSurface : MaskableGraphic
{
    public CityMapLayout layout;
    public Vector2 Center { get; private set; }
    public float Range { get; private set; }
    Texture2D artwork;
    public override Texture mainTexture => artwork != null ? artwork : Texture2D.whiteTexture;

    protected override void Awake()
    {
        base.Awake();
        artwork = Resources.Load<Texture2D>("CityMapArtwork");
        if (artwork == null) Debug.LogError("[CityMap] Rebuild Illustrated City Layout to create CityMapArtwork.png.", this);
    }

    public void SetView(Vector2 center, float range)
    {
        if (Center == center && Mathf.Approximately(Range, range)) return;
        Center = center;
        Range = range;
        SetVerticesDirty();
    }

    public Vector2 Project(Vector2 world) => (world - Center) * (rectTransform.rect.height / (2 * Mathf.Max(1, Range)));

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        if (layout == null || artwork == null) return;
        Rect viewport = rectTransform.rect;
        Vector2 min = Project(layout.min), max = Project(layout.max);
        Rect visible = Rect.MinMaxRect(Mathf.Max(viewport.xMin, min.x), Mathf.Max(viewport.yMin, min.y),
            Mathf.Min(viewport.xMax, max.x), Mathf.Min(viewport.yMax, max.y));
        if (visible.width <= 0 || visible.height <= 0) return;
        Vector2 size = max - min;
        Add(vh, new Vector2(visible.xMin, visible.yMin), min, size);
        Add(vh, new Vector2(visible.xMin, visible.yMax), min, size);
        Add(vh, new Vector2(visible.xMax, visible.yMax), min, size);
        Add(vh, new Vector2(visible.xMax, visible.yMin), min, size);
        vh.AddTriangle(0, 1, 2);
        vh.AddTriangle(0, 2, 3);
    }

    static void Add(VertexHelper vh, Vector2 point, Vector2 min, Vector2 size)
    {
        vh.AddVert(point, Color.white, new Vector2((point.x - min.x) / size.x, (point.y - min.y) / size.y));
    }
}
