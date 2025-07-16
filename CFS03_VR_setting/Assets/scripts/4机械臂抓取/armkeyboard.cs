using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class armkeyboard : MonoBehaviour
{
    // Start is called before the first frame update
    // 基本流程：
    // 确定目标 --> 预抓取 --> 抓取 --> 抬起-->放置-->回位
    // 抓取的目标物体
    public GameObject graspObeject;

    // 放置目标位置
    public GameObject placetarget;

    private float[] currJointAngles;

    // 手爪控制器和关节控制器
    public ArticulationGripperController gripperController;
    public ArticulationJointController jointController;

    // 反向运动学求解器
    public SolveIk solveIk;

    private float[] jointAngles;

    // 抬起的高度
    public float hoverdistance = 0.2f;
    // 姿势分配等待时间
    private float k_PoseAssignmentWait = 2f;
    // 关节分配等待时间
    private float k_jointAssignmentWait = 0.1f;
    // 存储结果的列表
    public List<float[]> results = new List<float[]>();
    // 是否可移动
    public bool moveable = false;

    // 不同阶段的关节角度数组
    public float[] r1 = new float[7];
    public float[] r2 = new float[7];
    public float[] r3 = new float[7];
    public float[] r4 = new float[7];
    public float[] r5 = new float[7];

    // 不同阶段的目标位置
    public Vector3 p1;
    public Vector3 p2;
    public Vector3 p3;
    public Vector3 p4;
    public Vector3 p5;
    // 当前抓取物体的旋转
    public Quaternion q;

    // 动作阶段索引
    public int poseIndex = 0;
    // 选择的索引
    public int selectedIndex = 0;

    // 关节运动速度
    public float jointSpeed = 0.2f;

    void Start()
    {
        // 获取当前的关节目标角度
        jointAngles = jointController.GetCurrentJointTargets();
    }

    void Update()
    {
        // 检测按键输入，控制手爪的开启和关闭
        if (Input.GetKeyDown(KeyCode.G))
        {
            gripperController.CloseGrippers(); // 按G键关闭手爪
        }
        else if (Input.GetKeyDown(KeyCode.R))
        {
            gripperController.OpenGrippers(); // 按R键打开手爪
        }

        // 按J键开始或停止轨迹计算
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (results.Count >= 5)
            {
                StopCoroutine(Gettrajectory()); // 如果轨迹已经计算，停止执行
            }
            else
            {
                poseIndex = 0;
                StartCoroutine(Gettrajectory()); // 开始计算新的轨迹
            }

            // 根据轨迹结果设置可移动标志
            if (results != null)
            {
                moveable = true;
            }
            else
            {
                moveable = false;
            }
        }
    }

    void FixedUpdate()
    {
        // 如果可移动，执行关节运动
        if (moveable)
        {
            Movejoints();
        }
    }

    // 手爪控制
    private void CloseGripper()
    {
        gripperController.CloseGrippers();
    }

    private void OpenGripper()
    {
        gripperController.OpenGrippers();
    }

    // 获取轨迹的协程
    IEnumerator Gettrajectory()
    {
        //清楚所有解
        results.Clear();

        // 获取抓取物体
        Vector3 p = graspObeject.transform.position + new Vector3(0, 0f, 0);
        //放置目标的位置
        Vector3 p_ = placetarget.transform.position + new Vector3(0.2f, 0.1f, 0);
        q = graspObeject.transform.rotation;

        // 预抓取位置（抬起一定高度）
        p1 = p + Vector3.up * hoverdistance;

        // 抓取位置（稍微抬起）
        p2 = p + Vector3.up * 0.02f;

        // 抬起位置（恢复到原来的抬起高度）
        p3 = p + Vector3.up * hoverdistance;

        // 放置位置（放置位置稍微抬起）
        p4 = p_ + Vector3.up * 0.02f;

        // 初始化抓取物体的旋转
        Quaternion q1 = q;
        Quaternion q2 = q;
        Quaternion q3 = q;
        Quaternion q4 = q;

        // 返回角度值，预抓取
        r1 = solveIk.SolveIK(p1, q1);
        // 输入当前角度，计算之后的角度，抓取
        r2 = solveIk.SolveIK4(r1, p2, q2);
        // 输入当前角度，计算之后的角度，抬起
        r3 = solveIk.SolveIK4(r2, p3, q3);
        // 放置
        r4 = solveIk.SolveIK4(r3, p4, q4);

        print("success back");

        // 如果得到有效的结果，则添加到轨迹列表中
        if (r1 != null && r2 != null && r3 != null)
        {
            results.Add(r1);
            results.Add(r2);
            results.Add(r3);
            results.Add(r4);
            results.Add(r1); // 再次添加r1作为归位动作

            Debug.Log("checking:" + results.Count);

            //等待0.1s返回，确保获取解后返回，保证足够计算时间
            yield return new WaitForSeconds(k_jointAssignmentWait);
        }
    }

    // 移动关节
    private void Movejoints()
    {
        if (results != null)
        {
            // 执行轨迹
            StartCoroutine(ExecuteTrajectories(results));
        }
        else
        {
            Debug.LogError("No trajectory returned from MoverService.");
        }
    }

    // 执行轨迹的协程
    private IEnumerator ExecuteTrajectories(List<float[]> re)
    {
        if (re.Count >= 5)
        {
            // 根据poseIndex依次执行轨迹
            if (poseIndex == 0)
            {
                float[] t = re[poseIndex];
                solveIk.MoveJoints(t);

                //等待2.5s
                yield return new WaitForSeconds(k_PoseAssignmentWait * 2.5f);
                poseIndex = 1;
            }
            else if (poseIndex == 1)
            {
                float[] t = re[poseIndex];
                solveIk.MoveJoints(t);
                yield return new WaitForSeconds(k_PoseAssignmentWait * 1.5f);

                // 在抓取阶段关闭手爪
                //poseindex==1
                if (poseIndex == (int)Poses.Grasp)
                {
                    CloseGripper();
                    Debug.Log("close");
                    yield return new WaitForSeconds(k_PoseAssignmentWait * 1f);
                }

                poseIndex = 2;
            }
            else if (poseIndex == 2)
            {
                float[] t = re[poseIndex];
                solveIk.MoveJoints(t);
                yield return new WaitForSeconds(k_PoseAssignmentWait * 2.5f);
                poseIndex = 3;
            }

            else if (poseIndex == 3)
            {
                float[] t = re[poseIndex];
                solveIk.MoveJoints(t);
                yield return new WaitForSeconds(k_PoseAssignmentWait * 2.5f);
                //放置物体，打开夹爪
                OpenGripper();
                yield return new WaitForSeconds(k_PoseAssignmentWait * 1f);
                poseIndex = 4;
            }
            else if (poseIndex == 4)
            {
                float[] t = re[poseIndex];
                solveIk.MoveJoints(t);
                yield return new WaitForSeconds(k_PoseAssignmentWait * 2f);
                poseIndex = 5;
            }
        }

        // 清空结果
        if (poseIndex == 5)
        {
            results.Clear();
        }
    }

    // 定义姿势阶段的枚举类型
    enum Poses
    {
        PreGrasp, // 预抓取阶段
        Grasp,    // 抓取阶段
        PickUp,   // 抬起阶段
        Place     // 放置阶段
    }
}
