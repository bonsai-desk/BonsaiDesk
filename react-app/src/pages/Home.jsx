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
        window.vuplex.addEventListener('message', event => {
            let json = JSON.parse(event.data);
            if (!(json.type === "video")) return;
            switch (json.command) {
                case "pause":
                    player.pauseVideo();
                    break;
                case "play":
                    player.playVideo();
                    break;
                case "resize":
                    let width = window.innerWidth;
                    let height = window.innerHeight;
                    player.setSize(width, height);
                    break;
                case "load":
                    if (json.video_id != null) {
                        player.cueVideoById(json.video_id);
                    }
                    break;
                default:
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
        setInterval(() => {console.log(player.getCurrentTime())}, 1000)
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