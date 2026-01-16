using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBezierBend : MonoBehaviour
{
    public Transform[] cpArr;
    public ForceItem forceItem;
    private Vector3[] oriVertices;
    private Mesh m_Mesh;
    private Vector3 topPos; // 末端相对p0的Z长度
    private Vector3 lastForceWorldPos;

    void Start()
    {
        // 用Z长度当“高”
        topPos = cpArr[cpArr.Length - 1].TransformPoint(new Vector3(0, 0, 0));
        topPos = cpArr[0].InverseTransformPoint(topPos); // z=长度
        lastForceWorldPos = forceItem.transform.position;

        m_Mesh = GetComponent<MeshFilter>().mesh;
        oriVertices = (Vector3[])m_Mesh.vertices.Clone();
        for (int i = 0; i < oriVertices.Length; i++)
        {
            oriVertices[i] = transform.TransformPoint(oriVertices[i]);
            oriVertices[i] = cpArr[0].InverseTransformPoint(oriVertices[i]);
        }
    }

    void Update()
    {
        UpdateControlPoint();
        UpdateBezierBend();
    }

    private void UpdateBezierBend()
    {
        Vector3 bendVector = Vector3.zero;
        bool isVertical = true;
        for (int i = 1; i < cpArr.Length; i++)
        {
            Vector3 pos = cpArr[i].TransformPoint(Vector3.zero);
            pos = cpArr[0].InverseTransformPoint(pos);
            if (!Mathf.Approximately(pos.x, 0f) || !Mathf.Approximately(pos.z, 0f))
            {
                bendVector.x = pos.x;
                bendVector.z = pos.z;
                isVertical = false;
                break;
            }
        }

        Vector3[] temp = (Vector3[])m_Mesh.vertices.Clone();
        for (int i = 0; i < oriVertices.Length; i++)
        {
            Vector3 oriPos = oriVertices[i];
            Vector3 bendPos;
            if (isVertical)
            {
                bendPos = oriPos;
            }
            else
            {
                float t = oriPos.z / topPos.z; // ← 用Z算t
                Vector3 bezierPos = CalculateBezier(t);
                Vector3 normalVector = GetBendNormalVector(t, oriPos, bendVector);
                Vector3 verticalVector =
                    new Vector3(oriPos.x, oriPos.y, 0)
                    - Vector3.Project(new Vector3(oriPos.x, oriPos.y, 0), bendVector);
                bendPos = bezierPos + normalVector + verticalVector;
            }
            bendPos = cpArr[0].TransformPoint(bendPos);
            bendPos = transform.InverseTransformPoint(bendPos);
            temp[i] = bendPos;
        }
        m_Mesh.vertices = temp;
    }

    private Vector3 CalculateBezier(float t)
    {
        Vector3 ret = Vector3.zero;
        int n = cpArr.Length - 1;
        for (int i = 0; i <= n; i++)
        {
            Vector3 pi = cpArr[i].TransformPoint(Vector3.zero);
            pi = cpArr[0].InverseTransformPoint(pi);
            ret += Mathf.Pow(1f - t, n - i) * Mathf.Pow(t, i) * Cn_m(n, i) * pi;
        }
        return ret;
    }

    private Vector3 CalculateBezierTangent(float t)
    {
        Vector3 ret = Vector3.zero;
        int n = cpArr.Length - 1;
        for (int i = 0; i <= n; i++)
        {
            Vector3 pi = cpArr[i].TransformPoint(Vector3.zero);
            pi = cpArr[0].InverseTransformPoint(pi);
            ret += (-(n - i) * Mathf.Pow(1f - t, n - i - 1) * Mathf.Pow(t, i) * Cn_m(n, i) * pi
                 + i * Mathf.Pow(1f - t, n - i) * Mathf.Pow(t, i - 1) * Cn_m(n, i) * pi);
        }
        return ret;
    }

    private Vector3 GetBendNormalVector(float t, Vector3 oriPos, Vector3 bendVector)
    {
        Vector3 tangent = CalculateBezierTangent(t);
        if (Mathf.Approximately(tangent.x, 0f) && Mathf.Approximately(tangent.z, 0f))
            return Vector3.Project(new Vector3(oriPos.x, oriPos.y, 0), bendVector);

        Vector3 normal = Vector3.zero;
        float flag = Vector3.Dot(bendVector, new Vector3(oriPos.x, 0, oriPos.z));
        if (flag > 0)
        {
            if (Mathf.Approximately(tangent.y, 0f)) normal.y = -1f;
            else
            {
                normal.x = tangent.y > 0 ? tangent.x : -tangent.x;
                normal.z = tangent.y > 0 ? tangent.z : -tangent.z;
                normal.y = -(tangent.x * normal.x + tangent.z * normal.z) / tangent.y;
            }
        }
        else
        {
            if (Mathf.Approximately(tangent.y, 0f)) normal.y = 1f;
            else
            {
                normal.x = tangent.y > 0 ? -tangent.x : tangent.x;
                normal.z = tangent.y > 0 ? -tangent.z : tangent.z;
                normal.y = -(tangent.x * normal.x + tangent.z * normal.z) / tangent.y;
            }
        }
        float magnitude = Vector3.Project(new Vector3(oriPos.x, oriPos.y, 0), bendVector).magnitude;
        return normal.normalized * magnitude;
    }

    private bool IsEqualZero(float value) => Mathf.Abs(value) < 1e-5f;

    private int Cn_m(int n, int m)
    {
        int ret = 1;
        for (int i = 0; i < m; i++) ret = ret * (n - i) / (i + 1);
        return ret;
    }

    private void UpdateControlPoint()
    {
        Vector3 delta = forceItem.transform.position - lastForceWorldPos;
        Transform endPt = cpArr[cpArr.Length - 1];
        endPt.position += delta;
        lastForceWorldPos = forceItem.transform.position;
    }
}