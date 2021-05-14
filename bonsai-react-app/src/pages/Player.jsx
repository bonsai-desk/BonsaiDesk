import React, {useRef, useState} from 'react';
import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import {
    postBrowseYouTube,
    postSeekPlayer,
    postSetVolume,
    postVideoEject,
    postVideoPause,
    postVideoPlay,
    postVideoRestart,
} from '../api';
import {grayButtonClass, greenButtonClass} from '../cssClasses';
import {KeySVG} from '../components/Keys';
import ResetImg from '../static/reset.svg';
import PlayImg from '../static/play.svg';
import PauseImg from '../static/pause.svg';
import {MenuContent} from '../components/MenuContent';
import EjectImg from '../static/eject-fill.svg';
import VolumeOff from '../static/volume-off.svg';
import VolumeHigh from '../static/volume-high.svg';
import {InfoItem} from '../components/InfoItem';
import YtImg from '../static/yt-small.png';
import {NormalButton} from '../components/Button';

export const PlayerPage = observer(() => {
    const {store} = useStore();
    const [preMuteVolume, setPreMuteVolume] = useState(null);

    const finished = (store.MediaInfo.Duration - store.MediaInfo.Scrub) < 0.25;

    let media = store.MediaInfo;
    
    let mediaActive = media.Active;

    const playerLevel = media.Scrub / media.Duration;

    const volumeApproxZero = store.MediaInfo.VolumeLevel < 0.001;

    function handleClickMute() {
        if (!mediaActive) {
            return;
        }
        
        if (store.MediaInfo.VolumeLevel < 0.0001) {
            postSetVolume(preMuteVolume);
        } else {
            setPreMuteVolume(store.MediaInfo.VolumeLevel);
            postSetVolume(0);
        }
    }

    function handleClickPlayer(level) {
        if (!mediaActive) {
            return;
        }
        
        let ts = level * store.MediaInfo.Duration;
        postSeekPlayer(ts);
    }

    function handleClickVolume(level) {
        if (!mediaActive) {
            return;
        }
        postSetVolume(level);
    }

    function handlePause() {
        if (!mediaActive) {
            return;
        }
        postVideoPause();
    }

    function handlePlay() {
        if (!mediaActive) {
            return;
        }
        postVideoPlay();
    }

    function handleEject() {
        if (!mediaActive) {
            return;
        }
        postVideoEject();
    }

    function handleRestart() {
        if (!mediaActive) {
            return;
        }
        postVideoRestart();
    }

    let mediaClass = grayButtonClass;

    function ControlButton() {
        if (finished) {
            return <KeySVG handleClick={handleRestart} imgSrc={ResetImg}/>;
        } else {
            if (store.MediaInfo.Paused) {
                return <KeySVG handleClick={handlePlay} imgSrc={PlayImg}/>;
            } else {
                return <KeySVG handleClick={handlePause} imgSrc={PauseImg}/>;
            }
        }
    }

    return <MenuContent name={'Media'}>

            <div className={'flex space-x-2'}>
                <ControlButton/>
                <Bar level={playerLevel} handleClickLevel={handleClickPlayer}/>
                <KeySVG handleClick={handleEject} imgSrc={EjectImg}
                        className={mediaClass}/>

            </div>

            {mediaActive ?

                    <div className={'flex space-x-2'}>
                        <KeySVG handleClick={handleClickMute}
                                imgSrc={volumeApproxZero ? VolumeOff : VolumeHigh}
                                className={mediaClass}/>
                        <Bar level={media.VolumeLevel} handleClickLevel={handleClickVolume}/>
                    </div> : ''
            }


        <InfoItem imgSrc={YtImg} title={'YouTube'}
                  slug={'Find videos to watch on the big screen'} rightPad={false}>
            <NormalButton className={greenButtonClass} onClick={postBrowseYouTube}>
                Browse
            </NormalButton>
        </InfoItem>

    </MenuContent>;

});

function Bar({level, handleClickLevel}) {
    const ref = useRef(null);

    function handleClick(e) {
        let clickedLevel = (e.clientX - ref.current.offsetLeft) /
                ref.current.offsetWidth;
        console.log(clickedLevel);
        if (handleClickLevel) {
            handleClickLevel(clickedLevel);
        }
    }

    const pct = 100 * level;

    return (<div className={'flex h-20 w-full'}>
        <div ref={ref} onPointerDown={handleClick}
             className={'relative bg-gray-600 rounded w-full'}>

            <div style={{width: pct + '%'}}
                 className={'h-full bg-gray-400 rounded'}/>
        </div>
    </div>);
}