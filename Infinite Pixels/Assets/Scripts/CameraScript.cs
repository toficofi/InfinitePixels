using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraScript : MonoBehaviour {
    public Camera currentCamera;
    public ChunkManager chunkManager;
    public SelectorController selector;
    public List<Camera> cameras;
    public int cameraLevel = 0;

    // These are updated as the current camera changes
    public float sensitivity;
    public float speedToSwitchToGentleMode;
    public float gentleModeSensitivity;
    public float maxSpeed = 2f;

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
        CameraDataScript camScript = currentCamera.gameObject.GetComponent<CameraDataScript>();

        chunkManager.viewingDistance = camScript.viewingArea;

        sensitivity = camScript.sensitivity;
        speedToSwitchToGentleMode = camScript.speedToSwitchToGentleMode;
        gentleModeSensitivity = camScript.gentleModeSensitivity;
        maxSpeed = camScript.maxSpeed;
        //selector.vectorFromCamera = selector.transform.position - currentCamera.transform.position;y
    }

    public void SetCameraLevel(int cameraLevel)
    {
        this.cameraLevel = cameraLevel;
        ChangeToCamera(cameras[cameraLevel]);
    }

	// Use this for initialization
	void Start () {
        ChangeToCamera(cameras[0]);
	}
	
	// Update is called once per frame
	void Update () {
		
	}

}
