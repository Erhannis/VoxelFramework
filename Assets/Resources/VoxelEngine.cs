using UnityEngine;
using System.Collections;
using System;

public class VoxelEngine : MonoBehaviour {
    public GameObject xRayPlane;

    private UnityEngine.Object voxelPrefab;
    private static Vector3 POS = new Vector3(-1, 1, -0.5f);
    private static Color TRANSPARENT = new Color(0, 0, 0, 0);
    private const float SCALE = 0.02f;
    private int xDim = 25;
    private int yDim = 25;
    private int zDim = 25;

    private float[,,] values;
    private GameObject[,,] objArray;
    private Material[,,] matArray;
    private int colorId;
    private GameObject voxelRoot;
    private long startTime;
    private float t = 0;

    public VoxelEngine(int xDim, int yDim, int zDim, GameObject xRayPlane) {
        this.xRayPlane = xRayPlane;
        Init(xDim, yDim, zDim);
    }

    // Use this for initialization
    void Start() {
        Init(25, 25, 25);
    }

    private void Init(int xDim, int yDim, int zDim) {
        voxelPrefab = Resources.Load("Voxel");
        this.xDim = xDim;
        this.yDim = yDim;
        this.zDim = zDim;

        voxelRoot = new GameObject();
        voxelRoot.transform.position = POS;
        voxelRoot.transform.localScale = new Vector3(SCALE, SCALE, SCALE);
        colorId = Shader.PropertyToID("_TintColor");
        objArray = new GameObject[xDim, yDim, zDim];
        matArray = new Material[xDim, yDim, zDim];
        //startTime = System.cu

        for (int x = 0; x < xDim; x++) {
            for (int y = 0; y < yDim; y++) {
                for (int z = 0; z < zDim; z++) {
                    GameObject voxel = MakeVoxel(new Vector3(x, y, z));
                    voxel.transform.SetParent(voxelRoot.transform, false);
                    objArray[x, y, z] = voxel;
                    matArray[x, y, z] = voxel.GetComponent<Renderer>().material;
                    matArray[x, y, z].SetColor(colorId, UnityEngine.Random.ColorHSV(0, 1, 0, 1, 0, 1, 0, 0.01f));
                }
            }
        }
    }

    private GameObject MakeVoxel(Vector3 pos) {
        GameObject voxel = (GameObject)Instantiate(voxelPrefab, pos, Quaternion.identity);
        return voxel;
    }

    // Update is called once per frame
    void Update() {
        DoUpdate((x, y, z, t) => {
            float val = Mathf.Sin(-t + Mathf.Sqrt((x * x) + (y * y) + (z * z)));
            return RedBlueValue(val, 0.5f);
        });
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
            //if (alpha <= 0) {return TRANSPARENT;}
            return new Color(1, 0, 0, alpha);
        } else {
            float alpha = (brightness * ((-val - cutoff) / (1f - cutoff)));
            //if (alpha <= 0) {return TRANSPARENT;}
            return new Color(0, 0, 1, alpha);
        }
    }

    public void DoUpdate(Func<int, int, int, float, Color> colorFunc) {
        t += 0.03f;
        Bounds xRayBounds = xRayPlane.GetComponent<Renderer>().bounds;
        MeshFilter mf = xRayPlane.GetComponent<MeshFilter>();
        Vector3 normal = mf.mesh.normals[0];
        normal = xRayPlane.transform.rotation * normal;
        Debug.Log(normal);
        for (int x = 0; x < xDim; x++) {
            for (int y = 0; y < yDim; y++) {
                for (int z = 0; z < zDim; z++) {
                    Color color = colorFunc(x, y, z, t);

                    //objArray[x, y, z].GetComponent<Renderer>().bounds.Intersects(xRayBounds)
                    if (IsPlaneCubeCollide(normal, , objArray[x, y, z].GetComponent<Renderer>().bounds)) {
                        color.a = 1;
                    }

                    if (color.a <= 0) {
                        objArray[x, y, z].SetActive(false);
                    } else {
                        matArray[x, y, z].SetColor(colorId, color);
                        objArray[x, y, z].SetActive(true);
                    }
                }
            }
        }
    }

    public static bool IsPlaneCubeCollide(Vector3 normal, float planeDistance, Bounds cube) {
        Vector3 vec1, vec2;
        Vector3 min = cube.min;
        Vector3 max = cube.max;
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
}
