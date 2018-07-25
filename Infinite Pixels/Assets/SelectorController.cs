using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectorController : MonoBehaviour {
    PixelManager pixelManager;
    public Vector3 vectorFromCamera;
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
    public GameObject spawnEffect;
    public GameObject cameras;

    // Only used in network-controlled mode
    public Vector3 targetPosition;



    // Use this for initialization
    void Start () {
        pixelManager = GameObject.Find("PixelCanvas").GetComponent<PixelManager>();
        dragScript = GameObject.Find("DragReciever").GetComponent<DragScript>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();
        if (playerSelector) vectorFromCamera =  this.transform.position - cameras.transform.position;
	}

    public void ChangeSelectorColour(Color color)
    {
        selectorCube.GetComponent<Renderer>().material.color = color;
        tvDude.GetComponent<TVDudeScript>().ChangeColour(color);
    }

	// Update is called once per frame
	void Update () {
        // If this is the client player selector, send position update after calculating
        if (playerSelector)
        {
            Vector3 exactPosition = cameras.transform.position + vectorFromCamera;

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

    public void PlaySpawnEffect()
    {
        spawnEffect.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
        spawnEffect.transform.GetChild(1).GetComponent<ParticleSystem>().Play();
    }

    public void PlacePixel()
    {
        if (velocity.magnitude > 0) return;
        PlaySpawnEffect();
        GameObject pixel = this.pixelManager.GetPixelAtPosition(this.transform.position);
        Debug.Log("Placing pixel at " + this.transform.position);
        if (pixel == null) this.pixelManager.PlacePixel(this.transform.position);
        else this.pixelManager.RemovePixel(this.transform.position);
    }
}
