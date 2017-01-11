using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class SnakeCubeManager : MonoBehaviour {

	// To be assigned in the Inspector:
	public Transform puzzle;
	public Transform mainCam;
	public Transform selectionCursor;
	public Material cubeMaterial;

	public Mesh darkMesh;
	public Mesh brightMesh;
	public Mesh errorMesh;
	public Mesh selectedMesh;

	// Level info
	int dimension = 3;
	public static int level;
	public static string levelName;
	public static List<Vector3> path;
	public static int solutionStep;

	// Control related
	public static Transform selectedCube;
	public int moveDirection = 1;
	public static List<Transform> moveHistory = new List<Transform>();
	Vector3 lastMousePos;
	bool cubeSpinSet;
	bool cubeSpinning;
	Vector2 spinTouchCentre;
	public Transform layer;

	// Animation related
	public static bool somethingSpinning;
	bool done = false;

	void Start () {
		// Initialize puzzle
		InitiatePuzzle();
		selectedCube = puzzle.GetChild(0);
		selectionCursor.position = selectedCube.position;
		selectionCursor.rotation = selectedCube.rotation;
		moveHistory = new List<Transform>();
		RefreshCubes();

		cubeSpinSet = false;
		cubeSpinning = false;
		somethingSpinning = false;
	}

	void Update () {
		Transform tappedTransform;
		if (!somethingSpinning && TransformTapped(out tappedTransform) && tappedTransform.root == puzzle) {
			if (selectedCube == tappedTransform) {
				StartCoroutine(SlowSpin(selectedCube, Vector3.right, moveDirection*90f));
				moveHistory.Add(selectedCube);
			} else {
				selectedCube = tappedTransform;
				RefreshCubes();
			}
   		}

		CubeSpinDetection();

	}

	[ContextMenu ("Func")]
	void Func() {
		Start();
	}
	// ########## UI-INVOKED FUNCTIONS ########## "Cause our buttons need functions!"
	public void Undo() {
		if (moveHistory.Count != 0 && !somethingSpinning) {
			Transform cube = moveHistory[moveHistory.Count-1];
			moveHistory.RemoveAt(moveHistory.Count-1);
			StartCoroutine(SlowSpin(cube, -Vector3.right));
		}
	}
	public void Restart() {
		Start();
	}
	public void Random() {
		puzzle.GetComponent<Puzzle>().path = Tools.FindPath();
		Start();
	}
	public void Back() {
		Application.LoadLevel("Menu");
	}
	// #################### FUNCTIONS ####################
	void InitiatePuzzle() {
		path = puzzle.GetComponent<Puzzle>().path;
		puzzle.eulerAngles = Vector3.zero;
		Transform currentCube = puzzle.GetChild(0);
		currentCube.transform.localPosition = -Vector3.one;
		currentCube.transform.localEulerAngles = Vector3.zero;

		currentCube = puzzle.GetComponentsInChildren<Transform>()[2];
		currentCube.transform.localPosition = Vector3.right;
		currentCube.transform.localEulerAngles = Vector3.zero;

		int changeCount = 0;
		for (int i = 2; i < path.Count; i++) {
			currentCube = puzzle.GetComponentsInChildren<Transform>()[i+1];
			if (path[i-1] - path[i-2] == path[i] - path[i-1]) {
				currentCube.transform.localPosition = Vector3.right;
				currentCube.transform.localEulerAngles = Vector3.zero;
			} else {
				currentCube.transform.localPosition = (2*(changeCount % 2) - 1)*-Vector3.forward;
				currentCube.transform.localEulerAngles = (2*(changeCount % 2) - 1)*90*Vector3.up;
				changeCount++;
			}
		}
		puzzle.position = Vector3.zero;
		return;
	}
	/// <summary>
	/// Solves the puzzle.
	/// </summary>
	public void SolvePuzzle() {
		StartCoroutine(SlowSolve(27));
  	}
	/// <summary>
	/// Solves the first steps of the puzzle.
	/// </summary>
	/// <param name="steps">Steps.</param>
	void QuickSolve(int steps) {
		int i = 1;
		int j = 0;
		foreach (Transform cube in puzzle.GetChild(0).GetComponentsInChildren<Transform>()) {
			if (cube.childCount != 0 && i < steps) {
				selectedCube = cube;
				while (Vector3.Magnitude(cube.GetChild(0).position/puzzle.lossyScale.x+Vector3.one - path[i]) > 0.5f && j<1000) {
					cube.Rotate(90f, 0f, 0f);
					j++;
				}
			}
			i++;
		}
  	}
	/// <summary>
	/// Checks if a tap was performed.
	/// </summary>
	/// <returns><c>true</c>, if tap was performed, <c>false</c> otherwise.</returns>
	/// <param name="position">Position of tap.</param>
	bool GetTap(out Vector2 position) {
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Ended) {
			position = Input.GetTouch(0).position;
			return true;
		} else if (Input.GetMouseButtonUp(0)) {
			position = Input.mousePosition;
			return true;
		} else {
			position = Vector2.zero;
			return false;
		}
  	}
	/// <summary>
	/// Checks if a transform with a collider was tapped.
	/// </summary>
	/// <returns><c>true</c>, if transform was tapped, <c>false</c> otherwise.</returns>
	/// <param name="t">Tapped transform.</param>
	bool TransformTapped(out Transform t) {
		Vector2 tapPosition;
		if (GetTap(out tapPosition)) {
			Ray ray = Camera.main.ScreenPointToRay(tapPosition);
			RaycastHit hit;
			if (Physics.Raycast(ray, out hit)) {
				t = hit.transform;
				return true;
			} else {
				t = null;
				return false;
			}
		}
		t = null;
		return false;
	}
	void RefreshCubes() {
		foreach (Transform cube in puzzle.GetChild(0).GetComponentsInChildren<Transform>()) {
			bool error = false;
			foreach (Transform other in puzzle.GetChild(0).GetComponentsInChildren<Transform>()) {
				if (Vector3.Magnitude(cube.position-other.position) < 0.5f*other.lossyScale.x && cube != other)
					error = true;
			}
			if (error)
				cube.GetComponent<MeshFilter>().sharedMesh = errorMesh;
			else
				if (System.Convert.ToInt32(cube.name.Substring(5)) % 2 == 1)
					cube.GetComponent<MeshFilter>().sharedMesh = brightMesh;
				else
					cube.GetComponent<MeshFilter>().sharedMesh = darkMesh;
		}
		selectedCube.GetComponent<MeshFilter>().sharedMesh = selectedMesh;
		selectionCursor.position = selectedCube.position;
		selectionCursor.eulerAngles = new Vector3(selectionCursor.eulerAngles.x, selectedCube.eulerAngles.y, selectedCube.eulerAngles.z+90);
	}
	void CubeSpinDetection() {
		if (Input.touchCount == 1 && !somethingSpinning) {
			if (Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).position.y/Screen.height < 0.8f) {
				spinTouchCentre = Input.GetTouch(0).position;
				cubeSpinSet = true;
			}
			if (cubeSpinSet) {
				if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).x) > 10) {
					StartCoroutine(CubeSpin("y",spinTouchCentre));
				}
				else if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).y) > 10 && spinTouchCentre.x/Screen.width > 0.5f) {
					StartCoroutine(CubeSpin("x",spinTouchCentre));
				}
				else if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).y) > 10 && spinTouchCentre.x/Screen.width < 0.5f) {
					StartCoroutine(CubeSpin("z",spinTouchCentre));
				}
			}
		}
	}
	// #################### COUROUTINES ####################

	/// <summary>
	/// Spins the cube until touch is released.
	/// </summary>
	IEnumerator CubeSpin(string direction, Vector2 centre) {
		cubeSpinSet = false;
		cubeSpinning = true;
		somethingSpinning = true;

		puzzle.SetParent(layer);
		while (Input.touchCount > 0) {
			switch (direction) {
			case "x": layer.eulerAngles = new Vector3(0.5f*(Input.GetTouch(0).position-centre).y,0,0); break;
			case "y": layer.eulerAngles = new Vector3(0,0.5f*(centre-Input.GetTouch(0).position).x,0); break;
			case "z": layer.eulerAngles = new Vector3(0,0,0.5f*(centre-Input.GetTouch(0).position).y); break;
			}
			yield return new WaitForEndOfFrame();
		}
		puzzle.SetParent(null);
		layer.eulerAngles = Vector3.zero;
		StartCoroutine(SnapCube());

		cubeSpinning = false;
	}
	/// <summary>
	/// Snaps cube rotation to closest right angle
	/// </summary>
	IEnumerator SnapCube() {
		somethingSpinning = true;
		Quaternion start = puzzle.rotation;
		Quaternion end = Quaternion.Euler(new Vector3(Mathf.RoundToInt(puzzle.eulerAngles.x/90)*90, Mathf.RoundToInt(puzzle.eulerAngles.y/90)*90, Mathf.RoundToInt(puzzle.eulerAngles.z/90)*90));
		float a = 0;
		float time = 0.1f;
		while (a < time) {
			puzzle.rotation = Quaternion.Lerp(start, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		puzzle.rotation = end;
		somethingSpinning = false;
	}
	/// <summary>
	/// (COROUTINE) Slowly spins a transform.
	/// </summary>
	/// <param name="t">The transform.</param>
	/// <param name="x">The x coordinate.</param>
	/// <param name="y">The y coordinate.</param>
	/// <param name="z">The z coordinate.</param>
	/// <param name="speed">Speed of spin.</param>
	IEnumerator SlowSpin(Transform t, Vector3 axis, float angle = 90f, float time = 0.2f) {
		somethingSpinning = true;

		float a = 0;
		Quaternion start = t.rotation;
		t.Rotate(axis, angle);
		Quaternion end = t.rotation;
		while (a < time) {
			t.rotation = Quaternion.Lerp(start, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		t.rotation = end;
		RefreshCubes();

		somethingSpinning = false;
	}
	/// <summary>
	/// (COROUTINE) Slowly solves the first n steps of the puzzle.
	/// </summary>
	IEnumerator SlowSolve(int n) {
		int i = 1;
		foreach (Transform cube in puzzle.GetChild(0).GetComponentsInChildren<Transform>()) {
			selectedCube = cube;
			selectionCursor.position = selectedCube.position;
			selectionCursor.rotation = selectedCube.rotation;
			selectionCursor.Rotate(new Vector3(0,0,90));
			if (cube.childCount != 0 && i < n) {
				while (Vector3.Magnitude(cube.GetChild(0).position-puzzle.position - puzzle.lossyScale.x*path[i]) > 0.2f) {
					if (!somethingSpinning) {
						StartCoroutine(SlowSpin(cube, Vector3.right));
						Debug.Log("HOI");
					}
					yield return new WaitForEndOfFrame();
				}
			}
			i++;
		}
      	}
	/// <summary>
	/// (COROUTINE) Slowly unfolds puzzle.
	/// </summary>
	IEnumerator SlowUnfold() {
		bool cubeout = true;
		while (cubeout || somethingSpinning) {
			cubeout = false;
			foreach(Transform cube in puzzle.GetComponentsInChildren<Transform>()) {
				if (Mathf.Abs(cube.localEulerAngles.x) > 10 || Mathf.Abs(cube.localEulerAngles.z) > 10) {
					cubeout = true;
				}
			}
			if (!somethingSpinning) {
				foreach(Transform cube in puzzle.GetComponentsInChildren<Transform>()) {
					if (Mathf.Abs(cube.localEulerAngles.x) > 10 || Mathf.Abs(cube.localEulerAngles.z) > 10) {
						StartCoroutine(SlowSpin(cube, Vector3.right));
					}
				}
			}
			yield return new WaitForEndOfFrame();
		}
	}

}
