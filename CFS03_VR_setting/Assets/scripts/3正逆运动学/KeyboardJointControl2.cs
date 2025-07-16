using System;
using System.Linq;
using UnityEngine;

namespace three
{
    public class KeyboardJointControl2 : MonoBehaviour
    {
        public GameObject jointRoot; //机械臂游戏对象
        public GameObject target;//目标点位置
        public ArticulationGripperController gripperController;//夹爪控制
        public ArticulationJointController jointController;//关节控制
        public CCDIK iK;//ik求解器

        private ArticulationBody[] articulationChain;//关节体
        private float[] currJointAngles;//当前角度
        private int jointLength;//关节数
        private Vector3 deltaPosition;//移动量（标志量）

        public float speed = 0.5f;

        void Start()
        {
            //初始化
            articulationChain = jointRoot.GetComponentsInChildren<ArticulationBody>()
                                         .Where(joint => joint.jointType != ArticulationJointType.FixedJoint)
                                         .ToArray();
            jointLength = iK.numJoint;
            currJointAngles = new float[jointLength];
            deltaPosition = Vector3.zero;
        }

        void FixedUpdate()
        {
            if (deltaPosition != Vector3.zero)
            {
                float[] newJoints = SolveIK();//利用SolveIK函数求解获取七个角度
                MoveJoints(newJoints);//移动机械臂
            }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.G))
                gripperController.CloseGrippers();
            else if (Input.GetKeyDown(KeyCode.R))
                gripperController.OpenGrippers();
            //按下J键，deltaPosition！=0，开始求解
            //deltaPosition = Input.GetKey(KeyCode.J) ? target.transform.position : Vector3.zero;
            deltaPosition = target.transform.position;
        }

        private float[] SolveIK()
        {
            //计算目标位置相对于机械臂底座的位置，这样保证机械臂移动，也可以知道相对位置
            Vector3 position = jointRoot.transform.InverseTransformPoint(target.transform.position);
           //这个需要根据实际情况调，与机械臂的朝向有关
            position.x = -position.x;
            position.z = -position.z;
            //获取机械臂当前各个关节角度，并将角度转为弧度
            for (int i = 0; i < jointLength; ++i)
                currJointAngles[i] = articulationChain[i].xDrive.target * Mathf.Deg2Rad;
            //设置关节角度，传入IK求解器
            iK.SetJointAngle(currJointAngles);
            //将目标姿态传入IK求解器
            iK.SetTarget(position, target.transform.rotation);
            //获取求解结果
            (float[] resultJointAngles, bool foundSolution) = iK.CCD();
            //打印求解结果
            string s = "";
            for (int i = 0; i < 7; i++)
            {

                s += resultJointAngles[i] + " ";
            }
            Debug.Log(s);
            return resultJointAngles;
        }

        private void MoveJoints(float[] joints)
        {
            for (int i = 0; i < joints.Length; ++i)
                jointController.SetJointTarget(i, joints[i], speed);
        }
    }
}
