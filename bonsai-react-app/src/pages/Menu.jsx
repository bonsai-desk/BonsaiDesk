import React, {useEffect, useState, useRef} from 'react';
import './Menu.css';
import {postJson} from '../utilities';
import {Button, ToggleButton} from '../components/Button';
import axios from 'axios';
import DoorOpen from '../static/door-open.svg';
import LinkImg from '../static/link.svg';
import LightImg from '../static/lightbulb.svg';
import PauseImg from '../static/pause.svg';
import PlayImg from '../static/play.svg';

import MinusImg from '../static/minus.svg';
import PlusImg from '../static/plus.svg';

import EjectImg from '../static/eject-fill.svg';
import {KeySVG} from '../components/Keys';
import YtImg from '../static/yt-small.png';
import ThinkingFace from '../static/thinking-face.svg';
import {useStore} from '../DataProvider';
import {BeatLoader, BounceLoader} from 'react-spinners';
import {observer} from 'mobx-react-lite';
import {action, autorun} from 'mobx';

const API_BASE = 'https://api.desk.link';

const roundButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center';
const redButtonClass = 'py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center';
const greenButtonClass = 'py-4 px-8 font-bold bg-green-800 active:bg-green-700 hover:bg-green-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClass = 'py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClassInert = 'py-4 px-8 font-bold bg-gray-800 rounded flex flex-wrap content-center';

// post data

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

function postVolumeIncrement() {
    postJson({Type: 'command', Message: 'volumeIncrement'});
}

function postVolumeDecrement() {
    postJson({Type: 'command', Message: 'volumeDecrement'});
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

function postLightsChange(level) {
    postJson({Type: 'command', Message: 'lightsChange', Data: level});
}

// utils

function showInfo(info) {
    switch (info[0]) {
        case 'player_info':
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
                <div className={'space-y-8'}>
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
        axios({
            method: 'delete',
            url: API_BASE + '/rooms/' + store._room_code,
        }).then(r => {
            if (r.status === 200) {
                console.log(`deleted room ${store._room_code}`);
            }
        }).catch(console.log);
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

    if (store.room_open) {
        return (
                <React.Fragment>
                    {CloseRoom}
                    <InfoItem title={'Desk Code'}
                              slug={'People who have this can join you'}
                              imgSrc={LinkImg}>
                        <div className={'h-20 flex flex-wrap content-center'}>
                            {store.room_code ?
                                    <div className={roomCodeCLass}>{store.room_code}</div>

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
                {store.player_info.length > 0 && store.room_open ?
                        <React.Fragment>
                            <div className={'text-xl'}>People in Your Room</div>
                            <div className={'flex space-x-2'}>
                                {store.player_info.map(info => <ConnectedClient info={info}/>)}
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
    //const store = {media_info: {Active:true, Scrub: 10, Duration: 33}}
    const ref = useRef(null);

    let media = store.media_info;

    const pct = 100 * media.Scrub / media.Duration;

    if (!store.media_info.Active) {
        return '';
    }

    function handleClick(e) {
        let pct = (e.clientX - ref.current.offsetLeft) / ref.current.offsetWidth;
        let ts = pct * store.media_info.Duration;
        postSeekPlayer(ts);
    }

    function VolumeDecrement() {
        postVolumeDecrement();
    }

    function VolumeIncrement() {
        postVolumeIncrement();
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

    let mediaClass = 'flex rounded h-16 w-24';
    mediaClass = grayButtonClass;

    return <MenuContent name={'Player'}>
        <div>Video Scrub</div>
        <div className={'flex space-x-2'}>
            {store.media_info.Paused ?
                    <KeySVG handleClick={handlePlay} imgSrc={PlayImg}/>
                    :
                    <KeySVG handleClick={handlePause} imgSrc={PauseImg}/>
            }

            <div ref={ref} onPointerDown={handleClick}
                 className={'relative bg-gray-600 rounded w-full'}>

                <div style={{width: pct + '%'}}
                     className={'h-full bg-gray-400 rounded'}/>

            </div>
            <KeySVG handleClick={handleEject} imgSrc={EjectImg}
                    className={mediaClass}/>

        </div>
        <div>Volume {parseInt(100 * media.VolumeLevel)}</div>
        <div className={'flex space-x-2'}>
            <KeySVG handleClick={VolumeDecrement} className={mediaClass}
                    imgSrc={MinusImg}/>
            <KeySVG handleClick={VolumeIncrement} className={mediaClass}
                    imgSrc={PlusImg}/>
        </div>
    </MenuContent>;

});

const HomePage = observer(() => {

    let {store} = useStore();

    let Inner;

    switch (store.network_state) {
        case 'Neutral':
        case 'HostWaiting':
        case 'Hosting':
            Inner = <HostHomePage/>;
            break;
        case 'ClientConnected':
            Inner = <ClientHomePage/>;
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
            let url = API_BASE + `/rooms/${code}`;
            axios({
                method: 'get',
                url: url,
            }).then(response => {
                console.log(response.data);
                console.log(store.ip_address, store.port);
                if (response.data.ip_address.toString() ===
                        store.ip_address.toString() &&
                        response.data.port.toString() === store.port.toString()) {
                    // trying to join your own room
                    setMessage(`Could not find ${code} try again`);
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
    }, [code, navHome, posting, store.ip_address, store.port]);

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
        store.ip_address = 1234;
        store.port = 4321;
    });
    let rmFakeIpPort = action(store => {
        store.ip_address = null;
        store.port = null;
    });

    let setNetState = action((store, netState) => {
        store.network_state = netState;
    });

    let addFakeClient = action(store => {
        if (store.player_info.length > 0) {
            store.player_info.push({Name: 'cam', ConnectionId: 1});
        } else {
            store.player_info.push(
                    {Name: 'loremIpsumLoremIpsumLorem', ConnectionId: 0});
        }
    });
    let rmFakeClient = action(store => {
        store.player_info.pop();
    });

    let toggleRoomOpen = action(store => {
        store.room_open = !store.room_open;
    });

    let addFakeVideoPlayerPaused = () => {
        store.media_info = {
            Active: true,
            Name: 'Video Name',
            Paused: true,
            Scrub: 20,
            Duration: 60,
            VolumeLevel: 0.5,
        };
    };

    let addFakeVideoPlayerPlaying = () => {
        store.media_info = {
            Active: true,
            Name: 'Video Name',
            Paused: false,
            Scrub: 20,
            Duration: 60,
            VolumeLevel: 0.5,
        };
    };

    let rmFakeVideoPlayer = () => {
        store.media_info = {
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
                    enabled={store.experimental_info.PinchPullEnabled}
                    handleClick={handleClickPinchPull}
            >
                toggle
            </ToggleButton>
        </InfoItem>
        <InfoItem title={'Block Break'}
                  slug={'Delete blocks by touching them (right index finger)'}>
            <ToggleButton
                    classEnabled={greenButtonClass}
                    classDisabled={grayButtonClass}
                    enabled={store.experimental_info.BlockBreakEnabled}
                    handleClick={handleClickBlockBreak}
            >
                toggle
            </ToggleButton>
        </InfoItem>
        <div className={'text-xl'}>
            Code Bundle: {store.app_info.BundleVersionCode}
        </div>
    </MenuContent>;
});

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
            if (store.room_code &&
                    (!store.ip_address || !store.port || !store.room_open)) {
                console.log('rm room code');
                pushStore({room_code: null});
                return;
            }

            // send ip/port out for a room code
            if (store.room_open && !store.room_code && !store.loading_room_code &&
                    store.ip_address &&
                    store.port) {
                console.log('fetch room code');
                pushStore({loading_room_code: true});
                let url = API_BASE + '/rooms';
                axios(
                        {
                            method: 'post',
                            url: url,
                            data: `ip_address=${store.ip_address}&port=${store.port}`,
                            header: {'content-type': 'application/x-www-form-urlencoded'},
                        },
                ).then(response => {
                    pushStore({room_code: response.data.tag, loading_room_code: false});
                }).catch(err => {
                    console.log(err);
                    pushStore({loading_room_code: false});
                });
            }
        });

    });

    useEffect(() => {
        return () => {
            pushStore({room_code: null});
        };
    }, [pushStore]);

    let pages = [
        {name: 'Home', component: HomePage},
        {name: 'Join Desk', component: JoinDeskPage},
        {name: 'Videos', component: VideosPage},
        {name: 'Settings', component: SettingsPage},
    ];

    if (store.media_info.Active) {
        pages.push({name: 'Player', component: PlayerPage});

    }

    if (store.build === 'DEVELOPMENT') {
        pages.push({name: 'Debug', component: DebugPage});
    }

    let SelectedPage;
    if (active > pages.length - 1) {
        setActive(0);
        SelectedPage = pages[0].component;
    } else {
        SelectedPage = pages[active].component;
    }

    let joinDeskActive = store.network_state === 'Hosting' && !store._room_open;

    return (
            <div className={'flex text-lg text-gray-500 h-full static'}>
                {!store.is_internet_good ?
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

export default Menu;
