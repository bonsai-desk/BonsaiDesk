let cSharpPageListeners = (history, event) => {
    let json = JSON.parse(event.data);

    console.log(json)

    if (!(json.type === "nav")) return;

    switch (json.command) {
        case "goHome":
            console.log("command: goHome pre ")
            history.push("/")
            window.location.reload(true)
            console.log("command: goHome post")
            break;
        case "reload":
            console.log("command: reload")
            window.location.reload(true)
            break;
        default:
            console.log("command: not handled (cSharpPageListeners) " + JSON.stringify(json))
            break;
    }
}

module.exports = {cSharpPageListeners};