using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Meshdeform : MonoBehaviour
{
    public MeshFilter meshFilter;
    public float changeForce = 10f;
    public float springForce = 20f;
    public float forceOffset = 0.1f;
    public float damping = 5f;
    public float uniformScale = 1f;
    public float uniformScaleRate = 0f;
    private Mesh mesh;
    public MeshCollider meshCollider; // 添加 MeshCollider
    private Vector3[] originVertices;
    private Vector3[] displayVertices;
    private Vector3[] vertexVelocities;
    // Start is called before the first frame update
    void Start()
    {
        uniformScale = transform.localScale.x;
        meshFilter = this.GetComponent<MeshFilter>();
        mesh = meshFilter.mesh;
        originVertices = (Vector3[])mesh.vertices.Clone();
        displayVertices = (Vector3[])mesh.vertices.Clone();
        vertexVelocities = new Vector3[displayVertices.Length];
        meshCollider = this.GetComponent<MeshCollider>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButton(0))
        {
            HandleInput();
        }
        for (int i = 0; i < displayVertices.Length; i++)
        {
            Vector3 v = vertexVelocities[i];
            Vector3 displacement = displayVertices[i] - originVertices[i];
            v -= displacement * springForce * Time.deltaTime; //回弹形状
            v *= 1f - damping * Time.deltaTime;               //阻尼
            vertexVelocities[i] = v;
            displayVertices[i] += v * (Time.deltaTime / uniformScale); //应用速度 除以uniformScale是为了兼容缩放了物体的情况 可理解为放大物体则形变更小，反之更大
            float uniform = (uniformScale * uniformScaleRate);
            if (uniform == 0)
            {
                uniform = 1;
            }
            displayVertices[i] += v * (Time.deltaTime / uniform);
        }
        mesh.vertices = displayVertices;
        mesh.RecalculateNormals();
    }

    void HandleInput()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        Debug.DrawRay(ray.origin, ray.direction * 100, Color.red, 2f); // 可视化射线
        RaycastHit hit;
        if (Physics.Raycast(ray, out hit))
        {
            Meshdeform meshdeform = hit.transform.GetComponent<Meshdeform>();
            if (meshdeform)
            {
                meshdeform.ChangeMesh(hit.point + hit.normal * forceOffset, changeForce);
            }
        }
    }

    public void ChangeMesh(Vector3 point, float force)
    {
        point = transform.InverseTransformPoint(point);
        for (int i = 0; i < displayVertices.Length; i++)
        {
            Vector3 pointToVertex = displayVertices[i] - point;
            //注意：物体缩放了，但是这里获取到的物体网格顶点实际是没有缩放时的顶点位置，也就是会是得到的一个错误的向量 如果你的物体被缩放了。
            pointToVertex *= uniformScale;//处理物体被缩放，但是模型本身顶点没缩放 导致向量与实际不符，现在乘上一个缩放系数它自身 如果是2倍则是会将向量还原到正确的倍数            
            float f = force / (1f + pointToVertex.sqrMagnitude); //Fv = F / (1 + d^2) 获取顶点受力公式
            float v = f * Time.deltaTime;                        //dv = a * dt  a = F/m 设m=1，则 dv = F * dt
            vertexVelocities[i] += pointToVertex.normalized * v;
        }
        SaveDeformedMesh();
    }
    public void SaveDeformedMesh()
    {
        Mesh newMesh = new Mesh();
        newMesh.vertices = displayVertices;
        newMesh.triangles = mesh.triangles;
        newMesh.normals = mesh.normals;
        newMesh.uv = mesh.uv;
        newMesh.RecalculateNormals();
        meshFilter.mesh = newMesh;
        // 更新 MeshCollider
        if (meshCollider != null)
        {
            meshCollider.sharedMesh = null;
            meshCollider.sharedMesh = mesh;
        }
    }

}
