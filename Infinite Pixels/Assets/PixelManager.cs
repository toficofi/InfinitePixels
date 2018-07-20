using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PixelManager : MonoBehaviour {
    public GameObject pixelCube;
    public float pixelSize;
    public NetworkManagerScript networkManager;
    ColourManager colourManager;
    ChunkManager chunkManager;
    List<Vector3> positionsBeingChecked = new List<Vector3>();

	// Use this for initialization
	void Start () {
        this.networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();
        this.colourManager = this.GetComponent<ColourManager>();
        this.chunkManager = this.GetComponent<ChunkManager>();
    }

    // Request a pixel placement from the network with the currently selected colour (doesn't actually put it in the game)
    public void PlacePixel(Vector3 position)
    {
        CreatePixelAtPosition(position, colourManager.ColorToMaterial(colourManager.selectedColour));
        networkManager.SendPixelPlaceBroadcast(position, colourManager.selectedColour);
        GameObject chunk = chunkManager.GetChunkAtPosition(position);

        // Trigger waiting for a reload of the chunk
        chunk.GetComponent<ChunkScript>().loaded = false;
    }

    // Request a pixel removal from the network (doesn't actually destroy the gameobject)
    public void RemovePixel(Vector3 position)
    {
        networkManager.SendPixelRemovalBroadcast(position);
    }

    // Creates and places a pixel gameobject in the game
    public void CreatePixelAtPosition(Vector3 position, Material colour)
    {
        // To center the pixel
        position += new Vector3(0.5f, 0f, 0.5f);
        GameObject chunk = chunkManager.GetChunkAtPosition(position);
        GameObject pixelCubeClone = Instantiate<GameObject>(pixelCube, position, pixelCube.transform.localRotation);
        pixelCubeClone.transform.GetChild(0).GetComponent<TextMesh>().text = position.x + ", " + position.y + ", " + position.z;
        pixelCubeClone.transform.SetParent(chunk.transform);
       // pixelCubeClone.GetComponent<Renderer>().material.color = this.GetComponent<ColourManager>().colours[this.GetComponent<ColourManager>().selectedColour];
        pixelCubeClone.GetComponent<Renderer>().material = colour;
        Vector3 untransformedPos = position - new Vector3(0.5f, 0f, 0.5f);
        pixelCubeClone.name = "Pixel" + untransformedPos.x + "," + untransformedPos.z;
    }

    public GameObject GetPixelAtPosition(Vector3 position)
    {
        return GameObject.Find("Pixel" + (int)position.x + "," + (int)position.z);

        /*
        positionsBeingChecked.Remove(position);
        positionsBeingChecked.Add(position);

        Collider[] colliders;
        position.y = 0;
        colliders = Physics.OverlapSphere(position, 0.25f);

        foreach (Collider collider in colliders)
        {
            if (collider.gameObject.tag == "Pixel") return collider.gameObject;
        }

        return null;*/
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
