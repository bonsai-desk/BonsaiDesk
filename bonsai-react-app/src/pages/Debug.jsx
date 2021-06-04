import {observer} from 'mobx-react-lite';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {action} from 'mobx';
import {MenuContent} from '../components/MenuContent';
import {showInfo} from '../esUtils';
import {InstantButton} from '../components/Button';
import {grayButtonClass} from '../cssClasses';
import React from 'react';
import {Layout, postSetLayout} from '../api';
import axios from 'axios';

export const DebugPage = observer(() => {
    let {store, mediaInfo} = useStore();

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
        store.NetworkInfo.RoomOpen = !store.NetworkInfo.RoomOpen;
    });

    let toggleRoomPublic = action(store => {
        store.NetworkInfo.PublicRoom = !store.NetworkInfo.PublicRoom;
    });

    let toggleRoomFull = action(store => {
        store.NetworkInfo.Full = !store.NetworkInfo.Full;
    });

    let setLayoutAcross = () => {
        postSetLayout(Layout.Across);
    };

    let setLayoutSideBySide = () => {
        postSetLayout(Layout.SideBySide);
    };

    let addFakeVideoPlayerPaused = () => {
        mediaInfo.Active = true;
        mediaInfo.Name = 'Video Name';
        mediaInfo.Paused = true;
        mediaInfo.Scrub = 20;
        mediaInfo.Duration = 60;
        mediaInfo.VolumeLevel = 0.5;
    };

    let addFakeVideoPlayerPlaying = () => {
        mediaInfo.Active = true;
        mediaInfo.Name = 'Video Name';
        mediaInfo.Paused = false;
        mediaInfo.Scrub = 20;
        mediaInfo.Duration = 60;
        mediaInfo.VolumeLevel = 0.5;
    };

    let rmFakeVideoPlayer = () => {
        mediaInfo.Active = false;
        mediaInfo.Name = 'None';
        mediaInfo.Paused = true;
        mediaInfo.Scrub = 0;
        mediaInfo.Duration = 1;
        mediaInfo.VolumeLevel = 0;
    };

    let postAuthTest = () => {
        axios({
            method: 'POST',
            url: store.ApiBase + '/auth_test',
            data: `token=${store.BonsaiToken}`,
            headers: {'content-type': 'application/x-www-form-urlencoded'},
        }).catch(err => console.log).then(response => {
            console.log(response);
        });

    };

    let containerClass = 'flex flex-wrap';

    return (
            <MenuContent name={'Debug'}>
                <div className={'flex'}>

                    <div className={'w-1/2'}>
                        <div className={'text-3xl'}>Store</div>
                        <ul>
                            {Object.entries(store).map(info => {
                                return <li className={'mb-2'} key={info[0]}>
                                    <span className={'font-bold'}>{info[0]}</span>{': '}<span
                                        className={'text-gray-400'}>{showInfo(info)}</span>
                                </li>;
                            })}
                        </ul>
                        <div className={'text-3xl'}>Media Info</div>
                        <ul>
                            {Object.entries(mediaInfo).map(info => {
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
                                toggle open
                            </InstantButton>
                            <InstantButton onClick={() => {
                                toggleRoomPublic(store);
                            }} className={grayButtonClass}>
                                toggle public
                            </InstantButton>
                            <InstantButton onClick={() => {
                                toggleRoomFull(store);
                            }} className={grayButtonClass}>
                                toggle full
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

                        <div>Layout</div>
                        <div className={containerClass}>
                            <InstantButton onClick={setLayoutAcross} className={grayButtonClass}>across</InstantButton>
                            <InstantButton onClick={setLayoutSideBySide}
                                           className={grayButtonClass}>side</InstantButton>
                        </div>
                        <div className={containerClass}>
                            <InstantButton onClick={postAuthTest} className={grayButtonClass}>auth test</InstantButton>
                        </div>

                    </div>

                </div>
            </MenuContent>
    );
});