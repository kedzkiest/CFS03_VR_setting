using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CCDIK1 : MonoBehaviour
{
    // 机器人参数
    public ForwardKinematics fK;
    public int numJoint = 7; // 关节数量
    private Vector3[] jPositions; // 关节位置数组
    private Quaternion[] jRotations; // 关节旋转数组

    // CCD参数
    public int maxItreration = 20; // 最大迭代次数
    public float tolerancePosition = 0.01f; // 位置公差，单位为米
    public float toleranceRotation = 0.0872665f; // 旋转公差，单位为弧度 (大约5度)
    private float[] jointAngles; // 关节角度数组
    private bool success; // 是否成功收敛

    void Start()
    {
        // 初始化关节角度数组
        jointAngles = new float[numJoint];
    }

    // 设置目标位置和旋转
    public (Vector3, Quaternion) SetTarget(Vector3 newTargetPosition, Quaternion newTargetRotation, bool fromRUF = true)
    {
        Vector3 targetPosition = newTargetPosition;
        Quaternion targetRotation = newTargetRotation;

        // 如果需要从右手坐标系转换为Unity坐标系
        if (fromRUF)
        {
            targetPosition = FromRUF(newTargetPosition);
            targetRotation = FromRUF(newTargetRotation);
        }
        return (targetPosition, targetRotation); // 返回目标位置和旋转
    }

    // CCD算法
    public (float[], bool) CCD(float[] convertjointangles, Vector3 targetPosition, Quaternion targetRotation)
    {
        // 更新前向运动学
        fK.UpdateAllH(convertjointangles);
        success = false;

        // CCD迭代
        for (int i = 0; i < maxItreration; ++i)
        {
            // 获取当前末端执行器的位置和旋转
            var (endPosition, endRotation) = fK.GetPose(numJoint);

            // 如果位置和旋转都收敛，结束迭代
            if (IsPositionConverged(endPosition, targetPosition) && IsRotationConverged(endRotation, targetRotation))
            {
                success = true;
                break;
            }

            // 检查位置收敛
            (endPosition, endRotation) = fK.GetPose(numJoint);
            if (!IsPositionConverged(endPosition, targetPosition))
            {
                // 逆向最小化位置误差
                for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint)
                {
                    (jPositions, jRotations) = fK.GetAllPose(); // 获取所有关节位姿
                    UpdateJointAngle(iJoint, targetPosition, targetRotation, true); // 更新关节角度
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]); // 更新关节位置
                }
            }

            // 检查旋转收敛
            (endPosition, endRotation) = fK.GetPose(numJoint);
            if (!IsRotationConverged(endRotation, targetRotation))
            {
                // 逆向最小化旋转误差
                for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint)
                {
                    (jPositions, jRotations) = fK.GetAllPose();
                    UpdateJointAngle(iJoint, targetPosition, targetRotation, false); // 更新关节角度
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]);
                }
            }

            // 检查位置收敛
            (endPosition, endRotation) = fK.GetPose(numJoint);
            if (!IsPositionConverged(endPosition, targetPosition))
            {
                // 正向最小化位置误差
                for (int iJoint = 0; iJoint < numJoint - 1; ++iJoint)
                {
                    (jPositions, jRotations) = fK.GetAllPose(); // 获取所有关节位姿
                    UpdateJointAngle(iJoint, targetPosition, targetRotation, true); // 更新关节角度
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]);
                }
            }
        }
        return (jointAngles, success); // 返回最终的关节角度和是否成功
    }

    // 更新关节角度
    private void UpdateJointAngle(int iJoint, Vector3 targetPosition, Quaternion targetRotation, bool forPosition = true)
    {
        // 获取当前关节的旋转矩阵
        Matrix4x4 H = Matrix4x4.TRS(Vector3.zero, jRotations[iJoint], Vector3.one);
        Vector3 jointZDirection = new Vector3(H[0, 2], H[1, 2], H[2, 2]); // 关节z轴方向

        float theta;
        if (forPosition)
        {
            // 计算位置的误差
            Vector3 endPosition = jPositions[numJoint]; // 末端执行器位置
            Vector3 jointPosition = jPositions[iJoint]; // 当前关节位置

            // 从当前关节到末端执行器的单位向量
            Vector3 endVector = (endPosition - jointPosition).normalized;
            // 从当前关节到目标位置的单位向量
            Vector3 targetVector = (targetPosition - jointPosition).normalized;

            // 计算当前关节与目标位置之间的旋转角度
            float vectorAngle = Mathf.Clamp(Vector3.Dot(endVector, targetVector), -1, 1);
            theta = Mathf.Abs(Mathf.Acos(vectorAngle));
            Vector3 direction = Vector3.Cross(endVector, targetVector);

            // 将期望角度映射到关节z轴方向
            theta = theta * Vector3.Dot(direction.normalized, jointZDirection.normalized);
        }
        else
        {
            Quaternion endRotation = jRotations[numJoint]; // 末端执行器旋转
            // 计算当前关节与目标旋转之间的误差
            float errRotation = error(targetRotation, endRotation);

            if (Mathf.Abs(Mathf.PI - errRotation) < 0.02f)
                theta = 0.2f; // 小的误差，设定为一个常量值
            else if (Mathf.Abs(errRotation) < 0.02f)
                theta = 0; // 已经足够接近，设置为0
            else
            {
                // 计算旋转误差
                Quaternion q = targetRotation * Quaternion.Inverse(endRotation);
                Vector3 direction = new Vector3(q.x, q.y, q.z) * q.w * 2 / Mathf.Sin(errRotation);

                // 将期望角度映射到关节z轴方向
                theta = errRotation * Vector3.Dot(direction.normalized, jointZDirection.normalized);
            }
        }
        jointAngles[iJoint] += theta; // 更新关节角度
        jointAngles[iJoint] = JointLimit(iJoint, jointAngles[iJoint]); // 限制关节角度
    }

    // 计算两个向量之间的误差
    private float error(Vector3 p1, Vector3 p2)
    {
        return (p1 - p2).magnitude; // 返回向量的长度
    }

    // 计算两个四元数之间的误差
    private float error(Quaternion q1, Quaternion q2)
    {
        Quaternion q = q1 * Quaternion.Inverse(q2);
        float theta = Mathf.Clamp(Mathf.Abs(q.w), -1, 1); // 防止溢出
        float errRotation = 2 * Mathf.Acos(theta); // 计算旋转误差
        return errRotation;
    }

    // 检查位置是否收敛
    private bool IsPositionConverged(Vector3 endPosition, Vector3 targetPosition)
    {
        float errPosition = error(targetPosition, endPosition); // 计算位置误差
        return errPosition < tolerancePosition; // 如果小于公差则认为收敛
    }

    // 检查旋转是否收敛
    private bool IsRotationConverged(Quaternion endRotation, Quaternion targetRotation)
    {
        float errRotation = error(targetRotation, endRotation); // 计算旋转误差
        return errRotation < toleranceRotation; // 如果小于公差则认为收敛
    }

    // 限制关节角度在最大最小角度范围内
    private float JointLimit(int iJoint, float angle)
    {
        // 获取关节的最小和最大角度
        float minAngle = fK.angleLowerLimits[iJoint];
        float maxAngle = fK.angleUpperLimits[iJoint];

        // 如果有角度限制，则在最小和最大角度之间进行限制
        if (minAngle != maxAngle)
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
        // 如果没有角度限制，则将角度限制在[-pi, pi]范围内
        else
            angle = WrapToPi(angle);

        return angle;
    }

    // 将角度限制在[-pi, pi]之间
    private float WrapToPi(float angle)
    {
        float pi = Mathf.PI;
        angle = angle - 2 * pi * Mathf.Floor((angle + pi) / (2 * pi)); // 将角度规范化到[-pi, pi]
        return angle;
    }

    // 从右手坐标系转换为Unity坐标系
    private Vector3 FromRUF(Vector3 p)
    {
        return new Vector3(p.z, -p.x, p.y); // 转换公式
    }

    private Quaternion FromRUF(Quaternion q)
    {
        return new Quaternion(q.z, -q.x, q.y, -q.w); // 转换公式
    }
}
