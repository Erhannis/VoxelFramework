using UnityEngine;
using System.Collections;

public class VoxelGeomScript : MonoBehaviour
{
    public Shader geomShader;
    Material material;

    public Texture2D sprite;
    public float size = 0.1f;
    public Color color = new Color(1.0f, 0.6f, 0.3f, 0.03f);

    VoxelComputeShaderOutput cso;

    // Use this for initialization
    void Start()
    {
        material = new Material(geomShader);

        cso = GetComponent<VoxelComputeShaderOutput>();
    }

    bool getData = true;

    const int cm = 5;
    Color[] colorArray = new Color[10*10*10 * cm*cm*cm];

    bool left = false;

    void OnRenderObject() {
        if (getData)
        {
            getData = false;
            cso.Dispatch();
        }

        if (left) {
            for (int x = 0; x < (10 * cm); x++) {
                for (int y = 0; y < (10 * cm); y++) {
                    for (int z = 0; z < (10 * cm); z++) {
                        int idx = x + (y * 10 * cm) + (z * 10 * cm * 10 * cm);

                        Color c = Random.ColorHSV();
                        c.a = 0.1f;
                        c.r *= 0.01f;
                        c.g *= 0.01f;
                        c.b *= 0.01f;
                        colorArray[idx] = c;
                    }
                }
            }
            cso.colorBuffer.SetData(colorArray);
        }
        left = !left;

        material.SetPass(0);
        //material.SetColor("_Color", color);
        material.SetBuffer("buf_Points", cso.outputBuffer);
        material.SetBuffer("buf_Colors", cso.colorBuffer);
        //material.SetTexture("_Sprite", sprite);

        material.SetFloat("_Size", size);
        material.SetMatrix("world", transform.localToWorldMatrix);

        Graphics.DrawProcedural(MeshTopology.Points, cso.outputBuffer.count);
    }
}
