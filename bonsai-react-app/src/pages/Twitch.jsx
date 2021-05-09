import React from 'react';
import ReactPlayer from 'react-player';

let Twitch = () => {
    let options = {
        url: 'https://www.twitch.tv/hamletva',
        width: window.innerWidth,
        height: window.innerHeight,
    };

    let onProgress = ({played, loaded, playedSeconds, loadedSeconds}) => {
        console.log(played, loaded, playedSeconds, loadedSeconds);
    };

    return <ReactPlayer {...options} onProgress={onProgress}/>;
};

export default Twitch;