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

let YoutTubeTest = (props) => {

    let { id } = props.match.params;
    let [ready, setReady] = useState(false);

    let [player, setPlayer] = useState(null);

    let onReady = (event) => {
        console.log("onReady", event);
        setPlayer(event.target);
        event.target.mute();
        event.target.loadVideoById(id);
    };

    let onError = (event) => {
        console.log("onError", event);
    };

    let onStateChange = (event) => {


        switch (event.data) {
            case PlayerState.UNSTARTED:
                console.log("bonsai: unstarted")
                break;
            case PlayerState.ENDED:
                console.log("bonsai: ended")
                break;
            case PlayerState.PLAYING:
                if (!ready) {
                    player.pauseVideo()
                } else {
                    console.log("bonsai: playing")
                }
                break;
            case PlayerState.PAUSED:
                if (!ready) {
                    player.seekTo(0);
                    player.unMute();
                    setReady(true);
                    console.log("bonsai: ready")
                } else {
                    console.log("bonsai: paused")
                }
                break;
            case PlayerState.BUFFERING:
                if (ready) {
                    console.log("bonsai: buffering")
                }
                break;
            case PlayerState.CUED:
                console.log("bonsai: videoCued")
                break;
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

export default YoutTubeTest;