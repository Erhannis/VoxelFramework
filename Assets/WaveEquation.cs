using UnityEngine;
using System.Collections;
using System;

public class WaveEquation : MonoBehaviour {
    public GameObject xRayPlane;
    public GameObject waveWand;

    private Valve.VR.EVRButtonId gripButton = Valve.VR.EVRButtonId.k_EButton_Grip;
    private Valve.VR.EVRButtonId triggerButton = Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
    private SteamVR_Controller.Device controller { get { return ((int)controllerTrackedObj.index) >= 0 ? SteamVR_Controller.Input((int)controllerTrackedObj.index) : null; } }
    private SteamVR_TrackedObject controllerTrackedObj;
    public GameObject controllerObject;

    private VoxelEngine engine;
    private System.Random random = new System.Random();

    private double[,,] ampFuture;
    private double[,,] amp;     // This is the canonical state
    private double[,,] amp1;
    private double[,,] amp2;

    private const int W = 25;
    private const int H = 25;
    private const int D = 100;

    //TODO THESE CONSTANTS ARE INAPPLICABLE
    private double voxelSize = 0.01; // size of a voxel in meters
    private double tickDur = 0.4; // Duration of a tick in seconds
    private double heatAlpha = 0.00001; // Alpha in the heat equation

    // Use this for initialization
    void Start () {
        ampFuture = new double[W, H, D];
        amp = new double[W, H, D];
        amp1 = new double[W, H, D];
        amp2 = new double[W, H, D];

        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    amp[x, y, z] = 0;
                    amp1[x, y, z] = 0;
                    amp2[x, y, z] = 0;
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
            //modifier = (v.x * 2) - 1;
            modifier = v.x;
        }
        //oneSidedDiff();
        centralDiff();
        int[] waveSpot = engine.GetCubeCoords(waveWand.transform.position);
        //Debug.Log(heatWand.transform.position + "(" + heatSpot[0] + ", " + heatSpot[1] + ", " + heatSpot[2] + ")");
        if (waveSpot != null) {
            ampFuture[waveSpot[0], waveSpot[1], waveSpot[2]] += modifier;
        }

        // Swap out buffers
        double[,,] swap = amp2;
        amp2 = amp1;
        amp1 = amp;
        amp = ampFuture;
        ampFuture = swap;

        //TODO Add sliders etc
        engine.DoUpdate((x, y, z, t) => {
            /*
            double dx = 0.1 * ((amp[Math.Min(x + 1, W - 1), y, z] - amp[Math.Max(x - 1, 0), y, z]) / (2 * voxelSize));
            double dy = 0.1 * ((amp[x, Math.Min(y + 1, H - 1), z] - amp[x, Math.Max(y - 1, 0), z]) / (2 * voxelSize));
            double dz = 0.1 * ((amp[x, y, Math.Min(z + 1, D - 1)] - amp[x, y, Math.Max(z - 1, 0)]) / (2 * voxelSize));
            return new Color((float)dx, (float)dy, (float)dz, 1);
            */
            return VoxelEngine.RedBlueValue((float)(amp[x, y, z]), 0.0f, 0.01f);
        });
    }

    private void oneSidedDiff() {
        //TODO Do threading?  GPU?
        //TODO Not sure if these constants are technically used right - I think voxelSize
        //       should maybe be squared, for instance.
        double dt2c2 = heatAlpha * (tickDur * tickDur) / (voxelSize * voxelSize);
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    double ac = amp[x, y, z];
                    double laplacian = (amp[Math.Max(x - 1, 0), y, z]
                                      + amp[Math.Min(x + 1, W - 1), y, z]
                                      + amp[x, Math.Max(y - 1, 0), z]
                                      + amp[x, Math.Min(y + 1, H - 1), z]
                                      + amp[x, y, Math.Max(z - 1, 0)]
                                      + amp[x, y, Math.Min(z + 1, D - 1)]
                                      - (6 * ac));
                    //TODO *2h^2
                    ampFuture[x, y, z] = (2 * ac) - amp1[x, y, z] + (dt2c2 * laplacian);
                }
            }
        }
    }

    private void centralDiff() {
        //TODO Do threading?  GPU?
        //TODO Not sure if these constants are technically used right - I think voxelSize
        //       should maybe be squared, for instance.
        double dt2c2 = heatAlpha * (2 * tickDur * tickDur) / (2 * voxelSize * voxelSize);
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    //TODO Went with what looked most accurate
                    //TODO I've...misplaced the /2h
                    double ac = amp[x, y, z];
                    double laplacian = (amp[Math.Max(x - 1, 0), y, z]
                                      + amp[Math.Min(x + 1, W - 1), y, z]
                                      + amp[x, Math.Max(y - 1, 0), z]
                                      + amp[x, Math.Min(y + 1, H - 1), z]
                                      + amp[x, y, Math.Max(z - 1, 0)]
                                      + amp[x, y, Math.Min(z + 1, D - 1)]
                                      - (6 * ac));
                    /*
                    double laplacian = (0
                                      + amp[Math.Max(x - 2, 0), y, z]
                                      - amp[Math.Max(x - 1, 0), y, z]
                                      - amp[Math.Min(x + 1, W - 1), y, z]
                                      + amp[Math.Min(x + 2, W - 1), y, z]
                                      + amp[x, Math.Max(y - 2, 0), z]
                                      - amp[x, Math.Max(y - 1, 0), z]
                                      - amp[x, Math.Min(y + 1, H - 1), z]
                                      + amp[x, Math.Min(y + 2, H - 1), z]
                                      + amp[x, y, Math.Max(z - 2, 0)]
                                      - amp[x, y, Math.Max(z - 1, 0)]
                                      - amp[x, y, Math.Min(z + 1, D - 1)]
                                      + amp[x, y, Math.Min(z + 2, D - 1)]
                                      );
                    */
                    ampFuture[x, y, z] = amp[x, y, z] + amp1[x, y, z] - amp2[x, y, z] + (dt2c2 * laplacian);
                }
            }
        }
    }
}
