using UnityEngine;
using System.Collections;

public class InstantInsanityManager : MonoBehaviour {

	bool somethingSpinning;
	public Transform layer;
	public Transform cubes;
	public Transform cube1;
	public Transform cube2;
	public Transform cube3;
	public Transform cube4;
	public Transform selectedCube;
	bool cubeSpinSet;
	bool cubeSpinning;
	Vector2 spinTouchCentre;
	RaycastHit cubeHit;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
		CubeSpinDetection();

	}


	/// <summary>
	/// Spins the cube until touch is released.
	/// </summary>
	IEnumerator CubeSpin(Transform cube, string direction, string slideDirection, Vector2 centre) {
		cubeHit = new RaycastHit();
		cubeSpinning = true;
		somethingSpinning = true;

		layer.position = cube.position;
		cube.SetParent(layer);
		while (Input.touchCount > 0) {
			if (direction == "x") {
				switch (slideDirection) {
				case "x": layer.eulerAngles = new Vector3(0.5f*(Input.GetTouch(0).position-centre).x,0,0); break;
				case "y": layer.eulerAngles = new Vector3(0.5f*(Input.GetTouch(0).position-centre).y,0,0); break;
				}
			}
			if (direction == "y") {
				switch (slideDirection) {
				case "x": layer.eulerAngles = new Vector3(0,0.5f*(centre-Input.GetTouch(0).position).x,0); break;
				case "y": layer.eulerAngles = new Vector3(0,0.5f*(centre-Input.GetTouch(0).position).y,0); break;
				}
			}
			if (direction == "z") {
				switch (slideDirection) {
				case "x": layer.eulerAngles = new Vector3(0,0,0.5f*(centre-Input.GetTouch(0).position).x); break;
				case "y": layer.eulerAngles = new Vector3(0,0,0.5f*(centre-Input.GetTouch(0).position).y); break;
				}
			}
			yield return new WaitForEndOfFrame();
		}
		cube.SetParent(cubes);
		layer.eulerAngles = Vector3.zero;
		StartCoroutine(SnapCube(cube));

		cubeSpinning = false;
	}

	/// <summary>
	/// Snaps cube rotation to closest right angle
	/// </summary>
	IEnumerator SnapCube(Transform cube) {
		somethingSpinning = true;
		Quaternion start = cube.rotation;
		Quaternion end = Quaternion.Euler(new Vector3(Mathf.RoundToInt(cube.eulerAngles.x/90)*90, Mathf.RoundToInt(cube.eulerAngles.y/90)*90, Mathf.RoundToInt(cube.eulerAngles.z/90)*90));
		float a = 0;
		float time = 0.1f;
		while (a < time) {
			cube.rotation = Quaternion.Lerp(start, end, Mathf.SmoothStep(0,1,a/time));
			a += Time.deltaTime;
			yield return new WaitForEndOfFrame();
		}
		cube.rotation = end;
		somethingSpinning = false;
	}

	/// <summary>
	/// Returns a RaycastHit of a tapped transform.
	/// </summary>
	RaycastHit TapHit() {
		if (Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) {
			Ray ray = Camera.main.ScreenPointToRay(Input.GetTouch(0).position);
			RaycastHit hit;
			Physics.Raycast(ray, out hit);
			return hit;
		}
		return new RaycastHit();
	}

	bool Approx(float n1, float n2 = 0f, float delta = 0.001f) {
		if (Mathf.Abs(n1-n2) < delta)
			return true;
		return false;
	}

	void CubeSpinDetection() {
		RaycastHit hit = TapHit();
		if (hit.transform != null) {
			cubeHit = hit;
			spinTouchCentre = Input.GetTouch(0).position;
		}
		if (cubeHit.transform != null && Input.touchCount == 1) {
			if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).x) > 10) {
				if (Approx(cubeHit.point.z-cubeHit.transform.position.z, -1f) || Approx(cubeHit.point.x-cubeHit.transform.position.x, -1f))
					StartCoroutine(CubeSpin(cubeHit.transform, "y","x", spinTouchCentre));
				else if (Approx(cubeHit.point.y-cubeHit.transform.position.y,1f))
					StartCoroutine(CubeSpin(cubeHit.transform, "z","x", spinTouchCentre));
			}
			else if (Mathf.Abs((Input.GetTouch(0).position-spinTouchCentre).y) > 10) {
				if (Approx(cubeHit.point.z-cubeHit.transform.position.z,-1) || Approx(cubeHit.point.y-cubeHit.transform.position.y, 1f))
					StartCoroutine(CubeSpin(cubeHit.transform, "x","y", spinTouchCentre));
				else if (Approx(cubeHit.point.x-cubeHit.transform.position.x, -1f))
					StartCoroutine(CubeSpin(cubeHit.transform, "z","y", spinTouchCentre));
			}
		}
	}

	public void XRay() {
		foreach (Transform cube in cubes) {
			cube.GetComponent<Renderer>().enabled = !cube.GetComponent<Renderer>().enabled;
		}
		foreach (Transform cube in layer) {
			cube.GetComponent<Renderer>().enabled = !cube.GetComponent<Renderer>().enabled;
		}
	}

	public void Back() {
		Application.LoadLevel("Menu");
	}

	public void Restart() {
		Application.LoadLevel(Application.loadedLevelName);
	}
}
