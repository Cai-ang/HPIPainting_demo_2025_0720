using UnityEngine;
public class FingerPinchDetector : Pen_3D
{
    [Header("手指物体引用")]
    public Transform thumbObject;    // 拇指物体
    public Transform indexObject;    // 食指物体
    public Transform middleObject;   // 中指物体

    [Header("捏合参数")]
    [Tooltip("判定为捏合状态的距离阈值")]
    public float pinchThreshold = 0.02f;  // 3厘米距离内视为捏合

    [Header("状态信息")]
    [SerializeField] private bool isThumbIndexPinching = false;  // 拇指和食指捏合状态
    [SerializeField] private bool isThumbMiddlePinching = false; // 拇指和中指捏合状态

    // 公开属性供其他脚本访问捏合状态
    public bool IsThumbIndexPinching => isThumbIndexPinching;
    public bool IsThumbMiddlePinching => isThumbMiddlePinching;

    // 事件委托，当捏合状态改变时触发
    public delegate void PinchStateChangeHandler(bool isPinching);
    public event PinchStateChangeHandler OnThumbIndexPinchStateChanged;
    public event PinchStateChangeHandler OnThumbMiddlePinchStateChanged;

    void Start()
    {
        if (!testing)
        {
            this.gameObject.SetActive(false);
        }
        currentColor = Color.blue;
    }
    void Update()
    {
        if (testing)
        {
            // 确保所有手指物体都被正确赋值
            if (thumbObject == null || indexObject == null || middleObject == null)
            {
                Debug.LogWarning("请先设置手指物体引用！");
                return;
            }

            // 计算拇指与食指间的距离
            float thumbIndexDistance = Vector3.Distance(thumbObject.position, indexObject.position);

            // 计算拇指与中指间的距离
            float thumbMiddleDistance = Vector3.Distance(thumbObject.position, middleObject.position);

            // 检测拇指-食指捏合状态变化
            bool newThumbIndexState = thumbIndexDistance < pinchThreshold;
            if (newThumbIndexState != isThumbIndexPinching)
            {
                isThumbIndexPinching = newThumbIndexState;
                OnThumbIndexPinchStateChanged?.Invoke(isThumbIndexPinching);

                if (isThumbIndexPinching)
                {
                    Debug.Log("拇指和食指开始捏合");
                    currentColor = Color.blue;
                    this.GetComponent<Material>().color = currentColor;
                }
                else
                {
                    Debug.Log("拇指和食指结束捏合");
                    stoppipe();
                }
            }

            // 检测拇指-中指捏合状态变化
            bool newThumbMiddleState = thumbMiddleDistance < pinchThreshold;
            if (newThumbMiddleState != isThumbMiddlePinching)
            {
                isThumbMiddlePinching = newThumbMiddleState;
                OnThumbMiddlePinchStateChanged?.Invoke(isThumbMiddlePinching);

                if (isThumbMiddlePinching)
                {
                    Debug.Log("拇指和中指开始捏合");
                    // 如果currentColor是红色，则变为蓝色，否则变为红色
                    // currentColor = currentColor == Color.red ? Color.blue : Color.red;
                    currentColor = Color.red;
                    this.GetComponent<Renderer>().material.color = currentColor;
                }
                else
                {
                    Debug.Log("拇指和中指结束捏合");
                    stoppipe();
                    UndoLastDrawing();
                }
            }
            if (isThumbIndexPinching)
            {
                //Debug.Log("finger:" + this.transform.position);
                Draw_Pipes(this.transform);
            }
            else if (isThumbMiddlePinching)
            {
                //Debug.Log("finger:" + this.transform.position);
                Draw_Pipes(this.transform);
            }
        }
    }

    // 可视化捏合阈值和手指位置（在Scene视图中显示）
    // private void OnDrawGizmos()
    // {
    //     if (thumbObject == null || indexObject == null || middleObject == null)
    //         return;

    //     // 拇指位置
    //     Gizmos.color = Color.red;
    //     Gizmos.DrawSphere(thumbObject.position, 0.01f);

    //     // 食指位置
    //     Gizmos.color = Color.green;
    //     Gizmos.DrawSphere(indexObject.position, 0.01f);

    //     // 中指位置
    //     Gizmos.color = Color.blue;
    //     Gizmos.DrawSphere(middleObject.position, 0.01f);

    //     // 显示捏合阈值
    //     Gizmos.color = isThumbIndexPinching ? Color.yellow : Color.gray;
    //     Gizmos.DrawLine(thumbObject.position, indexObject.position);

    //     Gizmos.color = isThumbMiddlePinching ? Color.yellow : Color.gray;
    //     Gizmos.DrawLine(thumbObject.position, middleObject.position);
    // }
}