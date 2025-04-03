using System.Collections.Generic;
using UnityEngine;

public class MapElementLayer : MonoBehaviour
{
    public List<MapElement> elements;
    private void Start()
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            elements.Add(transform.GetChild(i).GetComponent<MapElement>());
        }
    }
}