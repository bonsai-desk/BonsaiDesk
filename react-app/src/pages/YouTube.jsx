import React, {useState, useEffect} from "react";
import YouTube from "react-youtube";

let opts = {
    width: window.innerWidth,
    height: window.innerHeight,
    playerVars: {
        autoplay: 0,
        controls: 0,
        disablekb: 1,
        rel: 0
    }
}

const PlayerState = {
    UNSTARTED: -1,
    ENDED: 0,
    PLAYING: 1,
    PAUSED: 2,
    BUFFERING: 3,
    CUED: 5,
};

let Video = (props) => {

    let {id} = props.match.params;
    let [ready, setReady] = useState(false);
    let [player, setPlayer] = useState(null);

    // setup the event listeners
    useEffect(() => {
        if (player == null) return;

        if (window.vuplex != null) {
            addCSharpListeners(player);
        } else {
            window.addEventListener('vuplexready', _ => {
                addCSharpListeners(player)
            })
        }
    }, [player])

    let onReady = (event) => {
        console.log("onReady", event);
        setPlayer(event.target);
        event.target.mute();
        event.target.loadVideoById(id);
    };

    let addCSharpListeners = (player) => {

        // ping back the player time every 1/10th of a second
        setInterval(() => {
            window.vuplex.postMessage({
                type: "infoCurrentTime",
                current_time: player.getCurrentTime() == null ? 0 : player.getCurrentTime()
            })
        }, 100)

        window.vuplex.addEventListener('message', event => {

            let json = JSON.parse(event.data);

            if (!(json.type === "video")) return;

            switch (json.command) {
                case "play":
                    console.log("command: play")
                    player.playVideo();
                    break;
                case "pause":
                    console.log("command: pause")
                    player.pauseVideo();
                    break;
                case "seekTo":
                    console.log("command: seekTo " + json.seekTime)
                    player.seekTo(json.seekTime);
                    break;
                default:
                    console.log("command: not handled " + JSON.stringify(json))
                    break;
            }
        })
    };

    let onStateChange = (event) => {

        let postStateChange = (message) => {
            window.vuplex.postMessage({type: "stateChange", message: message, current_time: player.getCurrentTime()});
        }

        switch (event.data) {
            case PlayerState.UNSTARTED:
                console.log("bonsai: unstarted")
                postStateChange("UNSTARTED")
                break;
            case PlayerState.ENDED:
                console.log("bonsai: ended")
                postStateChange("ENDED")
                break;
            case PlayerState.PLAYING:
                if (!ready) {
                    player.pauseVideo()
                } else {
                    console.log("bonsai: playing")
                    postStateChange("PLAYING")
                }
                break;
            case PlayerState.PAUSED:
                if (!ready) {
                    player.seekTo(0);
                    player.unMute();
                    setReady(true);
                    console.log("bonsai: ready")
                    postStateChange("READY")
                } else {
                    console.log("bonsai: paused")
                    postStateChange("PAUSED")
                }
                break;
            case PlayerState.BUFFERING:
                if (ready) {
                    console.log("bonsai: buffering")
                    postStateChange("BUFFERING")
                }
                break;
            case PlayerState.CUED:
                console.log("bonsai: videoCued")
                postStateChange("CUED")
                break;
            default:
                break;
        }
    }

    let onError = (event) => {
        console.log("onError", event);
    };

    return (
        <div>
            <YouTube
                opts={opts}
                onReady={onReady}
                onError={onError}
                onStateChange={onStateChange}
            />
        </div>
    );
};

export default Video;