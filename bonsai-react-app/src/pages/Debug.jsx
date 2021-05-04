import {observer} from 'mobx-react-lite';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {action} from 'mobx';
import {MenuContent} from '../components/MenuContent';
import {showInfo} from '../esUtils';
import {InstantButton} from '../components/Button';
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
                            <InstantButton onClick={() => {
                                toggleRoomOpen(store);
                            }} className={grayButtonClass}>
                                toggle
                            </InstantButton>
                        </div>

                        <div>Host State</div>
                        <div className={containerClass}>
                            <InstantButton onClick={() => {
                                setNetState(store, NetworkManagerMode.Offline);
                            }} className={grayButtonClass}>
                                Offline
                            </InstantButton>
                            <InstantButton onClick={() => {
                                setNetState(store, NetworkManagerMode.ServerOnly);
                            }} className={grayButtonClass}>
                                Server Only
                            </InstantButton>
                            <InstantButton onClick={() => {
                                setNetState(store, NetworkManagerMode.ClientOnly);
                            }} className={grayButtonClass}>
                                Client Only
                            </InstantButton>
                            <InstantButton onClick={() => {
                                setNetState(store, NetworkManagerMode.Host);
                            }} className={grayButtonClass}>
                                Host
                            </InstantButton>


                        </div>

                        <div>Connection</div>
                        <div className={containerClass}>
                            <InstantButton onClick={() => {
                                addFakeClient(store);
                            }} className={grayButtonClass}>+ fake client
                            </InstantButton>
                            <InstantButton onClick={() => {
                                rmFakeClient(store);
                            }} className={grayButtonClass}>- fake client
                            </InstantButton>
                        </div>

                        <div>Player</div>
                        <div className={containerClass}>
                            <InstantButton onClick={rmFakeVideoPlayer}
                                    className={grayButtonClass}>none</InstantButton>
                            <InstantButton onClick={addFakeVideoPlayerPlaying}
                                    className={grayButtonClass}>playing</InstantButton>
                            <InstantButton onClick={addFakeVideoPlayerPaused}
                                    className={grayButtonClass}>paused</InstantButton>
                        </div>

                    </div>

                </div>
            </MenuContent>
    );
});