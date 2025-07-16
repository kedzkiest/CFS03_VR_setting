using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace three
{
    public class ForwardKinematics : MonoBehaviour
    {
        // DH parameters  SDH
        // 7+1
        // world -> joint 1 -> ... -> end effector
        public int numJoint = 7;
        public float[] alpha = new float[] {3.1415927f, 1.5707963f, 1.5707963f, 1.5707963f,
                                        1.5707963f, 1.5707963f, 1.5707963f, 3.1415927f};
        public float[] a = new float[] { 0, 0, 0, 0, 0, 0, 0, 0 };
        public float[] d = new float[] {0, -0.2848f, -0.0118f, -0.4208f,
                                    -0.0128f, -0.3143f, 0, -0.2874f};
        public float[] initialTheta = new float[] {0, 0, 3.1415927f, 3.1415927f, 3.1415927f,
                                               3.1415927f, 3.1415927f, 3.1415927f};

        //设置关节角度限制
        private float[] theta;
        public float[] angleLowerLimits = new float[] { 0, -2.41f, 0, -2.66f, 0, -2.23f, 0 };
        public float[] angleUpperLimits = new float[] { 0, 2.41f, 0, 2.66f, 0, 2.23f, 0 };

        // 正定矩阵用于构建齐次变换矩阵
        private Matrix4x4[] initH;
        private Matrix4x4[] H;

       //装载各个关节的位置和旋转
        private Vector3[] positions;
        private Quaternion[] rotations;

        void Start()
        {
            //clone函数避免改变原值
            theta = (float[])initialTheta.Clone();

            //初始化正定阵 8个
            initH = new Matrix4x4[numJoint + 1];
            for (int i = 0; i < numJoint + 1; ++i)
            {
                float ca = Mathf.Cos(alpha[i]);
                float sa = Mathf.Sin(alpha[i]);

                initH[i] = Matrix4x4.identity;
                //共有的部分
                initH[i].SetRow(0, new Vector4(1, -ca, sa, a[i]));
                initH[i].SetRow(1, new Vector4(1, ca, -sa, a[i]));
                initH[i].SetRow(2, new Vector4(0, sa, ca, d[i]));
                initH[i].SetRow(3, new Vector4(0, 0, 0, 1));
            }
            H = (Matrix4x4[])initH.Clone();

            // 初始化所有关节位置和姿态
            positions = new Vector3[numJoint + 1];
            rotations = new Quaternion[numJoint + 1];
            UpdateAllH(new float[] { 0f, 0f, 0f, 0f, 0f, 0f, 0f });
        }

        void Update()
        {
            // Example //
        
            //UpdateAllH(new float[] {0f, 0f, 1.5707f, 0, 0f, 0, 0f});
            //(Vector3 position, Quaternion rotation) = GetPose(7); // end effector
            //Debug.Log(position.ToString("0.000"));
        
        }
        //输入  关节索引和旋转角度
        public void UpdateH(int i, float rotateTheta)
        {
           
            // 每个角度是设置的初始角+旋转角
            theta[i] = initialTheta[i] + rotateTheta;
            // For computation
            float ct = Mathf.Cos(theta[i]);
            float st = Mathf.Sin(theta[i]);

            // 构建各个关节完整的齐次变换矩阵
            H[i] = initH[i];
            H[i][0, 0] *= ct;
            H[i][0, 1] *= st;
            H[i][0, 2] *= st;
            H[i][0, 3] *= ct;
            H[i][1, 0] *= st;
            H[i][1, 1] *= ct;
            H[i][1, 2] *= ct;
            H[i][1, 3] *= st;
            
            // Update joint positions and rotations
            UpdateAllPose();
        }
       
        public void UpdateAllH(float[] jointAngles)
        {
            // 更新所有关节
            UpdateH(0, 0);//设置第一个关节H
            for (int i = 0; i < numJoint; ++i)
            {
                UpdateH(i + 1, jointAngles[i]);//从1-7关节
            }

            // Update joint positions and rotations
            UpdateAllPose();
        }

        public void UpdateAllPose()
        {
            // 获取所有关节位置和旋转
            Matrix4x4 HEnd = Matrix4x4.identity;//单位矩阵
            for (int i = 0; i < numJoint + 1; ++i)
            {
                HEnd = HEnd * H[i];//Tend=I*T0*T1*T2....T7
                positions[i] = new Vector3(HEnd[0, 3], HEnd[1, 3], HEnd[2, 3]);
                rotations[i] = HEnd.rotation;
            }
        }
        //单个关节坐标转换
        public (Vector3, Quaternion) GetPose(int i, bool toRUF = false)//坐标系转化
        {
            // Unity coordinate
            if (toRUF)
                return (ToRUF(positions[i]), ToRUF(rotations[i]));
            else
                return (positions[i], rotations[i]);
        }
        //所有关节坐标转换
        public (Vector3[], Quaternion[]) GetAllPose(bool toRUF = false)
        {
            // Unity coordinate
            if (toRUF)
            {
                Vector3[] positionsRUF = new Vector3[numJoint + 1];
                Quaternion[] rotationsRUF = new Quaternion[numJoint + 1];
                for (int i = 0; i < numJoint + 1; ++i)
                {
                    positionsRUF[i] = ToRUF(positions[i]);
                    rotationsRUF[i] = ToRUF(rotations[i]);
                }
                return (positionsRUF, rotationsRUF);
            }
            else
                return (positions, rotations);
        }

        // 右手坐标系转为左手手坐标系
        private Vector3 ToRUF(Vector3 p)
        {
            return new Vector3(-p.y, p.z, p.x);
        }
        private Quaternion ToRUF(Quaternion q)
        {
            return new Quaternion(-q.y, q.z, q.x, -q.w);
        }
    }
}