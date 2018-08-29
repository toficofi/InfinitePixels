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

    public GameObject nameText;
    public CameraScript cameraScript;
    public Color colour;


    private int speedKeyID;
    // Use this for initialization
    void Start () {
        speedKeyID = Animator.StringToHash("speed");
        cameraScript = GameObject.Find("Cameras").GetComponent<CameraScript>();
        Color color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f) / 2f;
        color.a = 1;
        ChangeTVDudeColour(color);
    }
	
    public void ChangeTVDudeColour(Color color)
    {
        model.GetComponent<Renderer>().material.color = color;
        this.colour = color;
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
        
        Camera currentCam = cameraScript.currentCamera;
        OrientToCamera(nameText, currentCam);
    }

    public void OrientToCamera(GameObject obj, Camera camera)
    {
        Vector3 v = camera.transform.position - obj.transform.position;
        v.x = v.z = 0.0f;
        obj.transform.LookAt(camera.transform.position - v);
        obj.transform.Rotate(0, 180, 0);
    }

    public void ChangeName(string newName)
    {
        nameText.GetComponent<TextMesh>().text = newName;
    }

    public void ChangeColour(Color color)
    {
        float h, s, v = 0;
        Util.ColorToHSV(color, out h, out s, out v);

        v = 0.8f;

        color = Util.ColorFromHSV(h, s, v);



        //selectorCube.transform.GetChild(0).GetComponent<Light>().color = color;
        monitorLight.GetComponent<Renderer>().material.color = color; // Box covering face
        floatingPixel.GetComponent<Renderer>().material.color = color; // Floating box
        //tvDude.transform.GetChild(0).GetComponent<Light>().color = color; // Light

        Material boxCoverMat = monitorLight.GetComponent<Renderer>().material;
        boxCoverMat.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(50f));

        Material floatingBoxMat = floatingPixel.GetComponent<Renderer>().material;
        floatingBoxMat.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(50f));
    }
}
