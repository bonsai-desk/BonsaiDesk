import React, {useEffect, useState, useRef} from 'react';
import './Menu.css';
import {postJson, apiBase} from '../utilities';
import {Button, ToggleButton, UpButton} from '../components/Button';
import axios from 'axios';
import DoorOpen from '../static/door-open.svg';
import LinkImg from '../static/link.svg';
import LightImg from '../static/lightbulb.svg';
import PauseImg from '../static/pause.svg';
import PlayImg from '../static/play.svg';
import ResetImg from '../static/reset.svg';
import VolumeHigh from '../static/volume-high.svg';
import VolumeOff from '../static/volume-off.svg';
import {mpl, lgpl, apache} from '../static/licenses';

import DotsImg from '../static/dots-vertical.svg';

import EjectImg from '../static/eject-fill.svg';
import {KeySVG} from '../components/Keys';
import YtImg from '../static/yt-small.png';
import ThinkingFace from '../static/thinking-face.svg';
import {useStore} from '../DataProvider';
import {BeatLoader, BounceLoader} from 'react-spinners';
import {observer} from 'mobx-react-lite';
import {action, autorun} from 'mobx';
import {NetworkManagerMode} from '../DataProvider';


const roundButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center';
const redButtonClass = 'py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center';
const greenButtonClass = 'py-4 px-8 font-bold bg-green-800 active:bg-green-700 hover:bg-green-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClass = 'py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClassInert = 'py-4 px-8 font-bold bg-gray-800 rounded flex flex-wrap content-center';

// post data

function postRequestMicrophone() {
    postJson({Type: 'command', Message: 'requestMicrophone'});
}

function postTogglePinchPull() {
    postJson({Type: 'command', Message: 'togglePinchPull'});
}

function postToggleBlockBreak() {
    postJson({Type: 'command', Message: 'toggleBlockBreak'});
}

function postCloseMenu() {
    postJson({Type: 'command', Message: 'closeMenu'});
}

function postBrowseYouTube() {
    postJson({Type: 'command', Message: 'browseYouTube'});
}

function postOpenRoom() {
    postJson({Type: 'command', Message: 'openRoom'});
}

function postCloseRoom() {
    postJson({Type: 'command', Message: 'closeRoom'});
}

function postJoinRoom(data) {
    postJson({Type: 'command', Message: 'joinRoom', data: JSON.stringify(data)});
}

function postLeaveRoom() {
    postJson({Type: 'command', Message: 'leaveRoom'});

}

function postKickConnectionId(id) {
    postJson({Type: 'command', Message: 'kickConnectionId', Data: id});
}

function postSeekPlayer(ts) {
    postJson({Type: 'command', Message: 'seekPlayer', Data: ts});
}

function postSetVolume(level) {
    // [0,1]
    postJson({Type: 'command', Message: 'setVolume', Data: level});
}

function postVideoPlay() {
    postJson({Type: 'command', Message: 'playVideo'});
}

function postVideoPause() {
    postJson({Type: 'command', Message: 'pauseVideo'});
}

function postVideoEject() {
    postJson({Type: 'command', Message: 'ejectVideo'});
}

function postVideoRestart() {
    postJson({Type: 'command', Message: 'restartVideo'});
}

function postLightsChange(level) {
    postJson({Type: 'command', Message: 'lightsChange', Data: level});
}

// utils

function showInfo(info) {
    switch (info[0]) {
        case 'PlayerInfos':
            return showPlayerInfo(info[1]);
        case 'user_info':
            return JSON.stringify(info);
        default:
            return info[1] ? JSON.stringify(info[1], null, 2) : '';
    }
}

function showPlayerInfo(playerInfo) {
    return '[' + playerInfo.map(info => {
        return `(${info.Name}, ${info.ConnectionId})`;
    }).join(' ') + ']';
}

//

function ListItem(props) {
    let {
        selected,
        handleClick,
        inactive = false,
        buttonClassSelected = '',
        buttonClass = '',
        buttonClassInactive = '',
    } = props;

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

function ConnectedClient(props) {
    let {info} = props;
    let {Name, ConnectionId} = info;

    const hostClass = 'bg-gray-800 rounded-full p-4 h-20 flex flex-wrap content-center';
    const clientClass = 'bg-gray-800 active:bg-red-700 hover:bg-red-600 rounded-full p-4 cursor-pointer h-20 flex flex-wrap content-center';

    if (ConnectionId === 0) {
        return (
                <div className={hostClass}>
                    <div
                            className={'flex content-center p-2 space-x-4'}>
                        <div>
                            <img className={'h-9 w-9'} src={ThinkingFace} alt={''}/>
                        </div>
                        <div>
                            {Name}
                        </div>
                    </div>
                </div>
        );
    } else {
        return (
                <Button className={clientClass} handleClick={() => {
                    postKickConnectionId(ConnectionId);
                }}>
                    <div
                            className={'flex content-center p-2 space-x-4'}>
                        <div>
                            <img className={'h-9 w-9'} src={ThinkingFace} alt={''}/>
                        </div>
                        <div>
                            {Name}
                        </div>
                    </div>
                </Button>
        );

    }

}

function InfoItem({imgSrc, title, slug, children}) {
    return (
            <div className={'flex w-full justify-between'}>
                <div className={'flex w-auto'}>
                    <div className={'flex flex-wrap content-center  p-2 mr-2'}>
                        {imgSrc ?
                                <img className={'h-9 w-9'} src={imgSrc} alt={''}/>
                                : ''
                        }
                    </div>
                    <div className={'my-auto'}>
                        <div className={'text-xl'}>
                            {title}
                        </div>
                        <div className={'text-gray-400'}>
                            {slug}
                        </div>
                    </div>
                </div>
                {children}
            </div>
    );
}

function MenuContent(props) {
    let {name} = props;

    return (
            <div className={'text-white p-4 h-full pr-8'}>
                {name ?
                        <div className={'pb-8 text-xl'}>
                            {name}
                        </div>
                        : ''}
                <div className={'space-y-8 pb-8'}>
                    {props.children}
                </div>
            </div>
    );

}

function LoadingHomePage() {
    return <div className={'flex justify-center w-full flex-wrap'}>
        <BounceLoader size={200} color={'#737373'}/>
    </div>;
}

function ClientHomePage() {
    return (
            <div className={'flex'}>
                <InfoItem title={'Connected'} slug={'You are connected to a host'}
                          imgSrc={LinkImg}>
                    <Button handleClick={postLeaveRoom}
                            className={redButtonClass}>Exit</Button>
                </InfoItem>
            </div>
    );
}

const RoomInfo = observer(() => {
    let {store} = useStore();

    let handleCloseRoom = () => {
        if (store.RoomCode) {
            axios({
                method: 'delete',
                url: apiBase(store) + '/rooms/' + store.RoomCode,
            }).then(r => {
                if (r.status === 200) {
                    console.log(`deleted room ${store.RoomCode}`);
                }
            }).catch(console.log);
        }
        postCloseRoom();
    };

    let OpenRoom =
            <InfoItem title={'Room'} slug={'Invite others'} imgSrc={DoorOpen}>
                <Button className={greenButtonClass} handleClick={postOpenRoom}>
                    Open Up
                </Button>
            </InfoItem>;

    let CloseRoom =
            <InfoItem title={'Room'} slug={'Ready to accept connections'}
                      imgSrc={DoorOpen}>
                <Button className={redButtonClass} handleClick={handleCloseRoom}>
                    Close
                </Button>
            </InfoItem>;

    const roomCodeCLass = 'text-5xl ';

    if (store.NetworkInfo.RoomOpen) {
        return (
                <React.Fragment>
                    {CloseRoom}
                    <InfoItem title={'Desk Code'}
                              slug={'People who have this can join you'}
                              imgSrc={LinkImg}>
                        <div className={'h-20 flex flex-wrap content-center'}>
                            {store.RoomCode ?
                                    <div className={roomCodeCLass}>{store.RoomCode}</div>

                                    :
                                    <div className={grayButtonClassInert}><BeatLoader size={8}
                                                                                      color={'#737373'}/>
                                    </div>
                            }
                        </div>
                    </InfoItem>
                </React.Fragment>
        );
    } else {
        return (
                <React.Fragment>
                    {OpenRoom}
                </React.Fragment>
        );
    }

});

const HostHomePage = observer(() => {

    let {store} = useStore();

    return (
            <React.Fragment>
                <RoomInfo/>
                {store.PlayerInfos.length > 0 && store.NetworkInfo.RoomOpen ?
                        <React.Fragment>
                            <div className={'text-xl'}>People in Your Room</div>
                            <div className={'flex space-x-2'}>
                                {store.PlayerInfos.map(info => <ConnectedClient info={info}/>)}
                            </div>
                        </React.Fragment>
                        :
                        ''}
            </React.Fragment>
    );

});

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

const HomePage = observer(() => {

    let {store} = useStore();

    let Inner;

    switch (store.NetworkInfo.Mode) {
        case NetworkManagerMode.ClientOnly:
            Inner = <ClientHomePage/>;
            break;
        case NetworkManagerMode.Host:
            Inner = <HostHomePage/>;
            break;
        default:
            Inner = <LoadingHomePage/>;
            break;
    }

    return (
            <MenuContent name={'Home'}>
                {Inner}
            </MenuContent>
    );
});

let JoinDeskPage = observer((props) => {
    let {navHome} = props;
    let {store} = useStore();

    let [code, setCode] = useState('');
    let [posting, setPosting] = useState(false);
    let [message, setMessage] = useState('');

    useEffect(() => {
        if (posting) return;

        if (code.length === 4) {
            setPosting(true);
            let url = apiBase(store) + `/rooms/${code}`;
            axios({
                method: 'get',
                url: url,
            }).then(response => {

                let networkAddressResponse = response.data.network_address.toString();
                let networkAddressStore = store.NetworkInfo.NetworkAddress;

                // todo networkAddress is not unique per user
                console.log(networkAddressStore, networkAddressResponse)
                
                if (networkAddressResponse === networkAddressStore) {
                    // trying to join your own room
                    setMessage(`You can't join your own room`);
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
    }, [code, navHome, posting, store.NetworkInfo.NetworkAddress]);

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

function VideosPage() {
    return <MenuContent name={'Videos'}>
        <InfoItem imgSrc={YtImg} title={'YouTube'}
                  slug={'Find videos to watch on the big screen'}>
            <Button className={greenButtonClass} handleClick={postBrowseYouTube}>
                Browse
            </Button>
        </InfoItem>
    </MenuContent>;
}

const DebugPage = observer(() => {
    let {store} = useStore();

    let addFakeIpPort = action((store) => {
        //todo
        store.ip_address = 1234;
        store.port = 4321;
    });
    let rmFakeIpPort = action(store => {
        //todo
        store.ip_address = null;
        store.port = null;
    });

    let setNetState = action((store, netState) => {
        store.network_state = netState;
    });

    let addFakeClient = action(store => {
        if (store.PlayerInfos.length > 0) {
            store.PlayerInfos.push({Name: 'cam', ConnectionId: 1});
        } else {
            store.PlayerInfos.push(
                    {Name: 'loremIpsumLoremIpsumLorem', ConnectionId: 0});
        }
    });
    let rmFakeClient = action(store => {
        store.PlayerInfos.pop();
    });

    let toggleRoomOpen = action(store => {
        //todo
        store.RoomOpen = !store.RoomOpen;
    });

    let addFakeVideoPlayerPaused = () => {
        store.MediaInfo = {
            Active: true,
            Name: 'Video Name',
            Paused: true,
            Scrub: 20,
            Duration: 60,
            VolumeLevel: 0.5,
        };
    };

    let addFakeVideoPlayerPlaying = () => {
        store.MediaInfo = {
            Active: true,
            Name: 'Video Name',
            Paused: false,
            Scrub: 20,
            Duration: 60,
            VolumeLevel: 0.5,
        };
    };

    let rmFakeVideoPlayer = () => {
        store.MediaInfo = {
            Active: false,
            Name: 'None',
            Paused: true,
            Scrub: 0,
            Duration: 1,
            VolumeLevel: 0,
        };
    };

    let containerClass = 'flex flex-wrap';

    return (
            <MenuContent name={'Debug'}>
                <div className={'flex'}>

                    <div className={'w-1/2'}>
                        <ul>
                            {Object.entries(store).map(info => {
                                return <li className={'mb-2'} key={info[0]}>
                                    <span className={'font-bold'}>{info[0]}</span>{': '}<span
                                        className={'text-gray-400'}>{showInfo(info)}</span>
                                </li>;
                            })}
                        </ul>
                    </div>

                    <div className={'w-1/2'}>

                        <div>Host State</div>
                        <div className={containerClass}>
                            <Button handleClick={() => {
                                setNetState(store, 'Neutral');
                            }} className={grayButtonClass}>Neutral
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, 'HostWaiting');
                            }} className={grayButtonClass}>HostWaiting
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, 'Hosting');
                            }} className={grayButtonClass}>Hosting
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, 'ClientConnected');
                            }} className={grayButtonClass}>ClientConnected
                            </Button>


                        </div>

                        <div>Connection</div>
                        <div className={containerClass}>
                            <Button className={grayButtonClass} handleClick={() => {
                                addFakeIpPort(store);
                            }}>+ fake ip/port
                            </Button>
                            <Button className={grayButtonClass} handleClick={() => {
                                rmFakeIpPort(store);
                            }}>- fake ip/port
                            </Button>
                            <Button handleClick={() => {
                                addFakeClient(store);
                            }} className={grayButtonClass}>+ fake client
                            </Button>
                            <Button handleClick={() => {
                                rmFakeClient(store);
                            }} className={grayButtonClass}>- fake client
                            </Button>
                            <UpButton handleClick={() => {
                                postJoinRoom({
                                    id: '',
                                    ip_address: '192.168.1.117',
                                    network_address: '',
                                    port: 0,
                                    pinged: 0,
                                });
                            }} className={grayButtonClass}>join hard coded
                            </UpButton>
                        </div>

                        <div>Room Status</div>

                        <div className={containerClass}>
                            <Button handleClick={() => {
                                toggleRoomOpen(store);
                            }} className={grayButtonClass}>
                                toggle
                            </Button>
                        </div>
                        <div>Player</div>
                        <div className={containerClass}>
                            <Button handleClick={rmFakeVideoPlayer}
                                    className={grayButtonClass}>none</Button>
                            <Button handleClick={addFakeVideoPlayerPlaying}
                                    className={grayButtonClass}>playing</Button>
                            <Button handleClick={addFakeVideoPlayerPaused}
                                    className={grayButtonClass}>paused</Button>
                        </div>

                    </div>

                </div>
            </MenuContent>
    );
});

const SettingsPage = observer(() => {
    let {store} = useStore();

    let [about, setAbout] = useState(false);

    function handleClickVibes() {
        postLightsChange('vibes');
    }

    function handleClickBright() {
        postLightsChange('bright');
    }

    function handleClickPinchPull() {
        postTogglePinchPull();
    }

    function handleClickBlockBreak() {
        postToggleBlockBreak();

    }

    function toggleAbout() {
        setAbout(!about);
    }

    if (about) {
        return <AboutPage handleClickReturn={toggleAbout}/>;
    }

    return <MenuContent name={'Settings'}>
        <InfoItem title={'Lights'} slug={'Set the mood'}
                  imgSrc={LightImg}>
            <div className={'flex space-x-2'}>
                <Button handleClick={handleClickVibes}
                        className={grayButtonClass}>Vibes</Button>
                <Button handleClick={handleClickBright}
                        className={grayButtonClass}>Bright</Button>

            </div>
        </InfoItem>
        <div className={'text-xl'}>
            Experimental
        </div>
        <InfoItem title={'Pinch Pull'}
                  slug={'Point at object with pinched fingers'}
        >
            <ToggleButton
                    classEnabled={greenButtonClass}
                    classDisabled={grayButtonClass}
                    enabled={store.ExperimentalInfo.PinchPullEnabled}
                    handleClick={handleClickPinchPull}
            >
                Toggle
            </ToggleButton>
        </InfoItem>
        <InfoItem title={'Block Break'}
                  slug={'Delete blocks by touching them (right index finger)'}>
            <ToggleButton
                    classEnabled={greenButtonClass}
                    classDisabled={grayButtonClass}
                    enabled={store.ExperimentalInfo.BlockBreakEnabled}
                    handleClick={handleClickBlockBreak}
            >
                Toggle
            </ToggleButton>
        </InfoItem>
        <div className={'text-xl'}>
            Information
        </div>
        <InfoItem title={'Version'}
                  slug={store.AppInfo.Version + 'b' + store.AppInfo.BuildId}>
            <Button
                    className={grayButtonClass}
                    handleClick={toggleAbout}
            >
                About
            </Button>
        </InfoItem>
    </MenuContent>;
});

function AboutPage({handleClickReturn}) {
    // 0 : main
    // 1 : MPL
    // 2 : LGPL
    let [view, setView] = useState(0);

    function viewMain() {
        setView(0);
    }

    function viewMpl() {
        setView(1);
    }

    function viewLgpl() {
        setView(2);
    }

    function viewApache() {
        setView(3);
    }

    if (view === 1) {
        return <MozillaPublicLicense handleClickReturn={viewMain}/>;
    }

    if (view === 2) {
        return <LesserGlpl handleClickReturn={viewMain}/>;
    }

    if (view === 3) {
        return <ApacheLicense handleClickReturn={viewMain}/>;
    }

    return (
            <MenuContent name={'About'}>
                <div className={'flex'}>
                    <Button className={grayButtonClass} handleClick={handleClickReturn}>
                        Return to Settings
                    </Button>
                </div>
                <div className={'text-xl'}>
                    Credits
                </div>
                <InfoItem title={'GeckoView'} slug={'Mozilla Public License'}>
                    <Button className={grayButtonClass} handleClick={viewMpl}>
                        View
                    </Button>
                </InfoItem>
                <InfoItem title={'PDF.js'} slug={'Apache License'}>
                    <Button className={grayButtonClass} handleClick={viewApache}>
                        View
                    </Button>
                </InfoItem>
                <InfoItem title={'AdGuard AdBlocker'} slug={'GNU Lesser General Public License'}>
                    <Button className={grayButtonClass} handleClick={viewLgpl}>
                        View
                    </Button>
                </InfoItem>
            </MenuContent>
    );
}

function MozillaPublicLicense({handleClickReturn}) {
    return (
            <MenuContent name={'Mozilla Public License Version 2.0'}>
                <div className={'flex'}>
                    <Button className={grayButtonClass} handleClick={handleClickReturn}>
                        Return
                    </Button>
                </div>
                <div dangerouslySetInnerHTML={{__html: mpl}}/>
            </MenuContent>
    );
}

function LesserGlpl({handleClickReturn}) {
    return (
            <MenuContent name={'GNU LESSER GENERAL PUBLIC LICENSE'}>
                <div className={'flex'}>
                    <Button className={grayButtonClass} handleClick={handleClickReturn}>
                        Return
                    </Button>
                </div>
                <div dangerouslySetInnerHTML={{__html: lgpl}}/>
            </MenuContent>
    );
}

function ApacheLicense({handleClickReturn}) {
    return (
            <MenuContent name={'APACHE LICENSE, VERSION 2.0'}>
                <div className={'flex'}>
                    <Button className={grayButtonClass} handleClick={handleClickReturn}>
                        Return
                    </Button>
                </div>
                <div dangerouslySetInnerHTML={{__html: apache}}/>
            </MenuContent>
    );
}

//

let Menu = observer(() => {

    let {store, pushStore} = useStore();

    let [active, setActive] = useState(0);

    let navHome = () => {
        setActive(0);
    };

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
                    console.log(`Got room ${tag} ${secret}`)
                    pushStore({RoomSecret: secret})
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

    let pages = [
        {name: 'Home', component: HomePage},
        {name: 'Join Desk', component: JoinDeskPage},
        {name: 'Videos', component: VideosPage},
        {name: 'Settings', component: SettingsPage},
    ];

    if (store.MediaInfo.Active) {
        pages.push({name: 'Player', component: PlayerPage});

    }

    if (store.AppInfo.Build === 'DEVELOPMENT') {
        pages.push({name: 'Debug', component: DebugPage});
    }

    let SelectedPage;
    if (active > pages.length - 1) {
        setActive(0);
        SelectedPage = pages[0].component;
    } else {
        SelectedPage = pages[active].component;
    }

    let joinDeskActive = store.NetworkInfo.Mode === NetworkManagerMode.Host && !store.NetworkInfo.RoomOpen;

    if (!store.AppInfo.MicrophonePermission) {
        return <NoMicPage/>;
    }

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
                        {pages.map((info, i) => {
                            if (info.name.toLowerCase() === 'join desk' && !joinDeskActive) {
                                return <ListItem key={info.name}
                                                 inactive={true}>{info.name}</ListItem>;
                            }
                            if (info.name.toLowerCase() === 'player') {
                                const buttonClass = 'text-white py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center border-4 border-green-400';
                                const buttonClassSelected = 'py-4 px-8 bg-blue-700 text-white rounded cursor-pointer flex flex-wrap content-center border-4 border-green-400';

                                return <ListItem
                                        buttonClass={buttonClass}
                                        buttonClassSelected={buttonClassSelected}
                                        key={info.name} handleClick={() => {
                                    setActive(i);
                                }} selected={active === i}>{info.name}</ListItem>;

                            }
                            return <ListItem key={info.name} handleClick={() => {
                                setActive(i);
                            }} selected={active === i}>{info.name}</ListItem>;
                        })}
                    </SettingsList>
                    <div className={'w-full p-2'}>
                        <ExitButton/>
                    </div>
                </div>
                <div className={'bg-gray-900 z-10 w-full overflow-auto scroll-host'}>
                    <SelectedPage navHome={navHome}/>
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
