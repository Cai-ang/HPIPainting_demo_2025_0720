using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line_tasks : MonoBehaviour
{
    // Start is called before the first frame update
    public List<GameObject> tasks = new List<GameObject>();
    public bool isfinished = false;
    // 用于跟踪上一帧的完成状态
    private bool previousIsFinishedState = false;
    // 当前活跃任务的索引
    private int currentTaskIndex = 0;
    void Start()
    {
        // 从当前游戏对象的直接子物体中查找
        FindBlocksInChildren(transform);
        tasks[0].AddComponent<line_task>();
    }

    // Update is called once per frame
    void Update()
    {
        // 获取当前的完成状态
        bool currentIsFinishedState = tasks[currentTaskIndex].GetComponent<line_task>().isfinished;

        // 检测状态变化：从 false 变为 true
        if (currentIsFinishedState && !previousIsFinishedState)
        {
            // 状态刚刚从 false 变为 true
            //Debug.Log("Task 0 刚刚完成");
            currentTaskIndex++;
            if (currentTaskIndex == 5)
            {
                isfinished = true;
            }
            // 为 tasks[1] 添加 line_task 组件
            // 首先检查是否已经有这个组件，以避免重复添加
            else if (tasks[currentTaskIndex].GetComponent<line_task>() == null)
            {
                tasks[currentTaskIndex].AddComponent<line_task>();
                //Debug.Log("已为 Task 1 添加 line_task 组件");
            }
        }

        // 更新上一帧的状态，为下一次检测做准备
        previousIsFinishedState = currentIsFinishedState;
    }

    // 递归查找子物体中名称包含"block"的物体
    private void FindBlocksInChildren(Transform parent)
    {
        // 检查每个直接子物体
        foreach (Transform child in parent)
        {
            // 检查名称是否包含"block"(不区分大小写)
            if (child.name.ToLower().Contains("task"))
            {
                tasks.Add(child.gameObject);
            }

            // // 如果此子物体有自己的子物体，递归查找
            // if (child.childCount > 0)
            // {
            //     FindBlocksInChildren(child);
            // }
        }
    }
}
