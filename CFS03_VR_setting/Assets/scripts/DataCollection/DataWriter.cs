using UnityEngine;
using static UnityEngine.InputSystem.Controls.DiscreteButtonControl;

public static class DataWriter 
{

	public static void WriteColumnHeaders(string path, string headers, WriteMode writemode)
	{
		bool append = writemode == WriteMode.Append;
		using (var writer = new System.IO.StreamWriter(path, append))
		{
			if (!append || writer.BaseStream.Length == 0) // Only write headers if file is empty
			{
				writer.WriteLine(headers);
			}
		}
	}
	public static void WriteToCSV(string path, Vector3 position, Vector3 rotation, long time, WriteMode writemode)
	{
		string line = $"{position.x:F8},{position.y:F8},{position.z:F8},{rotation.x:F8},{rotation.y:F8},{rotation.z:F8},{time}";

		bool append = writemode == WriteMode.Append;
		using (var writer = new System.IO.StreamWriter(path, append))
		{
			writer.WriteLine(line);
		}
	}

	public static void WriteToCSV(string path, float distance, long time, WriteMode writemode)
	{
		bool append = writemode == WriteMode.Append;
		using (var writer = new System.IO.StreamWriter(path, append))
		{
			writer.WriteLine($"{distance:F8},{time}");
		}
	}
}
