using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour {

    public GameObject target;
    public float smoothTime = 0.3f;

    private Vector3 vectorFromTarget;
    private Vector3 velocity = Vector3.zero;

    void Start()
    {
        vectorFromTarget = target.transform.position - this.transform.position;
    }

    /*
    void FixedUpdate()
    {
        // Define a target position above and behind the target transform
        Vector3 targetPosition = target.transform.position - vectorFromTarget;

        // Smoothly move the camera towards that target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }*/
}
