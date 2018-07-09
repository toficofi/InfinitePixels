using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class ChunkUpdate
{
    public int x;
    public int z;
    public BinaryReader data;

    public ChunkUpdate(int x, int z)
    {
        this.x = x;
        this.z = z;
    }

    public ChunkUpdate(int x, int z, BinaryReader chunkData)
    {
        this.x = x;
        this.z = z;
        this.data = chunkData;
    }
}
public class ChunkManager : MonoBehaviour {
    public Transform cameraPosition;
    public GameObject chunkPlane;
    public GameObject selector;
    public float viewingDistance;
    public float chunkPlaneSize;
    public string worldFolder;

    NetworkManagerScript networkManager;
    List<GameObject> chunksToRemove = new List<GameObject>();
    List<ChunkUpdate> chunksNeedingUpdate = new List<ChunkUpdate>();
    Dictionary<string, GameObject> chunks = new Dictionary<string, GameObject>();

    // Use this for initialization
    void Start () {
        worldFolder = Application.persistentDataPath + "/World";
        networkManager = GameObject.Find("NetworkManager").GetComponent<NetworkManagerScript>();
        selector = GameObject.Find("selector");
        StartCoroutine(UnloadOldChunks());
        // Create initial starting chunk
        //CreateChunk(new Vector3(0, 0, 0));
    }
	
    public void ClearChunks()
    {
        foreach (KeyValuePair<string, GameObject> chunk in chunks) Destroy(chunk.Value);
        chunks.Clear();
    }

    public GameObject CreateChunkUnderPlayer()
    {
        return CreateChunk(SnapPositionToChunk(selector.transform.position));
    }

    public void ClearChunk(GameObject chunk)
    {
        foreach (Transform pixel in chunk.transform) if (pixel.gameObject != null) Destroy(pixel.gameObject);
    }

    string GetChunkId(GameObject chunk)
    {
        return "Chunk" + chunk.transform.position.x + "," + chunk.transform.position.z;
    }
    // Create a chunk and colour it in a random colour and add to the chunks list
    GameObject CreateChunk(Vector3 location, bool needsUpdate = true)
    {
        location.y = 0;
        
        GameObject newChunk = Instantiate<GameObject>(chunkPlane, location, chunkPlane.transform.rotation);
        newChunk.GetComponent<ChunkScript>().networkManager = networkManager;
        chunks.Add(GetChunkId(newChunk), newChunk);
    

        newChunk.name = GetChunkId(newChunk);

        if (needsUpdate) networkManager.RequestChunkUpdate(newChunk.transform.position);
        //newChunk.GetComponent<Renderer>().material.color = UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);

        //LoadChunk(newChunk);
        return newChunk;
    }

    GameObject CreateChunkIfNotOneInPlace(Vector3 location)
    {
        string id = "Chunk" + location.x + "," + location.z;

        if (chunks.ContainsKey(id)) {
            return null;
        } else
        {
            return CreateChunk(location);
        }
    }

    GameObject GetChunkFromExactPosition(Vector3 location)
    {
        string id = "Chunk" + location.x + "," + location.z;

        if (chunks.ContainsKey(id))
        {
            return chunks[id];
        }
        else return null;
    }

    public void LoadBlankChunkFromNetwork(BinaryReader reader)
    {
        int chunkx = reader.ReadInt16();
        int chunky = reader.ReadInt16();

        chunksNeedingUpdate.Add(new ChunkUpdate(chunkx, chunky));

    }

    public void ProcessChunkData(ChunkUpdate update)
    {
        GameObject chunk = GetChunkFromExactPosition(new Vector3(update.x, 0, update.z));


        if (!chunk)
        {
            Debug.Log("Got chunk that isn't in game yet");

            // Don't request another update for this new chunk
            chunk = CreateChunk(new Vector3(update.x, 0, update.z), false);
        }

        ClearChunk(chunk);

        chunk.GetComponent<Renderer>().material.color = new Color(0, 0, 0);//UnityEngine.Random.ColorHSV(0f, 1f, 1f, 1f, 0.5f, 1f);
        chunk.GetComponent<ChunkScript>().loaded = true;

        if (update.data == null) return; // If there is no binary data for this chunk update, it's a blank chunk

        for (int x = 0; x < chunkPlaneSize; x++)
        {
            for (int z = 0; z < chunkPlaneSize; z++)
            {
                Vector3 positionToCheck = new Vector3(x, 0, z);

                byte pixelColourAtPosition = update.data.ReadByte();
                
                if (pixelColourAtPosition > 0) // Not empty
                {
                    positionToCheck = chunkWisePixelPosToWorld(positionToCheck, new Vector3(update.x, 0, update.z));
                    this.GetComponent<PixelManager>().CreatePixelAtPosition(positionToCheck, this.GetComponent<ColourManager>().colours[pixelColourAtPosition]);
                }
            }
        }

    }

    public Vector3 chunkWisePixelPosToWorld(Vector3 pixelPos, Vector3 chunkPos)
    {
        Vector3 result = pixelPos;
        result += chunkPos;
        result -= new Vector3(chunkPlaneSize / 2, 0, chunkPlaneSize / 2);
        return result;
    }

    public void LoadChunkFromNetwork(BinaryReader reader)
    {
        int chunkx = reader.ReadInt16();
        int chunky = reader.ReadInt16();


        chunksNeedingUpdate.Add(new ChunkUpdate(chunkx, chunky, reader));
    }

    public int GetColourFromPixel(GameObject pixel)
    {
        return Array.IndexOf(this.GetComponent<ColourManager>().colours, pixel.GetComponent<Renderer>().material.color);
    }

    bool IsChunkWithinViewingArea(Vector3 chunk)
    {
        return Vector3.Distance(new Vector3(chunk.x, 0, chunk.z), new Vector3(cameraPosition.position.x, 0, cameraPosition.position.z)) < viewingDistance;
    }
    /*
    public void SaveChunk(GameObject chunk)
    {
        if (!Directory.Exists(worldFolder)) Directory.CreateDirectory(worldFolder);
        using (BinaryWriter writer = new BinaryWriter(File.Open(worldFolder + "/" + chunk.name, FileMode.Create)))
        {
            writer.Write(chunk.transform.position.x);
            writer.Write(chunk.transform.position.z);

            float pixelSize = this.GetComponent<PixelManager>().pixelSize;
            Vector3 chunkOffset = new Vector3(chunk.transform.position.x - chunkPlaneSize / 2 + pixelSize / 2, 0,  chunk.transform.position.z - chunkPlaneSize / 2 + pixelSize / 2);

            for (float x = chunkOffset.x; x < chunk.transform.position.x - chunkPlaneSize/2 +  (pixelSize * 16) + pixelSize/2; x += pixelSize)
            {
                for (float z = chunkOffset.z; z < chunk.transform.position.z - chunkPlaneSize/2 +  (pixelSize * 16) + pixelSize/2; z += pixelSize)
                {
                    Vector3 positionToCheck = new Vector3(x, 0, z);

                    GameObject pixelAtPosition = this.GetComponent<PixelManager>().GetPixelAtPosition(positionToCheck);

                    if (pixelAtPosition != null)
                    {
                        int pixelColour = this.GetColourFromPixel(pixelAtPosition);
                        writer.Write(pixelColour + 1);
                    } else
                    {
                        writer.Write(0);
                    }
                }
            }
        }
    }

    public void LoadChunk(GameObject chunk)
    {
        if (File.Exists(worldFolder + "/" + chunk.name))
        {
            using (BinaryReader reader = new BinaryReader(File.Open(worldFolder + "/" + chunk.name, FileMode.Open)))
            {
                float chunkx = reader.ReadSingle();
                float chunky = reader.ReadSingle();

                float pixelSize = this.GetComponent<PixelManager>().pixelSize;
                Vector3 chunkOffset = new Vector3(chunk.transform.position.x - chunkPlaneSize / 2 + pixelSize / 2, 0, chunk.transform.position.z - chunkPlaneSize / 2 + pixelSize / 2);

                for (float x = chunkOffset.x; x < chunk.transform.position.x - chunkPlaneSize / 2 + (pixelSize * 16) + pixelSize / 2; x += pixelSize)
                {
                    for (float z = chunkOffset.z; z < chunk.transform.position.z - chunkPlaneSize / 2 + (pixelSize * 16) + pixelSize / 2; z += pixelSize)
                    {
                        Vector3 positionToCheck = new Vector3(x, 0, z);

                        int pixelColourAtPosition = reader.ReadInt32();

                        if (pixelColourAtPosition > 0) // Not empty
                        {
                            this.GetComponent<PixelManager>().CreatePixelAtPosition(positionToCheck - new Vector3(0.5f, 0, 0.5f), false, this.GetComponent<ColourManager>().colours[pixelColourAtPosition]);
                        }
                    }
                }
            }
        } else
        {
         //   Debug.Log("No data for chunk " + chunk.name);
        }
    }*/

    public static GameObject GetChunkAtPosition(Vector3 position)
    {
        Collider[] colliders;
        colliders = Physics.OverlapSphere(position, 0.25f);
       
        if (colliders.Length > 0) //Presuming the object you are testing also has a collider 0 otherwise
        {
            foreach (var collider in colliders)
            {
                GameObject gm = collider.gameObject;
                if (gm.tag == "Chunk") return gm;
            }
        }

        return null;
    }

    public Vector3 SnapPositionToChunk(Vector3 position)
    {
        float x = Mathf.Round(position.x / chunkPlaneSize) * chunkPlaneSize;
        float z = Mathf.Round(position.z / chunkPlaneSize) * chunkPlaneSize;


        return new Vector3(x, 0, z);
    }


    IEnumerator UnloadOldChunks()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f);

            Dictionary<string, GameObject> chunksAfterRemovals = new Dictionary<string, GameObject>();

            foreach (KeyValuePair<string, GameObject> chunkPair in chunks)
            {
                GameObject chunk = chunkPair.Value;

                if (!chunk.GetComponent<ChunkScript>().isWithinViewingArea)
                {
                    Destroy(chunk);
                }
                else chunksAfterRemovals.Add(chunkPair.Key, chunk);
            }

            chunks = chunksAfterRemovals;
        }
    }

	// Update is called once per frame
	void Update () {
        // if we are not connected, clear chunks and await reconnection

        if (!networkManager.isConnected)
        {
            if (chunks.Count > 0) ClearChunks();
            return;
        }

        GameObject currentChunk = GetChunkAtPosition(selector.transform.position);
        if (currentChunk == null) currentChunk = CreateChunkUnderPlayer();

        float radius = viewingDistance * chunkPlaneSize;
        float startingX = (currentChunk.transform.position.x - radius);
        float startingZ = (currentChunk.transform.position.z - radius);

        foreach (KeyValuePair<string, GameObject> chunkPair in chunks) chunkPair.Value.GetComponent<ChunkScript>().isWithinViewingArea = false;
        
        for (float x = startingX; x < currentChunk.transform.position.x + radius; x += chunkPlaneSize)
        {
            for (float z = startingZ; z < currentChunk.transform.position.z + radius; z += chunkPlaneSize)
            {
                Vector3 loc = new Vector3(x, 0, z);

                GameObject chunkAlreadyAtPosition = GetChunkFromExactPosition(loc);
                if (chunkAlreadyAtPosition != null)
                {
                    // This chunk was recently in the viewing area, so it doesn't get unloaded
                    chunkAlreadyAtPosition.GetComponent<ChunkScript>().isWithinViewingArea = true;
                }
                else
                {
                    CreateChunk(loc);
                }
            }
        }

        if (chunksNeedingUpdate.Count > 0) {
            ChunkUpdate[] arrayCopy = new ChunkUpdate[chunksNeedingUpdate.Count];
            chunksNeedingUpdate.CopyTo(arrayCopy);
            List<ChunkUpdate> chunkNeedingUpdateCopy = new List<ChunkUpdate>(arrayCopy);
            try
            {
                foreach (ChunkUpdate chunkUpdate in chunkNeedingUpdateCopy) ProcessChunkData(chunkUpdate);
            } catch (EndOfStreamException exception)
            {
                // If end of stream was reached, the socket was disconnected
                Debug.Log(exception);
                networkManager.SocketDisconnected();
            }

            chunksNeedingUpdate.Clear();
        }


        /*
        foreach (GameObject chunk in new List<GameObject>(chunks))
        {
            if (IsChunkWithinViewingArea(chunk.transform.position))
            {
                //ChunkScript chunkData = chunk.GetComponent<ChunkScript>();

                Vector3 rightPosition = chunk.transform.position + new Vector3(chunkPlaneSize, 0, 0);
                Vector3 leftPosition = chunk.transform.position + new Vector3(-chunkPlaneSize, 0, 0);
                Vector3 topPosition = chunk.transform.position + new Vector3(0, 0, chunkPlaneSize);
                Vector3 bottomPosition = chunk.transform.position + new Vector3(0, 0, -chunkPlaneSize);

                if (GetChunkAtPosition(rightPosition) == null && IsChunkWithinViewingArea(rightPosition)) CreateChunk(rightPosition);
                if (GetChunkAtPosition(leftPosition) == null && IsChunkWithinViewingArea(leftPosition)) CreateChunk(leftPosition);
                if (GetChunkAtPosition(topPosition) == null && IsChunkWithinViewingArea(topPosition)) CreateChunk(topPosition);
                if (GetChunkAtPosition(bottomPosition) == null && IsChunkWithinViewingArea(bottomPosition)) CreateChunk(bottomPosition);

            } else
            {
                chunksToRemove.Add(chunk);
            }
        }

        foreach (ChunkUpdate chunkUpdate in chunksNeedingUpdate)
        {
            GameObject chunk = GameObject.Find("Chunk" + chunkUpdate.x + "," + chunkUpdate.z);
            if (!chunk)
            {
                Debug.Log("Got chunk that isn't in game yet");
                chunk = CreateChunk(new Vector3(chunkUpdate.x, 0, chunkUpdate.z));
            }

            chunk.GetComponent<Renderer>().material.color = new Color(1, 1, 1);
        }

        chunksNeedingUpdate.Clear();

        foreach (GameObject chunk in chunksToRemove)
        {
            chunks.Remove(chunk);
            Destroy(chunk);
        }

        chunksToRemove.Clear();
        */
    }
}
