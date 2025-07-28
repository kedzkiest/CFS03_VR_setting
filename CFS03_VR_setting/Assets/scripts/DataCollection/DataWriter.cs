using UnityEngine;
using static UnityEngine.InputSystem.Controls.DiscreteButtonControl;

public static class DataWriter 
{
	public static void WriteToCSV(string path, Vector3 position, Vector3 rotation, WriteMode writemode)
	{
		string line = $"{position.x},{position.y},{position.z},{rotation.x},{rotation.y},{rotation.z}";

		bool append = writemode == WriteMode.Append;
		using (var writer = new System.IO.StreamWriter(path, append))
		{
			writer.WriteLine(line);
		}
	}

	public static void WriteToCSV(string path, float distance, WriteMode writemode)
	{
		bool append = writemode == WriteMode.Append;
		using (var writer = new System.IO.StreamWriter(path, append))
		{
			writer.WriteLine(distance);
		}
	}
}
