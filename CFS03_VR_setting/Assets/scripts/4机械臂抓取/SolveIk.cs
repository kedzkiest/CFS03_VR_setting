using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/// <summary>
///     该脚本用于解决逆向运动学问题
/// </summary>
public class SolveIk : MonoBehaviour
{
    // 机器人相关参数
    public GameObject jointRoot;  // 机器人关节根节点
    private ArticulationBody[] articulationChain;  // 存储机器人的关节链
    private float[] currJointAngles;  // 当前关节角度数组
    private int jointLength;  // 关节数量
    // 控制器
    public ArticulationJointController jointController;  // 关节控制器
    public CCDIK1 iK;  // 第一种逆向运动学算法
    public CCDIK3 iK3;  // 第三种逆向运动学算法
    public float speed = 0.8f;  // 关节运动速度

    void Start()
    {
        // 获取机器人关节链
        articulationChain = jointRoot.GetComponentsInChildren<ArticulationBody>();
        articulationChain = articulationChain.Where(joint => joint.jointType
                                                    != ArticulationJointType.FixedJoint).ToArray();
        jointLength = iK.numJoint;  // 获取关节数量
        currJointAngles = new float[jointLength];  // 初始化关节角度数组
    }

    void Update()
    {
        // 每帧更新内容可添加在此处
    }

    /// <summary>
    /// 解决逆向运动学问题
    /// </summary>
    /// <param name="po">目标位置</param>
    /// <param name="qo">目标旋转</param>
    /// <returns>解决后的关节角度</returns>
    public float[] SolveIK(Vector3 po, Quaternion qo)
    {
        // 将目标位置转换到关节根节点的局部坐标系
        Vector3 p = jointRoot.transform.InverseTransformPoint(po);
        Vector3 position = p;
        p.x = -p.x;  // 翻转X轴
        p.z = -p.z;  // 翻转Z轴
        position = p;

        // 计算目标旋转相对于关节根节点的旋转
        Vector3 rotation = (Quaternion.Inverse(jointRoot.transform.rotation) *
                            qo).eulerAngles;

        // 解算IK，获取当前关节角度
        for (int i = 0; i < jointLength; ++i)
            currJointAngles[i] = articulationChain[i].xDrive.target * Mathf.Deg2Rad;  // 获取当前关节角度并转换为弧度

        // 使用逆向运动学算法求解目标位置和旋转的关节角度
        (Vector3 newpo, Quaternion newro) = iK.SetTarget(position, Quaternion.Euler(rotation.x, rotation.y, rotation.z));

        // 执行CCD算法求解关节角度
        (float[] resultJointAngles2, bool foundSolution) = iK.CCD(currJointAngles, newpo, newro);

        // 返回计算结果的关节角度
        return resultJointAngles2.Clone() as float[];
    }

    /// <summary>
    /// 使用第二种IK算法解决逆向运动学问题
    /// </summary>
    /// <param name="angles">当前关节角度</param>
    /// <param name="po">目标位置</param>
    /// <param name="qo">目标旋转</param>
    /// <returns>解决后的关节角度</returns>
    public float[] SolveIK4(float[] angles, Vector3 po, Quaternion qo)
    {
        // 将目标位置转换到关节根节点的局部坐标系
        Vector3 p = jointRoot.transform.InverseTransformPoint(po);
        Vector3 position = p;
        p.x = -p.x;  // 翻转X轴
        p.z = -p.z;  // 翻转Z轴
        position = p;

        // 计算目标旋转相对于关节根节点的旋转
        Vector3 rotation = (Quaternion.Inverse(jointRoot.transform.rotation) *
                            qo).eulerAngles;

        // 更新当前关节角度
        currJointAngles = angles.Clone() as float[];

        // 设置目标位置和旋转
        iK3.SetTarget(position, Quaternion.Euler(rotation.x, rotation.y, rotation.z));

        // 打印当前的角度信息（调试用）
        print("arrive IK");
        print("currentangles" + string.Join(",", currJointAngles));

        // 使用CCD算法进行逆向运动学计算
        (float[] resultJointAngles, bool foundSolution) = iK3.CCD(currJointAngles);

        // 返回计算得到的关节角度
        return resultJointAngles.Clone() as float[];
    }

    /// <summary>
    /// 移动关节到指定的角度
    /// </summary>
    /// <param name="joints">目标关节角度数组</param>
    public void MoveJoints(float[] joints)
    {
        // 设置每个关节的目标角度
        for (int i = 0; i < joints.Length; ++i)
        {
            jointController.SetJointTargetStep(i, joints[i], speed);  // 设置关节目标角度并控制运动速度
        }
    }
}
