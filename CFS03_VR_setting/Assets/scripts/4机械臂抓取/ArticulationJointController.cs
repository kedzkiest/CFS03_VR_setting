using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class ArticulationJointController : MonoBehaviour
{
    // Robot object
    public GameObject jointRoot;
    public int numJoint = 7;

    public float jointMaxSpeed = 1f;

    // Articulation Bodies
    public float[] homePosition = { 0f, 0f, 0f, 0f, 0f, 0f, 0f };//初始位置
    private ArticulationBody[] articulationChain;//初始化关节链条


    void Start()
    {
        // Get joints
        articulationChain = jointRoot.GetComponentsInChildren<ArticulationBody>();
        articulationChain = articulationChain.Where(joint => joint.jointType //左边的变量带入右边中，lamda用法
                                                    != ArticulationJointType.FixedJoint).ToArray();
        articulationChain = articulationChain.Take(numJoint).ToArray();//复制链条中的七个
        // HomeJoints();
    }
    
    
    public void SetJointTargetStep(int jointNum, float target, float speed)
    {
        if (jointNum >= 0 && jointNum < articulationChain.Length)
            SetJointTargetStep(articulationChain[jointNum], target, speed);
    }

    public void SetJointTargetStep(ArticulationBody joint, float target, float speed)
    {
       

        if (float.IsNaN(target))
            return;
        target = target * Mathf.Rad2Deg;

        // Get drive
        ArticulationDrive drive = joint.xDrive;
        float currentTarget = drive.target;

        // Speed limit//函数原理，每次增加一个deltaPosition，控制deltaPosition的大小就可以控制速度
        float deltaPosition = speed * Mathf.Rad2Deg * Time.fixedDeltaTime;//固定值
        if (Mathf.Abs(currentTarget - target) > deltaPosition)//Mathf.Abs取绝对值，target是输入的弧度值
            target = currentTarget + deltaPosition * Mathf.Sign(target - currentTarget);//判断正负数——Mathf.Sign（返回±1，其中0返回正数）

        // Joint limit
        if (joint.twistLock == ArticulationDofLock.LimitedMotion)
        {
            if (target > drive.upperLimit)
                target = drive.upperLimit;
            else if (target < drive.lowerLimit)
                target = drive.lowerLimit;
        }

        // Set target
        drive.target = target;
        joint.xDrive = drive;
    }

   
   

   

    public float[] GetCurrentJointTargets()
    {

        // Get joints
        articulationChain = jointRoot.GetComponentsInChildren<ArticulationBody>();
        articulationChain = articulationChain.Where(joint => joint.jointType //左边的变量带入右边中，lamda用法
                                                    != ArticulationJointType.FixedJoint).ToArray();
        articulationChain = articulationChain.Take(numJoint).ToArray();//复制链条中的七个
        // float[] targets = new float[articulationChain.Length];
        float[] targets = new float[7];
        for (int i = 0; i < articulationChain.Length; ++i)
        {
            targets[i] = articulationChain[i].xDrive.target;
        }

        targets = targets.Select(r => r * Mathf.Deg2Rad).ToArray();
        return targets;
    }



}
