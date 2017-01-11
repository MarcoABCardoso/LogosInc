using UnityEngine;
using System.Collections;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour {

	TextAsset dialog;
	public Transform dialogBox;

	// Use this for initialization
	void Start () {
		dialog = Resources.Load<TextAsset>("Dialog-EN");
		string[] sequences = Regex.Split(dialog.text, "\r\n\r");
	}
	
	// Update is called once per frame
	void Update () {
	
	}

	/// <summary>
	/// Initiates a dialog.
	/// </summary>
	public void InitiateDialog(string[] sequence) {
		
	}
}
