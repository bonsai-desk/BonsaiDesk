import React, {useState, useEffect} from "react";
import YouTube from "react-youtube";

let Video = () => {

    let [player, setPlayer] = useState(null);

    let videoID = "";

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
                    let videoId = json.video_id
                    if (videoId != null) {
                        player.loadVideoById(videoId);
                    }
                    break;
                default:
                    break;
            }
        })
    }

    let opts = {
        width: window.innerWidth,
        height: window.innerHeight,
        playerVars: {
            autoplay: 0,
            controls: 0,
            disablekb: 1,
            rel: 0
        }
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


    return (
        <div>
            <YouTube
                videoId={videoID}
                opts={opts}
                onReady={onReady}
                onPlay={onPlay}
                onPause={onPause}
                onEnd={onEnd}
            />
        </div>
    );
};

export default Video;