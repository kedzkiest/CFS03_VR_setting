using UnityEngine;
using five;
public class CollisionDetector : MonoBehaviour
{
    // 当刚体与其他物体发生碰撞时触发
    public five.KeyboardJointControl kj; 
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("发生碰撞：" + collision.gameObject.name);
        Debug.Log("kj.ds" + kj.ds);
        if (collision.gameObject.name == "end" & kj.ds)
        {
          
            transform.SetParent(collision.gameObject.transform, true);
            transform.GetComponent<Rigidbody>().useGravity = false;
            transform.GetComponent<Rigidbody>().isKinematic = true;
        
          
        }

    }
    void Update()
    {
        if (!kj.ds)
        {
            transform.SetParent(null);
            transform.GetComponent<Rigidbody>().useGravity = true;
            transform.GetComponent<Rigidbody>().isKinematic = false;
        }
    }


}
