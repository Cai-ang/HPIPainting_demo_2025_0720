using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshFaceController : MonoBehaviour
{
    // 面索引
    public int faceIndex = -1;

    // 面的顶点位置
    public Vector3[] vertices;

    // 原始顶点位置(用于历史记录)
    public Vector3[] originalVertices;

    // 面法线
    public Vector3 normal;

    // 回调委托定义
    public delegate void FaceSelectedDelegate(int faceIndex, GameObject faceObj);
    public delegate void FaceDragDelegate(int faceIndex, Vector3 worldPosition);

    // 回调事件
    public FaceSelectedDelegate onFaceSelected;
    public FaceDragDelegate onFaceDragStart;
    public FaceDragDelegate onFaceDrag;
    public FaceDragDelegate onFaceDragEnd;

    // 是否正在拖动
    private bool isDragging = false;

    // 鼠标悬停效果的初始颜色和高亮颜色
    private Color originalColor;
    private Renderer rend;

    void Start()
    {
        rend = GetComponent<Renderer>();
        if (rend)
        {
            originalColor = rend.material.color;
        }

        // 保存原始顶点位置用于撤销
        if (vertices != null)
        {
            originalVertices = new Vector3[vertices.Length];
            for (int i = 0; i < vertices.Length; i++)
            {
                originalVertices[i] = vertices[i];
            }
        }
    }

    void OnMouseEnter()
    {
        // 鼠标悬停高亮
        if (rend && !isDragging)
        {
            rend.material.color = Color.yellow;
        }
    }

    void OnMouseExit()
    {
        // 恢复原始颜色
        if (rend && !isDragging)
        {
            rend.material.color = originalColor;
        }
    }

    void OnMouseDown()
    {
        // 面被选中
        if (onFaceSelected != null)
        {
            onFaceSelected(faceIndex, gameObject);
        }
    }

    void OnMouseDrag()
    {
        if (!isDragging)
        {
            // 开始拖动
            isDragging = true;
            if (onFaceDragStart != null)
            {
                onFaceDragStart(faceIndex, GetMouseWorldPosition());
            }
        }

        // 拖动中
        if (onFaceDrag != null)
        {
            onFaceDrag(faceIndex, GetMouseWorldPosition());
        }
    }

    void OnMouseUp()
    {
        if (isDragging)
        {
            // 结束拖动
            if (onFaceDragEnd != null)
            {
                onFaceDragEnd(faceIndex, GetMouseWorldPosition());
            }
            isDragging = false;
        }
    }

    // 获取鼠标在世界空间的位置
    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Plane dragPlane = new Plane(transform.forward, transform.position);

        float distance;
        if (dragPlane.Raycast(ray, out distance))
        {
            return ray.GetPoint(distance);
        }

        return transform.position;
    }
}