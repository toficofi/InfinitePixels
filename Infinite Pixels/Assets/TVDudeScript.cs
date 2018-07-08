using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TVDudeScript : MonoBehaviour {

    public Transform target;
    public float followLooseness;
    public float closenessBeforeStop;
    Vector3 followerVelocity;
    public GameObject model;

    public GameObject monitorLight;
    public GameObject floatingPixel;

    private int speedKeyID;
    // Use this for initialization
    void Start () {
        speedKeyID = Animator.StringToHash("speed");
        
        model.GetComponent<Renderer>().material.color = Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f) / 2f;
    }
	
	// Update is called once per frame
	void Update () {
        Vector3 targetPosition = new Vector3(target.position.x, this.transform.position.y, target.position.z);
        bool shouldMove = true;

        if (Vector3.Distance(targetPosition, this.transform.position) < closenessBeforeStop)
        {
            this.followerVelocity /= 2;
            if (this.followerVelocity.magnitude < 0.3f) shouldMove = false;


        } else
        {
            this.transform.LookAt(targetPosition);
        }

        
        this.GetComponent<Animator>().speed = this.followerVelocity.magnitude;
        if (this.GetComponent<Animator>().speed < 0.5f) this.GetComponent<Animator>().speed = 1;
        this.GetComponent<Animator>().SetFloat(speedKeyID, this.followerVelocity.magnitude);


        if (shouldMove) this.transform.position = Vector3.SmoothDamp(this.transform.position, targetPosition, ref followerVelocity, followLooseness);
        this.transform.position = new Vector3(this.transform.position.x, 0, this.transform.position.z);
    }

    public void ChangeColour(Color color)
    {
        //selectorCube.transform.GetChild(0).GetComponent<Light>().color = color;
        monitorLight.GetComponent<Renderer>().material.color = color; // Box covering face
        floatingPixel.GetComponent<Renderer>().material.color = color; // Floating box
        //tvDude.transform.GetChild(0).GetComponent<Light>().color = color; // Light

        Material boxCoverMat = monitorLight.GetComponent<Renderer>().material;
        boxCoverMat.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(20f));

        Material floatingBoxMat = floatingPixel.GetComponent<Renderer>().material;
        floatingBoxMat.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(20f));
    }
}
