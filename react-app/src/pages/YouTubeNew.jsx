import YouTube from "react-youtube";
import React, {useState} from "react";

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

let showState = (state) => {
    let name;
    switch (state) {
        case -1:
            name = "unstarted"
            break;
        case 0:
            name = "ended"
            break;
        case 1:
            name = "playing"
            break;
        case 2:
            name = "paused"
            break;
        case 3:
            name = "buffering"
            break;
        case 5:
            name = "cued"
            break;
    }
    return name

}

let Video = (props) => {

    let dev_mode = window.location.pathname.split("/")[1] === "youtube_test";

    let {id, timeStamp} = props.match.params;

    let [player, setPlayer] = useState(null);
    let [init, setInit] = useState(false);

    // this is the 'I'm ready at a timestamp' 'ready', not the 'player is ready' of 'onReady'
    let [ready , setReady] = useState(false);
    let [readying, setReadying] = useState(false);
    let [justBuffered, setJustBuffered] = useState(false);

    let postStateChange = (message) => {
        if (message !== "READY") setReady(false);
        console.log("POST " + message)
    }

    let readyUp = (_player, timeStamp) => {
        if (_player == null){
            console.log("bonsai: ignoring attempt to ready up while player is null")
            return;
        }

        if (ready && Math.abs(player.getCurrentTime() - timeStamp) < 0.01) {
            console.log("bonsai: ready-up called while ready")
            postStateChange("READY")
            return;
        }

        let state = _player.getPlayerState();

        if (!init) {
            console.log("bonsai: ignoring attempt to ready-up before init")
        } else {
            console.log("bonsai: readying up from " + showState(state))
            prepareToReadyUp(_player);
            if (state === PlayerState.PAUSED) {
                _player.playVideo();
            }
            _player.seekTo(timeStamp, true)
            _player.pauseVideo()
        }
    }

    let prepareToReadyUp = (_player) => {
        console.log("bonsai: prepare to ready up")
        setReady(false)
        setReadying(true)
        _player.mute();
    }

    let onReady = (event) => {
        // load the player to the url timestamp when ready
        let _player = event.target;
        setPlayer(_player);
        prepareToReadyUp(_player)
        _player.loadVideoById(id, parseFloat(timeStamp));
    }
    let onError = (event) => {
        console.log("bonsai youtube error: " + event)
    }

    function handleCued() {
        console.log("bonsai: " + showState(player.getPlayerState()));
    }
    function handleUnstarted() {
        console.log(
            "bonsai: " +
            showState(player.getPlayerState()) + " " +
            player.getCurrentTime()
        )
    }
    function handleEnded() {
        console.log("bonsai: " + showState(player.getPlayerState()));
        postStateChange("ENDED")
    }

    function handlePlaying() {
        if (init) {
            if (readying) {
                console.log("bonsai: while readying -> play")
            } else {
                if (justBuffered) {
                    console.log("bonsai: playing after buffer")
                } else {
                    console.log("bonsai: playing")
                }
                postStateChange("PLAYING")
            }
        } else {
            player.pauseVideo()
        }
        if (justBuffered) {
            setJustBuffered(false);
        }
    }
    function handlePaused() {
        if (init) {
            if (justBuffered){
                if (readying && !ready) {
                    console.log("bonsai: ready (pause after buffer)")
                    setReady(true);
                    setReadying(false);
                    postStateChange("READY")
                } else {
                    console.log("bonsai: paused after buffering")
                    postStateChange("PAUSED")
                }
            } else {
                if (readying) {
                    console.log("bonsai: while readying -> paused")
                } else {
                    console.log("bonsai: paused");
                    postStateChange("PAUSED");
                }
            }
        } else {
            console.log("bonsai: init complete")
            player.seekTo(timeStamp, true)
            player.unMute();
            setInit(true)
            setReady(true)
            setReadying(false)
            postStateChange("READY")
        }
        if (justBuffered) {
            setJustBuffered(false);
        }
    }
    function handleBuffering() {
        if (readying) {
            console.log("bonsai: while readying -> buffering")
        } else {
            console.log("bonsai: buffering")
        }
        setJustBuffered(true);
    }


    let onStateChange = (event) => {
        switch (event.data) {
            case PlayerState.CUED:
                handleCued();
                break;
            case PlayerState.UNSTARTED:
                handleUnstarted();
                break;
            case PlayerState.PLAYING:
                handlePlaying();
                break;
            case PlayerState.PAUSED:
                handlePaused();
                break;
            case PlayerState.BUFFERING:
                handleBuffering();
                break;
            case PlayerState.ENDED:
                handleEnded();
                break;
            default:
                console.log("bonsai error: did not handle state change " + event.data)
                break;
        }
    }

    return (
        <div>
            <p onClick={() => {readyUp(player, 40)}}>ready up</p>
            <YouTube
                opts={opts}
                onReady={onReady}
                onError={onError}
                onStateChange={onStateChange}
            />
        </div>
    )
}

export default Video;