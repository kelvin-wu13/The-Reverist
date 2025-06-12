using UnityEngine;

public class BackgroundScroller : MonoBehaviour
{
    [Range(-1f,1f)]

    [SerializeField] public float scrollSpeed = 0.05f;

    private float offset;
    private Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;

    }

    void Update()
    {
        offset += (Time.deltaTime*scrollSpeed) / 10f;
        mat.SetTextureOffset("_MainTex", new Vector2(offset,0));
    }
}
