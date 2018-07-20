using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkScript : MonoBehaviour {
    public bool loaded = false;
    public bool isWithinViewingArea = true;
    public int secondsSinceLastView = 0;
    public NetworkManagerScript networkManager;

    // Use this for initialization
    void Start () {
       StartCoroutine(CheckIfChunkLoaded());

	}
	
    IEnumerator CheckIfChunkLoaded()
    {
        // If chunk hasn't updated within 2 seconds request reload
        while (this.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(5f);
            if (!this.loaded) networkManager.RequestChunkUpdate(this.transform.position);
            if (isWithinViewingArea) secondsSinceLastView = 0;
            else secondsSinceLastView += 5;
        } 
    }

	// Update is called once per frame
	void Update () {
		
	}
}
