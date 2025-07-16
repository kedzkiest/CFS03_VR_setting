using System;
using System.Linq;
using UnityEngine;

namespace five
{
    public class KeyboardJointControl : MonoBehaviour
    {
        
        public ArticulationGripperController gripperController;//夹爪控制
        public ArticulationBody grasp;//夹爪本体
        int moveState = 0;
        float speed = 0.6f;
        public bool ds=false;//抓取标志
        void Start()
        {
         
        }


        void Update()
        {
            //关闭夹爪
            if (Input.GetKeyDown(KeyCode.G))
            {
                gripperController.CloseGrippers();
                ds = true;
            }
            //打开夹爪
            else if (Input.GetKeyDown(KeyCode.R))
            {
                gripperController.OpenGrippers();
                ds = false;
            }
            //控制夹爪上下移动
            if (Input.GetKey(KeyCode.W))
            {
                moveState = 1;
                MoveGrasp();
            }
            else if (Input.GetKey(KeyCode.S)) {
                moveState = -1;
                MoveGrasp();
            }
              
            else
                moveState = 0;
           
           

        }

        void MoveGrasp()
        {

            ArticulationBody articulation = grasp;//获取夹爪关节体

            //沿着y轴上下运动
            float xDrivePostion = articulation.jointPosition[0];
          //  Debug.Log(xDrivePostion);

            //movestate决定当前运动状态
            float targetPosition = xDrivePostion + -(float)moveState * Time.fixedDeltaTime * speed;

            //set joint Drive to new position
            var drive = articulation.xDrive;
            drive.target = targetPosition;
            articulation.xDrive = drive;



  
       

        }

    }
}
