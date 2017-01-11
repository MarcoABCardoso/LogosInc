using UnityEngine;
using System.Linq;
using System.Collections;
using System.Collections.Generic;

public static class Tools {

	/// <summary>
	/// Shuffle the specified list.
	/// </summary>
	/// <param name="list">The list.</param>
	/// <typeparam name="State">The 1st type parameter.</typeparam>
	public static void Shuffle<State>(this List<State> list) {
		int i = 0;
		while (i < list.Count) {
			int k = UnityEngine.Random.Range(0,list.Count);
			State s = list[k];
			list[k] = list[i];
			list[i] = s;
			i++;
		}
	}
		
	/// <summary>
	/// Gets positions adjacent to given position.
	/// </summary>
	/// <returns>The adjacent positions.</returns>
	/// <param name="position">Position.</param>
	/// <param name="dimension">Dimension.</param>
	public static List<Vector3> GetAdjacent(this Vector3 position, int dimension = 3) {
		List<Vector3> output = new List<Vector3>();
		if (position.x > 0) {
			output.Add(position+Vector3.left);
		}
		if (position.x < dimension-1) {
			output.Add(position+Vector3.right);
		}
		if (position.y > 0) {
			output.Add(position+Vector3.down);
		}
		if (position.y < dimension-1) {
			output.Add(position+Vector3.up);
		}
		if (position.z > 0) {
			output.Add(position+Vector3.back);
		}
		if (position.z < dimension-1) {
			output.Add(position+Vector3.forward);
		}
		return output;
	}

	/// <summary>
	/// Finds random path in cube.
	/// </summary>
	/// <returns>The path.</returns>
	/// <param name="dimension">Dimension of cube.</param>
	public static List<Vector3> FindPath(int dimension = 3) {

		Vector3 initialPos = new Vector3(0,0,0);
		List<Vector3> initialPath = new List<Vector3>();

		initialPath.Add(initialPos);
		State initialState = new State(initialPos, initialPath);

		List<State> expansionList = new List<State>();
		expansionList.Add(initialState);
		while (expansionList.Count != 0) {
			State currentState = expansionList[expansionList.Count-1];
			expansionList.RemoveAt(expansionList.Count-1);

			if (currentState.isGoalState(dimension)) {
				return currentState.path;
			} else {
				List<State> successors = currentState.SmartSuccessors(dimension);
				expansionList.AddRange(successors);
			}

		}
		return null;
	}
		
	/// <summary>
	/// Creates puzzle from path.
	/// </summary>
	/// <returns>The puzzle.</returns>
	/// <param name="path">Path.</param>
	public static Transform CreatePuzzle(List<Vector3> path) {
		Transform puzzle = new GameObject().transform;
		puzzle.gameObject.AddComponent<Puzzle>();
		puzzle.GetComponent<Puzzle>().path = path;
		puzzle.GetComponent<Puzzle>().solutionStep = SnakeCubeManager.solutionStep;
		puzzle.name = SnakeCubeManager.levelName;
		Object cubeA = Resources.Load("Prefabs/Cube A");
		Object cubeB = Resources.Load("Prefabs/Cube B");

		GameObject lastCube = GameObject.Instantiate(cubeA) as GameObject;
		lastCube.transform.SetParent(puzzle);
		lastCube.name = "Cube 0";
		lastCube.transform.localPosition = -Vector3.one;

		GameObject currentCube = GameObject.Instantiate(cubeB) as GameObject;
		currentCube.transform.SetParent(lastCube.transform);
		currentCube.name = "Cube 1";
		lastCube = currentCube;
		currentCube.transform.localPosition = Vector3.right;

		int changeCount = 0;
		for (int i = 2; i < path.Count; i++) {
			if (i % 2 == 0) {
				currentCube = GameObject.Instantiate(cubeA) as GameObject;
			} else {
				currentCube = GameObject.Instantiate(cubeB) as GameObject;
			}
			currentCube.transform.SetParent(lastCube.transform);
			currentCube.name = "Cube " + System.Convert.ToString(i);
			lastCube = currentCube;
			if (path[i-1] - path[i-2] == path[i] - path[i-1]) {
				currentCube.transform.localPosition = Vector3.right;
				currentCube.transform.localEulerAngles = Vector3.zero;
			} else {
				currentCube.transform.localPosition = (2*(changeCount % 2) - 1)*-Vector3.forward;
				currentCube.transform.localEulerAngles = (2*(changeCount % 2) - 1)*90*Vector3.up;
				changeCount++;
			}
		}
		puzzle.localScale = new Vector3(1.5f,1.5f,1.5f);
		puzzle.position = Vector3.zero;
		return puzzle;
	}

	public static int DeadEndCount(List<Vector3> space, Vector3 position) {
		int neighborsi = 0;
		int output = 0;
		foreach (Vector3 posi in space) {
			if (posi != position) {
				foreach (Vector3 posj in space) {
					if (Vector3.Magnitude(posi-posj) == 1) {
						neighborsi++;
					}
				}
				if (neighborsi < 2) {
					output++;
				}
				neighborsi = 0;
			}
		}
		return output;
	}

	public static List<int> Range(int end, int start = 0, int step = 1) {
		List<int> output = new List<int>();
		for (int i = start; i < end; i += step) {
			output.Add(i);
		}
		return output;
	}

	public static void PrintAll<T>(List<T> list) {
		foreach(T elem in list) {
			Debug.Log(elem);
		}
	}
}

public class Variable {

	public Vector3 position;
	public List<int> dominion;
	public int value;

	public Variable(Vector3 pos) {
		position = pos;
		dominion = Tools.Range(27);
	}
}

public class CSPState {


}