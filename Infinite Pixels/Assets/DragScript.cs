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
    float frameCountAtMouseDown = 0; // Time.frameCount at the time of mousedown
	// Use this for initialization
	void Start () {
        selector = GameObject.Find("selector").GetComponent<SelectorController>();
	}

    public bool IsResting()
    {
        return selector.velocity.magnitude < selector.speedToSwitchToGentleMode;
    }

    // Update is called once per frame
    void Update()
    {
        if (mouseDown)
        {
            Vector2 delta = new Vector2(Input.mousePosition.x, Input.mousePosition.y) - lastPosition;
            //if (delta.magnitude < deltaBeforeClipping) delta = new Vector2(0, 0);
            delta.y *= 1.5f; // Compensate for horizontal display
            if (selector.velocity.magnitude < selector.speedToSwitchToGentleMode) delta *= selector.gentleModeSensitivity;
            else delta *= selector.sensitivity;
            selector.velocity += new Vector3(delta.x, 0, delta.y);
            lastPosition = Input.mousePosition;
        }


        selector.velocity *= selector.drag;
        if (selector.velocity.x > selector.maxSpeed) selector.velocity.x = selector.maxSpeed;
        if (selector.velocity.z > selector.maxSpeed) selector.velocity.z = selector.maxSpeed;
        if (selector.velocity.x < -selector.maxSpeed) selector.velocity.x = -selector.maxSpeed;
        if (selector.velocity.z < -selector.maxSpeed) selector.velocity.z = -selector.maxSpeed;
        if (selector.velocity.magnitude < thresholdBeforeClearingDrag && !mouseDown) selector.velocity = new Vector2(0, 0);
        cameras.transform.position -= selector.velocity;
    }

    public void MouseButtonDown()
    {
        lastPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouseDown = true;
        startPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        frameCountAtMouseDown = Time.frameCount;
    }

    public void MouseButtonPressed()
    {
        selector.PlacePixel();
    }

    public void MouseButtonUp()
    {
        // Only count this as a click if there has not been too far of a distance/time since the original MouseDown
        mouseDown = false;
        float framesSince = Time.frameCount - frameCountAtMouseDown;
        float delta = (startPosition - new Vector2(Input.mousePosition.x, Input.mousePosition.y)).magnitude;
        Debug.Log("Delta: " + delta + " - distance before drag: " + distanceBeforeDrag);
        Debug.Log("Frames since mouseDown: " + framesSince);
        if (delta < distanceBeforeDrag && framesSince < framesPassBeforeDrag) {
            
            selector.PlacePixel();
        }
    }
}

