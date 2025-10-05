using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Core;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
using UnityEditor.Timeline;
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
    
    public Material meshMaterial;
    public Texture2D heightMap;
    public Texture2D minMaxHeightMap;
    
    public List<QuadtreeNode> finalNodeList = new List<QuadtreeNode>();
    public MeshObjPool meshPool = new ();
    public CameraProjection _cameraProjection;

    private Camera _camera;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    bool lodComplete = false;
    public static mapmgr Instance { get; private set; }

    void Start()
    {
        _camera = Camera.main;
        Instance = this;
        _root = new QuadtreeNode(
            center: new Vector3(0,0,0),
            size: new Vector3(mapSize, 0 ,mapSize),
            lodLevel: 6
            );
        // _root.Segmentaion();
        lodComplete = _root.CaculateLodNode();
        cameraPosBuffer = _camera.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        //patchNumber = meshObjDict.Count;
        if (lodComplete)
        {
            lodComplete = false;
            Debug.Log("Lod Complete");
            GenerateMeshObj();
        }

        if (Vector3.Distance(_camera.transform.position, cameraPosBuffer) > cameraMoveLimit)
        {
            cameraPosBuffer = _camera.transform.position;
            foreach (var node in finalNodeList)
            {
                foreach (var o in node.meshObjDict.Values)
                {
                    o.SetActive(false);
                    meshPool.TryEnqueue(1,o);
                }
                node.meshObjDict.Clear();
            }
            finalNodeList.Clear();
            lodComplete = _root.CaculateLodNode();
        }

        FrustumCulling();
    }

    bool isCameraMoved = true;
    public void FrustumCulling()
    {
        if (!isCameraMoved) return;
        
        var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        foreach (var node in finalNodeList)
        {
            //先对node进行剔除
            bool b = GeometryUtility.TestPlanesAABB(planes, GetNodeBounds(node));
            if (!b)
            {
                foreach (var o in node.meshObjDict.Values)
                {
                    o.SetActive(false);
                    meshPool.TryEnqueue(1,o);
                }
                continue;
            }
            else
            {
                foreach (var m in node.meshObjDict)
                {
                    var bounds = m.Key.bounds;
                    bounds.center = m.Value.transform.position + m.Key.bounds.center;
                    b = GeometryUtility.TestPlanesAABB(planes, bounds);
                
                    if (!b)
                    {
                        m.Value.SetActive(false);
                    }
                    else
                    {
                        m.Value.SetActive(true);
                    }
                }
            }
        }
    }

    public Bounds GetNodeBounds(QuadtreeNode node)
    {
        int mipLevel = node.lodLevel + 6;
        int mipWidth = Mathf.Max(1, minMaxHeightMap.width >> mipLevel);
        int mipHeight = Mathf.Max(1, minMaxHeightMap.height >> mipLevel);
        
        float u = (node.center.x / mapSize + 0.5f);
        float v = (node.center.y / mapSize + 0.5f);
        
        int x = Mathf.Clamp(Mathf.FloorToInt(u * mipWidth), 0, mipWidth - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt(v * mipHeight), 0, mipHeight - 1);
        
        Color c = minMaxHeightMap.GetPixel(x, y, mipLevel);
        float minH = c.r * heightScale;
        float maxH = c.g * heightScale;
        
        Vector3 center = new Vector3(node.center.x, (minH + maxH) * 0.5f, node.center.z);
        Vector3 size = new Vector3(node.size.x, maxH - minH, node.size.z);

        return new Bounds(center, size);
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
                    node.meshObjDict[m] = go;
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
