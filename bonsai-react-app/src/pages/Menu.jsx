import React, {useEffect, useRef, useState} from 'react';
import {Link, Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import './Menu.css';
import {apiBase} from '../utilities';
import {Button} from '../components/Button';
import axios from 'axios';
import PauseImg from '../static/pause.svg';
import PlayImg from '../static/play.svg';
import ResetImg from '../static/reset.svg';
import VolumeHigh from '../static/volume-high.svg';
import VolumeOff from '../static/volume-off.svg';

import DotsImg from '../static/dots-vertical.svg';

import EjectImg from '../static/eject-fill.svg';
import {KeySVG} from '../components/Keys';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {observer} from 'mobx-react-lite';
import {autorun} from 'mobx';
import {MenuContent} from '../components/MenuContent';
import {
    postCloseMenu,
    postJoinRoom,
    postRequestMicrophone,
    postSeekPlayer,
    postSetVolume,
    postVideoEject,
    postVideoPause,
    postVideoPlay,
    postVideoRestart,
} from '../api';
import {grayButtonClass, roundButtonClass} from '../cssClasses';
import {DebugPage} from './Debug';
import {VideosPage} from './Videos';
import {SettingsPage} from './Settings';
import {HomePage} from './Home';

function ListItem(props) {
    let {
        selected,
        handleClick,
        inactive = false,
        buttonClassSelected = '',
        buttonClass = '',
        buttonClassInactive = '',
        to = '',
    } = props;

    let history = useHistory();

    buttonClass = buttonClass ?
            buttonClass :
            'py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center';
    buttonClassSelected = buttonClassSelected ?
            buttonClassSelected :
            'py-4 px-8 bg-blue-700 text-white rounded cursor-pointer flex flex-wrap content-center';
    buttonClassInactive = buttonClassInactive ?
            buttonClassInactive :
            'py-4 px-8 bg-gray-800 rounded cursor-pointer flex flex-wrap content-center';

    if (inactive) {
        return (
                <div className={buttonClassInactive}>
                    {props.children}
                </div>
        );
    }

    let className = selected ? buttonClassSelected : buttonClass;
    if (to) {
        className = window.location.pathname === to ? buttonClassSelected : buttonClass;
    }

    if (to) {
        return (
                <Button className={className} handleClick={() => {
                    history.push(to);
                }}>
                    <Link to={to}>
                        {props.children}
                    </Link>
                </Button>
        );

    }

    return (
            <Button className={className} handleClick={handleClick}>
                {props.children}
            </Button>
    );
}

function SettingsList(props) {
    return (
            <div className={'space-y-1 px-2'}>
                {props.children}
            </div>);

}

function SettingsTitle(props) {
    return <div
            className={'text-white font-bold text-xl px-5 pt-5 pb-2'}>{props.children}</div>;
}

function JoinDeskButton(props) {
    let {handleClick, char} = props;

    return (
            <Button className={roundButtonClass} handleClick={() => {
                handleClick(char);
            }}>
            <span className={'w-full text-center'}>
                {char}
            </span>
            </Button>
    );
}

//

const PlayerPage = observer(() => {
    const {store} = useStore();
    const [preMuteVolume, setPreMuteVolume] = useState(null);

    const finished = (store.MediaInfo.Duration - store.MediaInfo.Scrub) < 0.25;

    let media = store.MediaInfo;

    const playerLevel = media.Scrub / media.Duration;

    const volumeApproxZero = store.MediaInfo.VolumeLevel < 0.001;

    if (!store.MediaInfo.Active) {
        return '';
    }

    function handleClickMute() {
        if (store.MediaInfo.VolumeLevel < 0.0001) {
            postSetVolume(preMuteVolume);
        } else {
            setPreMuteVolume(store.MediaInfo.VolumeLevel);
            postSetVolume(0);
        }
    }

    function handleClickPlayer(level) {
        let ts = level * store.MediaInfo.Duration;
        postSeekPlayer(ts);
    }

    function handleClickVolume(level) {
        postSetVolume(level);
    }

    function handlePause() {
        postVideoPause();
    }

    function handlePlay() {
        postVideoPlay();
    }

    function handleEject() {
        postVideoEject();
    }

    function handleRestart() {
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

    return <MenuContent name={'Player Controls'}>
        <div className={'flex space-x-2'}>
            <ControlButton/>
            <Bar level={playerLevel} handleClickLevel={handleClickPlayer}/>
            <KeySVG handleClick={handleEject} imgSrc={EjectImg}
                    className={mediaClass}/>

        </div>
        <div className={'flex space-x-2'}>
            <KeySVG handleClick={handleClickMute} imgSrc={volumeApproxZero ? VolumeOff : VolumeHigh}
                    className={mediaClass}/>
            <Bar level={media.VolumeLevel} handleClickLevel={handleClickVolume}/>
        </div>
    </MenuContent>;

});

function Bar({level, handleClickLevel}) {
    const ref = useRef(null);

    function handleClick(e) {
        let clickedLevel = (e.clientX - ref.current.offsetLeft) / ref.current.offsetWidth;
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

let JoinDeskPage = observer(() => {
    let {store} = useStore();

    let [code, setCode] = useState('');
    let [posting, setPosting] = useState(false);
    let [message, setMessage] = useState('');

    let {history} = useHistory();

    let url = apiBase(store) + `/rooms/${code}`;

    useEffect(() => {

        function navHome() {
            history.push('/menu/home');
        }

        if (posting) return;

        if (code.length === 4) {
            setPosting(true);
            axios({
                method: 'get',
                url: url,
            }).then(response => {

                let networkAddressResponse = response.data.network_address.toString();
                let networkAddressStore = store.NetworkInfo.NetworkAddress;

                let {
                    //network_address,
                    //username,
                    version,
                } = response.data;

                // todo networkAddress is not unique per user
                console.log(networkAddressStore, networkAddressResponse);
                console.log(store.FullVersion, version);

                if (networkAddressResponse === networkAddressStore) {
                    // trying to join your own room
                    setMessage(`You can't join your own room`);
                    setCode('');
                    setPosting(false);
                } else if (store.FullVersion !== version) {
                    setMessage(`Your version (${store.FullVersion}) mismatch host (${version})`);
                    setCode('');
                    setPosting(false);
                } else {
                    postJoinRoom(response.data);
                    setCode('');
                    setPosting(false);
                    navHome();
                }
            }).catch(err => {
                console.log(err);
                setMessage(`Could not find ${code} try again`);
                setCode('');
                setPosting(false);
            });
        }
    }, [history, code, posting, url, store.NetworkInfo.NetworkAddress, store.FullVersion]);

    function handleClick(char) {
        setMessage('');
        switch (code.length) {
            case 4:
                setCode(char);
                break;
            default:
                setCode(code + char);
                break;
        }
    }

    function handleBackspace() {
        if (code.length > 0) {
            setCode(code.slice(0, code.length - 1));
        }
    }

    return (
            <MenuContent name={'Join Desk'}>
                <div className={'flex flex-wrap w-full content-center'}>
                    <div className={' w-1/2'}>
                        <div className={'text-xl'}>
                            {message}
                        </div>
                        <div
                                className={'text-9xl h-full flex flex-wrap content-center justify-center'}>
                            {code.length < 4 ? code : ''}
                        </div>
                    </div>
                    <div className={'p-2 rounded space-y-4 text-2xl'}>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'1'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'2'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'3'}/>
                        </div>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'4'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'5'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'6'}/>
                        </div>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'7'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'8'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'9'}/>
                        </div>
                        <div className={'flex flex-wrap w-full justify-around'}>
                            <JoinDeskButton
                                    handleClick={handleBackspace} char={'<'}/>
                        </div>
                    </div>
                </div>
            </MenuContent>
    );
});

//

let Menu = observer(() => {

    let {store, pushStore} = useStore();

    let match = useRouteMatch();

    useEffect(() => {
        autorun(() => {
            // remove room code if
            // DEVELOPMENT || PRODUCTION
            const networkAddress = store.NetworkInfo.NetworkAddress;
            const roomOpen = store.NetworkInfo.RoomOpen;
            const roomCode = store.RoomCode;
            const loadingRoomCode = store.LoadingRoomCode;
            const userName = store.SocialInfo.UserName;
            const version = `${store.AppInfo.Version}b${store.AppInfo.BuildId}`;

            if (roomCode && (!networkAddress || !roomOpen)) {
                console.log('Remove room code');
                pushStore({RoomCode: null});
                return;
            }

            // send ip/port out for a room code
            if (roomOpen && !roomCode && !loadingRoomCode && networkAddress) {
                console.log('fetch room code');
                pushStore({LoadingRoomCode: true});
                let url = apiBase(store) + '/rooms';
                axios(
                        {
                            method: 'post',
                            url: url,
                            data: `network_address=${networkAddress}&username=${userName}&version=${version}`,
                            header: {'content-type': 'application/x-www-form-urlencoded'},
                        },
                ).then(response => {
                    let tag = response.data.tag;
                    let secret = response.data.secret;

                    console.log(`Got room ${tag} ${secret}`);
                    pushStore({RoomSecret: secret});
                    pushStore({RoomCode: tag, LoadingRoomCode: false});
                }).catch(err => {
                    console.log(err);
                    pushStore({LoadingRoomCode: false});
                });
            }
        });

    });

    useEffect(() => {
        return () => {
            pushStore({RoomCode: null});
        };
    }, [pushStore]);

    let joinDeskActive = store.NetworkInfo.Mode === NetworkManagerMode.Host && !store.NetworkInfo.RoomOpen;

    if (!store.AppInfo.MicrophonePermission) {
        return <NoMicPage/>;
    }

    const playerButtonClass = 'text-white py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center border-4 border-green-400';
    const playerButtonClassSelected = 'py-4 px-8 bg-blue-700 text-white rounded cursor-pointer flex flex-wrap content-center border-4 border-green-400';

    return (
            <div className={'flex text-lg text-gray-500 h-full static'}>
                {!store.NetworkInfo.Online ?
                        <div className={'text-2xl p-4 flex flex-wrap content-center absolute text-white bg-red-800 bottom-2 right-2 z-20 rounded'}>
                            Internet Error: Check Your Connection
                        </div>
                        : ''

                }
                <div className={'w-4/12 bg-black overflow-auto scroll-host static'}>
                    <div className={'w-4/12 bg-black fixed'}>
                        <SettingsTitle>
                            Menu
                        </SettingsTitle>
                    </div>

                    <div className={'h-16'}/>
                    <SettingsList>
                        <ListItem to={'/menu/home'}>Home</ListItem>
                        {store.MediaInfo.Active ?
                                <ListItem to={'/menu/player'}
                                          buttonClass={playerButtonClass}
                                          buttonClassSelected={playerButtonClassSelected}>
                                    Player
                                </ListItem> : ''}
                        <ListItem to={'/menu/join-desk'} inactive={!joinDeskActive}>Join Desk</ListItem>
                        <ListItem to={'/menu/videos'}>Videos</ListItem>
                        <ListItem to={'/menu/settings'}>Settings</ListItem>

                        <ListItem to={'/menu/debug'} component={DebugPage}>Debug</ListItem>
                    </SettingsList>
                    <div className={'w-full p-2'}>
                        <ExitButton/>
                    </div>
                </div>

                <div className={'bg-gray-900 z-10 w-full overflow-auto scroll-host'}>
                    <Switch>
                        <Route path={`${match.path}/home`} component={HomePage}/>
                        <Route path={`${match.path}/join-desk`} component={JoinDeskPage}/>
                        <Route path={`${match.path}/videos`} component={VideosPage}/>
                        <Route path={`${match.path}/settings`} component={SettingsPage}/>
                        <Route path={`${match.path}/debug`} component={DebugPage}/>
                        <Route path={`${match.path}/player`} component={PlayerPage}/>
                        <Route path={`${match.path}`}>Page not found</Route>
                    </Switch>
                </div>

            </div>
    );
});

function ExitButton() {

    function handleClick() {
        postCloseMenu();
    }

    let buttonClass = 'rounded h-16 py-4 px-8 bg-red-800 hover:bg-red-700 active:bg-red-600 hover:text-white cursor-pointer flex flex-wrap content-center';

    return (
            <ListItem buttonClass={buttonClass} handleClick={handleClick}
                      className={'text-white'}>
                <span className={'text-white'}>Close Menu</span>
            </ListItem>
    );

}

function NoMicPage() {

    const className = 'py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center';

    function handleClick() {
        postRequestMicrophone();
    }

    return (
            <div className={'flex flex-wrap content-center justify-center bg-black w-full h-screen'}>
                <div className={''}>
                    <div className={'flex justify-center'}>
                        <div className={'text-2xl p-4 flex flex-wrap content-center text-white bg-red-800 rounded'}>
                            No Access to Microphone
                        </div>
                    </div>
                    <div className={'h-4'}/>
                    <div className={'flex justify-center'}>
                        <div className={'text-2xl font-normal text-white '}>
                            <Button className={className} handleClick={handleClick}>Request</Button>
                        </div>
                    </div>
                    <div className={'h-4'}/>
                    <div className={'flex text-white'}>
                        <span>If that does not work, check your app permissions under </span>
                        <img src={DotsImg} alt={''}/>
                    </div>
                </div>
            </div>
    );

}

export default Menu;
