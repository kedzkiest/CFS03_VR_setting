using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RootMotion.Demos.Turret;

public enum Format
{
    CSV,
    JSON
}

public enum WriteMode
{
	Append,
	Overwrite
}

public enum RobotPartType 
{
	Joint, 
	Gripper, 
}

public enum RecordingMode
{
	FK,
	IK
}

public class DataCollector : MonoBehaviour
{
    public Format format = Format.CSV;
	[Header("Create a Data folder before start recording, it is a relative path from Assets/")]
	public string dataFolder = "Data";
	public WriteMode writeMode = WriteMode.Append;
	public RecordingMode recordingMode = RecordingMode.FK;

	public GameObject bottle;
	public GameObject gripper;

	private GameObject m_robot;
	private GameObject[] m_robotParts;

	private bool m_recording = false;

	private long m_recordingStartTime;

	[SerializeField] private HybridInverseKinematicsNode ikNode;

	private List<HybridIKJoint> nodes;


    private void Start()
	{
		m_robot = GameObject.FindWithTag("robot");

		var partsList = new System.Collections.Generic.List<GameObject>();
		foreach (RobotPartType partType in System.Enum.GetValues(typeof(RobotPartType)))
		{
			GameObject[] foundParts = GameObject.FindGameObjectsWithTag(partType.ToString());
			partsList.AddRange(foundParts);
		}
		m_robotParts = partsList.ToArray();

		if (m_robotParts.Length == 0)
		{
			Debug.LogWarning("No robot parts found with the specified tags.");
		}
		else
		{
			Debug.Log($"Found {m_robotParts.Length} robot parts.");
		}
	}

	private void Update()
	{
		if (Input.GetKeyDown(KeyCode.R))
		{
			m_recording = !m_recording;

			if (m_recording)
			{
				Debug.Log("Start recording");
				m_recordingStartTime = (long)(Time.time * 1000);
			}

			else Debug.Log("Stop recording");
		}

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
            if (recordingMode == RecordingMode.FK)
            {
                recordingMode = RecordingMode.IK;

				ikNode.gameObject.SetActive(true);
            }
            else
            {
                recordingMode = RecordingMode.FK;

                StartCoroutine(SetCollider());
            }
        }

		if (m_recording)
		{
			// miliseconds since epoch
			long timeStampMs = (long)(Time.time * 1000);
			timeStampMs -= m_recordingStartTime;

            string filePath;

			if (recordingMode == RecordingMode.FK)
			{
                foreach (GameObject part in m_robotParts)
                {
                    // record position and rotation of each part
                    if (part != null)
                    {
                        Vector3 pos = part.transform.position;
                        Vector3 rot = part.transform.rotation.eulerAngles;

                        filePath = $"{Application.dataPath}/{dataFolder}/{part.name}.csv";
                        if (format == Format.CSV)
                        {
                            // Write column headers if the file is new or being overwritten
                            if (writeMode == WriteMode.Overwrite || !System.IO.File.Exists(filePath))
                            {
                                DataWriter.WriteColumnHeaders(filePath, "PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,TimeStampMs", writeMode);
                            }

                            DataWriter.WriteToCSV(filePath, pos, rot, timeStampMs, writeMode);
                        }
                    }
                }
            }
			else
			{
                nodes = ikNode.GetIKNodes();

				foreach(HybridIKJoint node in nodes)
				{
					Vector3 pos = node.jointTransform.position;
					Vector3 rot = node.jointTransform.rotation.eulerAngles;

                    filePath = $"{Application.dataPath}/{dataFolder}/{node.jointTransform.gameObject.name}.csv";

                    if (format == Format.CSV)
                    {
                        // Write column headers if the file is new or being overwritten
                        if (writeMode == WriteMode.Overwrite || !System.IO.File.Exists(filePath))
                        {
                            DataWriter.WriteColumnHeaders(filePath, "PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,TimeStampMs", writeMode);
                        }

                        DataWriter.WriteToCSV(filePath, pos, rot, timeStampMs, writeMode);
                    }
                }

                // Since the base of the robot arm is not managed in IK, add it manually to keep the same data length with other robot arm parts.
                DataWriter.WriteToCSV(
                    path: $"{Application.dataPath}/{dataFolder}/base.csv",
                    position: Vector3.zero,
                    rotation: Vector3.zero,
                    timeStampMs,
                    writeMode
                );
            }
			

			// make shift
			if (bottle != null && gripper != null)
			{
				// Record bottle position and rotation
				Vector3 bottlePos = bottle.transform.position;
				Vector3 bottleRot = bottle.transform.rotation.eulerAngles;
				filePath = $"{Application.dataPath}/{dataFolder}/bottle.csv";
				if (format == Format.CSV)
				{
					// Write column headers if the file is new or being overwritten
					if (writeMode == WriteMode.Overwrite || !System.IO.File.Exists(filePath))
					{
						DataWriter.WriteColumnHeaders(filePath, "PositionX,PositionY,PositionZ,RotationX,RotationY,RotationZ,TimeStampMs", writeMode);
					}

					DataWriter.WriteToCSV(filePath, bottlePos, bottleRot, timeStampMs, writeMode);
				}

				// Record distance between gripper and bottle
				Vector3 gripperPos = gripper.transform.position;
				float distance = (gripperPos - bottlePos).magnitude;
				filePath = $"{Application.dataPath}/{dataFolder}/distance.csv";
				if (format == Format.CSV)
				{
					// Write column headers if the file is new or being overwritten
					if (writeMode == WriteMode.Overwrite || !System.IO.File.Exists(filePath))
					{
						DataWriter.WriteColumnHeaders(filePath, "distance between gripper and bottle, TimeStampMs", writeMode);
					}

					DataWriter.WriteToCSV(filePath, distance, timeStampMs, writeMode);
				}
			}

			else 
			{
				Debug.LogWarning("Bottle or gripper is not assigned. Skipping distance recording.");
			}


		}
	}

    private IEnumerator SetCollider()
    {
        m_robot.SetActive(false);
        yield return new WaitForSeconds(0.1f);
        m_robot.SetActive(true);
        yield return new WaitForSeconds(0.1f);
        ikNode.gameObject.SetActive(false);
        yield return null;
    }
}
