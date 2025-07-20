using UnityEngine;
using System.Collections.Generic;

//FFD(Free-FormDeformation)自由变形算法
public class FFD_CS : MonoBehaviour
{
    struct TriangleIndices
    {
        public int ID0;
        public int ID1;
        public int ID2;
    }
    struct TriangleAdjFaces
    {
        public TriangleIndices Face0;
        public TriangleIndices Face1;
        public TriangleIndices Face2;
        public TriangleIndices Face3;
        public TriangleIndices Face4;
        public TriangleIndices Face5;
    }

    public enum FFDMode
    {
        FFD2x2x2 = 2,
        FFD3x3x3 = 3,
        FFD4x4x4 = 4
    };
    public FFDMode m_ffdMode = FFDMode.FFD2x2x2;
    public bool m_isDebug = true;
    public System.Action InitiateCompleteCallBack = null;

    ComputeShader m_computeShader = null;
    ComputeBuffer m_controlPointsBuffer = null;
    ComputeBuffer m_verticesIndexBuffer = null;
    ComputeBuffer m_verticesSTUBuffer = null;
    ComputeBuffer m_newVerticesBuffer = null;
    ComputeBuffer m_newNormalsBuffer = null;
    ComputeBuffer m_verticesAdjFacesBuffer = null;
    List<Vector3> m_controlPoints = new List<Vector3>();
    List<Transform> m_controlTransform = new List<Transform>();
    List<KGLine> m_kgLinesX = new List<KGLine>();
    List<KGLine> m_kgLinesY = new List<KGLine>();
    List<KGLine> m_kgLinesZ = new List<KGLine>();

    Material m_material = null;
    int m_ffd2x2x2Kernel = 0;
    int m_ffd3x3x3Kernel = 0;
    int m_ffd4x4x4Kernel = 0;
    int m_recalculateKernel = 0;
    int m_threadX = 32;
    int m_meshVerticesCount = 0;
    void Start()
    {
        if (!SystemInfo.supportsComputeShaders)
        {
            Debug.LogError("设备不支持ComputeShader!");
            return;
        }
        InitShaderAndMaterial();
        InitControlPointsBuffer();
        CreateControlPoints();
        InitVerticesSTUsBuffer();
        InitVerticesBuffer();
        ExecuteFFD();
        if (InitiateCompleteCallBack != null)
            InitiateCompleteCallBack();
        //Vector3[] data = new Vector3[m_newVerticesBuffer.count];
        //m_newVerticesBuffer.GetData(data);
        //for (int i = 0; i < data.Length; i++)
        //{
        //    Debug.Log(data[i].x.ToString() + "," + data[i].y.ToString() + "," + data[i].z.ToString());
        //}
    }

    public void SetControlPoint(int n, Vector3 pos)
    {
        if (n < m_controlTransform.Count && n >= 0)
        {
            m_controlTransform[n].position = pos;
            m_controlPoints[n] = m_controlTransform[n].position;
        }
        else
        {
            Debug.LogWarning("设置控制点序号错误!" + "（" + n.ToString() + ")");
        }
    }

    public Vector3 GetControlPoint(int n)
    {
        if (n < m_controlPoints.Count && n >= 0)
        {
            return m_controlPoints[n];
        }
        Debug.LogWarning("获取控制点序号错误!" + "（" + n.ToString() + ")");
        return Vector3.zero;
    }

    public int[] GetControlPointsCount()
    {
        int n = (int)m_ffdMode;
        return new int[] {n,n,n};
    }

    void OnDestroy()
    {
        if (m_controlPointsBuffer != null)
        {
            m_controlPointsBuffer.Release();
            m_controlPointsBuffer = null;
        }

        if (m_verticesSTUBuffer != null)
        {
            m_verticesSTUBuffer.Release();
            m_verticesSTUBuffer = null;
        }

        if (m_newVerticesBuffer != null)
        {
            m_newVerticesBuffer.Release();
            m_newVerticesBuffer = null;
        }

        if (m_verticesAdjFacesBuffer != null)
        {
            m_verticesAdjFacesBuffer.Release();
            m_verticesAdjFacesBuffer = null;
        }
        if (m_newNormalsBuffer != null)
        {
            m_newNormalsBuffer.Release();
            m_newNormalsBuffer = null;
        }
        if (m_verticesIndexBuffer != null)
        {
            m_verticesIndexBuffer.Release();
            m_verticesIndexBuffer = null;
        }   
    }

    public void ExecuteFFD()
    {
        UpdateControlPoints();

        m_computeShader.SetBuffer(GetKernelPass(), "gVerticesIndex", m_verticesIndexBuffer);
        m_computeShader.SetBuffer(GetKernelPass(), "gControlPoints", m_controlPointsBuffer);
        m_computeShader.SetBuffer(GetKernelPass(), "gVerticesSTUs", m_verticesSTUBuffer);
        m_computeShader.SetBuffer(GetKernelPass(), "gNewPoints", m_newVerticesBuffer);
        m_computeShader.Dispatch(GetKernelPass(), m_threadX, 1, 1);

        //重新计算法线
        m_computeShader.SetBuffer(m_recalculateKernel, "gVerticesIndex", m_verticesIndexBuffer);
        m_computeShader.SetBuffer(m_recalculateKernel, "gVerticesAdjFaces", m_verticesAdjFacesBuffer);
        m_computeShader.SetBuffer(m_recalculateKernel, "gVertices", m_newVerticesBuffer);
        m_computeShader.SetBuffer(m_recalculateKernel, "gNormals", m_newNormalsBuffer);
        m_computeShader.SetInts("gDispatch", new int[] { Mathf.CeilToInt((float)m_meshVerticesCount / 32.0f), 32, 1 });
        m_computeShader.Dispatch(m_recalculateKernel, Mathf.CeilToInt((float)m_meshVerticesCount/32.0f), 32, 1);
    }

    void InitShaderAndMaterial()
    {
        string path = "Shaders/CS_FFD";
        m_computeShader = Resources.Load(path) as ComputeShader;
        if (m_computeShader != null)
        {
            m_ffd2x2x2Kernel = m_computeShader.FindKernel("CSComputeNewVertices2x2x2");
            m_ffd3x3x3Kernel = m_computeShader.FindKernel("CSComputeNewVertices3x3x3");
            m_ffd4x4x4Kernel = m_computeShader.FindKernel("CSComputeNewVertices4x4x4");
            m_recalculateKernel = m_computeShader.FindKernel("CSRecalculateNormal");

            m_material = new Material(Shader.Find("FFD/Test"));
            gameObject.GetComponent<Renderer>().material = m_material;
        }
        else
        {
            Debug.LogError("错误的.compute路径:" + path);
        }
    }

    void InitControlPointsBuffer()
    {
        Renderer rd = gameObject.GetComponent<Renderer>();
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        if (rd != null && meshFilter != null)
        {
            Mesh mesh = meshFilter.mesh;
            //计算控制点坐标值(物体局部空间)
            Vector3 max = mesh.bounds.max - mesh.bounds.min;
            Vector3 vecS = new Vector3(max.x, 0, 0);
            Vector3 vecT = new Vector3(0, max.y, 0);
            Vector3 vecU = new Vector3(0, 0, max.z);
            Vector3 p000 = mesh.bounds.min;
            int n = (int)m_ffdMode;
            m_controlPointsBuffer = new ComputeBuffer(n * n * n, sizeof(float) * 3, ComputeBufferType.Default);//StructuredBuffer
            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {
                    for (int k = 0; k < n; k++)
                    {
                        Vector3 point = p000 + (float)i / ((float)n - 1.0f) * vecS + (float)j / ((float)n - 1.0f) * vecT + (float)k / ((float)n - 1.0f) * vecU;
                        m_controlPoints.Add(point);
                    }
                }
            }
            //变换到世界空间去
            for (int i = 0; i < m_controlPoints.Count; i++)
            {
                m_controlPoints[i] = rd.localToWorldMatrix.MultiplyPoint3x4(m_controlPoints[i]);
            }
            m_controlPointsBuffer.SetData(m_controlPoints.ToArray());
        }
    }

    void InitVerticesSTUsBuffer()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Renderer rd = gameObject.GetComponent<Renderer>();
        if (meshFilter != null && rd != null)
        {
            Mesh mesh = meshFilter.mesh;
            Vector3 max = mesh.bounds.max - mesh.bounds.min;
            Vector3 vecS = new Vector3(max.x, 0, 0);
            Vector3 vecT = new Vector3(0, max.y, 0);
            Vector3 vecU = new Vector3(0, 0, max.z);
            Vector3 p000 = mesh.bounds.min;
            //计算Mesh顶点的网格参数化坐标值(STU空间)
            Vector3[] vertices = mesh.vertices;
            m_verticesIndexBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(int), ComputeBufferType.Default);//StructuredBuffer
            m_verticesSTUBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Default);//StructuredBuffer
            m_newVerticesBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Counter);//RWStructuredBuffer
            Vector3[] stuVertices = new Vector3[mesh.vertexCount];
            Vector2[] verticesID = new Vector2[mesh.vertexCount];
            int[] verticesIndex = new int[mesh.vertexCount];
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector3 crossTU = Vector3.Cross(vecT, vecU);
                Vector3 crossSU = Vector3.Cross(vecS, vecU);
                Vector3 crossST = Vector3.Cross(vecS, vecT);

                float s = Vector3.Dot(crossTU, vertices[i] - p000) / Vector3.Dot(crossTU, vecS);
                float t = Vector3.Dot(crossSU, vertices[i] - p000) / Vector3.Dot(crossSU, vecT);
                float u = Vector3.Dot(crossST, vertices[i] - p000) / Vector3.Dot(crossST, vecU);
                stuVertices[i] = new Vector3(s, t, u);
                //为每个顶点做标记
                verticesIndex[i] = i;
                verticesID[i] = EffectHelp.S.EncodeFloatRG((float)i / (float)(mesh.vertexCount));
            }
            mesh.uv2 = verticesID;
            m_verticesSTUBuffer.SetData(stuVertices);
            m_verticesIndexBuffer.SetData(verticesIndex);
            m_material.SetBuffer("gNewPoints", m_newVerticesBuffer);
            m_material.SetInt("_VerticesCount", mesh.vertexCount - 1);
            m_threadX = Mathf.CeilToInt((float)mesh.vertexCount / 32.0f);
            m_meshVerticesCount = mesh.vertexCount;
        }
    }

    void InitVerticesBuffer()
    {
        MeshFilter meshFilter = gameObject.GetComponent<MeshFilter>();
        Renderer rd = gameObject.GetComponent<Renderer>();
        if (meshFilter != null && rd != null)
        {
            Mesh mesh = meshFilter.mesh;
            m_verticesAdjFacesBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(int) * 18, ComputeBufferType.Default);//StructuredBuffer
            int[] indices = mesh.triangles;
            TriangleAdjFaces []triangleAdjFaces = new TriangleAdjFaces[mesh.vertexCount];
            for (int j = 0; j < mesh.vertexCount; j++)
            {
                TriangleAdjFaces adjFaces = new TriangleAdjFaces();
                adjFaces.Face0.ID0 = -1;
                adjFaces.Face1.ID0 = -1;
                adjFaces.Face2.ID0 = -1;
                adjFaces.Face3.ID0 = -1;
                adjFaces.Face4.ID0 = -1;
                adjFaces.Face5.ID0 = -1;
                int count = 0;
                for (int i = 0; i < indices.Length; i += 3)
                {
                    if (count > 6)
                        continue;
                    if (j == indices[i] || j == indices[i + 1] || j == indices[i + 2])
                    {
                        if (count == 0)
                        {
                            adjFaces.Face0.ID0 = indices[i];
                            adjFaces.Face0.ID1 = indices[i + 1];
                            adjFaces.Face0.ID2 = indices[i + 2];
                        }
                        else if (count == 1)
                        {
                            adjFaces.Face1.ID0 = indices[i];
                            adjFaces.Face1.ID1 = indices[i + 1];
                            adjFaces.Face1.ID2 = indices[i + 2];
                        }
                        else if (count == 2)
                        {
                            adjFaces.Face2.ID0 = indices[i];
                            adjFaces.Face2.ID1 = indices[i + 1];
                            adjFaces.Face2.ID2 = indices[i + 2];
                        }
                        else if (count == 3)
                        {
                            adjFaces.Face3.ID0 = indices[i];
                            adjFaces.Face3.ID1 = indices[i + 1];
                            adjFaces.Face3.ID2 = indices[i + 2];
                        }
                        else if (count == 4)
                        {
                            adjFaces.Face4.ID0 = indices[i];
                            adjFaces.Face4.ID1 = indices[i + 1];
                            adjFaces.Face4.ID2 = indices[i + 2];
                        }
                        else if (count == 5)
                        {
                            adjFaces.Face5.ID0 = indices[i];
                            adjFaces.Face5.ID1 = indices[i + 1];
                            adjFaces.Face5.ID2 = indices[i + 2];
                        }
                        count++;
                    }
                }
                triangleAdjFaces[j] = adjFaces;
            }
            m_verticesAdjFacesBuffer.SetData(triangleAdjFaces);
            m_computeShader.SetBuffer(m_recalculateKernel,"gVerticesAdjFaces", m_verticesAdjFacesBuffer);
            m_computeShader.SetBuffer(m_recalculateKernel, "gVertices", m_newVerticesBuffer);
            m_newNormalsBuffer = new ComputeBuffer(mesh.vertexCount, sizeof(float) * 3, ComputeBufferType.Counter);//RWStructuredBuffer
            m_computeShader.SetBuffer(m_recalculateKernel, "gNormals", m_newNormalsBuffer);
            m_material.SetBuffer("gNewNormals", m_newNormalsBuffer);
        }

    }
    void CreateControlPoints()
    {
        GameObject root = new GameObject("Control");
        root.transform.parent = gameObject.transform;
        root.transform.localPosition = Vector3.zero;
        root.transform.localRotation = Quaternion.identity;
        root.transform.localScale = Vector3.one;

        for (int i = 0; i < m_controlPoints.Count; i++)
        {
            GameObject go = null;
            if (m_isDebug)
                go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            else
                go = new GameObject();
            go.name = "controlpoint" + i.ToString();
            go.transform.parent = root.transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * 0.05f;
            go.transform.position = m_controlPoints[i];

            m_controlTransform.Add(go.transform);
        }
        if (m_isDebug)
            CreateDebugLines();
    }

    void CreateDebugLines()
    {
        int n = (int)m_ffdMode;
        //i（x）方向
        for (int k = 0; k < n; k++)
        {
            for (int j = 0; j < n; j++)
            {
                GameObject line = new GameObject("line" + k.ToString() + j.ToString());
                line.transform.localPosition = Vector3.zero;
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = Vector3.one;

                KGLine kgline = line.AddComponent<KGLine>();
                for (int i = 0; i < n; i++)
                {
                    kgline.m_lineVerticesPosition.Add(m_controlPoints[k * n * n + j * n + i]);
                    kgline.m_lineVerticesColor.Add(Color.red);
                }
                m_kgLinesX.Add(kgline);
            }
        }
        //j（y）方向
        for (int i = 0; i < n; i++)
        {
            for (int k = 0; k < n; k++)
            {
                GameObject line = new GameObject("line" + i.ToString() + k.ToString());
                line.transform.localPosition = Vector3.zero;
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = Vector3.one;

                KGLine kgline = line.AddComponent<KGLine>();
                for (int j = 0; j < n; j++)
                {
                    kgline.m_lineVerticesPosition.Add(m_controlPoints[k * n * n + j * n + i]);
                    kgline.m_lineVerticesColor.Add(Color.green);
                }
                m_kgLinesY.Add(kgline);
            }
        }
        //k（z）方向
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                GameObject line = new GameObject("line" + i.ToString() + j.ToString());
                line.transform.localPosition = Vector3.zero;
                line.transform.localRotation = Quaternion.identity;
                line.transform.localScale = Vector3.one;

                KGLine kgline = line.AddComponent<KGLine>();
                for (int k = 0; k < n; k++)
                {
                    kgline.m_lineVerticesPosition.Add(m_controlPoints[k * n * n + j * n + i]);
                    kgline.m_lineVerticesColor.Add(Color.blue);
                }
                m_kgLinesZ.Add(kgline);
            }
        }
    }

    void UpdateControlPoints()
    {
        for (int i = 0; i < m_controlPoints.Count; i++)
        {
            m_controlPoints[i] = m_controlTransform[i].position;
        }

        m_controlPointsBuffer.SetData(m_controlPoints.ToArray());
        m_computeShader.SetBuffer(GetKernelPass(), "gControlPoints", m_controlPointsBuffer);
        if (m_isDebug)
            UpdateDebugLines();
    }

    void UpdateDebugLines()
    {
        int n = (int)m_ffdMode;
        //i（x）方向
        for (int k = 0; k < n; k++)
        {
            for (int j = 0; j < n; j++)
            {
                KGLine kgline = m_kgLinesX[k * n + j];
                for (int i = 0; i < n; i++)
                {
                    kgline.m_lineVerticesPosition[i] = m_controlPoints[k * n * n + j * n + i];
                }
            }
        }
        //j（y）方向
        for (int i = 0; i < n; i++)
        {
            for (int k = 0; k < n; k++)
            {
                KGLine kgline = m_kgLinesY[i * n + k];
                for (int j = 0; j < n; j++)
                {
                    kgline.m_lineVerticesPosition[j] = m_controlPoints[k * n * n + j * n + i];
                }
            }
        }
        //k（z）方向
        for (int i = 0; i < n; i++)
        {
            for (int j = 0; j < n; j++)
            {
                KGLine kgline = m_kgLinesZ[i * n + j];
                for (int k = 0; k < n; k++)
                {
                    kgline.m_lineVerticesPosition[k] = m_controlPoints[k * n * n + j * n + i];
                }
            }
        }
    }

    int GetKernelPass()
    {
        if (m_ffdMode == FFDMode.FFD2x2x2)
            return m_ffd2x2x2Kernel;
        else if (m_ffdMode == FFDMode.FFD3x3x3)
            return m_ffd3x3x3Kernel;
        else
            return m_ffd4x4x4Kernel;
    }
}
