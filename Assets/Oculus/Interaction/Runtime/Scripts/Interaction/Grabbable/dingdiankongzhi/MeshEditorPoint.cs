using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshEditorPoint : MonoBehaviour
{
    [HideInInspector] public string pointid;
    [HideInInspector] private Vector3 lastPosition;

    // 添加开始拖动的委托，用于记录操作起始状态
    public delegate void DragStartDelegate(string pid, Vector3 startPos);
    public delegate void MoveDelegate(string pid, Vector3 pos);
    public delegate void DragEndDelegate(string pid, Vector3 endPos);

    // 拖动开始时的回调
    public DragStartDelegate onDragStart = null;
    // 移动过程中的回调
    public MoveDelegate onMove = null;
    // 拖动结束的回调
    public DragEndDelegate onDragEnd = null;

    // 跟踪是否正在拖动
    private bool isDragging = false;
    // 拖动开始位置
    private Vector3 dragStartPosition;

    void Start()
    {
        lastPosition = transform.position;
        dragStartPosition = transform.position;
    }

    void Update()
    {
        // 检测拖动开始
        if (!isDragging && transform.position != lastPosition)
        {
            isDragging = true;
            dragStartPosition = lastPosition;
            if (onDragStart != null) onDragStart(pointid, dragStartPosition);
        }

        // 检测拖动中
        if (isDragging && transform.position != lastPosition)
        {
            if (onMove != null) onMove(pointid, transform.localPosition);
            lastPosition = transform.position;
        }

        // 检测拖动结束 (通过鼠标按键状态)
        if (isDragging && !Input.GetMouseButton(0))
        {
            isDragging = false;
            if (onDragEnd != null) onDragEnd(pointid, transform.localPosition);
        }
    }
}