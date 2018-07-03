
function getNearestChunkTo(position) {
    let x = Math.round(position.x / 16) * 16
    let y = Math.round(position.y / 16) * 16
    return {x: x, y: y}
}

console.log(getNearestChunkTo({x: -26, y: -20}))