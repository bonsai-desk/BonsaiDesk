let postJson = (json) => {
    if (window.vuplex != null) {
        console.log("post json " + JSON.stringify(json))
        window.vuplex.postMessage(json);
    }
}

module.exports = {postJson};