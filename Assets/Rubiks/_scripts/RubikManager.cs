using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;
/* A word on how this script works
 * 
 * 1. Face tracking: 			The trickiest part of this app is that movements need to happen according to current angle the cube is in. This was
 * 					 			solved with the RefreshFaces funtion, which makes references to each face ("faces") correspond to current position.
 * 								This was written to accomodate a 5x5x5 (professor's) cube, but pay no mind to that.
 * 
 * 2. Spinning: 				Mostly happens by adding whatever needs to be spun to a parent Transform (called "layer"), spinning that transform, 
 * 								and then detaching its children.
 * 
	 * a) Spinning the cube around: The function CubeSpinDetection detects a touch gesture on screen and start the CubeSpin coroutine. 
	 * 								This keeps running until touch is released, and then calls the SnapCube coroutine to snap to a right angle.
	 * 
	 * b) Spinning faces: 			UI element functions push commands (right now they look like "u", "r", "d'", etc) to a buffer (a list of strings). 
	 * 								This buffer's instructions are processed in the BufferProcess function, which will call the FaceSpin coroutine.
 * 
 * 3. Controls
 * 
 * a) Button interface:         Simply press buttons to spin faces. Hold down the arrow button and press the same buttons for inverted movements.
 * 
 * b) Knob Interface:           Move the knob to select a face (it will be highlighted) and press the buttons to spin it either way. It's BAD.
 */
public class RubikManager : MonoBehaviour {

	//  Important objects
	public Transform rubik;
	List<List<Transform>> faces = new List<List<Transform>>{new List<Transform>(),new List<Transform>(),new List<Transform>(),
															new List<Transform>(),new List<Transform>(),new List<Transform>(),
															new List<Transform>(),new List<Transform>(),new List<Transform>(),
															new List<Transform>(),new List<Transform>(),new List<Transform>(),
															new List<Transform>(),new List<Transform>(),new List<Transform>()};

	bool somethingSpinning;
	public Transform layer;
	public List<string> buffer = new List<string>();

	bool prime;
	public Transform spinButtons;
	public GameObject primeButton;

	public Transform knob;
	Vector2 KNOBCENTER;
	Vector2 deltaKnob;
	float knobAngle;
	string command;
	string lastHighlighted;
	public Mesh blackCube;
	public Mesh grayCube;

	bool cubeSpinSet;
	bool cubeSpinning;
	Vector2 spinTouchCentre;
	Vector2 swipePosition;
	Vector2 swipeDirection;
	string dir;
	float s;

	List<Transform> moveHistory;
	List<int> moveHistorySense;

	List<string> COMMANDS = new List<string> {"u","d","l","r","f","b","u'","d'","l'","r'","f'","b'"};

	public GameObject knobControl;
	public GameObject buttonControl;

	[ContextMenu ("Run Start")]
	void Start() {
		prime = false;
		KNOBCENTER = new Vector2(0.14f*Screen.width, 0.19f*Screen.height);
		s = rubik.localScale.x;
		moveHistory = new List<Transform>();
		moveHistorySense = new List<int>();

		RefreshFaces();
		//Unhighlight();

		StartCoroutine(LagSpikeFix());
	}

	void Update() {
		BufferProcess();
		KnobProcess();
		CubeSpinDetection();
	}

	[ContextMenu ("Func")]
	void Func() {
		rubik.Rotate(90*Vector3.up);
		RefreshFaces();
	}
	// ########## SUBROUTINES ########## "For all our non-instantaneous needs."
	IEnumerator FaceSpin(string faceName, bool addToHistory = true) {
		somethingSpinning = true;

		List<Transform> face = Id2List(faceName);
		Vector3 axis = Id2Axis(faceName);
		layer.rotation = Quaternion.identity;
		foreach (Transform cube in face)
			cube.SetParent(layer);

		float a = 0;
		float time = 0.1f;
		Quaternion end = Quaternion.Euler(90*axis);
		while (a < time) {
			layer.rotation = Quaternion.Lerp(Quaternion.identity, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		layer.rotation = end;

		if (addToHistory) {
			foreach (Transform cube in layer) {
				if (cube.tag == "Centre")
					moveHistory.Add(cube);
			}
			if (faceName.Length == 1)
				moveHistorySense.Add(1);
			else
				moveHistorySense.Add(-1);
		}
		
		while (layer.childCount != 0)
			layer.GetChild(0).SetParent(rubik);
		layer.rotation = Quaternion.identity;

		RefreshFaces();
		if (knobControl.activeSelf) {
			Unhighlight();
			HighlightFace(command);
		}

		somethingSpinning = false;
	}
	IEnumerator UndoFaceSpin() {
		somethingSpinning = true;

		Transform centre = moveHistory[moveHistory.Count-1];
		moveHistory.RemoveAt(moveHistory.Count-1);
		List<Transform> face = new List<Transform>();
		foreach (Transform cube in rubik) {
			if ((Mathf.Abs(cube.position.x) > 0.1 && Mathf.Abs(cube.position.x-centre.position.x) < 0.5) || 
				(Mathf.Abs(cube.position.y) > 0.1 && Mathf.Abs(cube.position.y-centre.position.y) < 0.5) || 
				(Mathf.Abs(cube.position.z) > 0.1 && Mathf.Abs(cube.position.z-centre.position.z) < 0.5)) {
				face.Add(cube);
			}
		}
		Vector3 axis = -moveHistorySense[moveHistorySense.Count-1]*centre.position;
		moveHistorySense.RemoveAt(moveHistorySense.Count-1);
		layer.rotation = Quaternion.identity;
		foreach (Transform cube in face)
			cube.SetParent(layer);

		float a = 0;
		float time = 0.1f;
		Quaternion end = Quaternion.Euler(90*axis);
		while (a < time) {
			layer.rotation = Quaternion.Lerp(Quaternion.identity, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		layer.rotation = end;

		List<Transform> faceSpun = new List<Transform>();
		while (layer.childCount != 0) {
			layer.GetChild(0).SetParent(rubik);
		}
		layer.rotation = Quaternion.identity;

		RefreshFaces();
		if (knobControl.activeSelf) {
			Unhighlight();
			HighlightFace(command);
		}

		yield return new WaitForEndOfFrame();
		somethingSpinning = false;
	}
	/// <summary>
	/// Spins the cube until touch is released.
	/// </summary>
	IEnumerator CubeSpin(string direction, Vector2 centre) {
		cubeSpinSet = false;
		cubeSpinning = true;
		somethingSpinning = true;
		Unhighlight();

		rubik.SetParent(layer);
		foreach (Button b in spinButtons.GetComponentsInChildren<Button>())
			b.enabled = false;
		while (Input.touchCount > 0) {
			switch (direction) {
			case "x": layer.eulerAngles = new Vector3(0.5f*(Input.GetTouch(0).position-centre).y,0,0); break;
			case "y": layer.eulerAngles = new Vector3(0,0.5f*(centre-Input.GetTouch(0).position).x,0); break;
			case "z": layer.eulerAngles = new Vector3(0,0,0.5f*(centre-Input.GetTouch(0).position).y); break;
			}
			yield return new WaitForEndOfFrame();
		}
		rubik.SetParent(null);
		layer.eulerAngles = Vector3.zero;
		StartCoroutine(SnapCube());
		foreach (Button b in spinButtons.GetComponentsInChildren<Button>())
			b.enabled = true;

		cubeSpinning = false;
	}
	/// <summary>
	/// Snaps cube rotation to closest right angle
	/// </summary>
	IEnumerator SnapCube() {
		somethingSpinning = true;
		Quaternion start = rubik.rotation;
		Quaternion end = Quaternion.Euler(new Vector3(Mathf.RoundToInt(rubik.eulerAngles.x/90)*90, Mathf.RoundToInt(rubik.eulerAngles.y/90)*90, Mathf.RoundToInt(rubik.eulerAngles.z/90)*90));
		float a = 0;
		float time = 0.1f;
		while (a < time) {
			rubik.rotation = Quaternion.Lerp(start, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		rubik.rotation = end;
		RefreshFaces();
		if (knobControl.activeSelf)
			HighlightFace("f");
		somethingSpinning = false;
  	}
	IEnumerator ShuffleCube() {
		int i = 0;
		while (i < 50) {
			if (!somethingSpinning) {
				StartCoroutine(FaceSpin(COMMANDS[Random.Range(0,11)], false));
				i++;
			}
			yield return new WaitForEndOfFrame();
		}
	}
	/// <summary>
	/// Shabby workaround for an issue with lag the first time a button is pressed. This presses a button real fast without changing its colors.
	/// </summary>
	IEnumerator LagSpikeFix() {
		ColorBlock temp = primeButton.GetComponent<Button>().colors;
		temp.pressedColor = new Color(54f/255f,54f/255f,54f/255f);
		primeButton.GetComponent<Button>().colors = temp;
		PointerEventData pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.pointerEnterHandler);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.submitHandler);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.pointerDownHandler);
		yield return new WaitForEndOfFrame();
		pointer = new PointerEventData(EventSystem.current);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.pointerEnterHandler);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.submitHandler);
		ExecuteEvents.Execute(primeButton, pointer, ExecuteEvents.pointerUpHandler);
		temp.pressedColor = new Color(200f/255f,200f/255f,200f/255f);
		primeButton.GetComponent<Button>().colors = temp;
	}

	// ########## UI-INVOKED FUNCTIONS ########## "Cause our buttons need functions!"
	/// <summary>
	/// Shuffles the cube with 50 random movements.
	/// </summary>
	public void Shuffle() {
		//(COMMANDS[Random.Range(0,11)])
		StartCoroutine(ShuffleCube());
	}
	/// <summary>
	/// Reloads this scene.
	/// </summary>
	public void Restart() {
		Application.LoadLevel(Application.loadedLevelName);
	}
	/// <summary>
	/// Toggles X-Ray vision (transparent cubes).
	/// </summary>
	public void XRay() {
		foreach (Transform cube in rubik) {
			cube.GetComponent<Renderer>().enabled = !cube.GetComponent<Renderer>().enabled;
		}
		foreach (Transform cube in layer) {
			if (cube != rubik)
				cube.GetComponent<Renderer>().enabled = !cube.GetComponent<Renderer>().enabled;
		}
	}
	/// <summary>
	/// Toggles between button mode and knob mode.
	/// </summary>
	public void InterfaceToggle() {
		Unhighlight();
		knobControl.SetActive(!knobControl.activeSelf);
		buttonControl.SetActive(!buttonControl.activeSelf);
		if (knobControl.activeSelf)
			HighlightFace(command);
	}
	/// <summary>
	/// Inverts face movements.
	/// </summary>
	public void PrimeButtonDown() {
		prime = true;
		foreach (Transform button in spinButtons) {
			button.GetChild(0).GetComponent<Text>().text += "'";
		}
	}
	/// <summary>
	/// Uninverts face movements.
	/// </summary>
	public void PrimeButtonUp() {
		prime = false;
		foreach (Transform button in spinButtons) {
			button.GetChild(0).GetComponent<Text>().text = button.GetChild(0).GetComponent<Text>().text[0].ToString();
		}
	}
	/// <summary>
	/// Pushes a command to buffer
	/// </summary>
	public void SpinButton(string faceName) {
		if (buffer.Count < 5 && !cubeSpinning) {
			if (prime)
				buffer.Add(faceName + "'");
			else
				buffer.Add(faceName);
		}
	}
	public void Undo() {
		if (buffer.Count < 5 && !cubeSpinning) {
			buffer.Add("undo");
		}
	}
	public void Back() {
		Application.LoadLevel("Menu");
	}

	// ########## GENERAL FUNCTIONS ########## "Here to make Update() clean as a whistle!"
	/// <summary>
	/// Processes input from the buffer.
	/// </summary>
	public void BufferProcess() {
		if (buffer.Count != 0 && !somethingSpinning) {
			string command = buffer[0];
			buffer.RemoveAt(0);
			if (command == "undo") {
				if (moveHistory.Count > 0)
					StartCoroutine(UndoFaceSpin());
			}
			else
				StartCoroutine(FaceSpin(command));
		}
	}
	/// <summary>
	/// Detects a gesture to spin the cube, and initiates coroutine accordingly.
	/// </summary>
	void CubeSpinDetection() {
		if (Input.touchCount == 1 && !somethingSpinning) {
			if (Input.GetTouch(0).phase == TouchPhase.Began && Input.GetTouch(0).position.x/Screen.width > 0.25f && Input.GetTouch(0).position.x/Screen.width < 0.75f && Input.GetTouch(0).position.y/Screen.height < 0.8f) {
				spinTouchCentre = Input.GetTouch(0).position;
				cubeSpinSet = true;
			}
			if (cubeSpinSet) {
				if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).x) > 10) {
					StartCoroutine(CubeSpin("y",spinTouchCentre));
				}
				else if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).y) > 10 && spinTouchCentre.x/Screen.width > 0.43f) {
					StartCoroutine(CubeSpin("x",spinTouchCentre));
				}
				else if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).y) > 10 && spinTouchCentre.x/Screen.width < 0.43f) {
					StartCoroutine(CubeSpin("z",spinTouchCentre));
				}
			}
		}
	}

	// ########## OTHER FUNCTIONS ##########
	/// <summary>
	/// Retrieves a list of cubes in a layer identified by id.
	/// </summary>
	List<Transform> Id2List(string id) {
		List<Transform> face = new List<Transform>();
		switch (id) {
		case "L" : face = faces[0]; break;
		case "L'": face = faces[0]; break;
		case "l" : face = faces[1]; break;
		case "l'": face = faces[1]; break;
		case "r" : face = faces[3]; break;
		case "r'": face = faces[3]; break;
		case "R" : face = faces[4]; break;
		case "R'": face = faces[4]; break;
		case "D" : face = faces[5]; break;
		case "D'": face = faces[5]; break;
		case "d" : face = faces[6]; break;
		case "d'": face = faces[6]; break;
		case "u" : face = faces[8]; break;
		case "u'": face = faces[8]; break;
		case "U" : face = faces[9]; break;
		case "U'": face = faces[9]; break;
		case "F" : face = faces[10]; break;
		case "F'": face = faces[10]; break;
		case "f" : face = faces[11]; break;
		case "f'": face = faces[11]; break;
		case "b" : face = faces[13]; break;
		case "b'": face = faces[13]; break;
		case "B" : face = faces[14]; break;
		case "B'": face = faces[14]; break;
		}
		return face;
	}
	/// <summary>
	/// Retrieves rotation axis for a layer identified by id.
	/// </summary>
	Vector3 Id2Axis(string id) {
		Vector3 axis = new Vector3();
		switch (id) {
		case "L" : axis = Vector3.left; break;
		case "L'": axis = Vector3.right; break;
		case "l" : axis = Vector3.left; break;
		case "l'": axis = Vector3.right; break;
		case "r" : axis = Vector3.right; break;
		case "r'": axis = Vector3.left; break;
		case "R" : axis = Vector3.right; break;
		case "R'": axis = Vector3.left; break;
		case "D" : axis = Vector3.down; break;
		case "D'": axis = Vector3.up; break;
		case "d" : axis = Vector3.down; break;
		case "d'": axis = Vector3.up; break;
		case "u" : axis = Vector3.up; break;
		case "u'": axis = Vector3.down; break;
		case "U" : axis = Vector3.up; break;
		case "U'": axis = Vector3.down; break;
		case "F" : axis = Vector3.back; break;
		case "F'": axis = Vector3.forward; break;
		case "f" : axis = Vector3.back; break;
		case "f'": axis = Vector3.forward; break;
		case "b" : axis = Vector3.forward; break;
		case "b'": axis = Vector3.back; break;
		case "B" : axis = Vector3.forward; break;
		case "B'": axis = Vector3.back; break;
		}
		return axis;
  	}
	/// <summary>
	/// Spins a face identified by id. Add ' to spin counter-clockwise.
	/// </summary>
	/// <param name="id">Identifier.</param>
	/// <summary>
	/// Refresh "faces" list to correspond to current orientation.
	/// </summary>
	public void RefreshFaces() {
		
		foreach (List<Transform> face in faces) {
			face.Clear();
		}

		foreach (Transform cube in rubik) {
			cube.position = new Vector3(Mathf.Round(cube.position.x), Mathf.Round(cube.position.y), Mathf.Round(cube.position.z));
			cube.eulerAngles = new Vector3(Mathf.Round(cube.eulerAngles.x), Mathf.Round(cube.eulerAngles.y), Mathf.Round(cube.eulerAngles.z));
			if (Mathf.Abs(cube.position.x + 2f*s) < 0.5f){
				faces[0].Add(cube);
			}
			if (Mathf.Abs(cube.position.x + 1f*s) < 0.5f){
				faces[1].Add(cube);
			}
			if (Mathf.Abs(cube.position.x) < 0.5f){
				faces[2].Add(cube);
			}
			if (Mathf.Abs(cube.position.x - 1f*s) < 0.5f){
				faces[3].Add(cube);
			}
			if (Mathf.Abs(cube.position.x - 2f*s) < 0.5f){
				faces[4].Add(cube);
			}
			if (Mathf.Abs(cube.position.y + 2f*s) < 0.5f){
				faces[5].Add(cube);
			}
			if (Mathf.Abs(cube.position.y + 1f*s) < 0.5f){
				faces[6].Add(cube);
			}
			if (Mathf.Abs(cube.position.y) < 0.5f){
				faces[7].Add(cube);
			}
			if (Mathf.Abs(cube.position.y - 1f*s) < 0.5f){
				faces[8].Add(cube);
			}
			if (Mathf.Abs(cube.position.y - 2f*s) < 0.5f){
				faces[9].Add(cube);
			}
			if (Mathf.Abs(cube.position.z + 2f*s) < 0.5f){
				faces[10].Add(cube);
			}
			if (Mathf.Abs(cube.position.z + 1f*s) < 0.5f){
				faces[11].Add(cube);
			}
			if (Mathf.Abs(cube.position.z) < 0.5f){
				faces[12].Add(cube);
			}
			if (Mathf.Abs(cube.position.z - 1f*s) < 0.5f){
				faces[13].Add(cube);
			}
			if (Mathf.Abs(cube.position.z - 2f*s) < 0.5f){
				faces[14].Add(cube);
			}
		}
  	}

	// ########## KNOB-RELATED FUNCTIONS ########## "No one likes these..."
	void KnobProcess() {
		if (knobControl.activeSelf) {
			if (Input.touchCount != 0 && Vector3.Magnitude(Input.GetTouch(0).position - KNOBCENTER) < 0.15f*Screen.width) {
				knob.position = KNOBCENTER + Vector2.ClampMagnitude(Input.GetTouch(0).position - KNOBCENTER, 0.05f*Screen.width);
				deltaKnob = new Vector2(knob.position.x - KNOBCENTER.x, knob.position.y - KNOBCENTER.y)*400/Screen.width;
				knobAngle = Vector2.Angle(Vector2.up, deltaKnob);
				if (deltaKnob.x < 0.5)
					knobAngle = 360-knobAngle;
				if (deltaKnob.magnitude < 10f)
					command = "b";
				else if (knobAngle < 45f)
					command = "u";
				else if (knobAngle < 135f)
					command = "r";
				else if (knobAngle < 225f)
					command = "d";
				else if (knobAngle < 315f)
					command = "l";
				else
					command = "u";
			} else {
				knob.position = KNOBCENTER;
				command = "f";
			}

			if (command != lastHighlighted) {
				Unhighlight();
				HighlightFace(command);
				lastHighlighted = command;
			}
		}
	}
	/// <summary>
	/// Called when clockwise button is clicked. Queues selected face to spin clockwise.
	/// </summary>
	public void ClockwiseButton() {
		if (buffer.Count < 3)
			buffer.Add(command);
	}
	/// <summary>
	/// Called when counter-clockwise button is clicked. Queues selected face to spin counter-clockwise.
	/// </summary>
	public void CounterClockwiseButton() {
		if (buffer.Count < 3)
			buffer.Add(command + "'");
	}
	/// <summary>
	/// Highlights a face.
	/// </summary>
	/// <param name="faceName">Face name.</param>
	public void HighlightFace(string faceName) {
		foreach (Transform cube in Id2List(faceName)) {
			cube.GetComponent<MeshFilter>().sharedMesh = grayCube;
		}
	}
	/// <summary>
	/// Unhighlights faces.
	/// </summary>
	public void Unhighlight() {
		foreach (List<Transform> face in faces) {
			foreach (Transform cube in face) {
				cube.GetComponent<MeshFilter>().sharedMesh = blackCube;
			}
		}
	}
}
