using System;
using UnityEngine;

public class CamaraEdgeScroll : MonoBehaviour
{
    public RectTransform observeWindow;
    public SpriteRenderer referenceBound;

    [Header("Observe Parameters")] 
    public Rect rfRect;
    public Rect obRect;

    public float zoomUpperBound;
    public float zoomLowerBound;
    public float zoomSpeed = 1f;

    public float scrollAreaSize = 5;
    public float scrollSpeed = 5;

    public Vector2 dragStartMousePosition;
    public Vector2 dragStartCameraPosition;

    private Camera c;
    private Vector2 d;

    private bool _isDragging = false;
    private bool _turnOnEdgeScroll = true;
    private void Start()
    {
        c = GetComponent<Camera>();
        UpdateParameter();
    }

    private void Update()
    {
        
        HandleDarg();
        if (!_isDragging)
        {
            HandleZoom();
            if (_turnOnEdgeScroll)
            {
                //HandleEdgeScroll();
            }
        }
        Adjust();
    }

    private void HandleEdgeScroll()
    {
        Vector2 mousePos = Input.mousePosition;
        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        if (mousePos.x < scrollAreaSize)
        {
            transform.Translate(scrollSpeed * Time.deltaTime * Vector3.left);
        }
        
        if (mousePos.y < scrollAreaSize)
        {
            transform.Translate(scrollSpeed * Time.deltaTime * Vector3.down);
        }
        
        if (mousePos.x > screenWidth - scrollAreaSize)
        {
            transform.Translate(scrollSpeed * Time.deltaTime * Vector3.right);
        }
        
        if (mousePos.y > screenHeight - scrollAreaSize)
        {
            transform.Translate(scrollSpeed * Time.deltaTime * Vector3.up);
        }
    }
    void UpdateParameter()
    {
        rfRect = Utils.GetWorldRect(referenceBound);
        obRect = Utils.GetWorldRect(observeWindow);

        zoomUpperBound = c.orthographicSize * Mathf.Min(rfRect.width / obRect.width, rfRect.height/obRect.height) * 0.98f;
        zoomLowerBound = zoomUpperBound / 2;

        scrollSpeed = obRect.width * 0.2f;
        zoomSpeed = zoomUpperBound * 0.05f;
    }

    void HandleDarg()
    {
        if (Input.GetMouseButtonDown(2))
        {
            _isDragging = true;
            dragStartMousePosition = Input.mousePosition;
            dragStartCameraPosition = transform.position;
        }

        if (Input.GetMouseButtonUp(2))
        {
            _isDragging = false;
        }

        if (_isDragging)
        {
            Vector2 dragCurrentMousePosition = Input.mousePosition;
            Vector2 distanceInWorldSpace = c.ScreenToWorldPoint(dragStartMousePosition)
                                           - c.ScreenToWorldPoint(dragCurrentMousePosition);
            Vector2 newPos = dragStartCameraPosition + distanceInWorldSpace;
            transform.position = new Vector3(newPos.x, newPos.y, transform.position.z);
        }
    }

    void HandleZoom()
    {
        float size = c.orthographicSize;
        //Zoom out
        if (Input.mouseScrollDelta.y < 0 && size < zoomUpperBound)
        {
            Vector2 screenP0 = Input.mousePosition;
            Vector2 worldP0 = c.ScreenToWorldPoint(screenP0);
            c.orthographicSize += zoomSpeed;
            c.orthographicSize = c.orthographicSize > zoomUpperBound ? zoomUpperBound : c.orthographicSize;
            Vector2 worldP1 = c.ScreenToWorldPoint(screenP0);
            transform.Translate(worldP0 - worldP1);
        }

        //Zoom in
        if (Input.mouseScrollDelta.y > 0 && size > zoomLowerBound)
        {
            Vector2 screenP0 = Input.mousePosition;
            Vector2 worldP0 = c.ScreenToWorldPoint(screenP0);
            c.orthographicSize -= zoomSpeed;
            c.orthographicSize = c.orthographicSize < zoomLowerBound ? zoomLowerBound : c.orthographicSize;
            Vector2 worldP1 = c.ScreenToWorldPoint(screenP0);
            transform.Translate(worldP0 - worldP1);

        }
    }

    void Adjust()
    {
        obRect = Utils.GetWorldRect(observeWindow);
        float dx = Mathf.Max(0, rfRect.xMin - obRect.xMin) - Mathf.Max(0, obRect.xMax - rfRect.xMax);
        float dy = Mathf.Max(0, rfRect.yMin - obRect.yMin) - Mathf.Max(0, obRect.yMax - rfRect.yMax);
        d = new(dx, dy);
        c.transform.Translate(d);
    }
}
