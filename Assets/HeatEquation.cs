using UnityEngine;
using System.Collections;
using System;

public class HeatEquation : MonoBehaviour {
    public const double TEMP_ROOM = 293.15; // Kelvin

    public GameObject xRayPlane;
    public GameObject heatWand;

    private VoxelEngine engine;
    private System.Random random = new System.Random();

    private double[,,] heat;                 // Kelvin

    // Double buffer
    private double[,,] heatB;                 // Kelvin

    private const int W = 20;
    private const int D = 20;
    private const int H = 20;

    private double voxelSize = 0.0001; // size of a voxel in meters
    private double tickDur = 0.1; // Duration of a tick in seconds
    private double heatAlpha = 0.0001; // Alpha in the heat equation

    // Use this for initialization
    void Start () {
        heat = new double[W, D, H];
        heatB = new double[W, D, H];

        for (int x = 0; x < W; x++) {
            for (int y = 0; y < D; y++) {
                for (int z = 0; z < H; z++) {
                    heat[x, y, z] = TEMP_ROOM + (2*random.NextDouble() - 1.5);
                    heatB[x, y, z] = TEMP_ROOM;
                }
            }
        }

        engine = new VoxelEngine(W, H, D, xRayPlane);
    }

    // Update is called once per frame
    void Update () {
        //TODO Do threading?  GPU?
        double r = heatAlpha * tickDur / voxelSize;
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < D; y++) {
                for (int z = 0; z < H; z++) {
                    double hc = heat[x, y, z];
                    heatB[x, y, z] = hc + (r * (heat[Math.Max(x - 1, 0), y, z] + heat[Math.Min(x + 1, W - 1), y, z] + heat[x, Math.Max(y - 1, 0), z] + heat[x, Math.Min(y + 1, D - 1), z] + heat[x, y, Math.Max(z - 1, 0)] + heat[x, y, Math.Min(z + 1, H - 1)] - (6 * hc)));
                }
            }
        }
        int[] heatSpot = engine.GetCubeCoords(heatWand.transform.position);
        if (heatSpot != null) {
            //Debug.Log("(" + heatSpot[0] + ", " + heatSpot[1] + ", " + heatSpot[2] + ")");
            heatB[heatSpot[0], heatSpot[1], heatSpot[2]] += 1;
        }
    
        // Swap out buffer
        double[,,] heatSwap = heat;

        heat = heatB;
    
        heatB = heatSwap;

        engine.DoUpdate((x, y, z, t) => {
            return VoxelEngine.RedBlueValue((float)(heat[x, y, z] - TEMP_ROOM), 0.0f, 0.01f);
        });
    }
}
