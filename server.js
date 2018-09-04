var net = require('net')
let fs = require('fs')
let publicip = require("public-ip")
var colors = require('colors');
let chunks = {}
let viewingDistance = 40
let chunkSize = 16
let port = 80
let worldSize = 5000
let connectToLocalhost = false

// LOGGING SETUP
var winston = require('winston');

//
// Requiring `winston-papertrail` will expose
// `winston.transports.Papertrail`
//
require('winston-papertrail').Papertrail;

var winstonPapertrail = new winston.transports.Papertrail({
  host: 'logs7.papertrailapp.com',
  port: 36057,
  colorization: "all",
  level: "info",
  colorize: true,
})

winstonPapertrail.on('error', function(err) {
    console.log("Logging error:")
    console.error(err)
});

var logger = new winston.Logger({
  level: 'debug',
  transports: [winstonPapertrail, new winston.transports.Console(), new winston.transports.File({ filename: 'combined.log' })]
});

logger.info('Loading Infinite Pixels server');

if (!fs.existsSync("world")) {
    logger.warn('No world folder detected, so making a new one')
    fs.mkdirSync("world")
}

//setInterval(chunkUpdateTick, 2000)
setInterval(saveChunks, 10000)
class Chunk {
    constructor(x, y) {
        this.x = x
        this.y = y
        this.pixels = {}
    }
}

class Client {
    constructor(clientid, socket) {
        this.position = {x: 0, y: 0}
        this.velocity = {x: 0, y: 0}
        this.clientid = clientid
        this.socket = socket
    }
}

let clients = {}

let server = net.createServer((socket) => {
    delete socket._readableState.decoder; // To force stream to read out numbers
    logger.info("Accepted".green + " connection from %s:%s, there are now %s connected", socket.remoteAddress.bold, socket.remotePort, (Object.keys(clients).length + 1).toString().bold)

    socket.on("close", () => {
        logger.info("Lost".red + " connection from %s:%s, there are now %s connected", socket.remoteAddress.bold, socket.remotePort, (Object.keys(clients).length - 1).toString().bold)
        if (socket.client) {
            //broadcastClientQuit(socket.client.clientid)
            delete clients[socket.client.clientid]
        } else {
            logger.warn("Connection %s:%s disconnected without authenticating", socket.remoteAddress.bold, socket.remotePort)
        }
    })
    
    currentPacketIdentifier = null

    socket.on('error', (er) => {
        switch (er.code) {
          // This is the expected case
          case 'ECONNRESET':
            if (socket.client) logger.debug("ECONNRESET".bold + " from player %s", socket.client.clientid)
            else logger.debug("ECONNRESET".bold + " from %s:%s", socket.remoteAddress.bold, socket.remotePort)
            break;
    
          // On Windows, this sometimes manifests as ECONNABORTED
          case 'ECONNABORTED':
            if (socket.client) logger.debug("ECONNABORTED".bold + " from player %s", socket.client.clientid)
            else logger.debug("ECONNABORTED".bold + " from %s:%s", socket.remoteAddress.bold, socket.remotePort)
            break;
    
          // This test is timing sensitive so an EPIPE is not out of the question.
          // It should be infrequent, given the 50 ms timeout, but not impossible.
          case 'EPIPE':
            if (socket.client) logger.warn("EPIPE".bold + " from player %s", socket.client.clientid)
            else logger.warn("EPIPE".bold + " from %s:%s", socket.remoteAddress.bold, socket.remotePort)
    
          default:
            if (socket.client) logger.warn("Unknown socket error %j".bold + " from player %s", er, socket.client.clientid)
            else logger.warn("Unknown socket error %j".bold + "from %s:%s", er, socket.remoteAddress.bold, socket.remotePort)
            break;
            }
        }
    )


    socket.on("data", (data) => readData(data, socket))
})

function readString(data, offset) {
    let stringLength = data.readUInt8(offset)
    let string = data.toString("ascii", offset + 1, offset + 1 + stringLength)
    logger.debug("Read string %s of length %d with offset %d", string.bold, stringLength, offset)
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

    if (currentPacketIdentifier === null) {
        // Why is the first byte sometimes null?
        return null
    }

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
    logger.silly("Looked up packet identifier %d and got a length of %d bytes", currentPacketIdentifier, length)
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
        logger.warn("Unauthenticated client %s:%s sent a non-0 packet with ident %d, " + "dropping".red.bold, socket.remoteAddress.bold, socket.remotePort, ident)
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
                logger.info("Authenticated".green + " %s:%s as %s", socket.remoteAddress.bold, socket.remotePort, clientid)
            } else {
                logger.info("Banned".red + " %s:%s as %s", socket.remoteAddress.bold, socket.remotePort, clientid)
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

            logger.debug(socket.client.clientid.bold + " updated position to p[%d, %d], v[%d, %d]", posx, posz, velx, velz)

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
                logger.warn(socket.client.clientid.bold + " tried to request chunk outwith world size of to %d, [%d, %d]", worldSize, chunkx, chunkz)
                break;
            }
           
            newChunk = getChunkAtPosition({x: chunkx, y: chunkz})

            logger.debug(socket.client.clientid.bold + " requested chunk [%d, %d]", chunkx, chunkz)
            sendChunkPacket(newChunk, socket.client)
            break
        case 7:
            // Pixel placement packet
            let pixelx = data.readInt32LE(0)
            let pixelz = data.readInt32LE(4)

            if (!isWithinWorldBounds(pixelx, pixelz)) {
                logger.warn(socket.client.clientid.bold + " tried to place pixel outwith world size of of to %d, [%d, %d]", worldSize, pixelx, pixelz)
                break;
            }

            let pixelid = data.readInt32LE(8)
            logger.verbose(socket.client.clientid.bold + " placed pixel at [%d, %d] in colour %d", pixelx, pixelz, pixelid)
            playerPlacedPixel(pixelx, pixelz, pixelid)
            break
        case 8:
            // Pixel removal packet
            let rpixelx = data.readInt32LE(0)
            let rpixelz = data.readInt32LE(4)

            if (!isWithinWorldBounds(rpixelx, rpixelz)) {
                logger.warn(socket.client.clientid.bold + " tried to remove pixel outwith world size of of to %d, [%d, %d]", worldSize, pixelx, pixelz)
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
            
            logger.verbose(socket.client.clientid.bold + " updated name to %s, colour to {%d,%d,%d} and selector colour to %d", playerName, r, g, b, selectorColour)

            
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
            logger.warn("Invalid packet identifier %s from %s - ending connection", identifier.toString().bold, socket.client.clientid.bold)
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
                if (currentPacketInfo == null) {
                    if (socket.client) logger.warn("Tried to read packet identifier from %s but got " + "null".red + "! Disconnecting...", socket.client.clientid)
                    else logger.warn("Tried to read packet identifier from " + "unauthed".bold + " %s:%s but got " + "null".red + "! Disconnecting...", socket.remoteAddress.bold, socket.remotePort)

                    endConnection(socket)
                    return  
                }

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
        if (socket.client) logger.warn("Error".red.bold + " while processing packet for %s. Current packet info: %j", socket.client.clientid.bold, currentPacketInfo)
        else logger.warn("Error".red.bold + " while processing packet for unauthed %s:%s. Current packet info: %j", socket.remoteAddress.bold, socket.remotePort, currentPacketInfo)
        logger.error(e)
        endConnection(socket)
    }
}

publicip.v4().then(ip => {
    if (connectToLocalhost) ip = "localhost"

    logger.info("Starting server with outwards-facing IP %s", ip.bold.green)
    server.listen(80, ip, (err) => {
        logger.info("READY".green + " - accepting connections")
    })
});

server.on("error", (err) => {
    logger.error("Uncaught server error: %j", err)
})

function shouldAcceptClient(clientid) {
    return true;
}


function getChunkAtPosition(position) {
    // Try and get chunk from memory, otherwise load from file
    let chunk = chunks[position.x + "," + position.y]
    if (!chunk) {
        return loadChunkAtPosition(position)
    } else {
        logger.silly("Sending chunk [%d, %d] from cache with %d pixels", position.x, position.y, Object.keys(chunk.pixels).length)
        return chunk
    }
}

// Defunct, players are removed from other people's games by not sending a position update in a while
function broadcastClientQuit(clientid) {
    for (cid in clients) {
        if (cid == clientid) continue
        let clnt = clients[cid]
        let packet = new Buffer(2)
        packet.writeInt8(0xA, 0)
        packet.writeUInt8(clientid.length, 1)
        packet = Buffer.concat([packet, new Buffer.from(clientid, "ascii")])
        clnt.socket.write(packet)
    }
}

function broadcastChunkPacket(chunk) {
    // TODO: Only update players nearby
    logger.silly("Broadcasting chunk change [%d, %d] to %d other players", chunk.x, chunk.y, Object.keys(clients).length)
    for (clientid in clients) {
        sendChunkPacket(chunk, clients[clientid])
    }
}

function sendChunkPacket(chunk, client) {
    try { 
        // If chunk has no pixel data just send a blank chunk packet
        if (Object.keys(chunk.pixels).length == 0) {
            logger.silly("Sending " + "blank".bold + " chunk [%d, %d] to %s", chunk.x, chunk.y, client.clientid)
            let emptyChunkPacket = new Buffer(9)
            emptyChunkPacket.writeInt8(0x05, 0)
            emptyChunkPacket.writeInt32LE(chunk.x, 1)
            emptyChunkPacket.writeInt32LE(chunk.y, 5)
        
            client.socket.write(emptyChunkPacket)
        } else {
            logger.silly("Sending chunk [%d, %d] to %s", chunk.x, chunk.y, client.clientid)
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
    } catch (e) {
        logger.warn("Error while sending chunk [%d, %d] to %s", chunk.x, chunk.y, client.clientid)
        logger.error(e)
    }
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

// DEFUNCT - clients now request the chunks they want
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
    logger.verbose("SAVING WORLD...".bold.green)
    for (chunkloc in chunks) {
        saveChunk(chunks[chunkloc])
    }
}


function saveChunk(chunk) {
    try {
        // Don't save if there are no pixels in the chunk
        if (Object.keys(chunk.pixels) == 0) {
            logger.debug("Skipping saving chunk [%d, %d] to file as it is blank", chunk.x, chunk.y)
            return
        }

        logger.debug("Saving chunk [%d, %d] with %d pixels to file world/Chunk" + chunk.x + "," + chunk.y, chunk.x, chunk.y, Object.keys(chunk.pixels).length)
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
    } catch (e) {
        logger.error("Error while saving chunk [%d, %d] to file", chunk.x, chunk.y)
        logger.error(e)
    }
}

function loadChunkAtPosition(position) {
    // TODO: Load chunk from file here, otherwise return empty chunk
    if (!fs.existsSync("world/Chunk" + position.x + "," + position.y)) {
        let newChunk = new Chunk(position.x, position.y)
        chunks[position.x + "," + position.y] = newChunk
        logger.silly("Created new chunk [%d, %d]", position.x, position.y)
        return newChunk
    }
    
     let chunk = new Chunk(position.x, position.y)
     chunks[position.x + "," + position.y] = chunk

     try {
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
        logger.silly("Loaded chunk [%d, %d] from file with %d pixels", position.x, position.y, Object.keys(chunk.pixels).length)
    } catch (e) {
        logger.error("While loading chunk [%d, %d] from file, got an " + "error".red, position.x, position.y)
        logger.error(e)
    }

    return chunk
}