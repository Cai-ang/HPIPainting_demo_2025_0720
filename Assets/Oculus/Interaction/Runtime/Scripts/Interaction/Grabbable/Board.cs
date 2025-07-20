using System.Linq;
using UnityEngine;
using static UnityEngine.Plane;
using Oculus.Interaction;

/// <summary>
/// 画板
/// </summary>
public class Board : MonoBehaviour
{
    //当画笔移动速度很快时，为了不出现断断续续的点，所以需要对两个点之间时行插值，lerp就是插值系数
    [Range(0, 1)]
    public float lerp = 0.05f;
    //初始化背景的图片
    public Texture2D initailizeTexture;
    //当前背景的图片
    private Texture2D currentTexture;
    //画笔所在的位置 
    private Vector2 paintPos;

    private bool isDrawing = false;//当前画笔是不是正在画板上
    //离开时画笔所在的位置 
    private int lastPaintX;
    private int lastPaintY;
    //画笔所代表的色块的大小
    private int painterTipsWidth = 30;
    private int painterTipsHeight = 15;
    //当前画板的背景图片的尺寸
    private int textureWidth;
    private int textureHeight;

    //画笔的颜色
    private Color32[] painterColor;

    private Color32[] currentColor;
    private Color32[] originColor;
    internal Plane boardPlane;
    public float distance;



    private void Start()
    {
        //获取原始图片的大小 
        Texture2D originTexture = GetComponent<MeshRenderer>().material.mainTexture as Texture2D;
        textureWidth = originTexture.width;//1920   
        textureHeight = originTexture.height;//1080

        //设置当前图片
        currentTexture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false, true);
        currentTexture.SetPixels32(originTexture.GetPixels32());
        currentTexture.Apply();

        //赋值给黑板
        GetComponent<MeshRenderer>().material.mainTexture = currentTexture;

        //初始化画笔的颜色
        painterColor = Enumerable.Repeat<Color32>(new Color32(255, 0, 0, 255), painterTipsWidth * painterTipsHeight).ToArray<Color32>();

        //初始化Plane，让它的法线是这个画板的forward向量，并且法线通过画板的中心位置，由此确定一个平面
        boardPlane = new Plane(transform.forward, transform.position);

        // 初始化临时纹理
        //tempTexture = new Texture2D(textureWidth, textureHeight);

    }

    private void LateUpdate()
    {
        int texPosX = (int)(paintPos.x * textureWidth);
        int texPosY = (int)(paintPos.y * textureHeight);
        if (isDrawing)
        {
            UpdatePixels(texPosX, texPosY);
            lastPaintX = texPosX;
            lastPaintY = texPosY;
        }
        else
        {
            lastPaintX = -1;
            lastPaintY = -1; // -1 用于区分未绘制状态
        }
    }

    private void UpdatePixels(int texPosX, int texPosY)
    {
        int brushSize = Mathf.Clamp((int)(distance * 140), 8, 40); // 限制笔刷尺寸
        if (lastPaintX != -1 && lastPaintY != -1)
        {
            DrawLine(lastPaintX, lastPaintY, texPosX, texPosY, brushSize);
        }
        else
        {
            DrawBrush(texPosX, texPosY, brushSize);
        }
        currentTexture.Apply();
    }

    private void DrawLine(int x0, int y0, int x1, int y1, int brushSize)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;

        while (true)
        {
            DrawBrush(x0, y0, brushSize);
            if (x0 == x1 && y0 == y1) break;
            int e2 = err * 2;
            if (e2 > -dy) { err -= dy; x0 += sx; }
            if (e2 < dx) { err += dx; y0 += sy; }
        }
    }

    private void DrawBrush(int x, int y, int brushSize)
    {
        // Ensure brush size does not exceed texture bounds
        int xStart = Mathf.Max(x - brushSize / 2, 0);
        int yStart = Mathf.Max(y - brushSize / 2, 0);
        int xEnd = Mathf.Min(x + brushSize / 2, textureWidth - 1);
        int yEnd = Mathf.Min(y + brushSize / 2, textureHeight - 1);
        Color32 color = painterColor.Length > 0 ? painterColor[0] : new Color32(0, 0, 0, 255); // Default to black if array is empty

        for (int i = xStart; i <= xEnd; i++)
        {
            for (int j = yStart; j <= yEnd; j++)
            {
                currentTexture.SetPixel(i, j, color);
            }
        }
    }

    /// <summary>
    /// 设置当前画笔所在的UV位置
    /// </summary>
    /// <param name="x"></param>
    /// <param name="y"></param>
    public void SetPainterPositon(float x, float y)
    {
        paintPos.Set(x, y);//画笔笔尖的xy坐标
    }

    /// <summary>
    /// 画笔当前是不是在画画
    /// </summary>
    public bool IsDrawing
    {
        get
        {
            return isDrawing;
        }
        set
        {
            isDrawing = value;
        }
    }

    /// <summary>
    /// 使用当前正在画板上的画笔的颜色
    /// </summary>
    /// <param name="color"></param>
    public void SetPainterColor(Color32 color)
    {
        if (!painterColor[0].IsEqual(color))
        {
            for (int i = 0; i < painterColor.Length; i++)
            {
                painterColor[i] = color;
            }
        }
    }

    /// <summary>
    /// 判断笔尖是在画板的正面还是背面
    /// </summary>
    /// <param name="point">笔尖的位置</param>
    /// <returns>true 在正面;false 在背面</returns>
    public bool GetSideOfBoardPlane(Vector3 point)
    {
        return boardPlane.GetSide(point);
    }

    /// <summary>
    /// 笔尖与平面的距离
    /// </summary>
    /// <param name="point">笔尖的位置</param>
    /// <returns>当在正面的时候返回正值，当在背面的时候返回负值</returns>
    public float GetDistanceFromBoardPlane(Vector3 point)
    {
        return boardPlane.GetDistanceToPoint(point);
    }

    /// <summary>
    /// 矫正后的笔尖应该在的位置
    /// </summary>
    /// <param name="point">笔尖的位置</param>
    /// <returns>矫正后的笔尖位置</returns>
    public Vector3 ProjectPointOnBoardPlane(Vector3 point)
    {//这将计算平面法线与从平面原点到点的矢量之间的点积。此点积表示该点沿平面法线的距离。
        float d = -Vector3.Dot(boardPlane.normal, point - transform.position);//normal是法线
        return point + boardPlane.normal * d;
    }



}
public static class MethodExtention
{
    /// <summary>
    /// 用于比较两个Color32类型是不是一样
    /// </summary>
    /// <param name="origin"></param>
    /// <param name="compare"></param>
    /// <returns></returns>
    public static bool IsEqual(this Color32 origin, Color32 compare)
    {
        if (origin.g == compare.g && origin.r == compare.r)
        {
            if (origin.a == compare.a && origin.b == compare.b)
            {
                return true;
            }
        }
        return false;
    }
}