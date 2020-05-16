using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugTexture : MonoBehaviour
{
    IndexedTextureAnimator itr;
    // Start is called before the first frame update
    void Start()
    {
        itr = this.GetComponent<IndexedTextureAnimator>();
    }
    Vector2 mpos;
    Collider2D hit;
    public Vector2 local;
    public Vector2 uv;
    public int index;
    public Color color;
    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            mpos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            hit = Physics2D.OverlapPoint(mpos);
            {
                if (hit)
                {

                    local = hit.transform.InverseTransformPoint(mpos);
                }
            }
        }
        uv = local;
        uv.x = uv.x * itr.texture.pixelsPerUnit / itr.texture.width;
        uv.y = uv.y * itr.texture.pixelsPerUnit / itr.texture.height;
       
        color = itr.GetColor((short)((uv.x + 0.5f) * itr.texture.width), (short)((uv.y + 0.5f) * itr.texture.height));
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(this.transform.TransformPoint(local), Vector3.one * 0.01f);
    }
}
