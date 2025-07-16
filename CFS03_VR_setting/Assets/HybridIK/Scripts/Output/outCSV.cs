using UnityEngine;
using System.IO;

public class JointLogger : MonoBehaviour
{
    public ArticulationBody[] jointBodies;
    private StreamWriter writer;

    void Start()
    {
        string projectRoot = Application.dataPath.Replace("/Assets", "");
        writer = new StreamWriter(projectRoot + "/joint_angles.csv");
        // writer = new StreamWriter(Application.dataPath + "/joint_angles.csv");
        writer.WriteLine("Time,Joint1,Joint2,Joint3,...");  // 根据关节数量调整
    }

    void Update()
    {
        string line = Time.time.ToString("F2") + ",";
        foreach (ArticulationBody joint in jointBodies)
        {
            line += (joint.jointPosition[0] * Mathf.Rad2Deg).ToString("F2") + ",";
        }
        writer.WriteLine(line); 
        writer.Flush();
    }

    void OnApplicationQuit()
    {
        writer.Close();
    }
}