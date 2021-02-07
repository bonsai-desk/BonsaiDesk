let postJson = (json) => {
    if (window.vuplex != null) {
        console.log("post json")
        window.vuplex.postMessage(json);
    }
}

module.exports = {postJson};