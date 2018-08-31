using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

struct OtherPlayerPositionUpdate
{
    public string id;
    public Vector2 position;
    public Vector2 velocity;
}

struct OtherPlayerInfoUpdate
{
    public string id;
    public Color colour;
    public int selectorColour;
    public string name;
}

public class NetworkManagerScript : MonoBehaviour
{
    public Dictionary<string, GameObject> players = new Dictionary<string, GameObject>();
    public int worldSize;
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
    List<OtherPlayerPositionUpdate> positionsToBeUpdatedInNextFrame = new List<OtherPlayerPositionUpdate>();
    List<OtherPlayerInfoUpdate> playerInfoToBeUpdatedInNextFrame = new List<OtherPlayerInfoUpdate>();
    public GameObject otherPlayerPrefab;
    public GameObject tvDudePrefab;
    public TVDudeScript tvDude;
    public bool connectToLocalhost;
    public string currentPlayerName;
    public MenuController menuController;
    public ColourManager colourManager;
    public int secondsBetweenInfoUpdate;
    private List<Vector3> chunksToUpdate = new List<Vector3>();
    private List<string> clientQuits = new List<string>();
    private ChunkManager chunkManager;
    bool blurred = false;
    

    #region private members 	
    private TcpClient socketConnection;
    private Thread clientReceiveThread;
    public float secondsBeforeOtherPlayerTimeout;
    #endregion
    // Use this for initialization 	
    void Start()
    {
        this.chunkManager = GameObject.Find("PixelCanvas").GetComponent<ChunkManager>();
        //uniqueIdentifier = SystemInfo.deviceUniqueIdentifier;

        uniqueIdentifier = "player" + UnityEngine.Random.Range(0, 1000);
        currentPlayerName = uniqueIdentifier;
        tvDude.ChangeName(currentPlayerName);
        shouldDisplayConnectingPanel = true;
        StartCoroutine(CheckIfSocketAlive());
        StartCoroutine(SendPlayerInfoUpdate());
        StartCoroutine(CheckIfOtherPlayersHaveTimedOut());
        UpdateConnectingStatus("CONNECTING");
        ConnectToTcpServer();
    }

    public bool IsPositionWithinWorldBounds(Vector3 position)
    {
        if (position.x > worldSize) return false;
        if (position.x < -worldSize) return false;
        if (position.z > worldSize) return false;
        if (position.z < -worldSize) return false;

        return true;
    }

    void UpdateConnectingStatus(string status)
    {
        connectingPanelTextMessage = status;
    }

    public IEnumerator SendPlayerInfoUpdate()
    {
        while (this.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(secondsBetweenInfoUpdate);
            if (!isConnected) continue;
            Debug.Log("Sending player info update");
            string name = currentPlayerName;
            Color playerColor = tvDude.colour;
            int selectorColor = colourManager.selectedColour;

            if (!isConnected) continue;
            BinaryWriter packet = GetPreparedPacket();

            if (packet == null)
            {
                Debug.Log("Couldn't send Player Info Update packet");
                continue;
            }

            byte ident = 0x09;

            packet.Write(ident);
            packet.Write(playerColor.r);
            packet.Write(playerColor.g);
            packet.Write(playerColor.b);
            packet.Write(selectorColor);
            packet.Write((byte)name.Length);
            packet.Write(Encoding.ASCII.GetBytes(name));


            currentMemoryStream.WriteTo(currentStream);
            currentMemoryStream.Flush();
        }
    }

    public IEnumerator CheckIfOtherPlayersHaveTimedOut()
    {
        while (this.isActiveAndEnabled)
        {
            yield return new WaitForSeconds(secondsBetweenInfoUpdate);

            List<string> otherIdsToRemove = new List<string>();
            foreach (KeyValuePair<string, GameObject> player in players)
            {
                SelectorController controller = player.Value.GetComponent<SelectorController>();
                controller.secondsSinceLastUpdate += secondsBetweenInfoUpdate;
                if (controller.secondsSinceLastUpdate >= secondsBeforeOtherPlayerTimeout) otherIdsToRemove.Add(player.Key);
            }

            foreach (string id in otherIdsToRemove)
            {
                GameObject otherPlayer = players[id];
                Destroy(otherPlayer.GetComponent<SelectorController>().tvDude);
                Destroy(otherPlayer);
                players.Remove(id);
            }
        }
    }


    // Update is called once per frame
    void Update()
    {
        connectingPanelText.GetComponent<Text>().text = connectingPanelTextMessage;
        connectingPanel.SetActive(shouldDisplayConnectingPanel);

        // This is to avoid the DoF option being toggled repeatedly every update
        if (shouldDisplayConnectingPanel)
        {
            if (!blurred)
            {
                menuController.BlurBackground();
                blurred = true;
            }
        }
        else
        {
            if (blurred)
            {
                menuController.UnBlurBackground();
                blurred = false;
            }
        }



        if (this.isConnected)
        {
            if (chunksToUpdate.Count > 0)
            {
                Vector3 chunkToRequestUpdate = chunksToUpdate[0];
                chunksToUpdate.RemoveAt(0);

                SendChunkUpdateRequest(chunkToRequestUpdate);
            }
           

            foreach (OtherPlayerPositionUpdate update in positionsToBeUpdatedInNextFrame)
            {
                ProcessUpdatePosition(update.id, update.position, update.velocity);
            }

            foreach (OtherPlayerInfoUpdate update in playerInfoToBeUpdatedInNextFrame)
            {
                ProcessInfoUpdate(update.id, update.colour, update.selectorColour, update.name);
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
            playerInfoToBeUpdatedInNextFrame.Clear();
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
        //currentMemoryStream.Flush();
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
        //currentMemoryStream.Flush();
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
                chunkManager.ReloadAllChunks(); // If this was a reconnect, reload every existing chunk
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
        if (socketConnection != null) socketConnection.Close();
        shouldDisplayConnectingPanel = true;
        UpdateConnectingStatus("RECONNECTING");
        this.isConnected = false;
        waitingForReconnect = true;
        tryingForReconnect = false;
        if (clientReceiveThread != null) clientReceiveThread.Abort();
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
                Debug.Log("Got chunk packet");
                // Copy the chunk data into memory to be read on the main thread
                BinaryReader chunkDataCopy = CopyBytesFromNetworkToMemory(reader, ((int)chunkManager.chunkPlaneSize * (int)chunkManager.chunkPlaneSize) + 8);
                chunkManager.LoadChunkFromNetwork(chunkDataCopy);
                break;
            case 0xA:
                // Another player has quit
                string clientQuitId = ReadASCII(reader);
                ClientHasQuit(clientQuitId);
                break;
            case 0xB:
                // Another player updated name
                string playerId = ReadASCII(reader);
                float r = reader.ReadSingle();
                float g = reader.ReadSingle();
                float b = reader.ReadSingle();
                int selectorColour = reader.ReadInt32();
                string playerName = ReadASCII(reader);
                GotOtherPlayerInfoPacket(playerId, new Color(r, g, b), selectorColour, playerName);
                break;
            default:
                Debug.Log("Invalid packet - " + identifier);
                break;
        }
    }

    void GotOtherPlayerInfoPacket(string playerId, Color colour, int selectorColour, string playerName)
    {
        Debug.Log("Player ID " + playerId + " updated their name to " + playerName + ", their colour to " + colour + ", and selector colour to " + selectorColour);
        OtherPlayerInfoUpdate update = new OtherPlayerInfoUpdate();
        update.id = playerId;
        update.name = playerName;
        update.colour = colour;
        update.selectorColour = selectorColour;

        playerInfoToBeUpdatedInNextFrame.Add(update);

    }

    public void ClientHasQuit(string clientQuitId)
    {
        clientQuits.Add(clientQuitId);
    }

    // Process a player update packet
    public void ProcessUpdatePosition(string otherid, Vector2 position, Vector2 velocity)
    {
        GameObject otherPlayer;

        if (!players.ContainsKey(otherid))
        {
            Debug.Log("New player nearby!");
            otherPlayer = Instantiate<GameObject>(otherPlayerPrefab);
            otherPlayer.name = otherid;

            players.Add(otherid, otherPlayer);

            GameObject otherTvDude = Instantiate<GameObject>(tvDudePrefab);
            otherPlayer.GetComponent<SelectorController>().tvDude = otherTvDude;
            otherTvDude.transform.position = new Vector3(position.x, 0, position.y);
            otherTvDude.GetComponent<TVDudeScript>().target = otherPlayer.transform;

        } else
        {
            otherPlayer = players[otherid];
        }

        SelectorController controller = otherPlayer.GetComponent<SelectorController>();

        controller.playerSelector = false;
        controller.secondsSinceLastUpdate = 0;

        controller.velocity = new Vector3(velocity.x, 0, velocity.y);
        controller.targetPosition = new Vector3(position.x, 0, position.y);
    }

    public void ProcessInfoUpdate(string otherid, Color colour, int selectorColor, string name)
    {
        GameObject otherPlayer = GameObject.Find(otherid);
        if (otherPlayer == null)
        {
            Debug.Log("Got info update for player " + otherid + " but not got position yet, discarding");
            return;
        }

        SelectorController selector = otherPlayer.GetComponent<SelectorController>();
        TVDudeScript otherTVDude = selector.tvDude.GetComponent<TVDudeScript>();


        selector.ChangeSelectorColour(colourManager.colours[selectorColor]);
        otherTVDude.ChangeTVDudeColour(colour);
        otherTVDude.ChangeName(name);
    }

    public void UpdatePositionForOtherPlayer(string otherid, Vector2 position, Vector2 velocity)
    {
        OtherPlayerPositionUpdate upd = new OtherPlayerPositionUpdate();
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
        packet.Write((byte)identifier.Length);
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