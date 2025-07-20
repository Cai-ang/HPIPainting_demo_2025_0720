using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using UnityEngine;
using static OVRGrabbable;
using TMPro;
using UnityEngine.UI;
using pipegenaration;
using UnityEditor;
using Oculus.Interaction.HandGrab;
using FFD;
using Fitter;

public class Pen_3D : MonoBehaviour
{
    [Header("Pen Properties")]
    public Transform tip;
    public Material drawingMaterial;

    [Range(0.01f, 0.1f)]
    public float penWidth = 0.01f;

    [Header("Hands & Grabbable")]
    private static Grabbable pengrabbed;
    private GameObject current_lineRenderer;
    private int points_index;
    public Color currentColor;
    public string penState = "G";
    public TextMeshProUGUI textMeshPro;
    public GameObject prompt;
    public Transform rayOrigin;
    private RaycastHit hitInfo;
    //这个画笔是不是正在被手柄抓着
    private bool IsGrabbing;
    public Board board;//设置成类型的成员，而不是类型实例的成员，因为所有画笔都是用的同一个board
    public List<Transform> handTransform;
    private bool handv = false;//设置预设手势是否开关

    //绘画间隔
    public float draw_interval = 0.02f;

    [Header("Draw_line2D")]
    private Mesh currentMesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> indices = new List<int>();
    private Vector3 lastPosition;
    public List<Vector3> Vpositions = new List<Vector3>();
    public List<GameObject> Lines2D = new List<GameObject>();
    public List<GameObject> Draws = new List<GameObject>();
    // 添加到类的成员变量中
    public List<GameObject> RedoDraws = new List<GameObject>(); // 存储被撤销的绘制，用于重做功能

    [Header("笔类型选择菜单")]
    public GameObject penTypeSelectionMenu;  // 显示笔类型的Canvas
    public List<GameObject> penTypeOptions;  // 不同笔类型的选项框
    public float handDetectionWidth = 0.1f;  // 每个选项的检测区域宽度
    public float longPressTime = 0.8f;       // 长按的持续时间
    private bool isTypeMenuCoroutineRunning = false;
    private Transform currentHandTransform;  // 用于跟踪手的位置
    // 双击检测相关变量
    private float lastMClickTime = 0f;
    public float doubleClickTimeThreshold = 0.8f;  // 双击的时间阈值
    private bool isDoubleClicking = false;

    // Right Click检测相关变量
    private float lastRClickTime = 0f;
    private bool isPenSizeMenuCoroutineRunning = false;
    private bool isRedoDoubleClicking = false;
    public GameObject penSizeSlider; // 用于调整笔大小的滑块UI
    public float minPenWidth = 0.0005f;
    public float maxPenWidth = 0.02f;
    private Vector3 initialHandPosition;

    //3D曲面生成
    [Header("3D曲面生成")]
    //菜单相关
    public GameObject Menu;
    public GameObject Pen_menu;
    public GameObject Color_menu;
    public float menuoffset = 0.5f;

    //绘制模式
    public int Brushtype = 0;

    //geometry绘制相关
    private GameObject current_geometry_mesh;
    public int geometry_mode = 0;

    //控制笔状态相关
    private string last_penstate = "G";
    public float pensize;

    //3DPipes绘制相关
    public int Segments = 6;
    public float Radius = 2;
    public float elbow_Radius = 4;
    public List<Vector3> pipe_ppos = new List<Vector3>();
    LineCreateOf3D LineCreateOf3D = new LineCreateOf3D();
    public bool drawlock = false;

    //笔迹优化相关
    Draws_Fitter fitter = new Draws_Fitter();

    private bool isUndoCoroutineRunning = false;
    private bool isMenuCoroutineRunning = false;
    public float waittotalTime = 0.8f;  // 总等待时间
    public bool drawonmesh = false;
    public int brushSize_drawonmesh = 5;
    public bool drawfit = false;
    //test相关
    public bool isdrawing = false;

    public bool testing = false;

    //画布存储相关
    public GameObject HPIcanvas;
    private List<GameObject> layers;
    private GameObject currentlayer;
    private int layerindex;
    private void Start()
    {
        //将画笔部件设置为画笔的颜色，用于识别这个画笔的颜色
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.transform == transform)
            {
                continue;
            }
            if (renderer.tag == "render")
                renderer.material.color = currentColor;
        }
        if (!pengrabbed)
        {
            pengrabbed = this.GetComponent<Grabbable>();
        }
        if (!board)
        {
            board = FindAnyObjectByType<Board>();
        }
        prompt.SetActive(false);
        Color_menu.SetActive(false);
        Menu.SetActive(false);
        penTypeSelectionMenu.SetActive(false);
        penSizeSlider.SetActive(false);

        HPIcanvas = GameObject.Find("HPIcanvas");
        GameObject layer1 = new GameObject("layer1");
        layer1.transform.parent = HPIcanvas.transform;
        currentlayer = layer1;
        layerindex = 0;
    }
    private GameObject current_Line2D;

    #region 判定笔的状态
    private void pencontroller()
    {
        if (pengrabbed.isGrabbed() == "pen")
        {
            prompt.SetActive(true);
            prompt.GetComponentInChildren<TextMeshProUGUI>().text = penState;
            handv = true;

            if (drawlock)
            {
                switch (penState)
                {
                    case "All":
                        HandleDrawState();
                        break;
                    case "M&R":
                        prompt.GetComponentInChildren<TextMeshProUGUI>().text = "Locking";
                        break;
                    case "M":
                        prompt.GetComponentInChildren<TextMeshProUGUI>().text = "Locking";
                        break;
                    case "R":
                        prompt.GetComponentInChildren<TextMeshProUGUI>().text = "Locking";
                        break;

                    case "G":
                        HandleIdleState();
                        break;

                    default:
                        break;
                }
            }
            else
            {
                switch (penState)
                {
                    case "All":
                        if (testing)
                            currentColor = Color.blue;
                        HandleDrawState();
                        break;
                    case "M&R":
                        if (!isMenuCoroutineRunning)
                        {
                            StartCoroutine(HandleMenuStateWithDelay());
                            isMenuCoroutineRunning = true;  // 标记Coroutine正在运行
                        }
                        break;

                    case "M":
                        if (testing)
                        {
                            currentColor = Color.red;
                            HandleDrawState();
                        }
                        else if (last_penstate == "G")
                        {
                            //Debug.Log("M状态下的手势检测:" + (Time.time - lastMClickTime));
                            // 检测双击
                            if (Time.time - lastMClickTime < doubleClickTimeThreshold)
                            {
                                isDoubleClicking = true;
                                // 这是双击
                                HandleUndoState();
                                lastMClickTime = 0f; // 重置双击时间
                            }
                            else
                            {
                                // 记录点击时间，用于下次检测双击
                                lastMClickTime = Time.time;

                                // 如果不是双击，启动长按检测
                                if (!isTypeMenuCoroutineRunning && !isDoubleClicking)
                                {
                                    StartCoroutine(HandleTypeMenuStateWithDelay());
                                    isTypeMenuCoroutineRunning = true;
                                }
                            }
                        }
                        break;


                    case "R":
                        if (testing)
                        {
                            currentColor = Color.green;
                            HandleDrawState();
                        }
                        else if (last_penstate == "G")
                        {
                            // 检测双击
                            if (Time.time - lastRClickTime < doubleClickTimeThreshold)
                            {
                                isRedoDoubleClicking = true;
                                // 这是双击，执行重做功能
                                HandleRedoState();
                                lastRClickTime = 0f; // 重置双击时间
                            }
                            else
                            {
                                // 记录点击时间，用于下次检测双击
                                lastRClickTime = Time.time;

                                // 如果不是双击，启动长按检测
                                if (!isPenSizeMenuCoroutineRunning && !isRedoDoubleClicking)
                                {
                                    StartCoroutine(HandlePenSizeAdjustmentWithDelay());
                                    isPenSizeMenuCoroutineRunning = true;
                                }
                            }
                        }
                        break;

                    case "G":
                        HandleIdleState();
                        break;

                    default:
                        break;
                }
            }

            last_penstate = penState; // 更新上一个状态
        }
        else
        {
            prompt.SetActive(false);
            if (handv)
            {
                foreach (Transform hand in handTransform)
                {
                    hand.gameObject.SetActive(false);
                }
                handv = false;
            }
            if (isUndoCoroutineRunning)
            {
                StopAllCoroutines();  // 如果pen不被抓取，停止所有协程
                isUndoCoroutineRunning = false;
            }
            if (isMenuCoroutineRunning)
            {
                StopAllCoroutines();  // 如果pen不被抓取，停止所有协程
                isMenuCoroutineRunning = false;
            }
            if (isTypeMenuCoroutineRunning)
            {
                StopAllCoroutines();  // 如果pen不被抓取，停止所有协程
                isTypeMenuCoroutineRunning = false;
                penTypeSelectionMenu.SetActive(false);  // 关闭菜单
            }
        }
    }

    private void Update()
    {
        pencontroller();
    }

    // 重做功能
    private void HandleRedoState()
    {
        RedoLastDrawing();
        // 重置双击状态
        isRedoDoubleClicking = false;
    }

    // 长按检测笔大小调整
    private IEnumerator HandlePenSizeAdjustmentWithDelay()
    {
        isPenSizeMenuCoroutineRunning = true;
        float intervalCheck = 0.2f;  // 检查间隔
        float elapsedTime = 0;      // 已经过时间

        while (elapsedTime < longPressTime)
        {
            if (penState != "R" || isRedoDoubleClicking)
            {
                isPenSizeMenuCoroutineRunning = false;
                yield break;  // 如果状态改变或检测到双击，退出协程
            }
            yield return new WaitForSeconds(intervalCheck);
            elapsedTime += intervalCheck;  // 更新已经过时间
        }

        // 如果在longPressTime秒后仍然处于R状态，开始调整笔大小
        if (penState == "R" && !isRedoDoubleClicking)
        {
            penSizeSlider.SetActive(true); // 显示笔大小调整UI
            penSizeSlider.transform.position = transform.position;

            penSizeSlider.transform.LookAt(Camera.main.transform);
            penSizeSlider.transform.Rotate(0, 180, 0);
            // 记录初始手部位置
            initialHandPosition = transform.position;

            float originalPenWidth = Radius;
            // 当处于R状态时，跟踪手部移动进行调整
            while (penState == "R")
            {
                // 计算与初始位置的水平偏移量
                float horizontalOffset = transform.position.x - initialHandPosition.x;

                // 根据手部位置计算应该改变的笔大小
                float newPenWidth = Mathf.Clamp(
                    originalPenWidth + horizontalOffset * 0.5f, // 乘以因子以控制灵敏度
                    minPenWidth,
                    maxPenWidth
                );
                Debug.Log("当前笔大小: " + horizontalOffset);

                // 如果笔大小改变，更新笔大小
                if (Radius != newPenWidth)
                {
                    UpdatePenSize(newPenWidth);
                }

                yield return null;
            }
            penSizeSlider.SetActive(false); // 隐藏笔大小调整UI
        }

        isPenSizeMenuCoroutineRunning = false;
    }
    private void UpdatePenSize(float newsize)
    {
        penSizeSlider.GetComponentInChildren<Slider>().value = newsize;
        Radius = newsize;
    }

    // 长按检测，与之前相同
    private IEnumerator HandleTypeMenuStateWithDelay()
    {
        isTypeMenuCoroutineRunning = true;
        float intervalCheck = 0.2f;  // 检查间隔
        float elapsedTime = 0;      // 已经过时间

        while (elapsedTime < longPressTime)
        {
            if (penState != "M" || isDoubleClicking)
            {
                isTypeMenuCoroutineRunning = false;
                yield break;  // 如果状态改变或检测到双击，退出协程
            }
            yield return new WaitForSeconds(intervalCheck);
            elapsedTime += intervalCheck;  // 更新已经过时间
        }

        // 如果在longPressTime秒后仍然处于M状态，打开类型选择菜单
        if (penState == "M" && !isDoubleClicking)
        {
            OpenPenTypeMenu();

            // 在菜单打开且保持M状态时跟踪手部位置
            currentHandTransform = this.transform;
            Vector3 lastHandPosition = currentHandTransform.position;
            int currentSelection = Brushtype;
            // 当处于M状态时，跟踪手部移动进行选择
            while (penState == "M")
            {
                // 计算与初始位置的垂直偏移量
                float horizontalOffset = lastHandPosition.y - currentHandTransform.position.y;

                // 根据手部位置计算应该选择哪个选项
                int newSelection = Mathf.Clamp(
                    Mathf.RoundToInt(horizontalOffset / handDetectionWidth) + currentSelection,
                    0,
                    penTypeOptions.Count - 1
                );
                //Debug.Log("当前选择的选项索引: " + horizontalOffset);

                // 如果选择改变，更新UI和Brushtype
                if (newSelection != currentSelection)
                {
                    UpdatePenTypeSelection(newSelection);
                    currentSelection = newSelection;
                    lastHandPosition = currentHandTransform.position;
                }
                yield return null;
            }
            // 关闭菜单并更新画笔类型
            Brushtype = currentSelection; // 更新画笔类型
            // 当M状态结束时关闭菜单
            ClosePenTypeMenu();
        }

        isTypeMenuCoroutineRunning = false;
    }

    private void OpenPenTypeMenu()
    {
        if (penTypeSelectionMenu != null)
        {
            penTypeSelectionMenu.SetActive(true);
            Vector3 menuPos = transform.position;
            penTypeSelectionMenu.transform.position = menuPos;

            penTypeSelectionMenu.transform.LookAt(Camera.main.transform);
            penTypeSelectionMenu.transform.Rotate(0, 180, 0);

            //penTypeSelectionMenu.SetActive(true);

            UpdatePenTypeSelection(Brushtype);
        }
    }

    private void ClosePenTypeMenu()
    {
        if (penTypeSelectionMenu != null)
        {
            penTypeSelectionMenu.SetActive(false);
        }
    }

    private void UpdatePenTypeSelection(int selectedIndex)
    {
        if (penTypeOptions != null && penTypeOptions.Count > 0)
        {
            foreach (var option in penTypeOptions)
            {
                var highlight = option.GetComponent<Image>();
                if (highlight != null)
                {
                    Color color = highlight.color;
                    color.a = 0.5f;
                    highlight.color = color;
                }
            }

            if (selectedIndex >= 0 && selectedIndex < penTypeOptions.Count)
            {
                var highlight = penTypeOptions[selectedIndex].GetComponent<Image>();
                if (highlight != null)
                {
                    Color color = highlight.color;
                    color.a = 1.0f;
                    highlight.color = color;
                }
            }
        }
    }

    // private IEnumerator HandleUndoStateWithDelay()
    // {
    //     isUndoCoroutineRunning = true;
    //     float intervalCheck = 0.2f;  // 检查间隔
    //     float totalTime = 0.4f;
    //     float elapsedTime = 0;  // 已经过时间

    //     while (elapsedTime < totalTime)
    //     {
    //         if (penState != "R")
    //         {
    //             isUndoCoroutineRunning = false;
    //             yield break;  // 如果状态改变，退出Coroutine
    //         }
    //         yield return new WaitForSeconds(intervalCheck);
    //         elapsedTime += intervalCheck;  // 更新已经过时间
    //     }

    //     // 如果2秒结束时，状态仍然是Undo，执行Undo操作
    //     if (penState == "R")
    //     {
    //         HandleUndoState();
    //         isUndoCoroutineRunning = false;
    //         yield break;
    //     }
    //     isUndoCoroutineRunning = false;
    // }

    private IEnumerator HandleMenuStateWithDelay()
    {
        isMenuCoroutineRunning = true;
        float intervalCheck = 0.2f;  // 检查间隔
        float elapsedTime = 0;  // 已经过时间

        while (elapsedTime < waittotalTime)
        {
            if (penState != "M&R")
            {
                isMenuCoroutineRunning = false;
                yield break;  // 如果状态改变，退出Coroutine
            }
            yield return new WaitForSeconds(intervalCheck);
            elapsedTime += intervalCheck;  // 更新已经过时间
        }

        // 如果2秒结束时，状态仍然是Menu，执行Menu操作
        if (penState == "M&R")
        {
            HandleMenuState();
            isMenuCoroutineRunning = false;
            yield break;
        }
        isMenuCoroutineRunning = false;
    }

    public void lockchange(Toggle islock)
    {
        drawfit = drawlock = islock.isOn;

    }


    private void HandleDrawState()
    {
        // 开始新绘制时清空重做历史
        RedoDraws.Clear();
        switch (Brushtype)
        {
            case 0: Draw_Pipes(); break;
            case 1: Draw_linerenderer(); break;
            case 2: Draw_Geometry(); break;
            default:
                break;
        }
    }

    private void HandleIdleState()
    {
        //结束绘制
        if (current_pipe_mesh != null)
        {
            isdrawing = false;//test相关
            current_pipe_mesh.tag = "canDeform";
            current_pipe_mesh.layer = 3;
            if (drawfit)
                Draws_Fitter();
            interactableobj(current_pipe_mesh, "pipe", true);
            current_pipe_mesh = null;
            // 完成一个绘制时清空重做历史
            RedoDraws.Clear();
        }

        if (current_geometry_mesh != null)
        {
            current_geometry_mesh.tag = "canDeform";
            current_geometry_mesh.layer = 3;
            interactableobj(current_geometry_mesh, "geo", true);

            //current_geometry_mesh.AddComponent<Deform_contorl>();

            current_geometry_mesh = null;
        }

        if (current_lineRenderer != null)
        {
            //interactableobj(current_lineRenderer, "linerenderer", true);
            Draws.Add(current_lineRenderer);
            current_lineRenderer = null;
        }

        Ray r = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(r, out hitInfo, 0.1f, 1 << 3))//0.1f是指射线最大检测距离，如果超过0.1f距离没有碰撞，射线将不再检测，防止射线无限延伸,1 << 3只检测第三层
        {
            //Debug.Log("Hit point: " + hitInfo.point);//显示笔的xyz坐标
            //Debug.Log("hitInfo:" + hitInfo.collider.gameObject.name);
            if (drawonmesh)
            {
                if (hitInfo.collider.tag == "Board")
                {


                    // 设置画笔所在位置对应画板图片的UV坐标
                    //Vector3 worldPosition = hitInfo.point;
                    //Debug.Log("Hit point in world coordinates: " + worldPosition);  //测试激光在幕布上的点坐标
                    board.distance = this.GetComponent<PenGrabFreeTransformer>().distance;
                    board.SetPainterPositon(hitInfo.textureCoord.x, hitInfo.textureCoord.y);//hitInfo.textureCoord通常是0到1之间


                    // 当前笔的颜色
                    board.SetPainterColor(currentColor);
                    board.IsDrawing = true;
                    IsGrabbing = true;

                    /*if (!currentDrawing)
                    {
                     int pointsCount = currentDrawing.positionCount;
                    Debug.Log("Number of points in the line: " + pointsCount);
                    }
                    */

                }
                else if (hitInfo.collider.tag == "canDeform")
                {
                    Renderer renderer = hitInfo.collider.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        Texture2D texture = renderer.material.mainTexture as Texture2D;

                        // 如果没有纹理，创建一个新的
                        if (texture == null)
                        {
                            texture = CreateNewTexture(renderer);
                        }

                        if (texture != null)
                        {
                            // 获取碰撞点的UV坐标
                            Vector2 uv = hitInfo.textureCoord;

                            // 将UV坐标转换为像素坐标
                            int x = Mathf.FloorToInt(uv.x * texture.width);
                            int y = Mathf.FloorToInt(uv.y * texture.height);

                            // 定义绘制的颜色和大小
                            Color drawColor = currentColor; // 假设currentColor是当前画笔颜色
                                                            // int brushSize = 5; // 画笔大小，可以根据需要调整

                            // 在纹理上绘制
                            for (int i = -brushSize_drawonmesh; i <= brushSize_drawonmesh; i++)
                            {
                                for (int j = -brushSize_drawonmesh; j <= brushSize_drawonmesh; j++)
                                {
                                    if (i * i + j * j <= brushSize_drawonmesh * brushSize_drawonmesh) // 创建圆形笔刷
                                    {
                                        int drawX = x + i;
                                        int drawY = y + j;
                                        if (drawX >= 0 && drawX < texture.width && drawY >= 0 && drawY < texture.height)
                                        {
                                            texture.SetPixel(drawX, drawY, drawColor);
                                        }
                                    }
                                }
                            }

                            // 应用更改
                            texture.Apply();
                        }
                    }
                }
            }
        }
        else if (IsGrabbing)
        {
            board.IsDrawing = false;
            IsGrabbing = false;
        }
    }
    #endregion

    // 创建新纹理的辅助方法
    private Texture2D CreateNewTexture(Renderer renderer)
    {
        // 创建一个新的纹理
        Texture2D newTexture = new Texture2D(1024, 1024); // 可以根据需要调整大小

        // 用白色填充纹理
        Color[] colors = new Color[newTexture.width * newTexture.height];
        for (int i = 0; i < colors.Length; i++)
        {
            colors[i] = Color.white;
        }
        newTexture.SetPixels(colors);
        newTexture.Apply();

        // 将新纹理应用到材质
        renderer.material.mainTexture = newTexture;

        return newTexture;
    }
    //public List<GameObject> Pipes = new List<GameObject>();
    private void HandleUndoState()
    {
        UndoLastDrawing();
        // 重置双击状态
        isDoubleClicking = false;
    }
    private GameObject current_pipe_mesh;

    private void HandleMenuState()
    {
        OpenMenu();
    }

    private void OpenMenu()
    {
        Vector3 menupos = transform.position + new Vector3(0, menuoffset, menuoffset);
        Menu.transform.position = menupos;
        Menu.transform.LookAt(Camera.main.transform);
        Menu.transform.Rotate(0, 180, 0);

        Menu.SetActive(true);
        Pen_menu.SetActive(true);
        Color_menu.SetActive(false);
    }



    // 修改现有的UndoLastDrawing方法
    public void UndoLastDrawing()
    {
        if (Draws.Count > 0)
        {
            // 获取最后一条线条
            GameObject lastDrawing = Draws[Draws.Count - 1];

            // 从列表中移除最后一条线条
            Draws.RemoveAt(Draws.Count - 1);

            // 将撤销的绘制添加到重做列表
            RedoDraws.Add(lastDrawing);

            // 隐藏而不是销毁最后一条线条的游戏对象
            lastDrawing.SetActive(false);
        }
    }

    // 添加重做方法
    public void RedoLastDrawing()
    {
        if (RedoDraws.Count > 0)
        {
            // 获取最后一个被撤销的绘制
            GameObject lastRedoDrawing = RedoDraws[RedoDraws.Count - 1];

            // 从重做列表中移除
            RedoDraws.RemoveAt(RedoDraws.Count - 1);

            // 将绘制添加回Draws列表
            Draws.Add(lastRedoDrawing);

            // 显示绘制对象
            lastRedoDrawing.SetActive(true);
        }
    }

    #region Linerender方法
    //用Linerender的方法画2D线段
    private void Draw_linerenderer()
    {
        if (current_lineRenderer == null)//这部分确保在没有现有资源的情况下创建新的资源。它使用组件初始化一个新的组件。
        {
            points_index = 0;
            current_lineRenderer = new GameObject("lineRenderer");
            LineRenderer currentDrawing = current_lineRenderer.AddComponent<LineRenderer>();//通过currentDrawing变量控制组件的行为
            currentDrawing.material = drawingMaterial;
            currentDrawing.material.color = currentColor;
            currentDrawing.startColor = currentDrawing.endColor = currentColor;
            currentDrawing.startWidth = currentDrawing.endWidth = penWidth;
            currentDrawing.positionCount = 1;
            currentDrawing.SetPosition(0, tip.position);
            currentDrawing.numCornerVertices = 5; // 控制拐角的平滑程度
            currentDrawing.numCapVertices = 5; // 控制末端的平滑程度
            currentDrawing.useWorldSpace = true; // 使用世界坐标系
        }
        else
        {
            LineRenderer currentDrawing = current_lineRenderer.GetComponent<LineRenderer>();
            var currentPos = currentDrawing.GetPosition(points_index);
            //if (Vector3.Distance(currentPos, tip.position) > 0.01f)
            if (Vector3.Distance(currentPos, tip.position) > draw_interval)//比较平方距离
            {
                points_index++;
                currentDrawing.positionCount = points_index + 1;
                //Debug.Log("xxxxxxxxxx" + currentDrawing.positionCount);
                currentDrawing.SetPosition(points_index, tip.position);
            }
        }
    }
    #endregion

    #region 绘制Geometry方法
    private Vector3 current_pos;
    private Vector3 start_pos;
    public bool useAxisScaling = false; // 新增变量：true为三轴独立缩放，false为等比例缩放

    // 可以提供一个公共方法来切换缩放模式
    public void ToggleScalingMode()
    {
        useAxisScaling = !useAxisScaling;
    }

    private void Draw_Geometry()
    {
        if (current_geometry_mesh == null)
        {
            current_pos = tip.position;
            start_pos = tip.position; // 记录起始位置，用于三轴独立缩放模式
            switch (geometry_mode)
            {
                case 0:
                    {
                        current_geometry_mesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                        Destroy(current_geometry_mesh.GetComponent<SphereCollider>());
                        current_geometry_mesh.AddComponent<MeshCollider>();
                        current_geometry_mesh.name = "sphere";
                        break;
                    }
                case 1:
                    {
                        current_geometry_mesh = GameObject.CreatePrimitive(PrimitiveType.Cube);
                        Destroy(current_geometry_mesh.GetComponent<BoxCollider>());
                        current_geometry_mesh.AddComponent<MeshCollider>();
                        current_geometry_mesh.name = "Cube";
                        break;
                    }
                case 2:
                    {
                        current_geometry_mesh = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                        Destroy(current_geometry_mesh.GetComponent<CapsuleCollider>());
                        current_geometry_mesh.AddComponent<MeshCollider>();
                        current_geometry_mesh.name = "Capsule";
                        break;
                    }
                case 3:
                    {
                        current_geometry_mesh = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        Destroy(current_geometry_mesh.GetComponent<CapsuleCollider>());
                        current_geometry_mesh.AddComponent<MeshCollider>();
                        current_geometry_mesh.name = "Cylinder";
                        break;
                    }
                case 4:
                    {
                        // 创建圆角立方体
                        current_geometry_mesh = new GameObject("RoundedCube");
                        MeshFilter meshFilter = current_geometry_mesh.AddComponent<MeshFilter>();
                        MeshRenderer meshRenderer = current_geometry_mesh.AddComponent<MeshRenderer>();

                        // 标准化尺寸，使其与Unity标准几何体一致
                        // 将尺寸设为1，圆角适当调整
                        xSize = ySize = zSize = 1;
                        roundness = 0.25f; // 适当的圆角值，可调整

                        // 生成圆角立方体网格
                        Mesh roundedCubeMesh = GenerateRoundedCubeMesh();

                        meshFilter.mesh = roundedCubeMesh;
                        current_geometry_mesh.AddComponent<MeshCollider>().sharedMesh = roundedCubeMesh;
                        current_geometry_mesh.name = "RoundedCube";
                        break;
                    }
            }

            current_geometry_mesh.transform.localScale = new Vector3(0.01f, 0.01f, 0.01f);
            MeshRenderer mr = current_geometry_mesh.GetComponent<MeshRenderer>();
            mr.material = drawingMaterial;
            mr.material.color = currentColor;
            current_geometry_mesh.transform.position = current_pos;
        }
        else
        {
            if (Vector3.Distance(current_pos, tip.position) > 0.01f)
            {
                if (useAxisScaling)
                {
                    // 三轴独立缩放模式
                    // 计算从起始点到当前笔尖的差值向量
                    Vector3 diff = tip.position - start_pos;

                    // 计算每个轴向上的尺寸（取绝对值确保为正值）
                    float xSize = Mathf.Abs(diff.x);
                    float ySize = Mathf.Abs(diff.y);
                    float zSize = Mathf.Abs(diff.z);

                    // 确保最小尺寸
                    xSize = Mathf.Max(xSize, 0.01f);
                    ySize = Mathf.Max(ySize, 0.01f);
                    zSize = Mathf.Max(zSize, 0.01f);

                    // 设置几何体的大小（在每个轴向上独立缩放）
                    current_geometry_mesh.transform.localScale = new Vector3(xSize, ySize, zSize);

                    // 更新几何体位置为起始点和当前点的中间
                    current_geometry_mesh.transform.position = (start_pos + tip.position) / 2;
                }
                else
                {
                    // 原始的等比例缩放模式
                    // 计算两点间的距离
                    float distance = Vector3.Distance(current_geometry_mesh.transform.position, tip.position);
                    // 半径是距离的一半
                    float radius = Mathf.Clamp(distance, 0.01f, 0.35f);
                    // 设置几何体的大小
                    current_geometry_mesh.transform.localScale = new Vector3(2 * radius, 2 * radius, 2 * radius);
                }

                // 更新当前位置
                current_pos = tip.position;
            }
        }
    }

    // 以下是圆角立方体相关方法
    // 修改类型为float以支持更精细的控制
    public float xSize = 1f, ySize = 1f, zSize = 1f;
    public float roundness = 0.25f;
    private Vector3[] vertices_cube;
    private Vector3[] normals;
    private Color32[] cubeUV;

    private Mesh GenerateRoundedCubeMesh()
    {
        Mesh mesh = new Mesh();
        mesh.name = "Procedural Rounded Cube";
        CreateRoundedCubeVertices(mesh);
        //CenterMesh(mesh); // 居中网格
        CreateRoundedCubeTriangles(mesh);
        return mesh;
    }

    private void CenterMesh(Mesh mesh)
    {
        // 计算网格的中心点
        Vector3 center = Vector3.zero;
        foreach (Vector3 vertex in vertices_cube)
        {
            center += vertex;
        }
        center /= vertices_cube.Length;

        // 将所有顶点向负中心点方向平移，使网格居中
        for (int i = 0; i < vertices_cube.Length; i++)
        {
            vertices_cube[i] -= center;
        }

        // 更新网格顶点
        mesh.vertices = vertices_cube;
    }

    private void CreateRoundedCubeVertices(Mesh mesh)
    {
        // 网格分辨率 - 较高的值会使网格更平滑但更复杂
        int resolution = 6;
        int cornerVertices = 8;
        int edgeVertices = (resolution + resolution + resolution - 3) * 4;
        int faceVertices = (
            (resolution - 1) * (resolution - 1) +
            (resolution - 1) * (resolution - 1) +
            (resolution - 1) * (resolution - 1)) * 2;
        vertices_cube = new Vector3[cornerVertices + edgeVertices + faceVertices];
        normals = new Vector3[vertices_cube.Length];
        cubeUV = new Color32[vertices_cube.Length];

        int v = 0;
        // 创建顶点时使用标准化的坐标 (0到1)
        for (int y = 0; y <= resolution; y++)
        {
            float yCoord = (float)y / resolution * ySize; // 转换为0到ySize范围
            for (int x = 0; x <= resolution; x++)
            {
                float xCoord = (float)x / resolution * xSize; // 转换为0到xSize范围
                SetRoundedCubeVertex(v++, xCoord, yCoord, 0);
            }
            for (int z = 1; z <= resolution; z++)
            {
                float zCoord = (float)z / resolution * zSize; // 转换为0到zSize范围
                SetRoundedCubeVertex(v++, xSize, yCoord, zCoord);
            }
            for (int x = resolution - 1; x >= 0; x--)
            {
                float xCoord = (float)x / resolution * xSize;
                SetRoundedCubeVertex(v++, xCoord, yCoord, zSize);
            }
            for (int z = resolution - 1; z > 0; z--)
            {
                float zCoord = (float)z / resolution * zSize;
                SetRoundedCubeVertex(v++, 0, yCoord, zCoord);
            }
        }
        for (int z = 1; z < resolution; z++)
        {
            float zCoord = (float)z / resolution * zSize;
            for (int x = 1; x < resolution; x++)
            {
                float xCoord = (float)x / resolution * xSize;
                SetRoundedCubeVertex(v++, xCoord, ySize, zCoord);
            }
        }
        for (int z = 1; z < resolution; z++)
        {
            float zCoord = (float)z / resolution * zSize;
            for (int x = 1; x < resolution; x++)
            {
                float xCoord = (float)x / resolution * xSize;
                SetRoundedCubeVertex(v++, xCoord, 0, zCoord);
            }
        }

        mesh.vertices = vertices_cube;
        mesh.normals = normals;
        mesh.colors32 = cubeUV;
    }

    private void SetRoundedCubeVertex(int i, float x, float y, float z)
    {
        // 使用浮点数进行精确计算
        Vector3 inner = vertices_cube[i] = new Vector3(x, y, z);

        if (x < roundness)
        {
            inner.x = roundness;
        }
        else if (x > xSize - roundness)
        {
            inner.x = xSize - roundness;
        }
        if (y < roundness)
        {
            inner.y = roundness;
        }
        else if (y > ySize - roundness)
        {
            inner.y = ySize - roundness;
        }
        if (z < roundness)
        {
            inner.z = roundness;
        }
        else if (z > zSize - roundness)
        {
            inner.z = zSize - roundness;
        }

        normals[i] = (vertices_cube[i] - inner).normalized;
        vertices_cube[i] = inner + normals[i] * roundness;

        // 颜色值仍然使用byte，但归一化坐标
        cubeUV[i] = new Color32((byte)(x / xSize * 255), (byte)(y / ySize * 255), (byte)(z / zSize * 255), 0);
    }

    private void CreateRoundedCubeTriangles(Mesh mesh)
    {
        // 使用分辨率作为网格尺寸
        int resolution = 6;
        int[] trianglesZ = new int[(resolution * resolution) * 12];
        int[] trianglesX = new int[(resolution * resolution) * 12];
        int[] trianglesY = new int[(resolution * resolution) * 12];
        int ring = (resolution + resolution) * 2;
        int tZ = 0, tX = 0, tY = 0, v = 0;

        for (int y = 0; y < resolution; y++, v++)
        {
            for (int q = 0; q < resolution; q++, v++)
            {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < resolution; q++, v++)
            {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < resolution; q++, v++)
            {
                tZ = SetQuad(trianglesZ, tZ, v, v + 1, v + ring, v + ring + 1);
            }
            for (int q = 0; q < resolution - 1; q++, v++)
            {
                tX = SetQuad(trianglesX, tX, v, v + 1, v + ring, v + ring + 1);
            }
            tX = SetQuad(trianglesX, tX, v, v - ring + 1, v + ring, v + 1);
        }

        tY = CreateTopFace(trianglesY, tY, ring, resolution);
        tY = CreateBottomFace(trianglesY, tY, ring, resolution);

        // 合并所有三角形为一个数组
        int[] triangles = new int[trianglesX.Length + trianglesY.Length + trianglesZ.Length];
        System.Array.Copy(trianglesZ, 0, triangles, 0, trianglesZ.Length);
        System.Array.Copy(trianglesX, 0, triangles, trianglesZ.Length, trianglesX.Length);
        System.Array.Copy(trianglesY, 0, triangles, trianglesZ.Length + trianglesX.Length, trianglesY.Length);

        // 只设置一个子网格（使用一个材质）
        mesh.triangles = triangles;
    }

    private int CreateTopFace(int[] triangles, int t, int ring, int resolution)
    {
        int v = ring * resolution;
        for (int x = 0; x < resolution - 1; x++, v++)
        {
            t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + ring);
        }
        t = SetQuad(triangles, t, v, v + 1, v + ring - 1, v + 2);

        int vMin = ring * (resolution + 1) - 1;
        int vMid = vMin + 1;
        int vMax = v + 2;

        for (int z = 1; z < resolution - 1; z++, vMin--, vMid++, vMax++)
        {
            t = SetQuad(triangles, t, vMin, vMid, vMin - 1, vMid + resolution - 1);
            for (int x = 1; x < resolution - 1; x++, vMid++)
            {
                t = SetQuad(
                    triangles, t,
                    vMid, vMid + 1, vMid + resolution - 1, vMid + resolution);
            }
            t = SetQuad(triangles, t, vMid, vMax, vMid + resolution - 1, vMax + 1);
        }

        int vTop = vMin - 2;
        t = SetQuad(triangles, t, vMin, vMid, vTop + 1, vTop);
        for (int x = 1; x < resolution - 1; x++, vTop--, vMid++)
        {
            t = SetQuad(triangles, t, vMid, vMid + 1, vTop, vTop - 1);
        }
        t = SetQuad(triangles, t, vMid, vTop - 2, vTop, vTop - 1);

        return t;
    }

    private int CreateBottomFace(int[] triangles, int t, int ring, int resolution)
    {
        int v = 1;
        int vMid = vertices_cube.Length - (resolution - 1) * (resolution - 1);
        t = SetQuad(triangles, t, ring - 1, vMid, 0, 1);
        for (int x = 1; x < resolution - 1; x++, v++, vMid++)
        {
            t = SetQuad(triangles, t, vMid, vMid + 1, v, v + 1);
        }
        t = SetQuad(triangles, t, vMid, v + 2, v, v + 1);

        int vMin = ring - 2;
        vMid -= resolution - 2;
        int vMax = v + 2;

        for (int z = 1; z < resolution - 1; z++, vMin--, vMid++, vMax++)
        {
            t = SetQuad(triangles, t, vMin, vMid + resolution - 1, vMin + 1, vMid);
            for (int x = 1; x < resolution - 1; x++, vMid++)
            {
                t = SetQuad(
                    triangles, t,
                    vMid + resolution - 1, vMid + resolution, vMid, vMid + 1);
            }
            t = SetQuad(triangles, t, vMid + resolution - 1, vMax + 1, vMid, vMax);
        }

        int vTop = vMin - 1;
        t = SetQuad(triangles, t, vTop + 1, vTop, vTop + 2, vMid);
        for (int x = 1; x < resolution - 1; x++, vTop--, vMid++)
        {
            t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vMid + 1);
        }
        t = SetQuad(triangles, t, vTop, vTop - 1, vMid, vTop - 2);

        return t;
    }

    private static int SetQuad(int[] triangles, int i, int v00, int v10, int v01, int v11)
    {
        triangles[i] = v00;
        triangles[i + 1] = triangles[i + 4] = v01;
        triangles[i + 2] = triangles[i + 3] = v10;
        triangles[i + 5] = v11;
        return i + 6;
    }
    #endregion

    #region 3Dpipe方法
    //3Dpipe生成
    private void Draw_Pipes()
    {
        if (!isdrawing) isdrawing = true;
        if (current_pipe_mesh == null)
        {
            //points_index = 0;
            current_pipe_mesh = new GameObject("PipeMesh");
            MeshFilter mf = current_pipe_mesh.AddComponent<MeshFilter>();
            MeshRenderer mr = current_pipe_mesh.AddComponent<MeshRenderer>();
            mr.material = drawingMaterial;
            mr.material.color = currentColor;
            currentMesh = new Mesh();
            mf.mesh = currentMesh;

            pipe_ppos.Clear(); // 清除之前的位置
            pipe_ppos.Add(tip.position);
            //current_Pipe = LineCreateOf3D.CreateLine(pipe_ppos.ToArray(), pipe_mat, Segments, Radius, elbow_Radius);
            //Pipes.Add(current_Pipe);
        }
        else
        {
            var currentPos = pipe_ppos[pipe_ppos.Count - 1];
            //Debug.Log("tip:" + tip.position);
            if (Vector3.Distance(currentPos, tip.position) > draw_interval)
            {
                pipe_ppos.Add(tip.position);
                Vector3[] points = pipe_ppos.ToArray();
                //current_Pipe.GetComponent<MeshFilter>().mesh.Clear();
                // if (current_Pipe)
                //     Destroy(current_Pipe);
                current_pipe_mesh = LineCreateOf3D.CreateLine(current_pipe_mesh, points, Segments, Radius, elbow_Radius);
            }
        }
    }

    //给test的重载
    public void Draw_Pipes(Transform tip)
    {
        if (current_pipe_mesh == null)
        {
            //points_index = 0;
            current_pipe_mesh = new GameObject("PipeMesh");
            MeshFilter mf = current_pipe_mesh.AddComponent<MeshFilter>();
            MeshRenderer mr = current_pipe_mesh.AddComponent<MeshRenderer>();
            mr.material = drawingMaterial;
            mr.material.color = currentColor;
            currentMesh = new Mesh();
            mf.mesh = currentMesh;

            pipe_ppos.Clear(); // 清除之前的位置
            pipe_ppos.Add(tip.position);
            //current_Pipe = LineCreateOf3D.CreateLine(pipe_ppos.ToArray(), pipe_mat, Segments, Radius, elbow_Radius);
            //Pipes.Add(current_Pipe);
        }
        else
        {
            var currentPos = pipe_ppos[pipe_ppos.Count - 1];
            //Debug.Log("tip:" + tip.position);
            if (Vector3.Distance(currentPos, tip.position) > draw_interval)
            {
                pipe_ppos.Add(tip.position);
                Vector3[] points = pipe_ppos.ToArray();
                //current_Pipe.GetComponent<MeshFilter>().mesh.Clear();
                // if (current_Pipe)
                //     Destroy(current_Pipe);
                current_pipe_mesh = LineCreateOf3D.CreateLine(current_pipe_mesh, points, Segments, Radius, elbow_Radius);
            }
        }
    }

    public void stoppipe()
    {
        if (current_pipe_mesh != null)
        {
            current_pipe_mesh.tag = "canDeform";
            current_pipe_mesh.layer = 3;
            if (drawfit)
                Draws_Fitter();
            //interactableobj(current_pipe_mesh, "pipe", true);
            current_pipe_mesh = null;
        }
    }

    public void Draws_Fitter()
    {
        //尝试拟合圆
        (bool shouldcircle, Vector3 center, float radius, Vector3 normal) = fitter.ShouldCreateCircle(pipe_ppos);
        //EditorApplication.isPaused = true;
        if (shouldcircle)
        {
            // 创建一个新的、规则的圆
            int numPoints = 25;  // 圆上的点数
            Vector3[] circlePoints = new Vector3[numPoints + 1];

            // 计算圆的方向
            Vector3 direction = (pipe_ppos[pipe_ppos.Count - 1] - pipe_ppos[0]).normalized;

            // 确保normal与direction的方向一致
            if (Vector3.Dot(normal, direction) < 0)
            {
                normal = -normal;
            }

            // 创建一个正交基
            Vector3 tangent = Vector3.Cross(normal, direction).normalized;
            Vector3 bitangent = Vector3.Cross(normal, tangent);

            for (int i = 0; i < numPoints; i++)
            {
                float angle = 2 * Mathf.PI * i / numPoints;
                Vector3 pointOnCircle = center + (tangent * Mathf.Cos(angle) + bitangent * Mathf.Sin(angle)) * radius;
                circlePoints[i] = pointOnCircle;
            }
            circlePoints[numPoints] = circlePoints[0];



            // 使用这些新的点来创建管道
            current_pipe_mesh = LineCreateOf3D.CreateLine(current_pipe_mesh, circlePoints, Segments, Radius, elbow_Radius);
        }
        // else if()
        // {

        // }
        else if (fitter.IsConsistentLine(pipe_ppos, out Vector3 direction))
        {
            // 如果点云数据近似直线，直接使用输入的起始点和终点
            Vector3 startPoint = pipe_ppos[0];
            Vector3 endPoint = pipe_ppos[pipe_ppos.Count - 1];
            Vector3[] linePoints = new Vector3[] { startPoint, endPoint };
            current_pipe_mesh = LineCreateOf3D.CreateLine(current_pipe_mesh, linePoints, Segments, Radius, elbow_Radius);
        }
        // else
        // {// 仍然使用原来的方法
        //     //current_pipe_mesh = LineCreateOf3D.CreateLine(current_pipe_mesh, pipe_ppos.ToArray(), Segments, Radius, elbow_Radius);
        // }


    }
    public GameObject interactableobj(GameObject Mesh, string name, bool draw)
    {
        GameObject Visuals = new GameObject("Visuals");
        GameObject grabInteractable = new GameObject("GrabInteractable");
        GameObject handGrabInteractable = new GameObject("HandGrabInteractable");
        GameObject obj = new GameObject(name);
        obj.tag = "canScale";

        Mesh.transform.parent = Visuals.transform;
        if (name == "pipe")
            Mesh.AddComponent<MeshCollider>();

        Visuals.transform.parent = obj.transform;
        grabInteractable.transform.parent = obj.transform;
        handGrabInteractable.transform.parent = obj.transform;

        Rigidbody rigidbody = obj.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;


        TwoGrabFreeTransformer twoGrabFreeTransformer = obj.AddComponent<TwoGrabFreeTransformer>();
        obj.AddComponent<OneGrabFreeTransformer>();
        // 创建一个新的 TwoGrabFreeConstraints 实例
        TwoGrabFreeTransformer.TwoGrabFreeConstraints constraints = new TwoGrabFreeTransformer.TwoGrabFreeConstraints
        {
            ConstraintsAreRelative = true, // 设置为 true 或 false
            MinScale = new FloatConstraint
            {
                Constrain = true,
                Value = 0.5f // 最小缩放值
            },
            MaxScale = new FloatConstraint
            {
                Constrain = true,
                Value = 2.0f // 最大缩放值
            }
        };
        twoGrabFreeTransformer.InjectOptionalConstraints(constraints);
        Grabbable grabbable = obj.AddComponent<Grabbable>();
        grabbable.Reset();

        PhysicsGrabbable physicsGrabbable = obj.AddComponent<PhysicsGrabbable>();
        physicsGrabbable.Reset();

        GrabInteractable grabInteractable1 = grabInteractable.AddComponent<GrabInteractable>();
        grabInteractable1.Reset0();
        HandGrabInteractable handGrabInteractable1 = handGrabInteractable.AddComponent<HandGrabInteractable>();
        handGrabInteractable1.Reset0();
        InteractableGroupView interactableGroupView = Visuals.AddComponent<InteractableGroupView>();
        interactableGroupView.Reset0();
        MaterialPropertyBlockEditor materialPropertyBlockEditor = Visuals.AddComponent<MaterialPropertyBlockEditor>();
        materialPropertyBlockEditor.Reset();
        InteractableColorVisual interactableColorVisual = Visuals.AddComponent<InteractableColorVisual>();
        interactableColorVisual.Reset();

        if (draw)
        {
            Draws.Add(obj);
        }
        obj.transform.parent = currentlayer.transform;
        return obj;
    }

    public GameObject interactableobjs(List<GameObject> Meshlist, string name, bool draw)
    {
        GameObject Visuals = new GameObject("Visuals");
        GameObject grabInteractable = new GameObject("GrabInteractable");
        GameObject handGrabInteractable = new GameObject("HandGrabInteractable");
        GameObject obj = new GameObject(name);
        obj.tag = "canScale";

        foreach (GameObject obj0 in Meshlist)
        {
            GameObject parent = obj0.transform.parent.parent.gameObject;
            obj0.transform.parent = Visuals.transform;
            Destroy(parent);
            if (name == "pipe")
                obj0.AddComponent<MeshCollider>();
        }

        Visuals.transform.parent = obj.transform;
        grabInteractable.transform.parent = obj.transform;
        handGrabInteractable.transform.parent = obj.transform;

        Rigidbody rigidbody = obj.AddComponent<Rigidbody>();
        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;


        TwoGrabFreeTransformer twoGrabFreeTransformer = obj.AddComponent<TwoGrabFreeTransformer>();
        obj.AddComponent<OneGrabFreeTransformer>();
        // 创建一个新的 TwoGrabFreeConstraints 实例
        TwoGrabFreeTransformer.TwoGrabFreeConstraints constraints = new TwoGrabFreeTransformer.TwoGrabFreeConstraints
        {
            ConstraintsAreRelative = true, // 设置为 true 或 false
            MinScale = new FloatConstraint
            {
                Constrain = true,
                Value = 0.5f // 最小缩放值
            },
            MaxScale = new FloatConstraint
            {
                Constrain = true,
                Value = 2.0f // 最大缩放值
            }
        };
        twoGrabFreeTransformer.InjectOptionalConstraints(constraints);
        Grabbable grabbable = obj.AddComponent<Grabbable>();
        grabbable.Reset();

        PhysicsGrabbable physicsGrabbable = obj.AddComponent<PhysicsGrabbable>();
        physicsGrabbable.Reset();

        GrabInteractable grabInteractable1 = grabInteractable.AddComponent<GrabInteractable>();
        grabInteractable1.Reset0();
        HandGrabInteractable handGrabInteractable1 = handGrabInteractable.AddComponent<HandGrabInteractable>();
        handGrabInteractable1.Reset0();
        // InteractableGroupView interactableGroupView = Visuals.AddComponent<InteractableGroupView>();
        // interactableGroupView.Reset0();
        // MaterialPropertyBlockEditor materialPropertyBlockEditor = Visuals.AddComponent<MaterialPropertyBlockEditor>();
        // materialPropertyBlockEditor.Reset();
        // InteractableColorVisual interactableColorVisual = Visuals.AddComponent<InteractableColorVisual>();
        // interactableColorVisual.Reset();

        if (draw)
        {
            Draws.Add(obj);
        }
        obj.transform.parent = currentlayer.transform;
        return obj;
    }

    #endregion

    #region 自己创建mesh画平面线段
    //自己创建mesh画2D线段
    private void Draw_Line2D()
    {
        if (current_Line2D == null)
        {
            CreateNewMesh();
        }

        Vector3 newPosition = tip.position;
        if (vertices.Count == 0 || Vector3.Distance(lastPosition, newPosition) > 0.01f)
        {
            AddVertex(newPosition);
            Vpositions.Add(newPosition);
            lastPosition = newPosition;
        }
    }

    void CreateNewMesh()
    {
        current_Line2D = new GameObject("LineMesh");
        MeshFilter mf = current_Line2D.AddComponent<MeshFilter>();
        MeshRenderer mr = current_Line2D.AddComponent<MeshRenderer>();
        mr.material = drawingMaterial;
        mr.material.color = currentColor;

        currentMesh = new Mesh();
        mf.mesh = currentMesh;

        vertices.Clear();
        indices.Clear();
        Lines2D.Add(current_Line2D);
    }

    void AddVertex(Vector3 position)
    {
        vertices.Add(position + Vector3.forward * penWidth); // Right offset
        vertices.Add(position - Vector3.forward * penWidth); // Left offset

        if (vertices.Count >= 4)
        {
            int vCount = vertices.Count;
            indices.Add(vCount - 4);
            indices.Add(vCount - 3);
            indices.Add(vCount - 2);

            indices.Add(vCount - 3);
            indices.Add(vCount - 1);
            indices.Add(vCount - 2);
        }

        currentMesh.Clear();
        currentMesh.vertices = vertices.ToArray();
        currentMesh.triangles = indices.ToArray();
        currentMesh.RecalculateNormals();
    }
    #endregion


    #region 切换笔的颜色
    //切换笔的颜色
    private void SwitchColor()
    {
        // if (currentColorIndex == penColors.Length - 1)
        // {
        //     currentColorIndex = 0;
        // }
        // else
        // {
        //     currentColorIndex++;
        // }
        //将画笔部件设置为画笔的颜色，用于识别这个画笔的颜色
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.transform == transform)
            {
                continue;
            }
            if (renderer.tag == "render")
                renderer.material.color = currentColor;
        }
    }

    //切换笔的颜色
    public void MenuSwitchColor(Color Color)
    {
        currentColor = Color;
        //将画笔部件设置为画笔的颜色，用于识别这个画笔的颜色
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.transform == transform)
            {
                continue;
            }
            if (renderer.tag == "render")
            {
                renderer.material.color = currentColor;
            }
        }
    }

    #endregion


}