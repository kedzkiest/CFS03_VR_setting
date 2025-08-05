using UnityEngine;
using static UnityEngine.InputSystem.Controls.DiscreteButtonControl;

public static class DataWriter 
{
	public static void WriteToCSV(string path, Vector3 position, Vector3 rotation, WriteMode writemode)
	{
		string line = $"{position.x:F8},{position.y:F8},{position.z:F8},{rotation.x:F8},{rotation.y:F8},{rotation.z:F8}";

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
			writer.WriteLine($"{distance:F8}");
		}
	}
}
