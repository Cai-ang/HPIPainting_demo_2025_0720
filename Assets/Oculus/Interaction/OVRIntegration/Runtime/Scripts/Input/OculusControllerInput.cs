using UnityEngine;
using System.Collections;

public class OculusControllerInput : Pen_3D
{
    void Start()
    {
        currentColor = Color.blue;
    }
    void Update()
    {
        // 获取扳机(Trigger)输入
        // 模拟值(0-1)
        float rightTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        float leftTriggerValue = OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        // 按下状态(布尔值)
        bool rightTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch);
        bool leftTriggerPressed = OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.LTouch);

        // // 获取握把(Grip)按钮
        // float rightGripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        // float leftGripValue = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.LTouch);

        // bool rightGripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch);
        // bool leftGripPressed = OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.LTouch);

        // 获取A/B按钮(右手控制器)
        bool buttonAPressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.RTouch);
        bool buttonBPressed = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.RTouch);

        // 获取X/Y按钮(左手控制器)
        bool buttonXPressed = OVRInput.Get(OVRInput.Button.One, OVRInput.Controller.LTouch);
        bool buttonYPressed = OVRInput.Get(OVRInput.Button.Two, OVRInput.Controller.LTouch);

        // // 获取拇指摇杆按下状态
        // bool rightThumbstickPressed = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.RTouch);
        // bool leftThumbstickPressed = OVRInput.Get(OVRInput.Button.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // // 获取拇指摇杆的方向值(二维向量)
        // Vector2 rightThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        // Vector2 leftThumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // // 获取菜单按钮(通常在左手控制器)
        // bool menuButtonPressed = OVRInput.Get(OVRInput.Button.Start, OVRInput.Controller.LTouch);

        // // 检测拇指接触到摇杆或按钮上方(不一定按下)
        // bool rightThumbRest = OVRInput.Get(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.RTouch);
        // bool leftThumbRest = OVRInput.Get(OVRInput.Touch.PrimaryThumbRest, OVRInput.Controller.LTouch);

        // // 获取触摸状态(拇指是否触摸在摇杆上)
        // bool rightThumbstickTouch = OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.RTouch);
        // bool leftThumbstickTouch = OVRInput.Get(OVRInput.Touch.PrimaryThumbstick, OVRInput.Controller.LTouch);

        // 检测按钮Down事件(当前帧按下)
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Debug.Log("A按钮刚刚被按下");
            currentColor = currentColor == Color.red ? Color.blue : Color.red;
            this.GetComponent<Renderer>().material.color = currentColor;
        }

        // 检测按钮Up事件(当前帧释放)
        if (OVRInput.GetUp(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            Debug.Log("A按钮刚刚被释放");
        }

        // 示例: 如何使用这些输入
        if (rightTriggerPressed)
        {
            Debug.Log($"右手扳机被按下，力度: {rightTriggerValue}");
            if (rightTriggerValue > 0.7f)
            {
                Draw_Pipes(this.transform);
            }
        }
        else
        {
            stoppipe();
        }

        // if (buttonAPressed)
        // {
        //     Debug.Log("A按钮被按下");
        // }

        // if (rightThumbstick.magnitude > 0.5f)
        // {
        //     Debug.Log($"右手摇杆方向: ({rightThumbstick.x}, {rightThumbstick.y})");
        // }
    }

    // 可选: 获取控制器振动反馈
    public void TriggerHapticFeedback(float intensity, float duration, OVRInput.Controller controller)
    {
        OVRInput.SetControllerVibration(1.0f, intensity, controller);

        // 使用协程来停止振动
        StartCoroutine(StopHapticFeedback(duration, controller));
    }

    private IEnumerator StopHapticFeedback(float duration, OVRInput.Controller controller)
    {
        yield return new WaitForSeconds(duration);
        OVRInput.SetControllerVibration(0, 0, controller);
    }

}