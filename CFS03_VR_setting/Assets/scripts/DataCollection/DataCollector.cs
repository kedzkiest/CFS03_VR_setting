using UnityEngine;

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

public class DataCollector : MonoBehaviour
{
    public Format format = Format.CSV;
	[Header("Create a Data folder before start recording, it is a relative path from Assets/")]
	public string dataFolder = "Data";
	public WriteMode writeMode = WriteMode.Append;

	public GameObject bottle;
	public GameObject gripper;

	private GameObject m_robot;
	private GameObject[] m_robotParts;



	private bool m_recording = false;

	// should be made a static function for another class
	

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
			}
		}

		if (m_recording)
		{
			string filePath;
			foreach (GameObject part in m_robotParts)
			{
				// record position and rotation of each part
				if (part != null)
				{
					Vector3 pos = part.transform.position;
					Vector3 rot = part.transform.rotation.eulerAngles;
					filePath = $"{Application.dataPath}/{dataFolder}/{part.name}.csv";
					if (format == Format.CSV) DataWriter.WriteToCSV(filePath, pos, rot, writeMode);
				}
			}

			// make shift
			if (bottle != null && gripper != null)
			{
				// Record bottle position and rotation
				Vector3 bottlePos = bottle.transform.position;
				Vector3 bottleRot = bottle.transform.rotation.eulerAngles;
				filePath = $"{Application.dataPath}/{dataFolder}/bottle.csv";
				if (format == Format.CSV) DataWriter.WriteToCSV(filePath, bottlePos, bottleRot, writeMode);

				// Record distance between gripper and bottle
				Vector3 gripperPos = gripper.transform.position;
				float distance = (gripperPos - bottlePos).magnitude;
				filePath = $"{Application.dataPath}/{dataFolder}/distance.csv";
				if (format == Format.CSV) DataWriter.WriteToCSV("distance", distance, writeMode);
			}

			else 
			{
				Debug.LogWarning("Bottle or gripper is not assigned. Skipping distance recording.");
			}


		}
	}

}
