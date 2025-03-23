
using UnityEngine;

public static class Utils
{
    public static Rect GetWorldRect(SpriteRenderer sr)
    {
        var position = sr.bounds.center - sr.bounds.size / 2;
        var size = sr.bounds.size;
        return new Rect(position, size);
    }

    public static Rect GetWorldRect(RectTransform rt)
    {
        float xmin, xmax, ymin, ymax;
        float scaleFactor = rt.GetComponentInParent<Canvas>().scaleFactor;
        xmin = rt.anchorMin.x * Screen.width + rt.offsetMin.x* scaleFactor;
        xmax = rt.anchorMax.x * Screen.width + rt.offsetMax.x* scaleFactor;
        ymin = rt.anchorMin.y * Screen.height + rt.offsetMin.y* scaleFactor;
        ymax = rt.anchorMax.y * Screen.height + rt.offsetMax.y* scaleFactor;

        Vector2 leftBottom = new Vector2(xmin, ymin);
        Vector2 rightTop = new Vector2(xmax, ymax);
        leftBottom = Camera.main.ScreenToWorldPoint(leftBottom);
        rightTop = Camera.main.ScreenToWorldPoint(rightTop);
        return new Rect(leftBottom, rightTop - leftBottom);
    }
}
