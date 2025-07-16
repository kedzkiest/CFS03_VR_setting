using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace three
{
    public class CCDIK : MonoBehaviour
    {
        // Robot parameters
        public ForwardKinematics fK;
        public int numJoint = 7;
        private Vector3[] jPositions;
        private Quaternion[] jRotations;

        // CCD parameters
        public int maxItreration = 20; // 最大迭代数
        public float tolerancePosition = 0.01f; // 允许误差
        public float toleranceRotation = 0.0872665f; // 允许角度误差

        private float[] jointAngles;
        //目标位姿
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private bool success;

        void Start()
        {
            // 当前关节角度
            jointAngles = new float[numJoint];
        }

       
        public void SetJointAngle(float[] currentJointAngles)
        {
            jointAngles = currentJointAngles;
        }
        public void SetTarget(Vector3 newTargetPosition,
                              Quaternion newTargetRotation,
                              bool fromRUF = true)
        {
            targetPosition = newTargetPosition;
            targetRotation = newTargetRotation;
            if (fromRUF)
            {
                //左手转为右手坐标系
                targetPosition = FromRUF(newTargetPosition);
                targetRotation = FromRUF(newTargetRotation);
            }
        }
        
        public (float[], bool) CCD()
        {
            fK.UpdateAllH(jointAngles);//输入当前关节角度，通过正运动学获取当前各个关节位姿
            success = false;
            // CCD循环迭代
            for (int i = 0; i < maxItreration; ++i)
            {
                // Check convergence
                var (endPosition, endRotation) = fK.GetPose(numJoint);//得到末端关节位姿 numJoint=7
              //  Debug.Log("endposition:" + endPosition);
               //判断位姿差
                if (IsPositionConverged(endPosition) && IsRotationConverged(endRotation))
                {
                    success = true;
                    break;
                }

                // Check position convergence
                //获取的是末端end的位置和姿态
                (endPosition, endRotation) = fK.GetPose(numJoint);
                //位置差距更新位置
                if (!IsPositionConverged(endPosition))
                {
                    // Minimize position error - backwards
                    for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint)//0-6
                    {
                        (jPositions, jRotations) = fK.GetAllPose();//返回一个元组 当前机械臂各个关节的位姿
                        UpdateJointAngle(iJoint, true);
                        fK.UpdateH(iJoint + 1, jointAngles[iJoint]);//1-7
                    }
                }

                // Check rotation convergence
                //获取的是末端位置和姿态
                (endPosition, endRotation) = fK.GetPose(numJoint);
                //姿态差距更新姿态
                if (!IsRotationConverged(endRotation))
                {
                    // Minimize rotation error - backwards
                    for (int iJoint = numJoint - 1; iJoint >= 0; --iJoint)
                    {
                        (jPositions, jRotations) = fK.GetAllPose();
                        UpdateJointAngle(iJoint, false);
                        fK.UpdateH(iJoint + 1, jointAngles[iJoint]);
                    }
                }

                // Check position convergence again
                (endPosition, endRotation) = fK.GetPose(numJoint);
                if (!IsPositionConverged(endPosition))
                {
                    // Minimize position error - forwards
                    for (int iJoint = 0; iJoint < numJoint - 1; ++iJoint)
                    {
                        (jPositions, jRotations) = fK.GetAllPose();//后面要用
                        UpdateJointAngle(iJoint, true);
                        fK.UpdateH(iJoint + 1, jointAngles[iJoint]);
                    }
                }

               
            }
            return (jointAngles, success);
        }
        //迭代函数
        private void UpdateJointAngle(int iJoint, bool forPosition = true)
        {
            // Update position 
            Matrix4x4 H = Matrix4x4.TRS(Vector3.zero, jRotations[iJoint], Vector3.one);//得到一个其次旋转矩阵，其中p为0，缩放因子为1
            // z轴方向
            Vector3 jointZDirection = new Vector3(H[0, 2], H[1, 2], H[2, 2]);

            // Get rotation angle of next step
            float theta;
            //计算位置差距
            if (forPosition)
            {
                Vector3 endPosition = jPositions[numJoint];//7
                Vector3 jointPosition = jPositions[iJoint];//0-6

                // Unit vector from current joint to end effector
                Vector3 endVector = (endPosition - jointPosition).normalized;
                // Unit vector from current joint to target
                Vector3 targetVector = (targetPosition - jointPosition).normalized;

                // Rotate current joint to match end effector vector to target vector
                float vectorAngle = Mathf.Clamp(Vector3.Dot(endVector, targetVector), -1, 1);//限制大小
                theta = Mathf.Abs(Mathf.Acos(vectorAngle));
                Vector3 direction = Vector3.Cross(endVector, targetVector);

               
                theta = theta * Vector3.Dot(direction.normalized, jointZDirection.normalized);
            }
            //计算姿态差距
            else
            {
                Quaternion endRotation = jRotations[numJoint];//rotation是旋转矩阵，末端执行器
                                                              // Rotate current joint to match end effector rotation to target rotation
                float errRotation = error(targetRotation, endRotation);

                if (Mathf.Abs(Mathf.PI - errRotation) < 0.02f)
                    theta = 0.2f;//0.2f大约是11.5度
                else if (Mathf.Abs(errRotation) < 0.02f)
                    theta = 0;
                else
                {
                    //计算关节姿态差距
                    Quaternion q = targetRotation * Quaternion.Inverse(endRotation);
                    //利用四元数计算旋转方向
                    Vector3 direction = new Vector3(q.x, q.y, q.z) * q.w * 2 / Mathf.Sin(errRotation);
                    //计算旋转角度
                    theta = errRotation * Vector3.Dot(direction.normalized, jointZDirection.normalized);
                }
            }
            jointAngles[iJoint] += theta;
            //限制旋转a
            jointAngles[iJoint] = JointLimit(iJoint, jointAngles[iJoint]);
        }
        //位姿误差计算
        private float error(Vector3 p1, Vector3 p2)
        {
            return (p1 - p2).magnitude;//返回向量的长度
        }
        //四元数计算，知道这么算就可以了
        private float error(Quaternion q1, Quaternion q2)
        {
            Quaternion q = q1 * Quaternion.Inverse(q2);//计算角度差
            float theta = Mathf.Clamp(Mathf.Abs(q.w), -1, 1); // avoid overflow 计算方向
            float errRotation = 2 * Mathf.Acos(theta);//获取旋转角度
            return errRotation;
        }
        //检查末端与目标位置差距
        private bool IsPositionConverged(Vector3 endPosition)
        {
            float errPosition = error(targetPosition, endPosition);
            //允许范围判断
            return errPosition < tolerancePosition;
        }
        //检查末端与目标姿态差距
        private bool IsRotationConverged(Quaternion endRotation)
        {
            float errRotation = error(targetRotation, endRotation);
            return errRotation < toleranceRotation;
        }

        private float JointLimit(int iJoint, float angle)
        {
            // Apply joint limits
            float minAngle = fK.angleLowerLimits[iJoint];
            float maxAngle = fK.angleUpperLimits[iJoint];
            // If given joint limit
            if (minAngle != maxAngle)
                angle = Mathf.Clamp(angle, minAngle, maxAngle);
            // If no joint limit
            else
                angle = WrapToPi(angle);

            return angle;
        }
        private float WrapToPi(float angle)
        {
            // Wrap angle to [-pi, pi]
            float pi = Mathf.PI;
            angle = angle - 2 * pi * Mathf.Floor((angle + pi) / (2 * pi)); //取最小整数
            return angle;
        }

        // 左手转右手
        private Vector3 FromRUF(Vector3 p)
        {
            return new Vector3(p.z, -p.x, p.y);
        }
        private Quaternion FromRUF(Quaternion q)
        {
            return new Quaternion(q.z, -q.x, q.y, -q.w);
        }
    }
}