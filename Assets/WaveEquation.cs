using UnityEngine;
using System.Collections;

public class WaveEquation : MonoBehaviour {
    public GameObject xRayPlane;

    private VoxelEngine engine;

	// Use this for initialization
	void Start () {
        engine = new VoxelEngine(25, 25, 25, xRayPlane);
	}
	
	// Update is called once per frame
	void Update () {
        //TODO This is not the wave equation yet.
        engine.DoUpdate((x, y, z, t) => {
            float rx = (((float)x) - 12) / 4;
            float ry = (((float)y) - 12) / 4;
            float rz = (((float)z) - 12) / 4;
            float val = Mathf.Sin(-t + Mathf.Sqrt((rx * rx) + (ry * ry) + (rz * rz)));
            val = val * val * val;
            //if (ry != 0) {val = 0;}
            return VoxelEngine.RedBlueValue(val, 0.7f, 0.01f);
        });
    }
}
