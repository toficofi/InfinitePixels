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
    public int distanceBeforeTeleporting;
    public float sensitivity;
    public float speedToSwitchToGentleMode;
    public float gentleModeSensitivity;
    public GameObject tvDude;
    public GameObject spawnEffect;
    public GameObject cameras;

    public float secondsSinceLastUpdate = 0;

    private Vector3 lastPosition = new Vector3(0, 0, 0);
    // Only used in network-controlled mode
    public Vector3 targetPosition;
    public float distanceBeforePausingUpdates;

    AudioSource audio;

    public AudioClip pixelPlaceSound;
    public AudioClip pixelRemoveSound;

    public void Awake()
    {
        Application.targetFrameRate = 60;
    }

    // Use this for initialization
    void Start () {
        pixelManager = GameObject.Find("PixelCanvas").GetComponent<PixelManager>();
        dragScript = GameObject.Find("DragReciever").GetComponent<DragScript>();
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();
        audio = this.GetComponent<AudioSource>();
        if (playerSelector) vectorFromCamera =  this.transform.position - cameras.transform.position;
	}

    public void ChangeSelectorColour(Color color)
    {
        selectorCube.GetComponent<Renderer>().material.color = color;
        tvDude.GetComponent<TVDudeScript>().ChangeColour(color);
    }

    public void ChangePosition(Vector3 position)
    {
        if (playerSelector)
        {
            cameras.transform.position = position - vectorFromCamera;
            this.transform.position = position;
        }
    }
    
    // Clamps the given position to the world size. Returns true if a clamp was required and false otherwise.
    bool ClampPositionWithinWorldBounds(ref Vector3 position)
    {
        bool hadToClamp = false;
        if (position.x > networkManager.worldSize) {
            position.x = networkManager.worldSize;
            hadToClamp = true;
        }
        if (position.x < -networkManager.worldSize) {
            position.x = -networkManager.worldSize;
            hadToClamp = true;
        }
        if (position.z > networkManager.worldSize) {
            position.z = networkManager.worldSize;
            hadToClamp = true;
        }
        if (position.z < -networkManager.worldSize) {
            position.z = -networkManager.worldSize;
            hadToClamp = true;
        }

        return hadToClamp;
    }

	// Update is called once per frame
	void Update () {
        // If this is the client player selector, send position update after calculating
        if (playerSelector)
        {
            Vector3 exactPosition = cameras.transform.position + vectorFromCamera;
            if (ClampPositionWithinWorldBounds(ref exactPosition))
            {
                Debug.Log("Clamped position: " + exactPosition);
                // If we had to clamp within the world bounds, force selector to that position and the camera too
                this.transform.position = new Vector3(Mathf.Round(exactPosition.x), 0, Mathf.Round(exactPosition.z));
                cameras.transform.position = exactPosition - vectorFromCamera;
            }


            // Move the selector smoothly if still moving, otherwise snap to the grid
            if (dragScript.IsResting()) this.transform.position = new Vector3(Mathf.Round(exactPosition.x), 0, Mathf.Round(exactPosition.z));

            // Debug
            if (Vector3.Distance(this.transform.position, lastPosition) > distanceBeforePausingUpdates)
            {
                networkManager.SendPositionUpdate(this.transform.position, velocity);
            }

            lastPosition = this.transform.position;
        } else
        {
            velocity *= drag;
            this.targetPosition -= velocity;

            // Position is controlled by network, just snap to grid
            Vector3 exactPosition = this.targetPosition;

            if (this.velocity.magnitude < speedToSwitchToGentleMode) this.targetPosition = new Vector3(Mathf.Round(exactPosition.x), 0, Mathf.Round(exactPosition.z));

            // Lerp between client position and network reported position
            transform.position = Vector3.MoveTowards(transform.position, targetPosition,  networkCorrectionSpeed);
            if (Vector3.Distance(transform.position, targetPosition) > distanceBeforeTeleporting) transform.position = targetPosition;
        }
    }

    public Vector3 GetSnappedPosition()
    {
        return new Vector3(Mathf.Round(this.transform.position.x), 0, Mathf.Round(this.transform.position.z));
    }

    public void PlaySpawnEffect()
    {
        spawnEffect.transform.GetChild(0).GetComponent<ParticleSystem>().Play();
        spawnEffect.transform.GetChild(1).GetComponent<ParticleSystem>().Play();
    }

    public void RandomizePitch()
    {
        audio.pitch = Random.Range(1f, 1.5f);
    }

    public void PlacePixel()
    {
        if (velocity.magnitude > 0) return;
        if (!networkManager.IsPositionWithinWorldBounds(this.transform.position)) return;
        PlaySpawnEffect();
        GameObject pixel = this.pixelManager.GetPixelAtPosition(this.transform.position);
        Debug.Log("Placing pixel at " + this.transform.position);
        if (pixel == null)
        {
            this.pixelManager.PlacePixel(this.transform.position);
            RandomizePitch();
            audio.PlayOneShot(pixelPlaceSound);
        }
        else
        {
            this.pixelManager.RemovePixel(this.transform.position);
            RandomizePitch();
            audio.PlayOneShot(pixelRemoveSound);
        }

    }
}
