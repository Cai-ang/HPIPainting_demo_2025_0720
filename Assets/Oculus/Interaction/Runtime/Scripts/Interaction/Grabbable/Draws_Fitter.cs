using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MathNet.Numerics.LinearAlgebra;
using System.Linq;

namespace Fitter
{
    public class Draws_Fitter : MonoBehaviour
    {
        #region  圆拟合
        public (Vector3 center, float radius, Vector3 normal) FitCircle3DRANSAC(List<Vector3> points, int iterations = 100, float planeDistanceThreshold = 0.1f, float radiusErrorThreshold = 0.1f)
        {
            int maxInnerCount = 0;
            Vector3 bestCenter = Vector3.zero;
            float bestRadius = 0f;
            Vector3 bestNormal = Vector3.up;

            System.Random random = new System.Random();

            for (int iter = 0; iter < iterations; iter++)
            {
                // 随机选择三个点
                int n1 = random.Next(points.Count);
                int n2 = random.Next(points.Count);
                int n3 = random.Next(points.Count);

                Vector3 p1 = points[n1];
                Vector3 p2 = points[n2];
                Vector3 p3 = points[n3];

                // 计算平面方程
                float A1 = (p2.y - p1.y) * (p3.z - p1.z) - (p2.z - p1.z) * (p3.y - p1.y);
                float B1 = (p2.z - p1.z) * (p3.x - p1.x) - (p2.x - p1.x) * (p3.z - p1.z);
                float C1 = (p2.x - p1.x) * (p3.y - p1.y) - (p2.y - p1.y) * (p3.x - p1.x);
                float D1 = -(A1 * p1.x + B1 * p1.y + C1 * p1.z);

                // 计算圆心方程
                float A2 = 2 * (p2.x - p1.x);
                float B2 = 2 * (p2.y - p1.y);
                float C2 = 2 * (p2.z - p1.z);
                float D2 = p1.x * p1.x + p1.y * p1.y + p1.z * p1.z - p2.x * p2.x - p2.y * p2.y - p2.z * p2.z;

                float A3 = 2 * (p3.x - p1.x);
                float B3 = 2 * (p3.y - p1.y);
                float C3 = 2 * (p3.z - p1.z);
                float D3 = p1.x * p1.x + p1.y * p1.y + p1.z * p1.z - p3.x * p3.x - p3.y * p3.y - p3.z * p3.z;

                // 解线性方程组
                var A = Matrix<float>.Build.DenseOfArray(new float[,] {
                { A1, B1, C1 },
                { A2, B2, C2 },
                { A3, B3, C3 }
            });

                var D = Vector<float>.Build.Dense(new float[] { -D1, -D2, -D3 });

                var X = A.Solve(D);

                Vector3 center = new Vector3(X[0], X[1], X[2]);
                float radius = (Vector3.Distance(center, p1) + Vector3.Distance(center, p2) + Vector3.Distance(center, p3)) / 3f;

                int innerCount = 0;
                foreach (var point in points)
                {
                    float planeDistance = Mathf.Abs(A1 * point.x + B1 * point.y + C1 * point.z + D1) / Mathf.Sqrt(A1 * A1 + B1 * B1 + C1 * C1);
                    float radiusError = Mathf.Abs(Vector3.Distance(center, point) - radius);

                    if (planeDistance < planeDistanceThreshold && radiusError < radiusErrorThreshold)
                    {
                        innerCount++;
                    }
                }

                if (innerCount > maxInnerCount)
                {
                    maxInnerCount = innerCount;
                    bestCenter = center;
                    bestRadius = radius;
                    bestNormal = new Vector3(A1, B1, C1).normalized;
                }
            }

            return (bestCenter, bestRadius, bestNormal);
        }
        public (bool shouldCircle, Vector3 center, float radius, Vector3 normal) ShouldCreateCircle(List<Vector3> points)
        {
            if (points.Count < 5) // 确保有足够的点
            {
                return (false, Vector3.zero, float.NaN, Vector3.zero);
            }

            // 使用RANSAC拟合圆
            (Vector3 center, float radius, Vector3 normal) = FitCircle3DRANSAC(points);

            // 计算平均误差
            float totalError = 0;
            foreach (Vector3 point in points)
            {
                Vector3 projectedPoint = Vector3.ProjectOnPlane(point - center, normal) + center;
                float distanceToCenter = Vector3.Distance(projectedPoint, center);
                totalError += Mathf.Abs(distanceToCenter - radius);
            }
            float averageError = totalError / points.Count;

            // 计算点的分布范围
            Vector3 minBounds = points[0], maxBounds = points[0];
            foreach (Vector3 point in points)
            {
                minBounds = Vector3.Min(minBounds, point);
                maxBounds = Vector3.Max(maxBounds, point);
            }
            float distributionSize = (maxBounds - minBounds).magnitude;

            float starttoend_distance = Vector3.Distance(points[0], points[points.Count - 1]);

            // 设定阈值
            float maxAllowedError = radius * 0.2f; // 允许10%的误差
            float minDistributionSize = radius * 1.5f; // 点的分布范围应至少是半径的1.5倍
            float starttoenderror = radius * 0.4f; // 首尾点的距离误差允许10%

            // 判定条件
            bool errorIsSmall = averageError < maxAllowedError;
            bool distributionIsLarge = distributionSize > minDistributionSize;
            bool starttoendisnotfar = starttoenderror > starttoend_distance;

            return (errorIsSmall && distributionIsLarge && starttoendisnotfar, center, radius, normal);
        }
        #endregion

        #region 直线拟合
        public bool FitLine3D(List<Vector3> points)
        {
            if (points == null || points.Count < 5)
            {
                Debug.LogError("Need at least two points to fit a line.");
                return false;
            }

            // 计算质心
            Vector3 centroid = points.Aggregate(Vector3.zero, (sum, p) => sum + p) / points.Count;

            // 构建协方差矩阵
            Matrix4x4 covMatrix = new Matrix4x4();
            foreach (Vector3 p in points)
            {
                Vector3 diff = p - centroid;
                covMatrix[0, 0] += diff.x * diff.x;
                covMatrix[0, 1] += diff.x * diff.y;
                covMatrix[0, 2] += diff.x * diff.z;
                covMatrix[1, 1] += diff.y * diff.y;
                covMatrix[1, 2] += diff.y * diff.z;
                covMatrix[2, 2] += diff.z * diff.z;
            }
            covMatrix[1, 0] = covMatrix[0, 1];
            covMatrix[2, 0] = covMatrix[0, 2];
            covMatrix[2, 1] = covMatrix[1, 2];

            // 除以点的数量
            float invCount = 1.0f / points.Count;
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    covMatrix[i, j] *= invCount;
                }
            }

            // 计算最大特征值对应的特征向量
            Vector3 direction = GetPrincipalComponent(covMatrix);
            float lineError = CalculateError(points, centroid, direction);

            // 设定一个阈值来判断是否足够接近直线
            float lineErrorThreshold = 0.03f; // 可以根据需要调整这个值
            Debug.Log("lineError:" + lineError);
            if (lineError < lineErrorThreshold)
            {
                return true;
            }
            else return false;
        }

        private static Vector3 GetPrincipalComponent(Matrix4x4 matrix)
        {
            // 使用幂法计算最大特征值对应的特征向量
            Vector3 v = new Vector3(1, 1, 1).normalized;
            for (int i = 0; i < 10; i++) // 10次迭代通常足够
            {
                Vector3 vNew = matrix.MultiplyVector(v);
                v = vNew.normalized;
            }
            return v;
        }

        public float CalculateError(List<Vector3> points, Vector3 linePoint, Vector3 lineDirection)
        {
            float totalError = 0;
            foreach (Vector3 point in points)
            {
                Vector3 toPoint = point - linePoint;
                Vector3 projection = Vector3.Project(toPoint, lineDirection);
                float distance = Vector3.Distance(toPoint, projection);
                totalError += distance * distance;
            }
            return Mathf.Sqrt(totalError / points.Count);
        }

        public bool IsConsistentLine(List<Vector3> points, out Vector3 direction)
        {
            direction = Vector3.zero;
            if (points.Count < 3)
                return false;

            Vector3 overallDirection = (points[points.Count - 1] - points[0]).normalized;
            float totalDeviation = 0;
            float maxDeviation = 0;

            for (int i = 1; i < points.Count; i++)
            {
                Vector3 segmentDirection = (points[i] - points[i - 1]).normalized;
                float deviation = Vector3.Angle(segmentDirection, overallDirection);
                totalDeviation += deviation;
                maxDeviation = Mathf.Max(maxDeviation, deviation);
                //Debug.Log("deviation:" + deviation);
                // 如果任何段的方向与整体方向相差太大，就不是一致的直线
                if (deviation > 90) // 可以根据需要调整这个角度
                {
                    return false;
                }
            }

            float averageDeviation = totalDeviation / (points.Count - 1);
            //Debug.Log("averageDeviation:" + averageDeviation);
            //Debug.Log("maxDeviation" + maxDeviation);
            // 检查平均偏差和最大偏差是否在可接受范围内
            if (averageDeviation < 30 && maxDeviation < 90) // 这些阈值也可以根据需要调整
            {
                direction = overallDirection;
                return true;
            }

            return false;
        }
        #endregion
    }
}