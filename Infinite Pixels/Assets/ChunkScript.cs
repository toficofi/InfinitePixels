using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChunkScript : MonoBehaviour {
    public bool loaded = false;
    public bool isWithinViewingArea = true;
    public NetworkManagerScript networkManager;

    // Use this for initialization
    void Start () {
        StartCoroutine(CheckIfChunkLoaded());
	}
	
    IEnumerator CheckIfChunkLoaded()
    {
        // If chunk hasn't updated within 2 seconds request reload
        while (this.isWithinViewingArea)
        {
            yield return new WaitForSeconds(1f);
            if (!this.loaded) networkManager.RequestChunkUpdate(this.transform.position);
        }
    }

	// Update is called once per frame
	void Update () {
		
	}
}
