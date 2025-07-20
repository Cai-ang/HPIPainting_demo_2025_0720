using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using FFD;
using Oculus.Interaction;
using UnityEditor;

public class Menu_manager : MonoBehaviour
{
    public TMP_Dropdown brush_type;
    public TMP_Dropdown affect_type;
    public TMP_Dropdown geometry_type;
    public Button endFFD;
    public Button combine;
    public Slider line_width;
    public Slider pipe_size;
    public Pen_3D pen;
    public Toggle affect;
    public Toggle OBJ_Lock;
    public Toggle iscontrolbyline;
    public GameObject affect_trigger;
    private int i = 0;
    private int affect_mode = 0;
    private GameObject affecting;
    private List<GameObject> combinelist;
    private Deform_contorl deformControl;
    public Toggle Drawonmesh;
    public Slider Drawonmesh_size;
    // Start is called before the first frame update
    void Start()
    {
        affect.isOn = false;
        brush_type.value = 0;
        geometry_type.gameObject.SetActive(false); line_width.gameObject.SetActive(false); pipe_size.gameObject.SetActive(true);
        endFFD.gameObject.SetActive(false);
        affect.gameObject.SetActive(true);
        OBJ_Lock.gameObject.SetActive(false);
        combine.gameObject.SetActive(false);
        //iscontrolbyline.gameObject.SetActive(false);
        combinelist = new List<GameObject>();
    }

    // Update is called once per frame
    // void Update()
    // {

    // }
    public void change_brush_type()
    {
        pen.Brushtype = brush_type.value;
        switch (brush_type.value)
        {
            case 0: geometry_type.gameObject.SetActive(false); line_width.gameObject.SetActive(false); pipe_size.gameObject.SetActive(true); break;
            case 1: geometry_type.gameObject.SetActive(false); line_width.gameObject.SetActive(true); pipe_size.gameObject.SetActive(false); break;
            case 2: geometry_type.gameObject.SetActive(true); line_width.gameObject.SetActive(false); pipe_size.gameObject.SetActive(false); break;
            default: break;
        }
    }
    public void unlock()
    {
        pen.drawlock = false;
    }
    public void DrawOnMesh()
    {
        pen.drawonmesh = Drawonmesh.isOn;
    }
    public void Drawonmesh_Size()
    {
        pen.brushSize_drawonmesh = (int)Drawonmesh_size.value;
    }
    public void changepipe_radius()
    {
        pen.Radius = pipe_size.value;
    }

    public void changeline_width()
    {
        pen.penWidth = line_width.value;
    }

    public void change_affect_type()
    {
        affect_mode = affect_type.value;
        combinelist.Clear();
    }
    public void change_geometry_type()
    {
        pen.geometry_mode = geometry_type.value;

    }
    public void switchcontrolmethod()
    {
        //deformControl.iscontrolbyline = iscontrolbyline.isOn;
    }

    public void Affecttrigger()
    {
        if (affect.isOn)
        {
            affect_trigger.SetActive(true);
        }
        else
        {
            affect_trigger.SetActive(false);
        }
    }
    public void EndFFD()
    {
        Deform_contorl deformControl = affecting.GetComponent<Deform_contorl>();
        switch (affect_mode)
        {
            case 1:
                deformControl.RemoveMod("FFD2x2x2", i++); break;
            case 2:
                deformControl.RemoveMod("FFD3x3x3", i++); break;
            case 3:
                deformControl.RemoveMod("FFD4x4x4", i++); break;
        }
        Destroy(deformControl);
        affecting.GetComponent<MeshCollider>().enabled = true;
        affecting.transform.parent.parent.GetComponent<Grabbable>().enabled = true;
        endFFD.gameObject.SetActive(false);
        affect.gameObject.SetActive(true);
        affect_type.gameObject.SetActive(true);
        OBJ_Lock.gameObject.SetActive(false);
        //iscontrolbyline.gameObject.SetActive(false);
    }
    public void OBJLock()
    {
        affecting.GetComponent<MeshCollider>().enabled = !OBJ_Lock.isOn;
        affecting.transform.parent.parent.GetComponent<Grabbable>().enabled = !OBJ_Lock.isOn;
    }
    public void Combine()
    {
        //GameObject combined = new GameObject("combined");
        pen.interactableobjs(combinelist, "combined", false);
        // 清除列表
        combinelist.Clear();
        combine.gameObject.SetActive(false);
    }
    void SaveAsset(GameObject obj, string objName)
    {
#if UNITY_EDITOR
        // 生成唯一的资产路径
        string matPath = GenerateUniqueAssetPath($"Assets/Resources/Cache/Materials/{objName}.mat");
        string meshPath = GenerateUniqueAssetPath($"Assets/Resources/Cache/Meshes/{objName}.asset");
        Material mat = obj.GetComponent<MeshRenderer>().material;
        MeshFilter objmeshFilter = obj.GetComponent<MeshFilter>();
        // 保存材质
        if (mat)
        {
            AssetDatabase.CreateAsset(mat, matPath);
        }
        obj.GetComponent<MeshRenderer>().material = mat;
        // 复制并保存网格
        Mesh mesh = CopyMesh(objmeshFilter.sharedMesh);
        AssetDatabase.CreateAsset(mesh, meshPath);
        obj.GetComponent<MeshCollider>().sharedMesh = mesh;
        objmeshFilter.mesh = mesh; // 更新引用
        // 查找并重置MaterialPropertyBlockEditor（如果存在）
        MaterialPropertyBlockEditor editor = obj.GetComponentInParent<MaterialPropertyBlockEditor>();
        if (editor)
        {
            editor.Reset();
        }
#endif
    }
    private IEnumerator DelayedSave(GameObject obj, string objName)
    {
        yield return new WaitForEndOfFrame();  // 等待当前帧结束
        string prefabPath = GenerateUniqueAssetPath($"Assets/Resources/Cache/Prefabs/{objName}.prefab");
        Transform visualobj = obj.transform.parent;
        Debug.Log(visualobj);
        foreach (Transform child in visualobj.transform)
        {
            SaveAsset(child.gameObject, objName);
            Debug.Log(child.name);
        }
        // 将对象保存为预制件
        PrefabUtility.SaveAsPrefabAsset(visualobj.transform.parent.gameObject, prefabPath);
        AssetDatabase.Refresh();
    }
    // 复制网格数据
    Mesh CopyMesh(Mesh original)
    {
        Mesh newMesh = new Mesh();
        newMesh.vertices = original.vertices;
        newMesh.triangles = original.triangles;
        newMesh.normals = original.normals;
        newMesh.uv = original.uv;
        return newMesh;
    }
    // 检查资产名称是否存在，如果存在则顺延数字
    public static string GenerateUniqueAssetPath(string path)
    {
        if (!AssetDatabase.IsValidFolder(path))
        {
            string directory = System.IO.Path.GetDirectoryName(path);
            string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
            string extension = System.IO.Path.GetExtension(path);
            int counter = 1;

            // 当文件存在时，尝试添加数字后缀
            while (AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object)) != null)
            {
                path = $"{directory}/{fileName}{counter++}{extension}";
            }
        }
        return path;
    }
    public int Affect(GameObject affectable)
    {
        switch (affect_mode)
        {
            case 0://修改颜色
                {
                    //修改当前颜色且修改colorvisual的Normal颜色
                    if (affectable.name.EndsWith(" (Clone)"))
                    {
                        Material mat = new Material(Shader.Find("Standard"));
                        mat.color = pen.currentColor;
                        affectable.GetComponent<MeshRenderer>().material = mat;
                    }
                    affectable.GetComponent<MeshRenderer>().material.color = pen.currentColor; //Debug.Log("changecolor" + pen.currentColor);
                    InteractableColorVisual interactableColorVisual = affectable.transform.parent.GetComponent<InteractableColorVisual>();
                    if (interactableColorVisual)
                        interactableColorVisual.Reset0(pen.currentColor); break;
                }
            case 1://FFD2x2x2
                {
                    deformControl = affectable.GetComponent<Deform_contorl>();
                    if (!deformControl)
                    {
                        deformControl = affectable.AddComponent<Deform_contorl>();
                        if (affectable.name == "PipeMesh")
                        {
                            deformControl.sphereRadius *= 0.1f;
                            deformControl.lineWidth *= 0.1f;
                        }
                        deformControl.m_FFDMode = Deform_contorl.FFDMode.FFD2x2x2;
                        affecting = affectable;
                        endFFD.gameObject.SetActive(true);
                        affect.gameObject.SetActive(false);
                        OBJ_Lock.gameObject.SetActive(true);
                        affect_type.gameObject.SetActive(false);
                        //iscontrolbyline.gameObject.SetActive(true);
                        OBJ_Lock.isOn = false;
                    }
                    else
                    {
                        deformControl.RemoveMod("FFD2x2x2", i++);
                        Destroy(deformControl);
                    }
                    break;
                }
            case 2://FFD3x3x3
                {
                    deformControl = affectable.GetComponent<Deform_contorl>();
                    if (!deformControl)
                    {
                        deformControl = affectable.AddComponent<Deform_contorl>();
                        if (affectable.name == "PipeMesh")
                        {
                            deformControl.sphereRadius *= 0.1f;
                            deformControl.lineWidth *= 0.1f;
                        }
                        deformControl.m_FFDMode = Deform_contorl.FFDMode.FFD3x3x3;
                        affecting = affectable;
                        endFFD.gameObject.SetActive(true);
                        affect.gameObject.SetActive(false);
                        OBJ_Lock.gameObject.SetActive(true);
                        affect_type.gameObject.SetActive(false);
                        //iscontrolbyline.gameObject.SetActive(true);
                        OBJ_Lock.isOn = false;
                    }
                    else
                    {
                        deformControl.RemoveMod("FFD3x3x3", i++);
                        Destroy(deformControl);
                    }
                    break;
                }
            case 3://FFD4x4x4
                {
                    deformControl = affectable.GetComponent<Deform_contorl>();
                    if (!deformControl)
                    {
                        deformControl = affectable.AddComponent<Deform_contorl>();
                        if (affectable.name == "PipeMesh")
                        {
                            deformControl.sphereRadius *= 0.1f;
                            deformControl.lineWidth *= 0.1f;
                        }
                        deformControl.m_FFDMode = Deform_contorl.FFDMode.FFD4x4x4;
                        affecting = affectable;
                        endFFD.gameObject.SetActive(true);
                        affect.gameObject.SetActive(false);
                        OBJ_Lock.gameObject.SetActive(true);
                        affect_type.gameObject.SetActive(false);
                        //iscontrolbyline.gameObject.SetActive(true);
                        OBJ_Lock.isOn = false;
                    }
                    else
                    {
                        deformControl.RemoveMod("FFD4x4x4", i++);
                        Destroy(deformControl);
                    }
                    break;
                }
            case 4: // 复制
                {
                    // 找到顶级父物体
                    //Transform topParent = affectable.transform.parent.parent;

                    // 复制整个层级结构
                    GameObject copy = Instantiate(affectable);
                    copy = pen.interactableobj(copy, affectable.name + "(copy)", false);

                    // 稍微偏移复制对象的位置
                    copy.transform.position += new Vector3(0.1f, 0.1f, 0.1f);

                    // // 找到对应的被影响物体
                    // Transform correspondingAffectable = FindCorrespondingTransform(copy.transform, affectable.transform);
                    // if (correspondingAffectable != null)
                    // {
                    //     // 对相应的物体应用任何额外的逻辑（如果需要）
                    //     // 例如，你可能想要高亮显示这个物体
                    // }
                    break;
                }
            case 5: // 删除
                {
                    // 找到顶级父物体并删除
                    Transform topParent = affectable.transform.parent.parent;
                    Destroy(topParent.gameObject);
                    break;
                }
            case 6: // 合并
                {
                    if (!combinelist.Contains(affectable))
                    {
                        combinelist.Add(affectable);
                        List<GameObject> siblings = GetSiblings(affectable.gameObject);
                        foreach (GameObject sibling in siblings)
                        {
                            if (!combinelist.Contains(sibling))
                            {
                                combinelist.Add(sibling);
                            }
                        }
                        if (combinelist.Count > 1)
                        {
                            combine.gameObject.SetActive(true);
                        }
                    }
                    break;
                }
            case 7: // 保存
                {
                    StartCoroutine(DelayedSave(affectable, "savedobj"));
                    //SaveAsset(affectable, "savedobj");
                    break;
                }
        }
        return affect_mode;
    }
    private List<GameObject> GetSiblings(GameObject obj)
    {
        List<GameObject> siblings = new List<GameObject>();

        if (obj == null || obj.transform.parent == null)
            return siblings;

        Transform parent = obj.transform.parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.gameObject != obj)
            {
                siblings.Add(child.gameObject);
            }
        }

        return siblings;
    }
}


