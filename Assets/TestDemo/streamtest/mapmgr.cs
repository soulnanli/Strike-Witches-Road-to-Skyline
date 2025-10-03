using System;
using UnityEngine;
using SW.Core;
public class mapmgr : MonoBehaviour
{
    private QuadtreeNode _root;
    public float _minSize = 10f;
    public CameraProjection _cameraProjection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _root = new QuadtreeNode(
            center: new Vector3(0,0,0),
            size: new Vector3(1000, 0 ,1000)
            );
        _root.Segmentaion();
    }

    // Update is called once per frame
    void Update()
    {
        _root.Positioning(_cameraProjection.LeftForward, _minSize);
        _root.Positioning(_cameraProjection.RightForward, _minSize);
        _root.Positioning(_cameraProjection.LeftBack, _minSize);
        _root.Positioning(_cameraProjection.RightBack, _minSize);
        
    }

    private void OnDrawGizmos()
    {
        if(_root is not null)
            _root.DrowGizoms();
    }
}
