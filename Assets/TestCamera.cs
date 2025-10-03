using System;
using UnityEngine;
using SW.Core;
public class TestCamera : MonoBehaviour
{
    public Rigidbody rb;
    public DTransform dtf;
    public Transform transform;

    private void Start()
    {
        dtf.position = new Double3((double)transform.position.x, (double)transform.position.y,
            (double)transform.position.z);
    }

    private void Update()
    {
        dtf.position = dtf.position + new Double3(1, 0, 0);
        
    }

    private void LateUpdate()
    {
        //transform.position = dtf.position.ToVector3();
    }
}