using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

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
    private Mesh originalMesh;
    private Color[] heightMapData;
    private int heightMapWidth;
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    NativeArray<Vector3> jobPlanesNormal;
    NativeArray<float> jobPlanesDistance;
    NativeArray<Vector3> jobBoundsMaxPoint;
    NativeArray<Vector3> jobBoundsMinPoint;
    NativeArray<bool> result;
    private GameObject[] nodeObjArray;
    private bool needInitialGenerate = true;
    public static mapmgr Instance { get; private set; }

    void Start()
    {
        _camera = Camera.main;
        originalMesh = Resources.Load<Mesh>("plane");
        heightMapData = heightMap.GetPixels();
        heightMapWidth = heightMap.width;
        Instance = this;
        _root = new QuadtreeNode(
            center: new Vector3(0,0,0),
            size: new Vector3(mapSize, 0 ,mapSize),
            lodLevel: 6
            );
        jobPlanesNormal = new NativeArray<Vector3>(6, Allocator.Persistent);
        jobPlanesDistance = new NativeArray<float>(6, Allocator.Persistent);
        int len = 4096 * 64;
        if (!jobBoundsMaxPoint.IsCreated) jobBoundsMaxPoint = new NativeArray<Vector3>(len, Allocator.Persistent);
        if (!jobBoundsMinPoint.IsCreated) jobBoundsMinPoint = new NativeArray<Vector3>(len, Allocator.Persistent);
        if (!result.IsCreated) result = new NativeArray<bool>(len, Allocator.Persistent);
        nodeObjArray =  new GameObject[4096 * 64];

        finalNodeList.Clear();
        _root.CaculateLodNode(finalNodeList);
        cameraPosBuffer = _camera.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        if (needInitialGenerate)
        {
            foreach (var node in finalNodeList)
            {
                GenerateMeshObj(node);
            }

            needInitialGenerate = false;
            return;
        }

        if (Vector3.Distance(_camera.transform.position, cameraPosBuffer) > cameraMoveLimit)
        {
            cameraPosBuffer = _camera.transform.position;
            UploadLodNode();
        }

        FrustumCulling();
    }

    private List<QuadtreeNode> nextList = new ();
    private readonly HashSet<QuadtreeNode> finalNodeSet = new ();
    private readonly HashSet<QuadtreeNode> nextNodeSet = new ();

    void UploadLodNode()
    {
        nextList.Clear();
        _root.CaculateLodNode(nextList);

        finalNodeSet.Clear();
        nextNodeSet.Clear();
        foreach (var node in finalNodeList)
        {
            finalNodeSet.Add(node);
        }

        foreach (var node in nextList)
        {
            nextNodeSet.Add(node);
        }

        foreach (var node in finalNodeList)
        {
            if (!nextNodeSet.Contains(node))
            {
                RecycleMeshObj(node);
            }
        }

        foreach (var node in nextList)
        {
            if (finalNodeSet.Contains(node))
            {
                node.needReGen = false;
                continue;
            }

            GenerateMeshObj(node);
        }

        List<QuadtreeNode> oldList = finalNodeList;
        finalNodeList = nextList;
        nextList = oldList;
    }

    private void RecycleMeshObj(QuadtreeNode node)
    {
        foreach (var go in node.meshObjDict.Values)
        {
            go.SetActive(false);
            meshPool.TryEnqueue(1, go);
        }

        node.meshObjDict.Clear();
        node.needReGen = true;
    }

    private void OnDestroy()
    {
        if (jobPlanesNormal.IsCreated) jobPlanesNormal.Dispose();
        if (jobPlanesDistance.IsCreated) jobPlanesDistance.Dispose();
        if (jobBoundsMaxPoint.IsCreated) jobBoundsMaxPoint.Dispose();
        if (jobBoundsMinPoint.IsCreated) jobBoundsMinPoint.Dispose();
        if (result.IsCreated) result.Dispose();
    }

    bool isCameraMoved = true;


    public void FrustumCulling() 
    {
        if (!isCameraMoved) return;

        TestPlanesAABBJob job = new TestPlanesAABBJob();
        int index = -1;
        var planes = GeometryUtility.CalculateFrustumPlanes(_camera);
        for (int i = 0; i < 6; i++)
        {
            jobPlanesNormal[i] = planes[i].normal;
            jobPlanesDistance[i] = planes[i].distance;
        }

        foreach (var node in finalNodeList)
        {
            foreach (var m in node.meshObjDict)
            {
                ++index;
                var bounds = m.Value.GetComponent<MeshRenderer>().bounds;
                jobBoundsMaxPoint[index] = bounds.max;
                jobBoundsMinPoint[index] = bounds.min;
                nodeObjArray[index] = m.Value;
            }
        }

        job.jobPlanesNormal = jobPlanesNormal;
        job.jobBoundsMaxPoint = jobBoundsMaxPoint;
        job.jobBoundsMinPoint = jobBoundsMinPoint;
        job.jobPlanesDistance = jobPlanesDistance;
        job.result = result;

        int len = index + 1;
        patchNumber = len;
        if (len == 0) return;

        JobHandle handle = job.Schedule(len, 64);
        handle.Complete();

        for (int i = 0; i < len; i++)
        {
            if (result[i])
            {
                nodeObjArray[i].SetActive(true);
            }
            else
            {
                nodeObjArray[i].SetActive(false);
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

    public void GenerateMeshObj(QuadtreeNode node)
    {
        for (int i = 0; i < 8; i++)
        {
            for(int j = 0; j < 8; j++)
            {
                GameObject go = meshPool.TryDequeue(1);
                MeshFilter meshFilter = go.GetComponent<MeshFilter>();
                Mesh m = meshFilter.sharedMesh;
                if (m is null)
                {
                    m = Instantiate(originalMesh);
                    meshFilter.sharedMesh = m;
                }

                Vector3 v;
                var scale = Math.Pow(2, node.lodLevel);
                Vector3 pos = new Vector3(node.center.x + (int)scale * ( - 32 + 4) + j *  (int)scale * 8, 0f,
                    node.center.z + (int)scale * ( - 32 + 4) + i *  (int)scale * 8 );
                (m,v) = Utils.heightMap2Mesh(m,heightMapData,heightMapWidth,(int)scale,node.size.x,node.center, mapSize, heightScale,i,j, pos);

                node.meshObjDict[m] = go;
                go.GetComponent<MeshRenderer>().sharedMaterial = meshMaterial;
                go.GetComponent<NodeDescriptor>().lodLevel = node.lodLevel;
                go.GetComponent<NodeDescriptor>().offset = v;

                go.transform.position = pos;
                go.transform.localScale = new Vector3((float)scale, 1, (float)scale);
                go.SetActive(true);
            }
        }

        node.needReGen = false;
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
    
    [BurstCompile]
    public struct TestPlanesAABBJob : IJobParallelFor
    {
        [ReadOnly] public NativeArray<Vector3> jobPlanesNormal;
        [ReadOnly] public NativeArray<float> jobPlanesDistance;
        [ReadOnly] public NativeArray<Vector3> jobBoundsMaxPoint;
        [ReadOnly] public NativeArray<Vector3> jobBoundsMinPoint;
        public NativeArray<bool> result;
        public void Execute(int index)
        {
            bool f = true;
            for(int i = 0; i < 6; i++)
            {
                Vector3 normal = jobPlanesNormal[i];
                Vector3 p = jobBoundsMinPoint[index];
                Vector3 maxPos = jobBoundsMaxPoint[index];
                if (normal.x >= 0)
                    p.x = maxPos.x;
                if (normal.y >= 0)
                    p.y = maxPos.y;
                if (normal.z >= 0)
                    p.z = maxPos.z;
                if (Vector3.Dot(normal, p) + jobPlanesDistance[i] < 0)
                {
                    f = false;
                }
            }
            //Debug.Log("job Complete" + index);
            result[index] = f;
        }
    }
}
