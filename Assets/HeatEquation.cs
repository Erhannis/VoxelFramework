using UnityEngine;
using System.Collections;
using System;

public class HeatEquation : MonoBehaviour {
    public const double TEMP_ROOM = 293.15; // Kelvin

    public GameObject xRayPlane;
    public GameObject heatWand;

    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private SteamVR_Controller.Device controller { get { return ((int)controllerTrackedObj.index) >= 0 ? SteamVR_Controller.Input((int)controllerTrackedObj.index) : null; } }
    private SteamVR_TrackedObject controllerTrackedObj;
    public GameObject controllerObject;

    private VoxelEngine engine;
    private System.Random random = new System.Random();

    private double[,,] heat;                 // Kelvin

    // Double buffer
    private double[,,] heatB;                 // Kelvin

    private const int W = 25;
    private const int H = 25;
    private const int D = 25;

    //TODO Relate to actual scale?
    private double voxelSize = 0.0001; // size of a voxel in meters
    private double tickDur = 0.1; // Duration of a tick in seconds
    private double heatAlpha = 0.00001; // Alpha in the heat equation

    // Use this for initialization
    void Start () {
        heat = new double[W, H, D];
        heatB = new double[W, H, D];

        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    heat[x, y, z] = TEMP_ROOM + (2*random.NextDouble() - 1.5);
                    heatB[x, y, z] = TEMP_ROOM;
                }
            }
        }

        controllerTrackedObj = controllerObject.GetComponent<SteamVR_TrackedObject>();

        engine = GetComponent<VoxelEngine>();
        engine.Init(W, H, D);
    }

    private double modifier = -1;

    // Update is called once per frame
    void Update () {
        if (controller != null) {
            Vector2 v = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            modifier = (v.x * 2) - 1;
        }
        //TODO Do threading?  GPU?
        //TODO Not sure if these constants are technically used right - I think voxelSize
        //       should maybe be squared, for instance.
        double r = heatAlpha * tickDur / voxelSize;
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    double hc = heat[x, y, z];
                    double laplacian = (r * (heat[Math.Max(x - 1, 0), y, z] + heat[Math.Min(x + 1, W - 1), y, z] + heat[x, Math.Max(y - 1, 0), z] + heat[x, Math.Min(y + 1, H - 1), z] + heat[x, y, Math.Max(z - 1, 0)] + heat[x, y, Math.Min(z + 1, D - 1)] - (6 * hc)));
                    heatB[x, y, z] = hc + laplacian;
                }
            }
        }
        int[] heatSpot = engine.GetCubeCoords(heatWand.transform.position);
        //Debug.Log(heatWand.transform.position + "(" + heatSpot[0] + ", " + heatSpot[1] + ", " + heatSpot[2] + ")");
        if (heatSpot != null) {
            heatB[heatSpot[0], heatSpot[1], heatSpot[2]] += modifier;
        }
    
        // Swap out buffer
        double[,,] heatSwap = heat;
        heat = heatB;
        heatB = heatSwap;

        //TODO Add sliders etc
        engine.DoUpdate((x, y, z, t) => {
            return VoxelEngine.RedBlueValue((float)(heat[x, y, z] - TEMP_ROOM), 0.2f, 0.01f);
        });
    }
}
