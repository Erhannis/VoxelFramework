using UnityEngine;
using System.Collections;
using System;

public class VoxelEngine : MonoBehaviour {
    public GameObject xRayPlane;

    public Shader geomShader;
    public VoxelComputeShaderOutput cso;

    Material material;
    public float size = 0.1f;


    private static Vector3 POS = new Vector3(-1, 1, -0.5f);
    //private static Vector3 POS = new Vector3(0, 0, 0f);
    private const float SCALE = 0.025f;
    private int xBlocksDim = 10;
    private int yBlocksDim = 10;
    private int zBlocksDim = 10;
    Color[] colorArray;
    private bool getData;

    //private float[,,] values;
    private long startTime;
    private float t = 0;

    public VoxelEngine(int xDim, int yDim, int zDim, GameObject xRayPlane, Shader geomShader, VoxelComputeShaderOutput cso) {
        this.xRayPlane = xRayPlane;
        this.geomShader = geomShader;
        this.cso = cso;
        Init(xDim, yDim, zDim);
    }

    // Use this for initialization
    void Start() {
        //Init(10, 10, 10);
    }

    public void Init(int xDim, int yDim, int zDim) {
        getData = true;
        this.xBlocksDim = xDim;
        this.yBlocksDim = yDim;
        this.zBlocksDim = zDim;

        colorArray = new Color[10 * 10 * 10 * xBlocksDim * yBlocksDim * zBlocksDim];

        material = new Material(geomShader);

        cso = GetComponent<VoxelComputeShaderOutput>();

        //TODO Init points?
    }

    // Update is called once per frame
    void Update() {
        //DoUpdate((x, y, z, t) => {
        //    float val = Mathf.Sin(-t + Mathf.Sqrt((x * x) + (y * y) + (z * z)));
        //    return RedBlueValue(val, 0.5f);
        //});
    }

    public static Color RedBlueValue(float val) {
        return RedBlueValue(val, 0.1f, 0.01f);
    }

    public static Color RedBlueValue(float val, float cutoff) {
        return RedBlueValue(val, cutoff, 0.01f);
    }

    public static Color RedBlueValue(float val, float cutoff, float brightness) {
        if (val >= 0) {
            float alpha = (brightness * ((val - cutoff) / (1f - cutoff)));
            return new Color(1, 0, 0, alpha);
        } else {
            float alpha = (brightness * ((-val - cutoff) / (1f - cutoff)));
            return new Color(0, 0, 1, alpha);
        }
    }

    public void DoUpdate(Func<int, int, int, float, Color> colorFunc) {
        t += 0.03f;
        Bounds xRayBounds = xRayPlane.GetComponent<Renderer>().bounds;
        MeshFilter mf = xRayPlane.GetComponent<MeshFilter>();
        Vector3 normal = mf.mesh.normals[0];
        normal = xRayPlane.transform.rotation * normal;
        // I'm going to assume the normal is a unit vector
        float offset = -Vector3.Dot(xRayPlane.transform.localToWorldMatrix.MultiplyPoint3x4(mf.mesh.vertices[0]), normal);
        //Debug.Log(normal);
        for (int x = 0; x < (10 * xBlocksDim); x++) {
            for (int y = 0; y < (10 * yBlocksDim); y++) {
                for (int z = 0; z < (10 * zBlocksDim); z++) {
                    int idx = x + (y * 10 * xBlocksDim) + (z * 10 * xBlocksDim * 10 * yBlocksDim);

                    Color color = colorFunc(x, y, z, t);

                    //TODO Fix alpha
                    //TODO Fix plane stuff
                    //if (IsPlaneCubeCollide(normal, offset, asdf)) {
                    //    color.a = 1;
                    //}
                    
                    /*
                    Bounds bounds = objArray[x, y, z].GetComponent<Renderer>().bounds;
                    int pos = GetPlaneCubeRelative(normal, offset, bounds);
                    if (pos > 0) {
                        color = new Color(1, 0, 0, 1);
                    } else if (pos < 0) {
                        color = new Color(0, 0, 1, 1);
                    } else {
                        color = new Color(0, 1, 0, 1);
                    }
                    */

                    if (color.a <= 0) {
                        //TODO Can I do the equivalent of SetActive(false) here?
                    } else {
                        //TODO or set color and active=true
                    }
                    colorArray[idx] = color;
                }
            }
        }
        cso.colorBuffer.SetData(colorArray);
    }

    public int[] GetCubeCoords(Vector3 point) {
        //TODO This could be constant time
        //TODO Ugh, do we have to do all this 10 * blocksDim?
        //TODO Fix
        /*
        for (int x = 0; x < (10 * xBlocksDim); x++) {
            for (int y = 0; y < (10 * yBlocksDim); y++) {
                for (int z = 0; z < (10 * zBlocksDim); z++) {
                    //TODO Also note this won't work right if the grid is angled
                    if (objArray[x, y, z].GetComponent<Renderer>().bounds.Contains(point)) {
                        return new int[]{x, y, z};
                    }
                }
            }
        }*/
        return null;
    }

    // From internet: http://www.gamedev.net/topic/646404-box-vs-plane-collision-detection/
    public static bool IsPlaneCubeCollide(Vector3 normal, float planeDistance, Vector3 cubeMin, Vector3 cubeMax) {
        Vector3 vec1, vec2;
        // I don't know why I needed to reverse these.  But it works now.
        Vector3 min = cubeMax;
        Vector3 max = cubeMin;
        if (normal.x >= 0) {
            vec1.x = min.x;
            vec2.x = max.x;
        } else {
            vec1.x = max.x;
            vec2.x = min.x;
        }
        if (normal.y >= 0) {
            vec1.y = min.y;
            vec2.y = max.y;
        } else {
            vec1.y = max.y;
            vec2.y = min.y;
        }
        if (normal.z >= 0) {
            vec1.z = min.z;
            vec2.z = max.z;
        } else {
            vec1.z = max.z;
            vec2.z = min.z;
        }
        float posSide = (normal.x * vec2.x) + (normal.y * vec2.y) + (normal.z * vec2.z) + planeDistance;
        if (posSide > 0) {
            //box is completely on positive side of plane
            return false;
        }
        float negSide = (normal.x * vec1.x) + (normal.y * vec1.y) + (normal.z * vec1.z) + planeDistance;
        if (negSide < 0) {
            //box is completely on negative side of plane
            return false;
        }
        //if you get this far, box is currently intersecting the plane.
        return true;
    }

    public static int GetPlaneCubeRelative(Vector3 normal, float planeDistance, Bounds cube) {
        Vector3 vec1, vec2;
        // I don't know why I needed to reverse these.  But it works now.
        Vector3 min = cube.max;
        Vector3 max = cube.min;
        if (normal.x >= 0) {
            vec1.x = min.x;
            vec2.x = max.x;
        } else {
            vec1.x = max.x;
            vec2.x = min.x;
        }
        if (normal.y >= 0) {
            vec1.y = min.y;
            vec2.y = max.y;
        } else {
            vec1.y = max.y;
            vec2.y = min.y;
        }
        if (normal.z >= 0) {
            vec1.z = min.z;
            vec2.z = max.z;
        } else {
            vec1.z = max.z;
            vec2.z = min.z;
        }
        float posSide = (normal.x * vec2.x) + (normal.y * vec2.y) + (normal.z * vec2.z) + planeDistance;
        if (posSide > 0) {
            //box is completely on positive side of plane
            return 1;
        }
        float negSide = (normal.x * vec1.x) + (normal.y * vec1.y) + (normal.z * vec1.z) + planeDistance;
        if (negSide < 0) {
            //box is completely on negative side of plane
            return -1;
        }
        //if you get this far, box is currently intersecting the plane.
        return 0;
    }

    public void OnRenderObject() {
        if (getData) {
            getData = false;
            cso.Dispatch();
        }
        /*
        for (int x = 0; x < (10 * xBlocksDim); x++) {
            for (int y = 0; y < (10 * yBlocksDim); y++) {
                for (int z = 0; z < (10 * zBlocksDim); z++) {
                    int idx = x + (y * 10 * xBlocksDim) + (z * 10 * xBlocksDim * 10 * yBlocksDim);

                    Color c = UnityEngine.Random.ColorHSV();
                    c.a = 0.1f;
                    c.r *= 0.01f;
                    c.g *= 0.01f;
                    c.b *= 0.01f;
                    colorArray[idx] = c;
                }
            }
        }
        cso.colorBuffer.SetData(colorArray);
        */


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
