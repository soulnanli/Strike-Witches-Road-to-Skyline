using System;
using UnityEngine;
using SW.Core;
using Unity.Mathematics;

public class TestCube : MonoBehaviour
{
    public Rigidbody rb;
    public DTransform dtf;
    public DTransform cameraDtf;
    public Transform cameraTransform;
    public Transform transform;
    private void Start()
    {
        dtf.position = new Double3((double)transform.position.x, (double)transform.position.y,
            (double)transform.position.z);
    }
    Double3 d = new Double3();
    private void Update()
    {
        
        dtf.position = dtf.position + new Double3(1, 0, 0);
        
    }

    private void LateUpdate()
    {
     //   d = dtf.position - camaraDTf.position;
        cameraTransform.position = Vector3.zero;
        transform.position = (dtf.position - cameraDtf.position).ToVector3(); // + dtf.position.ToVector3();
    }

  
}
