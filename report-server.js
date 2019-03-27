// will be spawned by server.js
const express = require('express')
const multer = require("multer")
const fs = require("fs")
const discordLib = require("discord.js")
const bodyParser = require("body-parser")

const server = express()
const multerStorage = multer.diskStorage({
    destination: (req, file, cb) => cb(null, __dirname + "/reports"),
    filename: (req, file, cb) => cb(null, req.body.hash + ".png")
})
let uploader = multer({storage: multerStorage, limits: {fileSize: 1 * 1024 * 1024}})
const discord = new discordLib.Client()

server.post('/report', uploader.fields([{name: "image", maxCounts: 1}]), (req, res) => {
    let ip = req.connection.remoteAddress
    let hash = req.body.hash
    let x = req.body.x
    let y = req.body.y
    let fileName = req.files["image"][0].path
    

    console.log(JSON.stringify(req.files))
    console.log("Just got a report from " + ip + "#" + hash + " for coords " + x + " " + y + " - filename: " + fileName)



    const embed = new discordLib.RichEmbed()
    embed.setTitle("🚨 Abuse report")
    embed.addField("From", ip + "#" + hash)
    embed.addField("Location", "X: " + x + ", Y: " + y)
    embed.attachFile(req.files["image"][0].path)
    embed.setImage("attachment://" + req.files["image"][0].fileName)

    discord.channels.get("560418085476106240").send(embed)
})


process.once("message", (msg) => {
    // take msgs with .type start only
    if (!msg.type || msg.type !== "start") return

    let ip = msg.ip
    let port = msg.port

    discord.login("NTYwNDE3MTk5NTIxNjYwOTI4.D3zqDg.wxSa4V1MTgndjY-guD3Mn6OAMAU")
    server.listen(port, () => {
        console.log("Report server up and running on " + ip + ":" + port + "!")
    })
})

discord.on('ready', () => {
    console.log("Report server Discord bot is online")

})


server.on("error", (err) => {
    console.log("Uncaught server error: " + err)
})