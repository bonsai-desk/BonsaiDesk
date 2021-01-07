import React, {useState, useEffect} from "react";
import YouTube from "react-youtube";
import {useHistory} from "react-router-dom";

let opts = {
    width: window.innerWidth,
    height: window.innerHeight/2,
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

    let history = useHistory();

    let dev_mode = window.location.pathname.split("/")[1] === "youtube_test";
    let {id, timeStamp} = props.match.params;
    let [ready, setReady] = useState(false);
    let [player, setPlayer] = useState(null);
    let [justBuffered, setJustBuffered] = useState(false)

    let [ts, setTs] = useState(timeStamp);

    let readyUp = (timeStamp) => {
        switch (player.getPlayerState()) {
            case PlayerState.PAUSED:
                player.playVideo();
                player.seekTo(timeStamp, true);
                player.pauseVideo();
                break;
            case PlayerState.PLAYING:
                player.seekTo(timeStamp, true);
                player.pauseVideo();
                break;
            default:
                let msg = "ERROR READYUP SWITCH NOT HANDLED"
                window.vuplex.postMessage({type: "error", message: msg, current_time: player.getCurrentTime()});
                console.log("bonsai: " + msg)
                break;


        }
    }

    // setup the event listeners
    useEffect(() => {
        if (player == null || dev_mode) return;

        let playerListeners = (event) => {

            let json = JSON.parse(event.data);

            if (json.type !== "video") return;

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
                    player.seekTo(json.seekTime, true);
                    break;
                case "readyUp":
                    readyUp(json.timeStamp)
                    break;
                default:
                    console.log("command: not handled (video) " + JSON.stringify(json))
                    break;
            }
        }

        console.log("bonsai: add YouTube events+intervals")

        window.vuplex.addEventListener('message', playerListeners)

        let pingPlayerTime = setInterval(() => {
            window.vuplex.postMessage({
                type: "infoCurrentTime",
                current_time: player.getCurrentTime() == null ? 0 : player.getCurrentTime()
            })
        }, 100)

        return () => {
            console.log("bonsai: remove YouTube events+intervals")
            window.vuplex.removeEventListener('message', playerListeners)
            clearInterval(pingPlayerTime)
        }
    }, [player, dev_mode, history])

    let onReady = (event) => {
        setPlayer(event.target);
        event.target.mute();
        event.target.loadVideoById(id, parseFloat(ts));
    };

    let onStateChange = (event) => {

        let postStateChange = (message) => {
            if (dev_mode) {
                return;
            }

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
                if (justBuffered) {
                    setJustBuffered(false);
                    console.log("bonsai: play after buffer")
                }
                if (!ready) {
                    player.pauseVideo()
                } else {
                    console.log("bonsai: playing")
                    postStateChange("PLAYING")
                }
                break;
            case PlayerState.PAUSED:
                if (!ready) {
                    player.seekTo(ts);
                    player.unMute();
                    setReady(true);
                    console.log("bonsai: ready")
                    postStateChange("READY")
                } else {
                    if (!justBuffered) {
                        console.log("bonsai: paused")
                        postStateChange("PAUSED")
                    } else {
                        console.log("bonsai: ready (pause after buffer)")
                        postStateChange("READY")
                    }
                }
                setJustBuffered(false);
                break;
            case PlayerState.BUFFERING:
                setJustBuffered(true)
                if (ready) {
                    console.log("bonsai: buffering")
                    postStateChange("BUFFERING")
                } else {
                    console.log("bonsai: buffering before ready")
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
            {dev_mode ? <div onClick={()=>{readyUp(30)}}>readyup</div> : ""}

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