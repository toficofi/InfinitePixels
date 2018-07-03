using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorController : MonoBehaviour {
    PixelManager pixelManager;
    Vector3 vectorFromCamera;
    public GameObject selectorCube;
    DragScript dragScript;
    NetworkManagerScript networkManager;
    public bool playerSelector;

    // Drag settings
    public float drag = 0.25f;
    public float maxSpeed = 2f;
    public float networkCorrectionSpeed = 1f;
    public float networkUpdateFrequency = 10f;
    public Vector3 velocity;
    public float sensitivity;
    public float speedToSwitchToGentleMode;
    public float gentleModeSensitivity;
    public GameObject tvDude;

    // Only used in network-controlled mode
    public Vector3 targetPosition;



    // Use this for initialization
    void Start () {
        pixelManager = GameObject.Find("PixelCanvas").GetComponent<PixelManager>();
        dragScript = GameObject.Find("DragReciever").GetComponent<DragScript>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();


        vectorFromCamera =  this.transform.position - Camera.main.transform.position;
	}
	

    public void ChangeSelectorColour(Color color)
    {
        selectorCube.GetComponent<Renderer>().material.color = color;
        selectorCube.transform.GetChild(0).GetComponent<Light>().color = color;
        tvDude.transform.GetChild(2).GetComponent<Renderer>().material.color = color; // Box covering face
        tvDude.transform.GetChild(3).GetComponent<Renderer>().material.color = color; // Floating box
        tvDude.transform.GetChild(0).GetComponent<Light>().color = color; // Light

        Material boxCoverMat = tvDude.transform.GetChild(2).GetComponent<Renderer>().material;
        boxCoverMat.SetColor("_EmissionColor", color * Mathf.LinearToGammaSpace(100f));

        Material floatingBoxMat = tvDude.transform.GetChild(3).GetComponent<Renderer>().material;
        floatingBoxMat.SetColor("_EmissionColor", color);
    }

	// Update is called once per frame
	void Update () {
        // If this is the client player selector, send position update after calculating
        if (playerSelector)
        {
            Vector3 exactPosition = Camera.main.transform.position + vectorFromCamera;

            // Move the selector smoothly if still moving, otherwise snap to the grid
            if (dragScript.IsResting()) this.transform.position = new Vector3(Mathf.Round(exactPosition.x), 0, Mathf.Round(exactPosition.z));
            
            // Debug
            if (Time.frameCount % networkUpdateFrequency == 0) networkManager.SendPositionUpdate(this.transform.position, velocity);
        } else
        {
            velocity *= drag;
            this.targetPosition -= velocity;

            // Position is controlled by network, just snap to grid
            Vector3 exactPosition = this.targetPosition;

            if (this.velocity.magnitude < speedToSwitchToGentleMode) this.targetPosition = new Vector3(Mathf.Round(exactPosition.x), 0, Mathf.Round(exactPosition.z));

            // Lerp between client position and network reported position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition,  networkCorrectionSpeed);
        }
    }

    public void PlacePixel()
    {
        if (velocity.magnitude > 0) return;
        GameObject pixel;
        if ((pixel = this.pixelManager.GetPixelAtPosition(this.transform.position + new Vector3(0.5f, 0, 0.5f))) == null) this.pixelManager.PlacePixel(this.transform.position);
        else this.pixelManager.RemovePixel(this.transform.position);
    }
}
