using UnityEngine;
using System.Collections;

//挂到Renderer上
public class TestFFD_GPU : MonoBehaviour
{
    public FFD_CS.FFDMode m_ffdMode = FFD_CS.FFDMode.FFD2x2x2;
    public bool m_isShowControlPoints = true;

    FFD_CS m_ffd = null;
    void Start()
    {
        m_ffd = gameObject.GetComponent<FFD_CS>();
        if (m_ffd == null)
        {
            m_ffd = gameObject.AddComponent<FFD_CS>();
        }
        m_ffd.m_isDebug = m_isShowControlPoints;
        m_ffd.InitiateCompleteCallBack = null;
        m_ffd.m_ffdMode = m_ffdMode;
    }

    void Update()
    {
        if (m_ffd != null)
        {
            m_ffd.ExecuteFFD();
        }
    }
}
