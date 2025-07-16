using UnityEngine;
using UnityEngine.Animations;

public class RobotJointReader : MonoBehaviour
{
    public ArticulationBody[] jointBodies;

    void Start()
    {
        if (jointBodies == null || jointBodies.Length == 0)
        {
            Debug.LogError("No joint references assigned.");
        }
    }

    void Update()
    {
        string jointInfo = "Joint Angles: ";
        foreach (ArticulationBody joint in jointBodies)
        {
            float angle = joint.jointPosition[0] * Mathf.Rad2Deg;
            jointInfo += angle.ToString("F2") + "бу | ";
        }
        Debug.Log(jointInfo);
    }
}