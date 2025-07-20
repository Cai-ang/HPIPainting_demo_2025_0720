using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System;
using UnityEngine.UI;

public class ModelMeshEditor : MonoBehaviour
{
    public float pointScale = 0.1f;
    private float lastPointScale = 0.1f;

    // 原始网格和工作副本
    private Mesh originalMesh;
    private Mesh workingMesh;

    // 顶点列表
    List<Vector3> positionList = new List<Vector3>();
    List<GameObject> positionObjList = new List<GameObject>();

    Dictionary<string, List<int>> pointmap = new Dictionary<string, List<int>>();
    public GameObject editorpoint;

    // 新增 - 球形碰撞器引用
    public SphereCollider editorSphere;
    // 新增 - 是否显示所有控制点或只显示球内的点
    public bool showAllPoints = false;
    // 新增 - 当前可见的控制点集合
    private HashSet<string> visiblePoints = new HashSet<string>();
    // 新增 - 所有控制点的位置信息缓存
    private Dictionary<string, Vector3> allPointPositions = new Dictionary<string, Vector3>();

    // 操作历史系统
    [System.Serializable]
    private class MeshEditAction
    {
        public string pointId;
        public Vector3 startPosition;
        public Vector3 endPosition;

        public MeshEditAction(string id, Vector3 start, Vector3 end)
        {
            pointId = id;
            startPosition = start;
            endPosition = end;
        }
    }

    // 历史记录栈
    private List<MeshEditAction> actionHistory = new List<MeshEditAction>();
    private int currentHistoryIndex = -1;

    // 最大历史记录数
    private int maxHistoryCount = 100;

    // 用于UI按钮
    public Button undoButton;
    public Button redoButton;
    public Button saveButton;
    public Button resetButton;

    // 标记是否有未保存的更改
    private bool hasUnsavedChanges = false;

    void Start()
    {
        lastPointScale = pointScale;

        // 获取原始网格并创建工作副本
        originalMesh = GetComponent<MeshFilter>().sharedMesh;
        workingMesh = Instantiate(originalMesh);
        GetComponent<MeshFilter>().mesh = workingMesh; // 使用工作副本

        // 初始化按钮监听
        if (undoButton) undoButton.onClick.AddListener(Undo);
        if (redoButton) redoButton.onClick.AddListener(Redo);
        if (saveButton) saveButton.onClick.AddListener(SaveChanges);
        if (resetButton) resetButton.onClick.AddListener(ResetMesh);

        // 创建球形碰撞器（如果没有）
        if (editorSphere == null)
        {
            GameObject sphereObj = new GameObject("EditorSphere");
            sphereObj.transform.parent = transform;
            sphereObj.transform.localPosition = Vector3.zero;
            editorSphere = sphereObj.AddComponent<SphereCollider>();
            editorSphere.radius = 1.0f;
            editorSphere.isTrigger = true;
        }

        // 初始化顶点信息
        InitializeVertexData();

        // 更新可见的控制点
        UpdateVisiblePoints();

        UpdateButtonStates();
    }

    // 初始化顶点数据
    private void InitializeVertexData()
    {
        positionList = new List<Vector3>(workingMesh.vertices);
        pointmap.Clear();
        allPointPositions.Clear();

        // 先收集所有顶点信息
        for (int i = 0; i < workingMesh.vertices.Length; i++)
        {
            string vstr = Vector2String(workingMesh.vertices[i]);

            if (!pointmap.ContainsKey(vstr))
            {
                pointmap.Add(vstr, new List<int>());
                allPointPositions.Add(vstr, workingMesh.vertices[i]);
            }
            pointmap[vstr].Add(i);
        }
    }

    // 更新可见的控制点
    public void UpdateVisiblePoints()
    {
        // 清除现有控制点
        foreach (var obj in positionObjList)
        {
            if (obj != null) Destroy(obj);
        }
        positionObjList.Clear();
        visiblePoints.Clear();

        if (showAllPoints)
        {
            // 显示所有点
            foreach (string key in pointmap.Keys)
            {
                CreatePointObject(key, allPointPositions[key]);
                visiblePoints.Add(key);
            }
        }
        else
        {
            // 只显示球内的点
            foreach (string key in pointmap.Keys)
            {
                Vector3 worldPos = transform.TransformPoint(allPointPositions[key]);
                Vector3 sphereWorldPos = editorSphere.transform.position;
                float distance = Vector3.Distance(worldPos, sphereWorldPos);

                if (distance <= editorSphere.radius)
                {
                    CreatePointObject(key, allPointPositions[key]);
                    visiblePoints.Add(key);
                }
            }
        }
    }

    // 创建单个控制点对象
    private void CreatePointObject(string pointId, Vector3 position)
    {
        GameObject pointObj = Instantiate(editorpoint);
        pointObj.transform.parent = transform;
        pointObj.transform.localPosition = position;
        pointObj.transform.localScale = new Vector3(pointScale, pointScale, pointScale);

        MeshEditorPoint editorPoint = pointObj.GetComponent<MeshEditorPoint>();
        editorPoint.onDragStart = PointDragStart;
        editorPoint.onMove = PointMove;
        editorPoint.onDragEnd = PointDragEnd;
        editorPoint.pointid = pointId;

        positionObjList.Add(pointObj);
    }

    // 更新按钮状态（启用/禁用）
    private void UpdateButtonStates()
    {
        if (undoButton) undoButton.interactable = CanUndo();
        if (redoButton) redoButton.interactable = CanRedo();
        if (saveButton) saveButton.interactable = hasUnsavedChanges;
    }

    // 当控制点开始拖动时调用
    public void PointDragStart(string pointid, Vector3 startPos)
    {
        // 无需实时记录，只在拖动结束时记录操作
    }

    // 控制点移动时调用，实时更新网格但不记录历史
    public void PointMove(string pointid, Vector3 position)
    {
        if (!pointmap.ContainsKey(pointid))
        {
            return;
        }

        List<int> _list = pointmap[pointid];

        for (int i = 0; i < _list.Count; i++)
        {
            positionList[_list[i]] = position;
        }

        // 更新缓存的位置信息
        allPointPositions[pointid] = position;

        workingMesh.vertices = positionList.ToArray();
        workingMesh.RecalculateNormals();
    }

    // 当控制点拖动结束时调用，记录操作历史
    public void PointDragEnd(string pointid, Vector3 endPos)
    {
        // 找到起始位置 (从字符串ID转换)
        Vector3 startPos = String2Vector(pointid);

        // 如果位置没有改变，不记录历史
        if (Vector3.Distance(startPos, endPos) < 0.0001f)
            return;

        // 添加到历史记录
        AddHistoryAction(new MeshEditAction(pointid, startPos, endPos));

        // 标记有未保存的更改
        hasUnsavedChanges = true;
        UpdateButtonStates();
    }

    // 添加操作到历史记录
    private void AddHistoryAction(MeshEditAction action)
    {
        // 如果当前不是最新状态，移除后面的历史
        if (currentHistoryIndex < actionHistory.Count - 1)
        {
            actionHistory.RemoveRange(currentHistoryIndex + 1,
                                     actionHistory.Count - currentHistoryIndex - 1);
        }

        // 添加新操作
        actionHistory.Add(action);
        currentHistoryIndex = actionHistory.Count - 1;

        // 限制历史记录长度
        if (actionHistory.Count > maxHistoryCount)
        {
            actionHistory.RemoveAt(0);
            currentHistoryIndex--;
        }

        UpdateButtonStates();
    }

    // 检查是否可以撤销
    public bool CanUndo()
    {
        return currentHistoryIndex >= 0;
    }

    // 撤销操作
    public void Undo()
    {
        if (!CanUndo()) return;

        MeshEditAction action = actionHistory[currentHistoryIndex];
        ApplyVertexPosition(action.pointId, action.startPosition);
        currentHistoryIndex--;

        hasUnsavedChanges = true;
        UpdateButtonStates();
    }

    // 检查是否可以重做
    public bool CanRedo()
    {
        return currentHistoryIndex < actionHistory.Count - 1;
    }

    // 重做操作
    public void Redo()
    {
        if (!CanRedo()) return;

        currentHistoryIndex++;
        MeshEditAction action = actionHistory[currentHistoryIndex];
        ApplyVertexPosition(action.pointId, action.endPosition);

        hasUnsavedChanges = true;
        UpdateButtonStates();
    }

    // 应用顶点位置更改 (用于撤销/重做)
    private void ApplyVertexPosition(string pointid, Vector3 position)
    {
        if (!pointmap.ContainsKey(pointid)) return;

        List<int> indices = pointmap[pointid];

        // 更新工作网格顶点
        for (int i = 0; i < indices.Count; i++)
        {
            positionList[indices[i]] = position;
        }

        // 更新缓存的位置信息
        allPointPositions[pointid] = position;

        workingMesh.vertices = positionList.ToArray();
        workingMesh.RecalculateNormals();

        // 更新对应控制点位置
        foreach (GameObject obj in positionObjList)
        {
            MeshEditorPoint point = obj.GetComponent<MeshEditorPoint>();
            if (point.pointid == pointid)
            {
                obj.transform.localPosition = position;
                break;
            }
        }
    }

    // 保存更改到原始网格
    public void SaveChanges()
    {
        // 复制工作网格数据到原始网格
        originalMesh.vertices = workingMesh.vertices;
        originalMesh.normals = workingMesh.normals;

        // 标记为已保存
        hasUnsavedChanges = false;
        UpdateButtonStates();

        Debug.Log("Mesh changes saved!");
    }

    // 重置为原始网格
    public void ResetMesh()
    {
        // 清空历史记录
        actionHistory.Clear();
        currentHistoryIndex = -1;

        // 重新创建工作网格
        workingMesh = Instantiate(originalMesh);
        GetComponent<MeshFilter>().mesh = workingMesh;
        positionList = new List<Vector3>(workingMesh.vertices);

        // 重新初始化顶点数据
        InitializeVertexData();

        // 更新可见控制点
        UpdateVisiblePoints();

        hasUnsavedChanges = false;
        UpdateButtonStates();

        Debug.Log("Mesh reset to original state!");
    }

    void Update()
    {
        // 检测控制点尺寸变化
        if (Math.Abs(lastPointScale - pointScale) > 0.1f)
        {
            lastPointScale = pointScale;
            for (int i = 0; i < positionObjList.Count; i++)
            {
                positionObjList[i].transform.localScale = new Vector3(pointScale, pointScale, pointScale);
            }
        }

        // 如果只显示球内的点，则每帧更新可见的控制点
        if (!showAllPoints)
        {
            // 检查是否有新的点进入或离开球体
            CheckForPointsInSphere();
        }
    }

    // 检查是否有点进入或离开球体
    private void CheckForPointsInSphere()
    {
        bool changed = false;
        HashSet<string> newVisiblePoints = new HashSet<string>();

        foreach (string key in pointmap.Keys)
        {
            Vector3 worldPos = transform.TransformPoint(allPointPositions[key]);
            Vector3 sphereWorldPos = editorSphere.transform.position;
            float distance = Vector3.Distance(worldPos, sphereWorldPos);

            if (distance <= editorSphere.radius)
            {
                newVisiblePoints.Add(key);

                // 如果这个点不在当前可见集合中，我们需要创建它
                if (!visiblePoints.Contains(key))
                {
                    changed = true;
                }
            }
        }

        // 检查是否有点需要移除
        foreach (string key in visiblePoints)
        {
            if (!newVisiblePoints.Contains(key))
            {
                changed = true;
                break;
            }
        }

        // 如果有变化，更新控制点
        if (changed)
        {
            // 清除现有控制点
            foreach (var obj in positionObjList)
            {
                if (obj != null) Destroy(obj);
            }
            positionObjList.Clear();

            // 创建新的可见点
            foreach (string key in newVisiblePoints)
            {
                CreatePointObject(key, allPointPositions[key]);
            }

            visiblePoints = newVisiblePoints;
        }
    }

    // 退出时提示保存
    void OnApplicationQuit()
    {
        if (hasUnsavedChanges)
        {
            Debug.LogWarning("You have unsaved mesh changes!");
            // 在实际应用中，这里可以弹出对话框询问是否保存
        }
    }

    string Vector2String(Vector3 v)
    {
        StringBuilder str = new StringBuilder();
        str.Append(v.x).Append(",").Append(v.y).Append(",").Append(v.z);
        return str.ToString();
    }

    Vector3 String2Vector(string vstr)
    {
        try
        {
            string[] strings = vstr.Split(',');
            return new Vector3(float.Parse(strings[0]), float.Parse(strings[1]), float.Parse(strings[2]));
        }
        catch (Exception e)
        {
            Debug.LogError(e.ToString());
            return Vector3.zero;
        }
    }

    // 添加公共方法以便从外部切换显示模式
    public void ToggleShowAllPoints(bool showAll)
    {
        showAllPoints = showAll;
        UpdateVisiblePoints();
    }

    // 添加公共方法以便从外部移动碰撞球
    public void MoveSphere(Vector3 newPosition)
    {
        if (editorSphere != null)
        {
            editorSphere.transform.position = newPosition;
            if (!showAllPoints)
            {
                CheckForPointsInSphere();
            }
        }
    }
}