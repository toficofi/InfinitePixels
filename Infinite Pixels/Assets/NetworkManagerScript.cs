using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

struct PositionUpdate
{
    public string id;
    public Vector2 position;
    public Vector2 velocity;
}

public class NetworkManagerScript : MonoBehaviour
{
    public string host;
    public short port;
    bool shouldRun = true;
    public GameObject connectingPanel;
    public GameObject connectingPanelText;
    bool shouldDisplayConnectingPanel = false;
    string connectingPanelTextMessage;
    string uniqueIdentifier;
    public NetworkStream currentStream = null;
    public BinaryWriter currentBinaryWriter = null;
    public MemoryStream currentMemoryStream = null;
    public bool isConnected = false;
    bool requiresConnectionRequest = true;
    bool waitingForReconnect = false;
    bool tryingForReconnect = false;
    List<PositionUpdate> positionsToBeUpdatedInNextFrame = new List<PositionUpdate>();
    public GameObject otherPlayerPrefab;
    public GameObject tvDudePrefab;
    public bool connectToLocalhost;


    private List<Vector3> chunksToUpdate = new List<Vector3>();
    private List<string> clientQuits = new List<string>();
    private ChunkManager chunkManager;

    

    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    #endregion
    // Use this for initialization 	
    void Start()
    {
        this.chunkManager = GameObject.Find("PixelCanvas").GetComponent<ChunkManager>();
        //uniqueIdentifier = SystemInfo.deviceUniqueIdentifier;

        uniqueIdentifier = "player" + UnityEngine.Random.Range(0, 1000);
        shouldDisplayConnectingPanel = true;
        StartCoroutine(CheckIfSocketAlive());
        UpdateConnectingStatus("Connecting...");
        ConnectToTcpServer();
    }

    void UpdateConnectingStatus(string status)
    {
        connectingPanelTextMessage = status;
    }


    // Update is called once per frame
    void Update()
    {
        connectingPanelText.GetComponent<Text>().text = connectingPanelTextMessage;
        connectingPanel.SetActive(shouldDisplayConnectingPanel);

        if (this.isConnected)
        {
            if (chunksToUpdate.Count > 0)
            {
                Vector3 chunkToRequestUpdate = chunksToUpdate[0];
                chunksToUpdate.RemoveAt(0);

                SendChunkUpdateRequest(chunkToRequestUpdate);
            }
           

            foreach (PositionUpdate update in positionsToBeUpdatedInNextFrame)
            {
                ProcessUpdatePosition(update.id, update.position, update.velocity);
            }

            foreach (string clientQuitId in clientQuits)
            {
                Debug.Log("Client " + clientQuitId + " has quit");
                GameObject otherPlayer = GameObject.Find(clientQuitId);

                if (otherPlayer == null) return;

                Destroy(otherPlayer.GetComponent<SelectorController>().tvDude);
                Destroy(otherPlayer);
            }

            clientQuits.Clear();
            positionsToBeUpdatedInNextFrame.Clear();
        }

        if (requiresConnectionRequest) SendConnectionRequest(uniqueIdentifier);

        if (this.waitingForReconnect)
        {
            ConnectToTcpServer();
        }
    }

    // Requests a chunk update from the server
    public void RequestChunkUpdate(Vector3 position)
    {
        chunksToUpdate.Add(position);
    }

    public void SendChunkUpdateRequest(Vector3 position)
    {
        if (!isConnected) return;
        BinaryWriter packet = GetPreparedPacket();

        if (packet == null)
        {
            Debug.Log("Couldn't send Chunk Update Request packet");
            return;
        }

        byte ident = 0x06;

        packet.Write(ident);
        packet.Write((int)position.x);
        packet.Write((int)position.z);

        currentMemoryStream.WriteTo(currentStream);
        currentMemoryStream.Flush();
    }

    public void SendPositionUpdate(Vector3 position, Vector3 velocity)
    {
        if (!isConnected) return;
        BinaryWriter packet = GetPreparedPacket();

        if (packet == null)
        {
            Debug.Log("Couldn't send Position Update packet");
            return;
        }

        byte ident = 0x03;

        packet.Write(ident);
        packet.Write(position.x);
        packet.Write(position.z);
        packet.Write(velocity.x);
        packet.Write(velocity.z);


        currentMemoryStream.WriteTo(currentStream);
        currentMemoryStream.Flush();
    }

    /// <summary> 	
    /// Setup socket connection. 	
    /// </summary> 	
    private void ConnectToTcpServer()
    {
        if (tryingForReconnect) return;

        Debug.Log("Connecting TCP...");

        tryingForReconnect = true;
        {
            try
            {
                clientReceiveThread = new Thread(new ThreadStart(ListenForData));
                clientReceiveThread.IsBackground = true;
                clientReceiveThread.Start();
            }
            catch (Exception e)
            {
                Debug.Log("On client connect exception " + e);
            }
        }
    }

    public IEnumerator CheckIfSocketAlive()
    {
        while (true)
        {
            yield return new WaitForSeconds(1f);
            if (!isConnected) continue;
            if (!currentStream.CanWrite)
            {
                SocketDisconnected();
            }
        }
    }

    public void SocketDisconnected() {
        socketConnection.Close();
        shouldDisplayConnectingPanel = true;
        UpdateConnectingStatus("Reconnecting...");
        this.isConnected = false;
        waitingForReconnect = true;
        tryingForReconnect = false;
        clientReceiveThread.Abort();
    }


    private void ListenForData()
    {
        try
        {
            Debug.Log("Making connection to " + host + ":" + port);
            if (connectToLocalhost) host = "localhost";
            socketConnection = new TcpClient(host, port);
            currentStream = socketConnection.GetStream();
            //socketConnection.Client.SetKeepAlive(1000, 2);
            requiresConnectionRequest = true;
            while (shouldRun)
            {
                // Current issue - when packets are going fast, more than one packet is being read into the byte buffer.
                // Possible solutions: keep track of how much is being read (with the position of a memorystream?) and restart a memorystream for each
                // packet by starting the memstream again from the offet equal to the prev memstream size (last packet size)
                // option 2: just read directly from the networkstream and loop when a packet arrives. set a flag for next packet and wait until readbytes > 0s
                BinaryReader reader = new BinaryReader(currentStream);
                byte identifier = Convert.ToByte(reader.ReadByte());
                ProcessPacket(identifier, reader);
                    /*
                    int length;
                    // Read incomming stream into byte arrary. 					
                    while ((length = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        string bytesString = "[";
                        foreach (byte byt in bytes)
                        {
                            bytesString += byt.ToString() + ", ";
                        }

                        Debug.Log(bytesString + "]");

                        Debug.Log("Read " + length + "bytes");
                        MemoryStream memStream = new MemoryStream();
                        memStream.Write(bytes, 0, bytes.Length);
                        memStream.Seek(0, SeekOrigin.Begin);
                        BinaryReader reader = new BinaryReader(memStream);
                        byte identifier = Convert.ToByte(reader.ReadByte());
                        Debug.Log("Recieved packet with identifier of " + identifier.ToString("X2"));
                        ProcessPacket(identifier, reader, length);
                    }*/
            }
        }
        catch (Exception socketException)
        {
            Debug.Log("Socket exception: " + socketException);
            SocketDisconnected();
        }
    }

    // Sends a packet saying that a pixel has been placed by this player, with the absolute position of the pixel coords
    public void SendPixelPlaceBroadcast(Vector3 position, int colourIndex)
    { 
        if (!isConnected) return;


        BinaryWriter packet = GetPreparedPacket();

        if (packet == null)
        {
            Debug.Log("Couldn't send Broadcast Pixel packet");
            return;
        }

        byte ident = 0x07;

        packet.Write(ident);
        packet.Write(Mathf.RoundToInt(position.x));
        packet.Write(Mathf.RoundToInt((int)position.z));
        packet.Write(colourIndex);


        currentMemoryStream.WriteTo(currentStream);
        currentMemoryStream.Flush();
    }

    // Broadcast the player removal of a pixel, with pixel positions as absolute
    public void SendPixelRemovalBroadcast(Vector3 position)
    {
        if (!isConnected) return;
        BinaryWriter packet = GetPreparedPacket();

        if (packet == null)
        {
            Debug.Log("Couldn't send Pixel Removal packet");
            return;
        }

        byte ident = 0x08;

        packet.Write(ident);
        packet.Write((int)position.x);
        packet.Write((int)position.z);


        currentMemoryStream.WriteTo(currentStream);
        currentMemoryStream.Flush();
    }

    public static string ReadASCIIFromReader(BinaryReader reader, int index, int bytesToRead)
    {
        byte[] bytes = new byte[bytesToRead + index];
        int count = reader.Read(bytes, index, bytesToRead);
        return Encoding.ASCII.GetString(bytes, 0, bytesToRead).Trim();
    }

    public static string ReadASCII(BinaryReader reader)
    {
        byte length = reader.ReadByte();
        byte[] characters = reader.ReadBytes(length);
        return Encoding.ASCII.GetString(characters);
    }

    // Process basic stuff or switch to packetprocessor
    void ProcessPacket(byte identifier, BinaryReader reader)
    {
        switch (identifier)
        {
            case 0x01: // Connection accepted
                Debug.Log("Connection accepted by server");
                shouldDisplayConnectingPanel = false;
                isConnected = true;
                this.waitingForReconnect = false;
                tryingForReconnect = false;
                break;
            case 0x02: // Connection rejected
                string message = ReadASCII(reader);//ReadASCIIFromReader(reader, 1, packetLength - (int)currentMemoryStream.Position);
                Debug.Log("Connection refused by server with message: " + message);
                UpdateConnectingStatus(message);
                break;
            case 0x04: // Position update for other players
                float posx = reader.ReadSingle();
                float posz = reader.ReadSingle();
                float velx = reader.ReadSingle();
                float velz = reader.ReadSingle();

                string otherid = ReadASCII(reader);//adASCIIFromReader(reader, 0, packetLength - (int)currentMemoryStream.Position);
                UpdatePositionForOtherPlayer(otherid, new Vector2(posx, posz), new Vector2(velx, velz));
                 
                break;
            case 0x05: // Blank chunk
                chunkManager.LoadBlankChunkFromNetwork(reader);
                break;
            case 0x09:
                // Copy the chunk data into memory to be read on the main thread
                BinaryReader chunkDataCopy = CopyBytesFromNetworkToMemory(reader, ((int)chunkManager.chunkPlaneSize * (int)chunkManager.chunkPlaneSize) + 4);
                chunkManager.LoadChunkFromNetwork(chunkDataCopy);
                break;
            case 0x10:
                // Another player has quit
                string clientQuitId = ReadASCII(reader);
                ClientHasQuit(clientQuitId);
                break;
            default:
                Debug.Log("Invalid packet - " + identifier);
                break;
        }
    }

    public void ClientHasQuit(string clientQuitId)
    {
        clientQuits.Add(clientQuitId);
    }

    public void ProcessUpdatePosition(string otherid, Vector2 position, Vector2 velocity)
    {
        GameObject otherPlayer = GameObject.Find(otherid);
        if (otherPlayer == null)
        {
            Debug.Log("New player nearby!");
            otherPlayer = Instantiate<GameObject>(otherPlayerPrefab);
            otherPlayer.name = otherid;

            GameObject otherTvDude = Instantiate<GameObject>(tvDudePrefab);
            otherPlayer.GetComponent<SelectorController>().tvDude = otherTvDude;
            otherTvDude.transform.position = new Vector3(position.x, 0, position.y);
            otherTvDude.GetComponent<TVDudeScript>().target = otherPlayer.transform;

        }

        SelectorController controller = otherPlayer.GetComponent<SelectorController>();

        controller.playerSelector = false;

        controller.velocity = new Vector3(velocity.x, 0, velocity.y);
        controller.targetPosition = new Vector3(position.x, 0, position.y);
    }


    public void UpdatePositionForOtherPlayer(string otherid, Vector2 position, Vector2 velocity)
    {
        PositionUpdate upd = new PositionUpdate();
        upd.id = otherid;
        upd.position = position;
        upd.velocity = velocity;
        positionsToBeUpdatedInNextFrame.Add(upd);
    }

    /// <summary> 	
    /// Send message to server using socket connection. 	
    /// </summary> 	
    /// 

    private void OnDestroy()
    {
        Debug.Log("Destroying");
        shouldRun = false;
        if (socketConnection != null && socketConnection.GetStream() != null) socketConnection.GetStream().Close();
    }

    void SendConnectionRequest(string identifier)
    {
        Debug.Log("Making connection request...");
        BinaryWriter packet = GetPreparedPacket();

        if (packet == null)
        {
            Debug.Log("Couldn't send Connection Request packet");
            return;
        }

        byte ident = 0x00;
        packet.Write(ident);
        packet.Write(Encoding.ASCII.GetBytes(identifier));
        currentMemoryStream.WriteTo(currentStream);
        currentMemoryStream.Flush();

        requiresConnectionRequest = false;
    }

    public BinaryReader CopyBytesFromNetworkToMemory(BinaryReader reader, int bytes)
    {
        MemoryStream memoryCopy = new MemoryStream();
        byte[] bytesCopy = reader.ReadBytes(bytes);
        memoryCopy.Write(bytesCopy, 0, bytes);
        memoryCopy.Seek(0, SeekOrigin.Begin);

        return new BinaryReader(memoryCopy);
    }

    BinaryWriter GetPreparedPacket()
    {
        if (currentBinaryWriter == null)
        {
            if (socketConnection == null) return null;
            
            currentMemoryStream = new MemoryStream();
            currentBinaryWriter = new BinaryWriter(currentMemoryStream, Encoding.ASCII);
        }

        if (currentStream != null && currentStream.CanWrite)
        {
            currentMemoryStream.SetLength(0);
            return currentBinaryWriter;
        }
        else return null;
    }
}