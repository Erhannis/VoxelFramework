using UnityEngine;
using System.Collections;
using System;

public class VoxelEngine : MonoBehaviour {
    public GameObject xRayPlane;

    public Shader geomShader;
    public ComputeBuffer outputBuffer;
    public ComputeBuffer colorBuffer;

    Material material;

    //private static Vector3 POS = new Vector3(-1, 1, -0.5f);
    //private static Vector3 POS = new Vector3(0, 0, 0f);
    //private const float SCALE = 0.025f;
    private int xDim;
    private int yDim;
    private int zDim;
    Color[] colorArray;
    private bool getData;

    //private float[,,] values;
    private long startTime;
    private float t = 0;

    /*
    public VoxelEngine(int xDim, int yDim, int zDim, GameObject xRayPlane) {
        this.xRayPlane = xRayPlane;
        Init(xDim, yDim, zDim);
    }
    */

    // Use this for initialization
    void Start() {
        //Init(10, 10, 10);
    }

    public void Init(int xDim, int yDim, int zDim) {
        getData = true;
        this.xDim = xDim;
        this.yDim = yDim;
        this.zDim = zDim;

        int vertCount = xDim * yDim * zDim;
        outputBuffer = new ComputeBuffer(vertCount, (sizeof(float) * 3));
        colorBuffer = new ComputeBuffer(vertCount, (sizeof(float) * 4));

        Vector3[] points = new Vector3[xDim * yDim * zDim];
        
        int idx = 0;
        for (int z = 0; z < zDim; z++) {
            for (int y = 0; y < yDim; y++) {
                for (int x = 0; x < xDim; x++) {
                    //TODO Double check
                    points[idx++] = new Vector3(x, y, z);
                }
            }
        }
        /*
        for (int x = 0; x < xDim; x++) {
            for (int y = 0; y < yDim; y++) {
                for (int z = 0; z < zDim; z++) {
                    int idx = x + (y * xDim) + (z * xDim * yDim);
                    points[idx] = new Vector3(x, y, z);
                }
            }
        }
        */
        outputBuffer.SetData(points);
        points = null;

        colorArray = new Color[xDim * yDim * zDim];

        material = new Material(geomShader);
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
        //TODO Fix alpha rendering
        if (val >= 0) {
            float alpha = (brightness * ((val - cutoff) / (1f - cutoff)));
            return new Color(1, 0, 0, alpha);
            //return new Color(alpha, 0, 0, alpha);
        } else {
            float alpha = (brightness * ((-val - cutoff) / (1f - cutoff)));
            return new Color(0, 0, 1, alpha);
            //return new Color(0, 0, alpha, alpha);
        }
    }

    public void DoUpdate(Func<int, int, int, float, Color> colorFunc) {
        t += 0.03f;
        if (xRayPlane != null) {
            Bounds xRayBounds = xRayPlane.GetComponent<Renderer>().bounds;
            MeshFilter mf = xRayPlane.GetComponent<MeshFilter>();
            Vector3 normal = mf.mesh.normals[0];
            normal = xRayPlane.transform.rotation * normal;
            normal = Quaternion.Inverse(transform.rotation) * normal;
            // I'm going to assume the normal is a unit vector
            float offset = -Vector3.Dot(transform.worldToLocalMatrix.MultiplyPoint3x4(xRayPlane.transform.localToWorldMatrix.MultiplyPoint3x4(mf.mesh.vertices[0])), normal);
            //Debug.Log(normal);
            for (int x = 0; x < xDim; x++) {
                for (int y = 0; y < yDim; y++) {
                    for (int z = 0; z < zDim; z++) {
                        int idx = x + (y * xDim) + (z * xDim * yDim);

                        Color color = colorFunc(x, y, z, t);

                        //TODO Fix alpha
                        //TODO Ignore cutoff...?
                        if (IsPlaneCubeCollide(normal, offset, new Vector3(x - 0.5f, y - 0.5f, z - 0.5f), new Vector3(x + 0.5f, y + 0.5f, z + 0.5f))) {
                            if (color.r > 0) {
                                color.r = 1;
                            }
                            if (color.g > 0) {
                                color.g = 1;
                            }
                            if (color.b > 0) {
                                color.b = 1;
                            }
                            color.a = 1;
                        }

                        if (color.a <= 0) {
                            //TODO Can I do the equivalent of SetActive(false) here?
                        } else {
                            //TODO or set color and active=true
                        }
                        colorArray[idx] = color;
                    }
                }
            }
        } else {
            for (int x = 0; x < xDim; x++) {
                for (int y = 0; y < yDim; y++) {
                    for (int z = 0; z < zDim; z++) {
                        int idx = x + (y * xDim) + (z * xDim * yDim);

                        Color color = colorFunc(x, y, z, t);

                        if (color.a <= 0) {
                            //TODO Can I do the equivalent of SetActive(false) here?
                        } else {
                            //TODO or set color and active=true
                        }
                        colorArray[idx] = color;
                    }
                }
            }
        }
        colorBuffer.SetData(colorArray);
    }

    public int[] GetCubeCoords(Vector3 point) {
        return GetCubeCoords(point, true);
    }

    public int[] GetCubeCoords(Vector3 point, bool nullOutsideRange) {
        Vector3 localPoint = transform.worldToLocalMatrix.MultiplyPoint3x4(point);
        localPoint = localPoint + new Vector3(0.5f, 0.5f, 0.5f);
        int[] result = new int[]{Mathf.FloorToInt(localPoint.x), Mathf.FloorToInt(localPoint.y), Mathf.FloorToInt(localPoint.z)};
        if (result[0] < 0 || result[0] >= xDim || result[1] < 0 || result[1] >= yDim || result[2] < 0 || result[2] >= zDim) {
            return null;
        } else {
            return result;
        }
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
        material.SetPass(0);
        material.SetBuffer("buf_Points", outputBuffer);
        material.SetBuffer("buf_Colors", colorBuffer);

        material.SetMatrix("world", transform.localToWorldMatrix);

        Graphics.DrawProcedural(MeshTopology.Points, outputBuffer.count);
    }

    void ReleaseBuffers() {
        outputBuffer.Release();
        colorBuffer.Release();
    }

    private void OnDisable() {
        ReleaseBuffers();
    }
}
