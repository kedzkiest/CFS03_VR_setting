using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class CCDIK3 : MonoBehaviour
{
    // 机器人参数
    public ForwardKinematics fK;  // 正向运动学对象
    public int numJoint = 7;  // 机器人关节数量
    private Vector3[] jPositions;  // 关节位置数组
    private Quaternion[] jRotations;  // 关节旋转数组

    // CCD参数
    public int maxItreration = 20;  // 最大迭代次数
    public float tolerancePosition = 0.01f;  // 位置收敛误差，单位：米
    public float toleranceRotation = 0.0872665f;  // 旋转收敛误差，单位：弧度

    private float[] jointAngles;  // 关节角度数组
    private Vector3 targetPosition;  // 目标位置
    private Quaternion targetRotation;  // 目标旋转
    private bool success;  // 是否成功

    void Start()
    {
        // 初始化关节角度
        jointAngles = new float[numJoint];
    }

    // 设置目标位置和目标旋转，转换坐标系
    public void SetTarget(Vector3 newTargetPosition, Quaternion newTargetRotation, bool fromRUF = true)
    {
        targetPosition = newTargetPosition;
        targetRotation = newTargetRotation;
        if (fromRUF)
        {
            // 如果需要从右手坐标系(RUF)转换到Unity坐标系
            targetPosition = FromRUF(newTargetPosition);
            targetRotation = FromRUF(newTargetRotation);
        }
        // return (targetPosition, targetRotation);  // 可选，返回目标位置和旋转
    }

    // CCD算法，计算关节角度
    public (float[], bool) CCD(float[] convertjointangles)
    {
        // 使用当前关节角度更新正向运动学模型
        fK.UpdateAllH(convertjointangles);
        success = false;

        // CCD迭代
        for (int i = 0; i < maxItreration; ++i)
        {
            // 获取末端执行器的位置和旋转
            var (endPosition, endRotation) = fK.GetPose(numJoint);

            // 判断位置和旋转是否收敛
            if (IsPositionConverged(endPosition) && IsRotationConverged(endRotation))
            {
                success = true;
                break;  // 如果收敛，跳出循环
            }

            // 检查位置收敛
            if (!IsPositionConverged(endPosition))
            {
                // 最小化位置误差 - 从末端关节往回调整
                for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint) // 从7到1
                {
                    (jPositions, jRotations) = fK.GetAllPose();  // 获取所有关节的位姿
                    UpdateJointAngle(iJoint, true);  // 更新关节角度（位置）
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]);  // 更新第i个关节的变换矩阵
                }
            }

            // 检查旋转收敛
            if (!IsRotationConverged(endRotation))
            {
                // 最小化旋转误差 - 从末端关节往回调整
                for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint)
                {
                    (jPositions, jRotations) = fK.GetAllPose();
                    UpdateJointAngle(iJoint, false);  // 更新关节角度（旋转）
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]);
                }
            }

            // 再次检查位置收敛
            if (!IsPositionConverged(endPosition))
            {
                // 最小化位置误差 - 从前向后调整
                for (int iJoint = 0; iJoint < numJoint - 1; ++iJoint)
                {
                    (jPositions, jRotations) = fK.GetAllPose();  // 获取所有关节的位姿
                    UpdateJointAngle(iJoint, true);  // 更新关节角度（位置）
                    fK.UpdateH(iJoint + 1, jointAngles[iJoint]);  // 更新第i个关节的变换矩阵
                }
            }
        }

        return (jointAngles, success);  // 返回计算得到的关节角度和是否成功
    }

    // 更新某个关节的角度
    private void UpdateJointAngle(int iJoint, bool forPosition = true)
    {
        // 获取当前关节的旋转矩阵
        Matrix4x4 H = Matrix4x4.TRS(Vector3.zero, jRotations[iJoint], Vector3.one);
        Vector3 jointZDirection = new Vector3(H[0, 2], H[1, 2], H[2, 2]);

        // 获取下一个步骤的旋转角度
        float theta;
        if (forPosition)
        {
            // 计算位置误差并调整关节角度
            Vector3 endPosition = jPositions[numJoint];  // 末端执行器位置
            Vector3 jointPosition = jPositions[iJoint];  // 当前关节位置

            // 从当前关节到末端执行器的单位向量
            Vector3 endVector = (endPosition - jointPosition).normalized;
            // 从当前关节到目标位置的单位向量
            Vector3 targetVector = (targetPosition - jointPosition).normalized;

            // 计算两个向量的夹角
            float vectorAngle = Mathf.Clamp(Vector3.Dot(endVector, targetVector), -1, 1);
            theta = Mathf.Abs(Mathf.Acos(vectorAngle));
            Vector3 direction = Vector3.Cross(endVector, targetVector);

            // 计算关节旋转角度
            theta = theta * Vector3.Dot(direction.normalized, jointZDirection.normalized);
        }
        else
        {
            // 计算旋转误差并调整关节角度
            Quaternion endRotation = jRotations[numJoint];  // 末端执行器旋转
            float errRotation = error(targetRotation, endRotation);  // 计算旋转误差

            // 根据误差选择合适的角度
            if (Mathf.Abs(Mathf.PI - errRotation) < 0.02f)
                theta = 0.2f;  // 如果误差接近180度，则小幅调整
            else if (Mathf.Abs(errRotation) < 0.02f)
                theta = 0;
            else
            {
                // 计算旋转角度
                Quaternion q = targetRotation * Quaternion.Inverse(endRotation);
                Vector3 direction = new Vector3(q.x, q.y, q.z) * q.w * 2 / Mathf.Sin(errRotation);
                theta = errRotation * Vector3.Dot(direction.normalized, jointZDirection.normalized);
            }
        }

        // 更新关节角度
        jointAngles[iJoint] += theta;
        jointAngles[iJoint] = JointLimit(iJoint, jointAngles[iJoint]);  // 限制关节角度
    }

    // 计算两点之间的欧几里得距离
    private float error(Vector3 p1, Vector3 p2)
    {
        return (p1 - p2).magnitude;
    }

    // 计算两个四元数之间的旋转误差
    private float error(Quaternion q1, Quaternion q2)
    {
        Quaternion q = q1 * Quaternion.Inverse(q2);
        float theta = Mathf.Clamp(Mathf.Abs(q.w), -1, 1);
        float errRotation = 2 * Mathf.Acos(theta);
        return errRotation;
    }

    // 判断位置是否收敛
    private bool IsPositionConverged(Vector3 endPosition)
    {
        float errPosition = error(targetPosition, endPosition);
        return errPosition < tolerancePosition;
    }

    // 判断旋转是否收敛
    private bool IsRotationConverged(Quaternion endRotation)
    {
        float errRotation = error(targetRotation, endRotation);
        return errRotation < toleranceRotation;
    }

    // 限制关节角度在允许的范围内
    private float JointLimit(int iJoint, float angle)
    {
        // 获取关节的最小和最大角度限制
        float minAngle = fK.angleLowerLimits[iJoint];
        float maxAngle = fK.angleUpperLimits[iJoint];

        // 如果有角度限制，则进行限制
        if (minAngle != maxAngle)
            angle = Mathf.Clamp(angle, minAngle, maxAngle);
        // 如果没有角度限制，则将角度限制在[-π, π]之间
        else
            angle = WrapToPi(angle);

        return angle;
    }

    // 将角度限制在[-π, π]之间
    private float WrapToPi(float angle)
    {
        float pi = Mathf.PI;
        angle = angle - 2 * pi * Mathf.Floor((angle + pi) / (2 * pi));  // 将角度规范化到[-π, π]
        return angle;
    }

    // 从右手坐标系（RUF）转换为Unity坐标系
    private Vector3 FromRUF(Vector3 p)
    {
        return new Vector3(p.z, -p.x, p.y);
    }

    // 从右手坐标系（RUF）转换为Unity四元数
    private Quaternion FromRUF(Quaternion q)
    {
        return new Quaternion(q.z, -q.x, q.y, -q.w);
    }
}
