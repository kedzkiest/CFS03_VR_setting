using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//命名空间
namespace three
{
    public class ArticulationBodyInitialization : MonoBehaviour
    {
        private ArticulationBody[] articulationChain;

        public GameObject robotRoot; // 机器人根节点
        public bool assignToAllChildren = true; // 是否分配给所有子关节
        public int robotChainLength = 0; // 机器人关节链的长度
        //机械臂相关参数
        public float stiffness = 10000f; // 刚度参数
        public float damping = 100f; // 阻尼参数
        public float forceLimit = 1000f; // 力限制参数

        void Start()
        {
            // 获取所有非固定关节（旋转关节）
            articulationChain = robotRoot.GetComponentsInChildren<ArticulationBody>();
            articulationChain = articulationChain.Where(joint => joint.jointType
                                                        != ArticulationJointType.FixedJoint).ToArray();

            // 要分配的关节长度（7）
            int assignLength = articulationChain.Length;
            if (!assignToAllChildren)
                assignLength = robotChainLength;

            // 设置刚度、阻尼和力限制
            int defDyanmicVal = 100;
            for (int i = 0; i < assignLength; i++)
            {   
                //循环设置关节体的一些属性
                ArticulationBody joint = articulationChain[i];
                ArticulationDrive drive = joint.xDrive;

                joint.jointFriction = defDyanmicVal; // 设置关节摩擦
                joint.angularDamping = defDyanmicVal; // 设置角阻尼

                drive.stiffness = stiffness; // 设置驱动力刚度
                drive.damping = damping; // 设置驱动力阻尼
                drive.forceLimit = forceLimit; // 设置驱动力限制
                joint.xDrive = drive;
            }
        }
    }
}
