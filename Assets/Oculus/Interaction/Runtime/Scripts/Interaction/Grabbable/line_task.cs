using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class line_task : MonoBehaviour
{
    public Pen_3D pen;
    private bool previousDrawingState = false; // 存储上一帧的绘制状态
    private Transform start;
    private Transform line;
    private Transform end;
    public bool isfinished = false;
    // Start is called before the first frame update
    void Start()
    {
        transparentchange(transform);
        pen = GameObject.Find("Pen_3D").GetComponent<Pen_3D>();
    }

    // Update is called once per frame
    void Update()
    {
        if (start.GetComponent<SphereCollider>().isTrigger || end.GetComponent<SphereCollider>().isTrigger)
        {
            // 检测状态变化
            if (pen.isdrawing != previousDrawingState)
            {
                // 状态发生变化
                if (pen.isdrawing)
                {
                    // 从非绘制状态变为绘制状态
                    //Debug.Log("开始绘制");
                    // 在这里添加开始绘制时的代码

                    // 获取当前颜色
                    Color currentColor = start.GetComponent<Renderer>().material.color;
                    end.GetComponent<Renderer>().material.color = currentColor;
                    // 只修改alpha值
                    currentColor.a = 0.2f;  // 1.0 = 100%不透明
                                            // 应用新颜色
                    start.GetComponent<Renderer>().material.color = currentColor;
                }
                else
                {
                    // 从绘制状态变为非绘制状态
                    //Debug.Log("停止绘制");
                    // 在这里添加停止绘制时的代码
                    // 获取当前颜色
                    Color currentColor = end.GetComponent<Renderer>().material.color;
                    // 只修改alpha值
                    currentColor.a = 0.2f;  // 1.0 = 100%不透明
                                            // 应用新颜色
                    line.GetComponent<Renderer>().material.color = currentColor;
                    end.GetComponent<Renderer>().material.color = currentColor;
                    isfinished = true;
                    pen.UndoLastDrawing();
                }

                // 更新先前状态
                previousDrawingState = pen.isdrawing;
            }
        }
    }
    //修改子物体的透明度
    private void transparentchange(Transform parent)
    {
        // 检查每个直接子物体
        foreach (Transform child in parent)
        {
            // 获取当前颜色
            Color currentColor = child.GetComponent<Renderer>().material.color;
            // 只修改alpha值
            currentColor.a = 0.8f;  // 1.0 = 100%不透明
                                    // 应用新颜色

            // 检查名称是否包含"block"(不区分大小写)
            if (child.name.ToLower().Contains("start"))
            {
                start = child;
                child.GetComponent<Renderer>().material.color = currentColor;
            }
            else if (child.name.ToLower().Contains("line"))
            {
                line = child;
                child.GetComponent<Renderer>().material.color = currentColor;
            }
            else if (child.name.ToLower().Contains("end"))
            {
                end = child;
            }
        }
    }
}
