using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

public class SaveManager : MonoBehaviour {

	public static SaveManager saveManager;
	public static Progress progress;

	void Awake() {
		if (saveManager == null) {
			DontDestroyOnLoad(gameObject);
			saveManager = this;
		} else if (saveManager != this) {
			Destroy(saveManager.gameObject);
		}
		LoadProgress();
	}

	/// <summary>
	/// Saves the progress.
	/// </summary>
	public static void SaveProgress() {

		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + "/saveData.dat");
		bf.Serialize(file, progress);
		file.Close();
	}

	/// <summary>
	/// Loads the progress.
	/// </summary>
	public void LoadProgress() {
		
		if (File.Exists(Application.persistentDataPath + "/saveData.dat")) {
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/saveData.dat", FileMode.Open);
			progress = (Progress)bf.Deserialize(file);
			file.Close();
		} else {
			progress = new Progress();
			progress.levelEnabled = 1;
			SaveProgress();
		}
	}

	/// <summary>
	/// Erases the progress.
	/// </summary>
	public static void EraseProgress() {
		SaveManager.progress.levelEnabled = 1;
		SaveProgress();
	}

	/// <summary>
	/// Generates a new random puzzle and saves it.
	/// </summary>
	/// <returns>The puzzle path data.</returns>
	/// <param name="dimension">Dimension of puzzle.</param>
	public List<Vector3> NewPuzzle(int dimension = 3) {

		BinaryFormatter bf = new BinaryFormatter();
		FileStream file = File.Create(Application.persistentDataPath + "/puzzleData.dat");

		PuzzleData puzzleData = new PuzzleData();
		List<Vector3> temp = Tools.FindPath(dimension);

		for (int i = 0; i < temp.Count; i++) {
			float[] elem = new float[3];
			elem[0] = temp[i].x;
			elem[1] = temp[i].y;
			elem[2] = temp[i].z;
			puzzleData.path.Add(elem);
		}

		bf.Serialize(file, puzzleData);
		file.Close();

		return temp;
	}

	/// <summary>
	/// Loads random puzzle from memory.
	/// If no data is found, generates and saves it.
	/// </summary>
	/// <returns>The puzzle path data.</returns>
	/// <param name="dimension">Dimension of puzzle.</param>
	public List<Vector3> LoadPuzzle(int dimension = 3) {

		if (File.Exists(Application.persistentDataPath + "/puzzleData.dat")) {
			BinaryFormatter bf = new BinaryFormatter();
			FileStream file = File.Open(Application.persistentDataPath + "/puzzleData.dat", FileMode.Open);

			PuzzleData puzzleData = (PuzzleData)bf.Deserialize(file);
			file.Close();

			List<Vector3> temp = new List<Vector3>();
			for (int i = 0; i < puzzleData.path.Count; i++) {
				Vector3 elem = new Vector3(puzzleData.path[i][0],puzzleData.path[i][1],puzzleData.path[i][2]);
				temp.Add(elem);
			}

			return temp;
		}
		return NewPuzzle(dimension);
	}
}

[Serializable]
class PuzzleData {
	public List<float[]> path = new List<float[]>();
}

[Serializable]
public class Progress {
	public int levelEnabled = 0;
}