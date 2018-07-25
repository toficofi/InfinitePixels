using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {
    public Camera currentCamera;
    public ChunkManager chunkManager;
    public SelectorController selector;
    public List<Camera> cameras;
    public int cameraLevel = 0;

    
    public void IncreaseCameraLevel()
    {
        cameraLevel++;
        if (cameraLevel > cameras.Count - 1) cameraLevel = cameras.Count - 1;
        ChangeToCamera(cameras[cameraLevel]);
    }

    public void DecreateCameraLevel()
    {
        cameraLevel--;
        if (cameraLevel < 0) cameraLevel = 0;
        ChangeToCamera(cameras[cameraLevel]);
    }

    public void ChangeToCamera(Camera cam)
    {
        currentCamera.enabled = false;
        currentCamera = cam;
        currentCamera.enabled = true;
        chunkManager.viewingDistance = currentCamera.gameObject.GetComponent<CameraDataScript>().viewingArea;
        //selector.vectorFromCamera = selector.transform.position - currentCamera.transform.position;y
    }

	// Use this for initialization
	void Start () {
        ChangeToCamera(cameras[0]);
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
