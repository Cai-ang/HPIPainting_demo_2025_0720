using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MegaFiers;
using UnityEditor;
using Unity.VisualScripting;

namespace FFD
{
    public class Deform_contorl : MonoBehaviour
    {
        MegaModifyObject megaModifyObject;
        public FFDMode m_FFDMode = FFDMode.FFD3x3x3;
        public float sphereRadius = 0.1f;
        public float lineWidth = 0.005f;
        public Pen_3D pen;
        private MegaFFD2x2x2 FFD2X2X2;
        private MegaFFD3x3x3 FFD3X3X3;
        private MegaFFD4x4x4 FFD4X4X4;
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        public Mesh tempMesh;


        public enum FFDMode
        {
            FFD2x2x2 = 2,
            FFD3x3x3 = 3,
            FFD4x4x4 = 4
        }
        //public bool isFFD = true;
        private GameObject[] controlPointSpheres;
        // Start is called before the first frame update
        void Start()
        {
            pen = GameObject.Find("Pen_3D").GetComponent<Pen_3D>();
            //Debug.Log("reset" + pen);
            if (GetComponent<MegaModifyObject>() == null)
                this.gameObject.AddComponent<MegaModifyObject>();
            megaModifyObject = this.GetComponent<MegaModifyObject>();
            megaModifyObject.Reset();
            megaModifyObject.ResetCorners();
            //megaModifyObject.enabled = true;
            //Debug.Log("m_FFDMode:" + m_FFDMode);
            if (m_FFDMode == FFDMode.FFD2x2x2)
                AddFFD222();
            else if (m_FFDMode == FFDMode.FFD3x3x3)
                AddFFD333();
            else if (m_FFDMode == FFDMode.FFD4x4x4)
                AddFFD444();
            GameObject controllers = new GameObject("controllers");
            controllers.transform.SetParent(transform, false);
            CreateControlPointSpheres(controllers.transform);
            CreateConnectingLines(controllers.transform);
            //megaFFD3X3X3.reset3x3x3();
        }

        private void AddFFD222()
        {
            FFD2X2X2 = gameObject.AddComponent<MegaFFD2x2x2>();
            //Debug.Log("reset!!!");
            reset0();
        }

        private void AddFFD333()
        {
            FFD3X3X3 = gameObject.AddComponent<MegaFFD3x3x3>();
            //Debug.Log("reset!!!");
            reset0();
        }
        private void AddFFD444()
        {
            FFD4X4X4 = gameObject.AddComponent<MegaFFD4x4x4>();
            //Debug.Log("reset!!!");
            reset0();
        }

        public void RemoveMod(string modname, int i)
        {
            MegaModifyObject modifyObject = this.GetComponent<MegaModifyObject>();
            int x = 0;
            foreach (MegaModifier m in modifyObject.mods)
            {
                if (m.ModName() == modname)
                {
                    Destroy(transform.Find("controllers").gameObject);
                    CreateTempMesh(GetComponent<MeshFilter>(), i);
                    switch (m.ModName())
                    {
                        case "FFD2x2x2": { MegaFFD2x2x2 megaFFD2X2X2 = GetComponent<MegaFFD2x2x2>(); Destroy(megaFFD2X2X2); break; }
                        case "FFD3x3x3": { MegaFFD3x3x3 megaFFD3X3X3 = GetComponent<MegaFFD3x3x3>(); Destroy(megaFFD3X3X3); break; }
                        case "FFD4x4x4": { MegaFFD4x4x4 megaFFD4X4X4 = GetComponent<MegaFFD4x4x4>(); Destroy(megaFFD4X4X4); break; }
                    }
                    modifyObject.mods[x] = null;
                    DestroyImmediate(m);
                    //modifyObject.BuildList();
                    //ApplyModsToGroup(modifyObject);
                    // 禁用 MegaModifyObject 组件
                    //modifyObject.enabled = false;
                    Destroy(modifyObject);
                    //Debug.Log(tempMesh.name);
                    GetComponent<MeshFilter>().sharedMesh = tempMesh;
                    GetComponent<MeshCollider>().sharedMesh = tempMesh;
                    break;
                }
                i++;
            }
        }
        void CreateTempMesh(MeshFilter sourceMeshFilter, int i)
        {
            // 创建一个新的Mesh对象
            tempMesh = new Mesh();
            //tempMesh.name = "1";
            // 复制源mesh的数据
            tempMesh.vertices = sourceMeshFilter.mesh.vertices;
            tempMesh.triangles = sourceMeshFilter.mesh.triangles;
            tempMesh.uv = sourceMeshFilter.mesh.uv;
            tempMesh.normals = sourceMeshFilter.mesh.normals;
            tempMesh.colors = sourceMeshFilter.mesh.colors;
            tempMesh.tangents = sourceMeshFilter.mesh.tangents;
            //tempMesh = sourceMeshFilter.mesh;
            string path = "Assets/Material/meshCache/" + i + ".asset";
            Debug.Log(path);
            AssetDatabase.CreateAsset(tempMesh, path);
            AssetDatabase.SaveAssets();

            // 加载创建的资产并赋值给tempMesh
            tempMesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            // 重新计算边界
            //tempMesh.RecalculateBounds();
        }

        public void reset0()
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2:
                    FFD2X2X2.FitFFD();
                    FFD2X2X2.FitFFDToMesh();
                    break;
                case FFDMode.FFD3x3x3:
                    FFD3X3X3.FitFFD();
                    FFD3X3X3.FitFFDToMesh();
                    break;
                case FFDMode.FFD4x4x4:
                    FFD4X4X4.FitFFD();
                    FFD4X4X4.FitFFDToMesh();
                    break;
            }
        }


        void CreateControlPointSpheres(Transform controller)
        {
            int numPoints = 0;
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2:
                    numPoints = FFD2X2X2.NumPoints();
                    break;
                case FFDMode.FFD3x3x3:
                    numPoints = FFD3X3X3.NumPoints();
                    break;
                case FFDMode.FFD4x4x4:
                    numPoints = FFD4X4X4.NumPoints();
                    break;
            }

            controlPointSpheres = new GameObject[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = Vector3.one * sphereRadius;
                //sphere.GetComponent<Renderer>().material = sphereMaterial;
                sphere = pen.interactableobj(sphere, "con_sphere", false);
                sphere.transform.SetParent(controller, false);
                controlPointSpheres[i] = sphere;
                //controlPointSpheres[i] = sphere;
            }

            UpdateControlPointPositions();
        }

        void UpdateControlPointPositions()
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2:
                    int size = FFD2X2X2.GridSize();
                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            for (int k = 0; k < size; k++)
                            {
                                int index = FFD2X2X2.GridIndex(i, j, k);
                                Vector3 worldPos = FFD2X2X2.GetPointWorld(i, j, k);
                                //controlPointSpheres[index].transform.position = worldPos;
                                controlPointSpheres[index].transform.position = worldPos;
                            }
                        }
                    }
                    break;
                case FFDMode.FFD3x3x3:
                    size = FFD3X3X3.GridSize();
                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            for (int k = 0; k < size; k++)
                            {
                                int index = FFD3X3X3.GridIndex(i, j, k);
                                Vector3 worldPos = FFD3X3X3.GetPointWorld(i, j, k);
                                //controlPointSpheres[index].transform.position = worldPos;
                                controlPointSpheres[index].transform.position = worldPos;
                            }
                        }
                    }
                    break;
                case FFDMode.FFD4x4x4:
                    size = FFD4X4X4.GridSize();
                    for (int i = 0; i < size; i++)
                    {
                        for (int j = 0; j < size; j++)
                        {
                            for (int k = 0; k < size; k++)
                            {
                                int index = FFD4X4X4.GridIndex(i, j, k);
                                Vector3 worldPos = FFD4X4X4.GetPointWorld(i, j, k);
                                //controlPointSpheres[index].transform.position = worldPos;
                                controlPointSpheres[index].transform.position = worldPos;
                            }
                        }
                    }
                    break;
            }
        }
        void UpdateFFDFromControlPoints()
        {
            int size = GetCurrentGridSize();

            for (int i = 0; i < size; i++)
            {
                for (int j = 0; j < size; j++)
                {
                    for (int k = 0; k < size; k++)
                    {
                        int index = GetGridIndexForCurrentMode(i, j, k);
                        Vector3 worldPos = controlPointSpheres[index].transform.position;
                        SetPointWorldForCurrentMode(i, j, k, worldPos);
                    }
                }
            }
        }
        int GetCurrentGridSize()
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2: return 2;
                case FFDMode.FFD3x3x3: return 3;
                case FFDMode.FFD4x4x4: return 4;
                default: return 3;
            }
        }

        int GetGridIndexForCurrentMode(int i, int j, int k)
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2: return FFD2X2X2.GridIndex(i, j, k);
                case FFDMode.FFD3x3x3: return FFD3X3X3.GridIndex(i, j, k);
                case FFDMode.FFD4x4x4: return FFD4X4X4.GridIndex(i, j, k);
                default: return FFD3X3X3.GridIndex(i, j, k);
            }
        }

        void CreateConnectingLines(Transform controller)
        {
            int size = GetCurrentGridSize();

            // Create lines along each axis
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        GameObject lineObj = new GameObject($"Line_{axis}_{i}_{j}");
                        lineObj.transform.SetParent(controller, false);
                        LineRenderer line = lineObj.AddComponent<LineRenderer>();
                        line.startWidth = lineWidth;
                        line.endWidth = lineWidth;
                        line.positionCount = size;
                        lineRenderers.Add(line);
                    }
                }
            }
        }
        void UpdateConnectingLines()
        {
            int size = GetCurrentGridSize();
            int lineIndex = 0;

            // Update lines along each axis
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        LineRenderer line = lineRenderers[lineIndex++];
                        for (int k = 0; k < size; k++)
                        {
                            Vector3 pos = GetPointWorldForCurrentMode(axis, i, j, k);
                            line.SetPosition(k, pos);
                        }
                    }
                }
            }
        }
        Vector3 GetPointWorldForCurrentMode(int axis, int i, int j, int k)
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2:
                    return axis == 0 ? FFD2X2X2.GetPointWorld(k, i, j) :
                           axis == 1 ? FFD2X2X2.GetPointWorld(i, k, j) :
                                       FFD2X2X2.GetPointWorld(i, j, k);
                case FFDMode.FFD3x3x3:
                    return axis == 0 ? FFD3X3X3.GetPointWorld(k, i, j) :
                           axis == 1 ? FFD3X3X3.GetPointWorld(i, k, j) :
                                       FFD3X3X3.GetPointWorld(i, j, k);
                case FFDMode.FFD4x4x4:
                    return axis == 0 ? FFD4X4X4.GetPointWorld(k, i, j) :
                           axis == 1 ? FFD4X4X4.GetPointWorld(i, k, j) :
                                       FFD4X4X4.GetPointWorld(i, j, k);
                default:
                    return FFD3X3X3.GetPointWorld(i, j, k);
            }
        }

        void SetPointWorldForCurrentMode(int i, int j, int k, Vector3 worldPos)
        {
            switch (m_FFDMode)
            {
                case FFDMode.FFD2x2x2:
                    FFD2X2X2.SetPointWorld(i, j, k, worldPos);
                    break;
                case FFDMode.FFD3x3x3:
                    FFD3X3X3.SetPointWorld(i, j, k, worldPos);
                    break;
                case FFDMode.FFD4x4x4:
                    FFD4X4X4.SetPointWorld(i, j, k, worldPos);
                    break;
            }
        }

        // Update is called once per frame
        void LateUpdate()
        {
            //megaModifyObject.switchstate("FFD3x3x3", isFFD);
            // if (isFFD)
            // {
            UpdateFFDFromControlPoints();
            UpdateConnectingLines();
            //}
        }
    }
}