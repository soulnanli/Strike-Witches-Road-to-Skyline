using System;
using System.Collections.Generic;
using UnityEngine;

public class MapElement : MonoBehaviour
{
    public bool isConstScale = false;
    public float _originalHeight;
    public float targetPixelHeight = 100f;
    private void Start()
    {
        _originalHeight = transform.localScale.y;
    }
}


