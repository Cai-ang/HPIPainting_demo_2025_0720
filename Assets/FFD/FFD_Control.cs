using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MegaFiers;
using UnityEditor;
using Unity.VisualScripting;

namespace FFD
{
    public class FFD_Control : MonoBehaviour
    {
        MegaModifyObject megaModifyObject;
        public FFDMode m_FFDMode = FFDMode.FFD3x3x3;
        public float sphereRadius = 0.1f;
        public float lineWidth = 0.02f;
        MegaFFD3x3x3 FFD3X3X3;
        private List<LineRenderer> lineRenderers = new List<LineRenderer>();
        public enum FFDMode
        {
            FFD2x2x2 = 2,
            FFD3x3x3 = 3,
            FFD4x4x4 = 4
        }
        public bool isFFD = false;
        private GameObject[] controlPointSpheres;
        // Start is called before the first frame update
        void Start()
        {
            megaModifyObject = this.AddComponent<MegaModifyObject>();
            if (this.GetComponent<MegaFFD3x3x3>() == null)
            { AddFFD333(); }
            CreateControlPointSpheres();
            CreateConnectingLines();
            //megaFFD3X3X3.reset3x3x3();
        }

        public void RemoveMod(string modname)
        {
            MegaModifyObject modifyObject = this.GetComponent<MegaModifyObject>();
            int i = 0;
            foreach (MegaModifier m in modifyObject.mods)
            {
                if (m.ModName() == modname)
                {
                    modifyObject.mods[i] = null;
                    DestroyImmediate(m);
                    modifyObject.BuildList();
                    //ApplyModsToGroup(modifyObject);
                    break;
                }
                i++;
            }
        }


        private void AddFFD333()
        {
            FFD3X3X3 = this.AddComponent<MegaFFD3x3x3>();
            FFD3X3X3.FitFFD();
            FFD3X3X3.FitFFDToMesh();
        }
        void CreateControlPointSpheres()
        {
            int numPoints = FFD3X3X3.NumPoints();
            controlPointSpheres = new GameObject[numPoints];

            for (int i = 0; i < numPoints; i++)
            {
                GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                sphere.transform.localScale = Vector3.one * sphereRadius;
                //sphere.GetComponent<Renderer>().material = sphereMaterial;
                sphere.transform.SetParent(transform, false);
                //controlPointSpheres[i] = pen.interactableobj(sphere, "con_sphere", false);
                //controlPointSpheres[i] = sphere;
            }

            UpdateControlPointPositions();
        }

        void UpdateControlPointPositions()
        {
            int size = FFD3X3X3.GridSize();
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
        }
        void UpdateFFDFromControlPoints()
        {
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    for (int k = 0; k < 3; k++)
                    {
                        int index = FFD3X3X3.GridIndex(i, j, k);
                        Vector3 worldPos = controlPointSpheres[index].transform.position;
                        FFD3X3X3.SetPointWorld(i, j, k, worldPos);
                    }
                }
            }
        }

        void CreateConnectingLines()
        {
            int size = FFD3X3X3.GridSize();

            // Create lines along each axis
            for (int axis = 0; axis < 3; axis++)
            {
                for (int i = 0; i < size; i++)
                {
                    for (int j = 0; j < size; j++)
                    {
                        GameObject lineObj = new GameObject($"Line_{axis}_{i}_{j}");
                        lineObj.transform.SetParent(transform, false);
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
            int size = FFD3X3X3.GridSize();
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
                            Vector3 pos;
                            switch (axis)
                            {
                                case 0: pos = FFD3X3X3.GetPointWorld(k, i, j); break;
                                case 1: pos = FFD3X3X3.GetPointWorld(i, k, j); break;
                                default: pos = FFD3X3X3.GetPointWorld(i, j, k); break;
                            }
                            line.SetPosition(k, pos);
                        }
                    }
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            megaModifyObject.switchstate("FFD3x3x3", isFFD);
            if (isFFD)
            {
                UpdateFFDFromControlPoints();
                UpdateConnectingLines();
            }
        }
    }
}