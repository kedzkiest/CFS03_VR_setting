using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
//命名空间 避免与声明相同变量的脚本冲突
namespace three
{

    public class ArticulationJointController : MonoBehaviour
    {
        // 机器人对象
        public GameObject jointRoot;
        public int numJoint = 7; // 关节数量

        public float jointMaxSpeed = 1f; // 关节最大速度
        private ArticulationBody[] articulationChain; // 关节链

        void Start()
        {
            // 获取所有 机械臂 ArticulationBody 组件
            articulationChain = jointRoot.GetComponentsInChildren<ArticulationBody>();

            // 过滤掉固定关节，只保留运动关节
            articulationChain = articulationChain.Where(joint => joint.jointType
                                                        != ArticulationJointType.FixedJoint).ToArray();
            // 取前 numJoint 个关节
            articulationChain = articulationChain.Take(numJoint).ToArray();

     
        }

  
  
        // 设置关节目标角度（传入 关节索引、角度目标值、速度）
        public void SetJointTarget(int jointNum, float target, float speed)
        {
            SetJointTarget(articulationChain[jointNum], target, speed);
        }
        //重载函数（传入 关节体、目标值、速度）
        public void SetJointTarget(ArticulationBody joint, float target, float speed)
        {
            //确保目标值有效
            if (float.IsNaN(target)) return;
            //弧度转为角度
            target *= Mathf.Rad2Deg;
            //设置关节旋转
            ArticulationDrive drive = joint.xDrive;
            //当前机械臂旋转位移量
            float currentTarget = drive.target;
            //设置机械臂固定帧移动量
            float deltaPosition = speed * Mathf.Rad2Deg * Time.fixedDeltaTime;
            //计算当前位移量与目标值差异
            if (Mathf.Abs(currentTarget - target) > deltaPosition)
            {   
                //Sign函数决定目标值正负，决定旋转关节移动方向
                target = currentTarget + deltaPosition * Mathf.Sign(target - currentTarget);
            }

            // 关节角度限制
            if (joint.twistLock == ArticulationDofLock.LimitedMotion)
            {
                target = Mathf.Clamp(target, drive.lowerLimit, drive.upperLimit);
            }

            drive.target = target;
            // 这个joint就是传入的关节体，对应关节
            joint.xDrive = drive;
        }


       
    }
}
