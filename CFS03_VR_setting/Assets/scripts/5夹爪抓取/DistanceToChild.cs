using UnityEngine;
using five;
public class DistanceToChild : MonoBehaviour
{
    public GameObject targetObject;  // 目标物体
    public float targetDistance = 0.1f;  // 设定的距离阈值
    bool canGrab = false;  // 控制是否可以抓取的布尔值

    public five.KeyboardJointControl kj;
    void Update()
    {
        // 检测当前物体和目标物体的距离
        float distance = Vector3.Distance(transform.position, targetObject.transform.position);
        canGrab = kj.ds;

        // 如果距离小于目标距离并且 canGrab 为 true，则将当前物体设为目标物体的子物体
        if (distance <= targetDistance && canGrab)
        {
            // 将当前物体设为目标物体的子物体
            transform.SetParent(targetObject.transform);

            // 可选：设置位置和旋转
            transform.localPosition = new Vector3(0, 0.8f, 0);  // 使当前物体与目标物体对齐
            transform.localRotation = Quaternion.identity;  // 使当前物体的旋转与目标物体一致
            Rigidbody rb = transform.GetComponent<Rigidbody>();
            rb.useGravity = false; //
        }
        else
        {
            Rigidbody rb = transform.GetComponent<Rigidbody>();
            rb.useGravity = true; //
        }
    }

    }

