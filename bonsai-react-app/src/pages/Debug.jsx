import {observer} from 'mobx-react-lite';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {action} from 'mobx';
import {MenuContent} from '../components/MenuContent';
import {showInfo} from '../esUtils';
import {Button} from '../components/Button';
import {grayButtonClass} from '../cssClasses';
import React from 'react';

export const DebugPage = observer(() => {
    let {store} = useStore();

    let setNetState = action((store, netState) => {
        store.NetworkInfo.Mode = netState;
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
        store.NetworkInfo.RoomOpen = !store.NetworkInfo.RoomOpen;
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

                        <div>Room Status</div>
                        <div className={containerClass}>
                            <Button handleClick={() => {
                                toggleRoomOpen(store);
                            }} className={grayButtonClass}>
                                toggle
                            </Button>
                        </div>

                        <div>Host State</div>
                        <div className={containerClass}>
                            <Button handleClick={() => {
                                setNetState(store, NetworkManagerMode.Offline);
                            }} className={grayButtonClass}>
                                Offline
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, NetworkManagerMode.ServerOnly);
                            }} className={grayButtonClass}>
                                Server Only
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, NetworkManagerMode.ClientOnly);
                            }} className={grayButtonClass}>
                                Client Only
                            </Button>
                            <Button handleClick={() => {
                                setNetState(store, NetworkManagerMode.Host);
                            }} className={grayButtonClass}>
                                Host
                            </Button>


                        </div>

                        <div>Connection</div>
                        <div className={containerClass}>
                            <Button handleClick={() => {
                                addFakeClient(store);
                            }} className={grayButtonClass}>+ fake client
                            </Button>
                            <Button handleClick={() => {
                                rmFakeClient(store);
                            }} className={grayButtonClass}>- fake client
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