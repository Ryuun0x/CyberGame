using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RoundedRect : BaseMeshEffect
{
    public float radius = 20f;

    public override void ModifyMesh(VertexHelper vh)
    {
    }
}