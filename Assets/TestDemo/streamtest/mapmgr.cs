using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Core;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEngine.TextCore.Text;

public class mapmgr : MonoBehaviour
{
    private QuadtreeNode _root;
    public float minSize;
    public float mapSize;
    public float lodJudgeSector;
    public float cameraFov;
    public float heightScale;
    public float cameraMoveLimit;
    public Vector3 cameraPosBuffer;

    [Header("patch number")]
    public int patchNumber;
    
    public Mesh mesh;
    public Material meshMaterial;
    public Texture2D heightMap;
    
    public List<QuadtreeNode> finalNodeList = new List<QuadtreeNode>();
    public List<GameObject> meshObjList = new List<GameObject>();
    public MeshObjPool meshPool = new ();
    public CameraProjection _cameraProjection;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    bool lodComplete = false;
    public static mapmgr Instance { get; private set; }

    void Start()
    {
        Instance = this;
        _root = new QuadtreeNode(
            center: new Vector3(0,0,0),
            size: new Vector3(mapSize, 0 ,mapSize),
            lodLevel: 6
            );
        // _root.Segmentaion();
        lodComplete = _root.CaculateLodNode();
        cameraPosBuffer = Camera.main.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        patchNumber = meshObjList.Count;
        if (lodComplete)
        {
            lodComplete = false;
            Debug.Log("Lod Complete");
            GenerateMeshObj();
        }

        if (Vector3.Distance(Camera.main.transform.position, cameraPosBuffer) > cameraMoveLimit)
        {
            cameraPosBuffer = Camera.main.transform.position;
            for (int i = meshObjList.Count - 1; i >= 0; i--)
            {
                if (meshObjList[i] != null)
                {
                    meshObjList[i].SetActive(false);
                    meshPool.TryEnqueue(1,meshObjList[i]);
                }
            }
            meshObjList.Clear(); // 清空列表
            finalNodeList.Clear();
            lodComplete = _root.CaculateLodNode();
        }

        foreach (var o in meshObjList)
        {
            Mesh mesh = o.GetComponent<MeshFilter>().mesh;
            var planes = GeometryUtility.CalculateFrustumPlanes(Camera.main);
            Bounds bounds = mesh.bounds;
            bounds.center = o.gameObject.transform.position;
            bounds.Expand(25f);
            bool b = GeometryUtility.TestPlanesAABB(planes, bounds);

            if (!b)
            {
                o.SetActive(false);
            }
            else
            {
                o.SetActive(true);
            }
        }
    }

    public void GenerateMeshObj()
    {
        foreach (var node in finalNodeList)
        {
            for (int i = 0; i < 8; i++)
            {
                for(int j = 0; j < 8; j++)
                {
                    
                    Mesh m;
                    Vector3 v;
                    var scale = Math.Pow(2, node.lodLevel);
                    Vector3 pos = new Vector3(node.center.x + (int)scale * ( - 32 + 4) + j *  (int)scale * 8, 0f,
                        node.center.z + (int)scale * ( - 32 + 4) + i *  (int)scale * 8 );
                    (m,v) = Utils.heightMap2Mesh(heightMap,(int)scale,node.size.x,node.center, mapSize, heightScale,i,j, pos);

                    if (m is null)
                    {
                        continue;
                    }
                    GameObject go = meshPool.TryDequeue(1);
                    meshObjList.Add(go);
                    go.SetActive(true);
                    go.GetComponent<MeshFilter>().mesh = m;
                    go.GetComponent<MeshRenderer>().material = meshMaterial;
                    go.GetComponent<NodeDescriptor>().lodLevel = node.lodLevel;
                    go.GetComponent<NodeDescriptor>().offset = v;
                    
                    go.transform.position = pos;
                    go.transform.localScale = new Vector3((float)scale, 1, (float)scale);
                }
            }

        }
    }

    private void OnDrawGizmos()
    {
        if(_root is not null)
            _root.DrowGizoms();
    }

    public class NodeDescriptor : MonoBehaviour
    {
        public int lodLevel;
        public Vector3 offset;
    }
    
    public class MeshObjPool : ObjPool<GameObject>
    {
        private Dictionary<int, int> objCount = new();
        public override GameObject TryDequeue(int id)
        {
            var q = AcessQueue(id);
            if (q.Count > 0)
            {
                var ee = q.Dequeue();
                return ee;
            }

            objCount.TryAdd(id, 0);
            objCount[id]++;
            var go = new GameObject();
            go.AddComponent<MeshFilter>();
            go.AddComponent<MeshRenderer>();
            go.AddComponent<NodeDescriptor>();
            return go;
        }
    }
}
