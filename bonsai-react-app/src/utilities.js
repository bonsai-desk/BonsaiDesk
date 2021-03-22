let postJson = (json) => {
    console.log("post json " + JSON.stringify(json))
    if (window.vuplex != null) {
        window.vuplex.postMessage(json);
    }
}

module.exports = {postJson};