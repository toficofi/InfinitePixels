﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragScript : MonoBehaviour {
    public GameObject cameras;
    public float thresholdBeforeClearingDrag;
    bool mouseDown = false;
    public Vector2 lastPosition = new Vector2(0, 0);
    public Vector2 startPosition = new Vector2(0, 0);
    public float deltaBeforeClipping;
    public float distanceBeforeDrag;
    public float framesPassBeforeDrag;
    public SelectorController selector;
    CameraScript cameraScript;

    float frameCountAtMouseDown = 0; // Time.frameCount at the time of mousedown

    Vector3 lastPanPosition;
    public float PanSpeed;

    //Vector3 cameraPositionBeforeDrag;
    //Vector3 touchPositionBeforeDrag;

	// Use this for initialization
	void Start () {
        selector = GameObject.Find("selector").GetComponent<SelectorController>();
        cameraScript = cameras.GetComponent<CameraScript>();
	}

    public bool IsResting()
    {
        return selector.velocity.magnitude < cameraScript.speedToSwitchToGentleMode;
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Touch touch in Input.touches)
        {
            //if (touch.phase == touchphase.began)
            //{
            //    camerapositionbeforedrag = cameras.transform.position;
            //    touchpositionbeforedrag = new vector3(touch.position.x, 0, touch.position.y);
            //}

            if (touch.phase == TouchPhase.Began)
            {
                lastPanPosition = touch.position;
            }

            if (touch.phase == TouchPhase.Moved)
            {
                Debug.Log("touchy boye " + touch.position.x + ", " + touch.position.y);
                Vector3 newPanPosition = new Vector3(touch.position.x, touch.position.y, 0);

                float distanceFromCamera = -cameras.transform.position.z;

                Vector3 panDelta = lastPanPosition - newPanPosition;
                Vector3 offset = cameraScript.currentCamera.ScreenToViewportPoint(new Vector3(panDelta.x, panDelta.y, distanceFromCamera));
                Vector3 move = new Vector3(offset.x * PanSpeed, 0, offset.y * PanSpeed);

                // Perform the movement
                cameras.transform.Translate(move, Space.World);


                //Vector3 touchDelta = touchPositionBeforeDrag - new Vector3(touch.position.x, 0, touch.position.y);
                //cameras.transform.position = cameraPositionBeforeDrag + touchDelta;

                lastPanPosition = newPanPosition;
            }
        }

        /*
        if (mouseDown)
        {
            Vector2 delta = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastPosition;
            //if (delta.magnitude < deltaBeforeClipping) delta = new Vector2(0, 0);
            //delta.y *= 1.5f; // Compensate for horizontal display
            if (selector.velocity.magnitude < cameraScript.speedToSwitchToGentleMode) delta *= cameraScript.gentleModeSensitivity;
            else delta *= cameraScript.sensitivity;
            selector.velocity += new Vector3(delta.x, 0, delta.y);
            lastPosition = Input.mousePosition;
        }


        selector.velocity *= selector.drag;
        if (selector.velocity.x > cameraScript.maxSpeed) selector.velocity.x = cameraScript.maxSpeed;
        if (selector.velocity.z > cameraScript.maxSpeed) selector.velocity.z = cameraScript.maxSpeed;
        if (selector.velocity.x < -cameraScript.maxSpeed) selector.velocity.x = -cameraScript.maxSpeed;
        if (selector.velocity.z < -cameraScript.maxSpeed) selector.velocity.z = -cameraScript.maxSpeed;
        if (selector.velocity.magnitude < thresholdBeforeClearingDrag && !mouseDown) selector.velocity = new Vector2(0, 0);
        cameras.transform.position -= selector.velocity;
        */
    }

    public void MouseButtonDown()
    {
        //lastPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        //mouseDown = true;
        //startPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        //frameCountAtMouseDown = Time.frameCount;
    }

    public void MouseButtonPressed()
    {
        //selector.PlacePixel();
    }

    public void MouseButtonUp()
    {
        //// Only count this as a click if there has not been too far of a distance/time since the original MouseDown
        //mouseDown = false;
        //float framesSince = Time.frameCount - frameCountAtMouseDown;
        //float delta = (startPosition - new Vector2(Input.mousePosition.x, Input.mousePosition.y)).magnitude;
        //Debug.Log("Delta: " + delta + " - distance before drag: " + distanceBeforeDrag);
        //Debug.Log("Frames since mouseDown: " + framesSince);
        //if (IsResting() && delta < distanceBeforeDrag && framesSince < framesPassBeforeDrag) {
            
        //    selector.PlacePixel();
        //}
    }
}

