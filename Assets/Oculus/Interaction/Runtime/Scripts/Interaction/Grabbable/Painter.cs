using Oculus.Interaction;
using UnityEngine;

public class Painter : MonoBehaviour
{
    /// <summary>
    /// 画笔的颜色
    /// </summary>
    public Color32 penColor;

    public Transform rayOrigin;

    private RaycastHit hitInfo;
    //这个画笔是不是正在被手柄抓着
    private bool IsGrabbing;
    public Transform pentip;
    public Board board;//设置成类型的成员，而不是类型实例的成员，因为所有画笔都是用的同一个board
    private static Grabbable pengrab;

    private void Start()
    {
        //将画笔部件设置为画笔的颜色，用于识别这个画笔的颜色
        foreach (var renderer in GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.transform == transform)
            {
                continue;
            }
            if(renderer.tag == "render")
            renderer.material.color = penColor;
        }
        if (!board)
        {
            board = FindObjectOfType<Board>();
        }

        if (!pengrab)
        {
            pengrab = this.GetComponent<Grabbable>();
        }
      
    }

    private void Update()
    {
        Ray r = new Ray(rayOrigin.position, rayOrigin.forward);
        if (Physics.Raycast(r, out hitInfo, 0.1f))
        {
            if (hitInfo.collider.tag == "Board")
            {
                //设置画笔所在位置对应画板图片的UV坐标 
                board.SetPainterPositon(hitInfo.textureCoord.x, hitInfo.textureCoord.y);
                //当前笔的颜色
                board.SetPainterColor(penColor);
                board.IsDrawing = true;
                IsGrabbing = true;
            }
        }
        else if(IsGrabbing)
        {
            board.IsDrawing = false;
            IsGrabbing = false;
        }
        //Debug.Log(board.GetSideOfBoardPlane(pentip.position));
        //ProcessFixedUpdate();
    }

    public void ProcessFixedUpdate()
    {
        //Debug.Log(pengrab.isgrab());
        if (pengrab.isGrabbed()=="pen")//只有抓住物体后，grabbedObject才不会
        {
            float distance = board.GetDistanceFromBoardPlane(pentip.position);//笔尖距离平面的距离
            bool isPositiveOfBoardPlane = board.GetSideOfBoardPlane(pentip.position);//笔尖是不是在笔尖的正面
            Vector3 direction = this.transform.position - pentip.position;//笔尖位置指向笔的位置的差向量
            //当笔尖穿透的时候，需要矫正笔的位置 
            if (isPositiveOfBoardPlane || distance > 0.0001f)
            {
                Vector3 pos = board.ProjectPointOnBoardPlane(pentip.position);
                this.transform.position = pos - board.boardPlane.normal * 0.001f + direction;//pos是笔尖的位置，而不是笔的位置，加上direction后才是笔的位置 
            }
        }
    }

    

}
