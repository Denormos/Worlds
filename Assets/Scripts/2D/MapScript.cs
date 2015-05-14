﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class MapScript : MonoBehaviour {

	public RawImage Image;

	// Use this for initialization
	void Start () {

		UpdateTexture();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	public void UpdateTexture () {
		
		Texture2D texture = Manager.CurrentTexture;
		
		Image.texture = texture;
	}
}
