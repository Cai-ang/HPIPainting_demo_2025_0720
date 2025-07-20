using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Line_blocks : MonoBehaviour
{
    public List<GameObject> blocks = new List<GameObject>();
    private bool previousIsFinishedState = false;
    // 当前活跃任务的索引
    private int currentTaskIndex = 0;

    // Start is called before the first frame update
    void Start()
    {
        // 从当前游戏对象的直接子物体中查找
        FindBlocksInChildren(transform);
        blocks[0].AddComponent<Line_tasks>();
        blocks[1].SetActive(false);
        blocks[2].SetActive(false);
        blocks[3].SetActive(false);
        blocks[4].SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        // 获取当前的完成状态
        bool currentIsFinishedState = blocks[currentTaskIndex].GetComponent<Line_tasks>().isfinished;

        // 检测状态变化：从 false 变为 true
        if (currentIsFinishedState && !previousIsFinishedState)
        {
            // 状态刚刚从 false 变为 true
            //Debug.Log("Task 0 刚刚完成");
            blocks[currentTaskIndex].SetActive(false);
            currentTaskIndex++;
            blocks[currentTaskIndex].SetActive(true);
            if (blocks[currentTaskIndex].GetComponent<Line_tasks>() == null)
            {
                blocks[currentTaskIndex].AddComponent<Line_tasks>();
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
            if (child.name.ToLower().Contains("block"))
            {
                blocks.Add(child.gameObject);
            }

            // // 如果此子物体有自己的子物体，递归查找
            // if (child.childCount > 0)
            // {
            //     FindBlocksInChildren(child);
            // }
        }
    }
}
