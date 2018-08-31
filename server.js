var net = require('net')
let fs = require('fs')
let publicip = require("public-ip")
let chunks = {}
let viewingDistance = 40
let chunkSize = 16
let port = 80
let worldSize = 5000
let connectToLocalhost = false
console.log("Infinite Pixels server loading...")
if (!fs.existsSync("world")) fs.mkdirSync("world")

//setInterval(chunkUpdateTick, 2000)
setInterval(saveChunks, 10000)
class Chunk {
    constructor(x, y) {
        this.x = x
        this.y = y
        this.dirty = true
        this.pixels = {}
    }
}

class Client {
    constructor(clientid, socket) {
        this.position = {x: 0, y: 0}
        this.velocity = {x: 0, y: 0}
        this.clientid = clientid
        this.socket = socket
        this.loadedChunkPositions = {}
    }
}

let clients = {}

let server = net.createServer((socket) => {
    delete socket._readableState.decoder; // To force stream to read out numbers
    console.log("CONNECTED: " + socket.remoteAddress + ":" + socket.remotePort)

    socket.on("close", () => {
        console.log("DISCONNECTED: " + socket.remoteAddress + ":" + socket.remotePort)
        if (socket.client) {
            //broadcastClientQuit(socket.client.clientid)
            delete clients[socket.client.clientid]
        }
    })
    
    currentPacketIdentifier = null

    socket.on('error', (er) => {
        switch (er.code) {
          // This is the expected case
          case 'ECONNRESET':
          console.log("A client forcifully disconnected!")
            break;
    
          // On Windows, this sometimes manifests as ECONNABORTED
          case 'ECONNABORTED':
            break;
    
          // This test is timing sensitive so an EPIPE is not out of the question.
          // It should be infrequent, given the 50 ms timeout, but not impossible.
          case 'EPIPE':
            break;
    
          default:
            console.log("Got unknown error: ")
            console.log(er)
            break;
            }
        }
    )


    socket.on("data", (data) => readData(data, socket))
})

function readString(data, offset) {
    let stringLength = data.readUInt8(offset)
    let string = data.toString("ascii", offset + 1, offset + 1 + stringLength)
    return string
}

function isWithinWorldBounds(x, y) {
    if (x > worldSize) return false;
    if (x < -worldSize) return false
    if (y > worldSize) return false;
    if (y < -worldSize) return false;
    return true;
}

// Call this function at the start of a packet to pull information from the header
function getPacketInformationFromHeader (data) {
    let currentPacketIdentifier = data.readUInt8()
    let length = 0 // Expected length of the packet not including the identifier

    if (currentPacketIdentifier === null) return null

    switch (currentPacketIdentifier) {
        // Connection request
        case 0:
            stringLength = data.readUInt8(1)
            length = stringLength + 1
            break
        // Position update 
        case 3:
            length = 16
            break
        // Chunk update request
        case 6:
            length = 8
            break
        // Pixel placement packet
        case 7:
            length = 12
            break
        // Pixel removeal packet
        case 8:
            length = 8
            break
        case 9:
            stringLength = data.readUInt8(17)
            length = stringLength + 17
            break
    }

    slicedData = data.slice(1)

    return {ident: currentPacketIdentifier,
            length: length, 
            slicedData: slicedData}
}

function endConnection(socket) {
    socket.destroy()
}

function processPacket(data, socket) {
    let identifier = currentPacketInfo.ident
    if (identifier !== 0 && !socket.client) { 
        console.log("Client tried to send packets before authing")
        return
    }

    switch (identifier) {
        case 0: 
            let clientid = readString(data, 0)

            let shouldAccept = shouldAcceptClient(clientid)

            response = null
            if (shouldAccept) {
                response = new Buffer([0x01])
                
          
                socket.client = new Client(clientid, socket)
                socket.client.name = clientid
                socket.client.colour = {r: 1.0, g: 1.0, b: 1.0}
                socket.client.selectorColour = 1
                clients[clientid] = socket.client
                console.log("Accepted client " + clientid)
            } else {
                console.log("Banned client " + clientid)
                response = new Buffer([0x02])
                response = Buffer.concat([response, new Buffer.from("Banned from server", "ascii")])
            }

            socket.write(response)
            break
        case 3:
            let posx = data.readFloatLE(0)
            let posz = data.readFloatLE(4)
            let velx = data.readFloatLE(8)
            let velz = data.readFloatLE(12)
            socket.client.position.x = posx
            socket.client.position.y = posz
            socket.client.velocity.x = velx
            socket.client.velocity.z = velz

            for (key in clients) {
                clnt = clients[key]
                if (clnt.clientid == socket.client.clientid) continue
            
                let updateOtherPlayersPacket = new Buffer(18)
                updateOtherPlayersPacket.writeInt8(0x04, 0)
                updateOtherPlayersPacket.writeFloatLE(posx, 1)
                updateOtherPlayersPacket.writeFloatLE(posz, 5)
                updateOtherPlayersPacket.writeFloatLE(velx, 9)
                updateOtherPlayersPacket.writeFloatLE(velz, 13)
                updateOtherPlayersPacket.writeUInt8(socket.client.clientid.length, 17)
                updateOtherPlayersPacket = Buffer.concat([updateOtherPlayersPacket, new Buffer.from(socket.client.clientid, "ascii")])
                clnt.socket.write(updateOtherPlayersPacket)
            }
            break
        case 6:
            let chunkx = data.readInt32LE(0)
            let chunkz = data.readInt32LE(4)

            if (!isWithinWorldBounds(chunkx, chunkz)) {
                console.log("Client " + socket.client.clientid + " tried to request chunk outwith world size of " + worldSize + ", chunk " + chunkx + ", " + chunkz)
                break;
            }

            newChunk = getChunkAtPosition({x: chunkx, y: chunkz})
            sendChunkPacket(newChunk, socket.client)
            break
        case 7:
            // Pixel placement packet
            let pixelx = data.readInt32LE(0)
            let pixelz = data.readInt32LE(4)

            if (!isWithinWorldBounds(pixelx, pixelz)) {
                console.log("Client " + socket.client.clientid + " tried to place pixel outwith world size of " + worldSize + ", position " + pixelx + ", " + pixelz)
                break;
            }

            let pixelid = data.readInt32LE(8)
            playerPlacedPixel(pixelx, pixelz, pixelid)
            console.log(socket.client.clientid + " placed pixel at " + pixelx + ", " + pixelz + " in colour " + pixelid)
            break
        case 8:
            // Pixel removal packet
            let rpixelx = data.readInt32LE(0)
            let rpixelz = data.readInt32LE(4)

            if (!isWithinWorldBounds(rpixelx, rpixelz)) {
                console.log("Client " + socket.client.clientid + " tried to place pixel outwith world size of " + worldSize + ", position " + rpixelx + ", " + rpixelz)
                break;
            }
            playerRemovedPixel(rpixelx, rpixelz)
            break
        case 9:
            // Player information packet
            let r = data.readFloatLE(0)
            let g = data.readFloatLE(4)
            let b = data.readFloatLE(8)
            let selectorColour = data.readInt32LE(12)

            let playerName = readString(data, 16)

            if (playerName.length > 20) {
                console.log("Got invalid player name length: " + playerName.length + " from " + socket.client.clientid)
                endConnection(socket)
                return
            }

            if (selectorColour < 0 || selectorColour > 14) {
                console.log("Got invalid selector colour " + selectorColour + " from " + socket.client.clientid)
                endConnection(socket)
                return
            }

            socket.client.name = playerName
            socket.client.colour = {r: r, g: g, b: b}
            socket.client.selectorColour = selectorColour
        
            console.log("Player " + socket.client.clientid + " updated name to " + playerName + ", colour to " + JSON.stringify(socket.client.colour) + " and selector colour to " + selectorColour)

            
            for (key in clients) {
                clnt = clients[key]
                if (clnt.clientid == socket.client.clientid) continue
            
                let updateOtherPlayersPacket = new Buffer(2)
                updateOtherPlayersPacket.writeInt8(0xB, 0)
                updateOtherPlayersPacket.writeUInt8(socket.client.clientid.length, 1)
                updateOtherPlayersPacket = Buffer.concat([updateOtherPlayersPacket, new Buffer.from(socket.client.clientid, "ascii")])

                let integerInfo = new Buffer(17)
                integerInfo.writeFloatLE(r, 0)
                integerInfo.writeFloatLE(g, 4)
                integerInfo.writeFloatLE(b, 8)
                integerInfo.writeInt32LE(selectorColour, 12)
                integerInfo.writeUInt8(playerName.length, 16)
                updateOtherPlayersPacket = Buffer.concat([updateOtherPlayersPacket, integerInfo, new Buffer.from(playerName, "ascii")])
                clnt.socket.write(updateOtherPlayersPacket)
            }
            break
        default:
            if (socket.client) console.log("Invalid packet recv, ident: " + currentPacketIdentifier + " from " + socket.client.clientid)
            else console.log("Invalid packet recv, ident: " + currentPacketIdentifier + " from unauthed player " + socket.remoteAddress)
            endConnection(socket)
            
    }
}

expectsNewPacket = true
packetBuffer = new Buffer(0)
expectedLengthRemaining = 0
currentPacketInfo = null
nextPacketData = null


function readData(data, socket) {
    try {
        while (data.length > 0) {
            // If we expect the next data to be a new packet, read the length and identifier
            if (expectsNewPacket) {
                packetBuffer = new Buffer(0)
                currentPacketInfo = getPacketInformationFromHeader(data)
                if (currentPacketInfo === null) {
                console.log("Got null data from socket " + socket.remoteAddress + " - look into this!")
                endConnection(socket)
                return  
                }// TODO: Figure out why this happens
                data = currentPacketInfo.slicedData
                expectedLengthRemaining = currentPacketInfo.length
                expectsNewPacket = false
            }


            // If the data is more than the rest of the packet, just concat the rest of the packet and remove it from our block
            if (data.length >= expectedLengthRemaining) {
                packetBuffer = Buffer.concat([packetBuffer, data.slice(0, expectedLengthRemaining)]) 
                data = data.slice(expectedLengthRemaining)

                processPacket(packetBuffer, socket)
                expectsNewPacket = true
            } else {
                // Or if the data length is less than what we need, just add all that we can and we'll add more later
                packetBuffer = Buffer.concat([packetBuffer, data.slice(0, data.length)])
                data = data.slice(data.length)
            }
        }
    } catch (e) {
        console.log("While processing packet for " + socket.remoteAddress + " - got this error:")
        console.log(e.stack)
        endConnection(socket)
    }
}

publicip.v4().then(ip => {
    if (connectToLocalhost) ip = "localhost"
    console.log("Starting server with outward-facing IP: " + ip)
    server.listen(80, ip, (err) => {
        console.log("Infinite Pixels server running!")
    })
});

server.on("error", (err) => {
    console.log("SERVER ERROR: " + err)
})

function shouldAcceptClient(clientid) {
    return true;
}




function getChunkAtPosition(position) {
    // Try and get chunk from memory, otherwise load from file
    let chunk = chunks[position.x + "," + position.y]
    if (!chunk) return loadChunkAtPosition(position)
    else return chunk

}



function sendChunkPacketIfDirty(chunk, client) {
    if (chunk.dirty) sendChunkPacket(chunk, client)
    // How do we make the dirty flag specific for one client?
}

function broadcastClientQuit(clientid) {
    for (cid in clients) {
        if (cid == clientid) continue
        let clnt = clients[cid]
        let packet = new Buffer(2)
        packet.writeInt8(0xA, 0)
        packet.writeUInt8(clientid.length, 1)
        packet = Buffer.concat([packet, new Buffer.from(clientid, "ascii")])
        console.log("Broadcasting client quit " + clientid)
        clnt.socket.write(packet)
    }
}

function broadcastChunkPacket(chunk) {
    // TODO: Only update players nearby

    for (clientid in clients) {
        sendChunkPacket(chunk, clients[clientid])
    }
}

function sendChunkPacket(chunk, client) {
    // If chunk has no pixel data just send a blank chunk packet
    if (Object.keys(chunk.pixels).length == 0) {
        let emptyChunkPacket = new Buffer(9)
        emptyChunkPacket.writeInt8(0x05, 0)
        emptyChunkPacket.writeInt32LE(chunk.x, 1)
        emptyChunkPacket.writeInt32LE(chunk.y, 5)
    
        client.socket.write(emptyChunkPacket)
    } else {
        // Otherwise send all the pixels
        let chunkPacket = new Buffer(9 + (chunkSize * chunkSize))
        chunkPacket.writeInt8(0x09, 0)
        chunkPacket.writeInt32LE(chunk.x, 1)
        chunkPacket.writeInt32LE(chunk.y, 5)
        let byteCount = 9

        for (let x = 0; x < chunkSize; x++) {
            for (let y = 0; y < chunkSize; y++) {
                // If the pixel is not found, set the colour to 0, which is blank
                let pixelColor = 0

                let pixelKey = x + "," + y
                if (pixelKey in chunk.pixels) pixelColor = chunk.pixels[pixelKey]

                chunkPacket.writeInt8(pixelColor, byteCount)
                byteCount++
            }
        }

        client.socket.write(chunkPacket)
    }

    // Store already loaded positions as a key as string x,y
    // Floating point ambiguity is not a problem as this will always be a whole number
    client.loadedChunkPositions[chunk.x + "," + chunk.y] = chunk
}


function playerPlacedPixel(x, y, colour) {
    let chunkPosition = getNearestChunkTo({x: x, y: y})
    if (!isWithinWorldBounds(chunkPosition.x, chunkPosition.y)) return
    let chunk = getChunkAtPosition(chunkPosition)

    let relativePixelPosition = getRelativePixelPos({x: x, y: y}, chunkPosition)
    chunk.pixels[relativePixelPosition.x + "," + relativePixelPosition.y] = colour

    broadcastChunkPacket(chunk)
}

function playerRemovedPixel(x, y) {
    let chunkPosition = getNearestChunkTo({x: x, y: y})
    if (!isWithinWorldBounds(chunkPosition.x, chunkPosition.y)) return
    let chunk = getChunkAtPosition(chunkPosition)

    let relativePixelPosition = getRelativePixelPos({x: x, y: y}, chunkPosition)
    delete chunk.pixels[relativePixelPosition.x + "," + relativePixelPosition.y]
    broadcastChunkPacket(chunk)
}

function getRelativePixelPos(pixelPosition, chunkPosition) {
    let x = pixelPosition.x - chunkPosition.x + (chunkSize/2)
    let y = pixelPosition.y - chunkPosition.y + (chunkSize/2)
    return {x: x, y: y}
}

function isChunkWithinViewingArea(client, position) {
    var a = client.position.x - position.x
    var b = client.position.y - position.y

    return Math.sqrt( a*a + b*b ) < viewingDistance
}

// Just snaps a pos to a 16x16 grid
function getNearestChunkTo(position) {
    let x = Math.round(position.x / chunkSize) * chunkSize
    let y = Math.round(position.y / chunkSize) * chunkSize
    return {x: x, y: y}
}


function saveChunks() {
    for (chunkloc in chunks) {
        saveChunk(chunks[chunkloc])
    }
}


function saveChunk(chunk) {
    // Don't save if there are no pixels in the chunk
    if (Object.keys(chunk.pixels) == 0) return

    var wstream = fs.createWriteStream("world/Chunk" + chunk.x + "," + chunk.y);
    var buffer = new Buffer(chunkSize*chunkSize)
    let byteCount = 0

    for (let x = 0; x < chunkSize; x++) {
        for (let y = 0; y < chunkSize; y++) {
            // If the pixel is not found, set the colour to 0, which is blank
            let pixelColor = 0

            let pixelKey = x + "," + y
            if (pixelKey in chunk.pixels) pixelColor = chunk.pixels[pixelKey]

            buffer.writeInt8(pixelColor, byteCount)
            byteCount++
        }
    }
    
    wstream.write(buffer)
    wstream.end();
}

function loadChunkAtPosition(position) {
    // TODO: Load chunk from file here, otherwise return empty chunk
    if (!fs.existsSync("world/Chunk" + position.x + "," + position.y)) {
        let newChunk = new Chunk(position.x, position.y)
        chunks[position.x + "," + position.y] = newChunk
        return newChunk
    }

    
     let chunk = new Chunk(position.x, position.y)
     chunks[position.x + "," + position.y] = chunk

     let buffer = fs.readFileSync("world/Chunk" + position.x + "," + position.y)
     //console.log("Buffer contents: '" + buffer.toString() + "'")

    let byteCount = 0
     for (let x = 0; x < chunkSize; x++) {
         for (let y = 0; y < chunkSize; y++) {
             // If the pixel is not found, set the colour to 0, which is blank
             let pixelKey = x + "," + y
            
             let pixelColor = buffer.readInt8(byteCount)
             if (pixelColor > 0) chunk.pixels[pixelKey] = pixelColor
             byteCount++
         }
     }

     return chunk
}