let postJson = (json) => {
    if (window.vuplex != null) {
        window.vuplex.postMessage(json);
    }
}

module.exports = {postJson};