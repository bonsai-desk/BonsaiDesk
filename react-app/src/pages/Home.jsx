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

const unstarted = -1;
const ended = 0;
const playing = 1;
const paused = 2;
const buffering = 3;
const videoCued = 5;

let Video = () => {

    let [player, setPlayer] = useState(null);

    let [cue, setCue] = useState(true);

    let [videoId, setVideoId] = useState("Cg0QwoHh9w4");

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
                        setVideoId(json.video_id);
                        setCue(true);
                        player.loadVideoById(json.video_id);
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
    }, [player])

    let onReady = (event) => {
        console.log("onReady", event);
        setPlayer(event.target);
    };
    let onPlay = (event) => {
        console.log("onPlay", event);
    };
    let onPause = (event) => {
        console.log("onPause", event);
    };
    let onEnd = (event) => {
        console.log("onEnd", event);
    };
    let onStateChange = (event) => {
        switch (event.data) {
            case unstarted:
                console.log("bonsai: unstarted")
                break
            case ended:
                console.log("bonsai: ended")
                break
            case playing:
                if (cue) {
                    console.log("bonsai: playing -> cue")
                    player.pauseVideo();
                    setCue(false);
                } else {
                    console.log("bonsai: playing")
                }
                break
            case paused:
                break;
            case buffering:
                break
            case videoCued:
                break
            default:
                break;
        }
    }

    return (
        <div>
            <YouTube
                videoId={videoId}
                opts={opts}
                onReady={onReady}
                onPlay={onPlay}
                onPause={onPause}
                onEnd={onEnd}
                onStateChange={onStateChange}
            />
        </div>
    );
};

export default Video;