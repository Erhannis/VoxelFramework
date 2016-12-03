using UnityEngine;
using System.Collections;
using System;

/**
 * Interesting.  I noticed the wave equation was almost exactly like the heat equation, but where the heat
 * equation had du/dt, the wave equation had d2u/dt2.  So I tried d3u/dt3.  Watching the results, I infer
 * the following.  The heat equation tracks position.  Its changes have no velocity or momentum.  The wave
 * equation tracks velocity.  Though, come to think of it, it's only directional velocity; everything has
 * a constant speed.  I think d3u/dt3, though, tracks acceleration.  A given value will not only tend to
 * continue increasing...oh.  These are position/velocity/acceleration with respect to value, not space.
 * Hmm.
 * Anyway, a given value here will tend not only to continue increasing, but continue increasing faster
 * and faster - until it gets dragged back down, at least.  (So it still displays that oscillating
 * behavior.)
 * ...
 * Now I want to display the derivative of these values.
 * 
 */
public class U3Equation : MonoBehaviour {
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
    private const int D = 25;

    //TODO THESE CONSTANTS ARE INAPPLICABLE
    private double voxelSize = 0.0001; // size of a voxel in meters
    private double tickDur = 0.00001; // Duration of a tick in seconds
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

        amp[7, 7, 7] = 0.001;
        amp[17, 17, 17] = -0.001;

        controllerTrackedObj = controllerObject.GetComponent<SteamVR_TrackedObject>();

        engine = GetComponent<VoxelEngine>();
        engine.Init(W, H, D);
    }

    private double modifier = -1;

    // Update is called once per frame
    void Update () {
        if (controller != null) {
            Vector2 v = controller.GetAxis(Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger);
            modifier = v.x * -0.01;
        }
        //TODO Do threading?  GPU?
        //TODO Not sure if these constants are technically used right - I think voxelSize
        //       should maybe be cubed, for instance.
        //double dt3c2 = heatAlpha * tickDur / voxelSize;
        double dt3c2 = 1e-5;
        double max = 0;
        Vector3 maxPos = new Vector3(-1, -1, -1);
        for (int x = 0; x < W; x++) {
            for (int y = 0; y < H; y++) {
                for (int z = 0; z < D; z++) {
                    double ac = amp[x, y, z];
                    double laplacian = (dt3c2 * (amp[Math.Max(x - 1, 0), y, z] + amp[Math.Min(x + 1, W - 1), y, z] + amp[x, Math.Max(y - 1, 0), z] + amp[x, Math.Min(y + 1, H - 1), z] + amp[x, y, Math.Max(z - 1, 0)] + amp[x, y, Math.Min(z + 1, D - 1)] - (6 * ac)));
                    ampFuture[x, y, z] = (3*ac) + (-3*amp1[x, y, z]) + (amp2[x, y, z]) + laplacian;
                    //ampFuture[x, y, z] = (2 * ac) - amp1[x, y, z] + laplacian; 
                    if (Math.Abs(ampFuture[x, y, z]) > max) {
                        max = ampFuture[x, y, z];
                        maxPos = new Vector3(x, y, z);
                    }
                }
            }
        }
        Debug.Log("max: " + max + " @ " + maxPos);
        int[] waveSpot = engine.GetCubeCoords(waveWand.transform.position);
        //Debug.Log(heatWand.transform.position + "(" + heatSpot[0] + ", " + heatSpot[1] + ", " + heatSpot[2] + ")");
        if (waveSpot != null) {
            ampFuture[waveSpot[0], waveSpot[1], waveSpot[2]] += modifier;
            Debug.Log("point: " + ampFuture[waveSpot[0], waveSpot[1], waveSpot[2]]);
        }

        // Swap out buffers
        double[,,] swap = amp2;
        amp2 = amp1;
        amp1 = amp;
        amp = ampFuture;
        ampFuture = swap;

        //TODO Add sliders etc
        engine.DoUpdate((x, y, z, t) => {
            return VoxelEngine.RedBlueValue((float)(amp[x, y, z]), 0.2f, 0.01f);
        });
    }
}
