using System;
using System.Collections.Generic;
using UnityEngine;
using SW.Core;
using Unity.VisualScripting;
using UnityEngine.TextCore.Text;

public class mapmgr : MonoBehaviour
{
    private QuadtreeNode _root;
    public float minSize;
    public float mapSize;
    public float lodJudgeSector;
    public float cameraFov;
    public float heightScale;
    
    public Mesh mesh;
    public Material meshMaterial;
    public Texture2D heightMap;
    
    public List<QuadtreeNode> finalNodeList = new List<QuadtreeNode>();
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
    }

    // Update is called once per frame
    void Update()
    {
        if (lodComplete)
        {
            lodComplete = false;
            Debug.Log("Lod Complete");
            foreach (var node in finalNodeList)
            {
                Mesh m = Utils.heightMap2Mesh(heightMap,node.lodLevel,node.size.x,node.center, mapSize, heightScale);
                GameObject go = new GameObject();
                go.AddComponent<MeshFilter>().mesh = m;
                go.AddComponent<MeshRenderer>().material = meshMaterial;
                go.AddComponent<NodeDescriptor>().lodLevel = node.lodLevel;
                go.transform.position = node.center;
                var scale = Math.Pow(2, node.lodLevel);
                go.transform.localScale = new Vector3((float)scale, 1, (float)scale);
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
    }
}
