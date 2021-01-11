import YouTube from "react-youtube";
import React, {useState, useEffect, useCallback} from "react";
import {useHistory, useLocation} from "react-router-dom";



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
        default:
            name = "BAD SWITCH STATE"
    }
    return name

}

let useQuery = () => {
    return new URLSearchParams(useLocation().search)
}

let Video = (props) => {

    let query = useQuery();

    let xRes = parseInt(query.get("x"));
    let yRes = parseInt(query.get("y"));

    let opts = {
        width: xRes && yRes ? xRes : window.innerWidth,
        height: xRes && yRes ? yRes : window.innerHeight,
        playerVars: {
            autoplay: 0,
            controls: 0,
            disablekb: 1,
            rel: 0
        }
    }

    let history = useHistory();

    let dev_mode = window.location.pathname.split("/")[1] === "youtube_test";

    let {id, timeStamp} = props.match.params;

    let [player, setPlayer] = useState(null);
    let [init, setInit] = useState(false);

    // this is the 'I'm ready at a timestamp' 'ready', not the 'player is ready' of 'onReady'
    let [ready, setReady] = useState(false);
    let [readying, setReadying] = useState(false);
    let [justBuffered, setJustBuffered] = useState(false);

    let postStateChange = useCallback((message) => {
        if (message !== "READY") setReady(false);
        console.log("POST " + message)
        if (!dev_mode) {
            window.vuplex.postMessage({type: "stateChange", message: message, current_time: player.getCurrentTime()});
        }
    }, [player, dev_mode])

    let readyUp = useCallback((_timeStamp) => {
        if (player == null) {
            console.log("bonsai: ignoring attempt to ready up while player is null")
            return;
        }

        if (ready && Math.abs(player.getCurrentTime() - _timeStamp) < 0.01) {
            console.log("bonsai: ready-up called while ready")
            postStateChange("READY")
            return;
        }

        let state = player.getPlayerState();

        if (!init) {
            console.log("bonsai: ignoring attempt to ready-up before init")
        } else {
            console.log("bonsai: readying up from " + showState(state) + " -> " + _timeStamp)
            prepareToReadyUp(player);
            if (state === PlayerState.PAUSED) {
                player.playVideo();
            }
            player.seekTo(_timeStamp, true)
            player.pauseVideo()
            player.unMute();
        }

    }, [init, player, postStateChange, ready])

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
            if (justBuffered) {
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

    useEffect(() => {
        if (player == null || dev_mode) {
            return;
        }

        let playerListeners = (event) => {
            let json = JSON.parse(event.data)
            if (json.type !== "video") {
                return;
            }
            console.log(event.data)
            switch (json.command) {
                case "play":
                    console.log("COMMAND: play")
                    player.playVideo()
                    break;
                case "pause":
                    console.log("COMMAND: pause")
                    player.pauseVideo()
                    break;
                case "readyUp":
                    console.log("COMMAND: readyUp")
                    readyUp(json.timeStamp)
                    break;
                default:
                    console.log("command: not handled (video) " + event.data)
                    break;
            }
        }
        window.vuplex.addEventListener('message', playerListeners)
        return () => {
            window.vuplex.removeEventListener('message', playerListeners)
        }
    }, [id, player, dev_mode, readyUp])
    useEffect(() => {
        console.log("bonsai: add ping interval")
        let pingPlayerTime = setInterval(() => {
            let current_time = 0;
            if (player != null && player.getCurrentTime() != null) {
                current_time = player.getCurrentTime();
            }
            if (dev_mode) {
                return;
            }
            window.vuplex.postMessage({
                type: "infoCurrentTime",
                current_time: current_time
            })
        }, 100)
        return () => {
            console.log("bonsai: remove ping interval")
            clearInterval(pingPlayerTime)
        }
    }, [id, player, dev_mode])

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
            {dev_mode ?
                <div>
                    <p onClick={() => {
                        history.push("/home")
                    }}>home</p>
                    {" "}
                    <p onClick={() => {
                        readyUp(40)
                    }}>ready up</p>
                </div>
                : ""
            }
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