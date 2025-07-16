using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace three
{
    public class ArticulationGripperController : MonoBehaviour
    {
        private ArticulationBody[] Finger1Chain; // 第一根手指的关节链
        private ArticulationBody[] Finger2Chain; // 第二根手指的关节链
        private ArticulationBody[] Finger3Chain; // 第三根手指的关节链
        public float[] closeValue = { 15f, 20f }; // 夹爪闭合角度，第一个和第二个指关节位移量
        public float[] openValue = { 0f, 0f }; // 夹爪张开角度，第一个和第二个指关节位移量
        public GameObject finger1; // 第一根手指对象
        public GameObject finger2; // 第二根手指对象
        public GameObject finger3; // 第三根手指对象

        void Start()
        {
            // 初始化每根手指的关节链，并筛选出非固定关节
            Finger1Chain = finger1.GetComponentsInChildren<ArticulationBody>();
            Finger1Chain = Finger1Chain.Where(joint => joint.jointType != ArticulationJointType.FixedJoint).ToArray();
            Finger2Chain = finger2.GetComponentsInChildren<ArticulationBody>();
            Finger2Chain = Finger2Chain.Where(joint => joint.jointType != ArticulationJointType.FixedJoint).ToArray();
            Finger3Chain = finger3.GetComponentsInChildren<ArticulationBody>();
            Finger3Chain = Finger3Chain.Where(joint => joint.jointType != ArticulationJointType.FixedJoint).ToArray();
        }

        // 设置夹爪的目标角度
        public void SetGrippers(float[] closeValue)
        {
            //循环一次性赋值所有指关节旋转值
            for (int i = 0; i < Finger1Chain.Length; i++)
            {
                SetfingerTarget(Finger1Chain[i], closeValue[i]); // 设置第一根手指的关节目标角度
                SetfingerTarget(Finger2Chain[i], closeValue[i]); // 设置第二根手指的关节目标角度
                SetfingerTarget(Finger3Chain[i], closeValue[i]); // 设置第三根手指的关节目标角度
            }
        }

        // 关闭夹爪
        public void CloseGrippers()
        {
            SetGrippers(closeValue); // 设定闭合角度
        }

        // 打开夹爪
        public void OpenGrippers()
        {
            SetGrippers(openValue); // 设定张开角度
        }

        // 设置单个手指关节的目标角度
        void SetfingerTarget(ArticulationBody joint, float target)
        {
            ArticulationDrive drive = joint.xDrive;
            drive.target = target;
            joint.xDrive = drive;
        }
    }
}
