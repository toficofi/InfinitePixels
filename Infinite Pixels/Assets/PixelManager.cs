﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour {
    public GameObject pixelCube;
    public float pixelSize;
    public NetworkManagerScript networkManager;

    List<Vector3> positionsBeingChecked = new List<Vector3>();

	// Use this for initialization
	void Start () {
        this.networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();
	}

    // Request a pixel placement from the network with the currently selected colour (doesn't actually put it in the game)
    public void PlacePixel(Vector3 position, Color colour = new Color())
    {
        networkManager.SendPixelPlaceBroadcast(position, this.GetComponent<ColourManager>().selectedColour);
    }

    // Request a pixel removal from the network (doesn't actually destroy the gameobject)
    public void RemovePixel(Vector3 position)
    {
        networkManager.SendPixelRemovalBroadcast(position);
    }

    // Creates and places a pixel gameobject in the game
    public void CreatePixelAtPosition(Vector3 position, Color colour = new Color())
    {
        // To center the pixel
        position += new Vector3(0.5f, 0f, 0.5f);
        GameObject chunk = ChunkManager.GetChunkAtPosition(position);
        GameObject pixelCubeClone = Instantiate<GameObject>(pixelCube, position, pixelCube.transform.localRotation);
        pixelCubeClone.transform.GetChild(0).GetComponent<TextMesh>().text = position.x + ", " + position.y + ", " + position.z;
        pixelCubeClone.transform.SetParent(chunk.transform);
       // pixelCubeClone.GetComponent<Renderer>().material.color = this.GetComponent<ColourManager>().colours[this.GetComponent<ColourManager>().selectedColour];
        pixelCubeClone.GetComponent<Renderer>().material.color = colour;
    }

    public GameObject GetPixelAtPosition(Vector3 position)
    {
        positionsBeingChecked.Remove(position);
        positionsBeingChecked.Add(position);

        Collider[] colliders;
        position.y = 0;
        colliders = Physics.OverlapSphere(position, 0.25f);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.tag == "Pixel") return collider.gameObject;
        }

        return null;
    }

    public void OnDrawGizmos()
    {
        foreach (Vector3 position in positionsBeingChecked)
        {
            Gizmos.DrawSphere(position, 0.25f);
        }
    }

    // Update is called once per frame
    void Update () {
		
	}
}
