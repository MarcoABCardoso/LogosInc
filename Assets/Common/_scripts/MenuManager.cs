using UnityEngine;
using System.Collections;

public class MenuManager : MonoBehaviour {
	public void RubiksStart() {
		Application.LoadLevel("Rubiks");
	}
	public void SnakeCubeStart() {
		Application.LoadLevel("Snake Cube");
	}
	public void InstantInsanityStart() {
		Application.LoadLevel("Instant Insanity");
	}
}
