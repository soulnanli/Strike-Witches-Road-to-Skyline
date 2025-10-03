using SW.Core;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    private CameraProjection _cameraProjection;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _cameraProjection = GetComponent<CameraProjection>();
    }

    // Update is called once per frame
    void Update()
    {
        _cameraProjection.DrowArea();
    }
}
