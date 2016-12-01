using UnityEngine;
using System.Collections;

public class VoxelComputeShaderOutput : MonoBehaviour
{

    #region Compute Shader Fields and Properties

    /// <summary>
    /// The Compute shader we will use
    /// </summary>
    public ComputeShader computeShader;

    /// <summary>
    /// The total number of verticies to calculate.
    /// 10 * 10 * 10 block rendered in 10 * 10 * 10 threads in 1 * 1 * 1 groups
    /// </summary>
    int vertCount;

    public int seed;

    /// <summary>
    /// This buffer will store the calculated data resulting from the Compute shader.
    /// </summary>
    public ComputeBuffer outputBuffer;
    public ComputeBuffer mapBuffer;
    public ComputeBuffer colorBuffer;

    public Shader pointShader;
    Material pointMaterial;

    public bool debugRender = false;

    public int cubeMultiplier = 5;

    /// <summary>
    /// A Reference to the CS Kernel we want to use.
    /// </summary>
    int csKernel;

    #endregion

    void InitializeBuffers()
    {
        Random random = new Random();

        vertCount = 10 * 10 * 10 * cubeMultiplier * cubeMultiplier * cubeMultiplier;

        // Set output buffer size.
        outputBuffer = new ComputeBuffer(vertCount, (sizeof(float) * 3) + (sizeof(int) * 6));
        mapBuffer = new ComputeBuffer(vertCount, sizeof(int));
        colorBuffer = new ComputeBuffer(vertCount, (sizeof(float) * 4));

        int width = 10 * cubeMultiplier;
        int height = 10 * cubeMultiplier;
        int depth = 10 * cubeMultiplier;

        PerlinNoise.Seed = seed;
        float[][] fmap = PerlinNoise.GeneratePerlinNoise(width, height, 8);
        //fmap = PerlinNoise.GeneratePerlinNoise(fmap, 8);

        int[] map = new int[vertCount];
        Color[] colorArray = new Color[vertCount];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                for (int z = 0; z < depth; z++)
                {
                    int idx = x + (y * 10 * cubeMultiplier) + (z * 10 * cubeMultiplier * 10 * cubeMultiplier);

                    if (fmap[x][z] >= y / (float)height)
                        map[idx] = 1;
                    else
                        map[idx] = 0;

                    Color color = Random.ColorHSV();
                    color.a = 0.1f;
                    colorArray[idx] = color;
                }
            }
        }

        mapBuffer.SetData(map);
        colorBuffer.SetData(colorArray);

        computeShader.SetBuffer(csKernel, "outputBuffer", outputBuffer);
        computeShader.SetBuffer(csKernel, "mapBuffer", mapBuffer);
        computeShader.SetBuffer(csKernel, "colorBuffer", colorBuffer);

        computeShader.SetVector("group_size", new Vector3(cubeMultiplier, cubeMultiplier, cubeMultiplier));

        if (debugRender)
            pointMaterial.SetBuffer("buf_Points", outputBuffer);

        transform.position -= (Vector3.one * 10 * cubeMultiplier) *.5f;
    }


    public void Dispatch()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogWarning("Compute shaders not supported (not using DX11?)");
            return;
        }

        computeShader.Dispatch(csKernel, cubeMultiplier, cubeMultiplier, cubeMultiplier);
    }

    void ReleaseBuffers()
    {
        outputBuffer.Release();
        mapBuffer.Release();
        colorBuffer.Release();
    }

    void Start()
    {
        csKernel = computeShader.FindKernel("CSMain");

        if (debugRender)
        {
            pointMaterial = new Material(pointShader);
            pointMaterial.SetVector("_worldPos", transform.position);
        }

        InitializeBuffers();
    }

    void OnRenderObject()
    {
        
        if (debugRender)
        {
            Dispatch();
            pointMaterial.SetPass(0);
            pointMaterial.SetVector("_worldPos", transform.position);

            Graphics.DrawProcedural(MeshTopology.Points, vertCount);
        }
    }

    private void OnDisable()
    {
        ReleaseBuffers();
    }
}