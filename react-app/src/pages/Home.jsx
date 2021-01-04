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

let Video = () => {

    let [player, setPlayer] = useState(null);

    let addEventListeners = (player) => {

        setInterval(() => {
            window.vuplex.postMessage({type: "infoCurrentTime", current_time: player.getCurrentTime() == null ? 0 : player.getCurrentTime()})
        }, 100)

        window.vuplex.addEventListener('message', event => {
            let json = JSON.parse(event.data);
            if (!(json.type === "video")) return;
            switch (json.command) {
                case "setContent":
                    console.log("command: setContent (" + json.x + "," + json.y + ") " + json.video_id)
                    player.setSize(json.x, json.y);
                    if (json.video_id != null) {
                        player.cueVideoById(json.video_id);
                    }
                    break;
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

    useEffect(() => {
        if (player == null) return;


        if (window.vuplex != null) {
            addEventListeners(player);
        } else {
            window.addEventListener('vuplexready', _ => {
                addEventListeners(player)
            })
        }
    }, [player])

    let onReady = (event) => {
        console.log("onReady", event);
        setPlayer(event.target);
    };

    let onError = (event) => {
        console.log("onError", event);
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
                break
            case PlayerState.PLAYING:
                console.log("bonsai: playing")
                postStateChange("PLAYING")
                break
            case PlayerState.PAUSED:
                console.log("bonsai: paused")
                postStateChange("PAUSED")
                break;
            case PlayerState.BUFFERING:
                console.log("bonsai: buffering")
                postStateChange("BUFFERING")
                break;
            case PlayerState.CUED:
                console.log("bonsai: videoCued")
                postStateChange("CUED")
                break
            default:
                break;
        }
    }


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