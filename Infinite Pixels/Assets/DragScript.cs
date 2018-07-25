using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragScript : MonoBehaviour {
    public GameObject cameras;
    public float thresholdBeforeClearingDrag;
    bool mouseDown = false;
    public Vector2 lastPosition = new Vector2(0, 0);
    public SelectorController selector;

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
        selector.gameObject.transform.position -= selector.velocity;
    }

    public void MouseButtonDown()
    {
        lastPosition = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
        mouseDown = true;
    }

    public void MouseButtonPressed()
    {
        selector.PlacePixel();
    }

    public void MouseButtonUp()
    {
        mouseDown = false;
    }
}

