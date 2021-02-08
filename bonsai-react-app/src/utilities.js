let postJson = (json) => {
    if (window.vuplex != null) {
        console.log("post json===")
        console.log(json)
        window.vuplex.postMessage(json);
        console.log("post json==")
    }
}

module.exports = {postJson};