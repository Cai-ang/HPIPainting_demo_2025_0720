using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AffectDetector : MonoBehaviour
{
    public Menu_manager menu;
    // Start is called before the first frame update
    void Start()
    {
        menu = GameObject.Find("Menu").GetComponent<Menu_manager>();
        this.gameObject.SetActive(false);
    }
    private void OnTriggerEnter(Collider other)
    {
        //Debug.Log(gameObject.name + " was triggered by " + other.gameObject.name);
        // 在这里添加你想要执行的操作
        //Debug.Log(other.gameObject.name);
        if (other.gameObject.tag == "canDeform")
        {
            int mode = menu.Affect(other.gameObject);
            if (mode != 5 && mode != 6)
            {
                menu.affect.isOn = false;
                this.gameObject.SetActive(false);
            }
        }
    }

    // private void OnTriggerStay(Collider other)
    // {
    //     Debug.Log(gameObject.name + " is still triggered by " + other.gameObject.name);
    //     // 这个方法会在触发器重叠期间的每一帧被调用
    // }

    // private void OnTriggerExit(Collider other)
    // {
    //     Debug.Log(other.gameObject.name + " has exited the trigger of " + gameObject.name);
    //     // 在这里添加物体离开触发器时要执行的操作
    // }
}
