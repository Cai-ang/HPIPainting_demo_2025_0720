using UnityEngine;
using EzySlice;

public class splitter : MonoBehaviour
{
    public Material cross;//材质
    private Pen_3D pen;
    private string penprestate;
    void Awake()
    {
        pen = transform.parent.GetComponent<Pen_3D>();
    }
    void Start()
    {
        penprestate = pen.penState;
    }
    void Update()
    {
        if (penprestate != pen.penState && pen.penState == "All")
        {
            Slice();
            penprestate = pen.penState;
        }
    }
    void Slice()
    {
        // //检测鼠标横向移动,带动切割平面旋转
        // float mx = Input.GetAxis("Mouse X");
        // transform.Rotate(0, 0, -mx * 2);

        //立方体射线检测,把他的高度设置为平面高度的一半（非常薄），也就相当于一个面射线检测
        Collider[] colliders = Physics.OverlapBox(transform.position,//检测碰撞的区域的中心点
                       new Vector3(4, 0.005f, 4),//检测碰撞的区域的大小
                       transform.rotation,//朝向
                       LayerMask.GetMask("Raycast"));//~表示取反，定义不会被切割的层，比如地面和切割面
                                                     //将每一个检测到的物体进行切割
        foreach (Collider c in colliders)
        {
            Destroy(c.gameObject);//每次切割是产生两个新物体，所以要先销毁之前的物体
                                  //GameObject[] objs=c.gameObject.SliceInstantiate(transform.position, transform.up);//切割出的面材质会丢失

            //切割并返回表皮
            SlicedHull hull = c.gameObject.Slice(transform.position, transform.up);
            print(hull);
            if (hull != null)
            {
                GameObject lower = hull.CreateLowerHull(c.gameObject, cross);//定义切割下半部分的材质
                GameObject upper = hull.CreateUpperHull(c.gameObject, cross);//定义切割上半部分的材质
                GameObject[] objs = new GameObject[] { lower, upper };

                foreach (GameObject o in objs)
                {
                    // Rigidbody rb = o.AddComponent<Rigidbody>();//添加刚体
                    // //因为切割之后是不规则物体，所以要选择 MeshCollider（网格碰撞）
                    // //如果一个MeshCollider是刚体，要想正常碰撞，一定要将convex设true
                    // //Unity的规定：这样会形成一个凸多面体，只有凸多面体才能是刚体
                    // o.AddComponent<MeshCollider>().convex = true;
                    // //在切割的地方添加一个爆炸力  参数解释：力大小 位置 爆炸半径
                    // rb.AddExplosionForce(100, o.gameObject.transform.position, 20);

                    o.AddComponent<MeshCollider>();
                }
            }
        }

    }
}
