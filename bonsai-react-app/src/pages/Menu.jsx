import React, {useEffect} from 'react';
import {Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import axios from 'axios';
import {observer} from 'mobx-react-lite';
import {autorun} from 'mobx';

import DotsImg from '../static/dots-vertical.svg';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {postCloseMenu, postRequestMicrophone} from '../api';

import {apiBase} from '../utilities';
import {InstantButton} from '../components/Button';
import './Menu.css';
import {DebugPage} from './Debug';
import {SettingsPage} from './Settings';
import {HomePage} from './Home';
import {PlayerPage} from './Player';
import PublicRoomsPage from './PublicRooms';
import {YourRoom} from './YourRoom';
import {BlocksPage} from './Blocks';

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
                            <InstantButton className={className} onClick={handleClick}>Request</InstantButton>
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

function ExitButton() {

    let buttonClass = 'rounded h-16 py-4 px-8 bg-bonsai-brown hover:bg-bonsai-orange active:bg-red-600 hover:text-white cursor-pointer flex flex-wrap content-center';

    return (
            <NavItem buttonClass={buttonClass} handleClick={postCloseMenu}
                     className={'text-white'}>
                <span className={'text-white'}>Close Menu</span>
            </NavItem>
    );

}

function NavItem(props) {
    let {
        handleClick,
        inactive = false,
        buttonClassSelected = '',
        buttonClass = '',
        buttonClassInactive = '',
        to = '',
        unread = false,
    } = props;

    let history = useHistory();

    buttonClass = buttonClass ?
            buttonClass :
            'py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center';
    buttonClassSelected = buttonClassSelected ?
            buttonClassSelected :
            'py-4 px-8 bg-bonsai-green text-white rounded cursor-pointer flex flex-wrap content-center';
    buttonClassInactive = buttonClassInactive ?
            buttonClassInactive :
            'py-4 px-8 bg-gray-800 rounded cursor-pointer flex flex-wrap content-center';

    let selected = window.location.pathname === to;

    let textClass = selected ? 'text-white' : 'text-gray-300';

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
                <InstantButton className={className} onClick={() => {
                    history.push(to);
                }}>
                    <div className={'w-full flex flex-wrap justify-between content-center'}>
                        <span className={textClass}>{props.children}</span>
                        {unread ? <div className={'mt-2 w-3 h-3 bg-gray-200 rounded-full'}/> : ''}
                    </div>
                </InstantButton>
        );

    }

    return (
            <InstantButton className={className} onClick={handleClick}>
                {props.children}
            </InstantButton>
    );
}

function NavList(props) {
    return (
            <div className={'space-y-1 px-2'}>
                {props.children}
            </div>);

}

function NavTitle(props) {
    return <div
            className={'text-white font-bold text-xl px-5 pt-5 pb-2'}>{props.children}</div>;
}

let Menu = observer(() => {
    console.log('menu');
    let {store, mediaInfo} = useStore();

    let debug = store.AppInfo.Build === 'DEVELOPMENT';

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
            const publicRoom = store.NetworkInfo.PublicRoom ? 1 : 0;

            if (roomCode && (!networkAddress || !roomOpen)) {
                console.log('Remove room code');
                store.RoomCode = null;
                return;
            }

            // send ip/port out for a room code
            if (roomOpen && !roomCode && !loadingRoomCode && networkAddress) {
                console.log('fetch room code');
                store.LoadingRoomCode = true;
                let url = apiBase(store) + '/rooms';
                let data = `network_address=${networkAddress}&username=${userName}&version=${version}&public_room=${publicRoom}`;
                axios(
                        {
                            method: 'post',
                            url: url,
                            data: data,
                            header: {'content-type': 'application/x-www-form-urlencoded'},
                        },
                ).then(response => {
                    let tag = response.data.tag;
                    let secret = response.data.secret;

                    console.log(`Got room ${tag} ${secret}`);
                    store.RoomSecret = secret;
                    store.RoomCode = tag;
                    store.LoadingRoomCode = false;
                }).catch(err => {
                    console.log(err);
                    store.LoadingRoomCode = false;
                });
            }
        });

    });

    useEffect(() => {
        return () => {
            store.RoomCode = null;
        };
    }, [store]);

    if (!store.AppInfo.MicrophonePermission) {
        return <NoMicPage/>;
    }

    let mediaButtonClass = '';
    let mediaButtonClassSelected = '';

    if (mediaInfo.Active) {
        mediaButtonClass = 'py-4 px-8 hover:bg-gray-800 active:bg-gray-900 hover:text-white rounded cursor-pointer flex flex-wrap content-center';
        mediaButtonClassSelected = 'py-4 px-8 bg-bonsai-green text-white rounded cursor-pointer flex flex-wrap content-center';
    }

    let homeActive = store.NetworkInfo.RoomOpen || store.NetworkInfo.Mode === NetworkManagerMode.ClientOnly;

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
                        <NavTitle>
                            Menu
                        </NavTitle>
                    </div>

                    <div className={'h-16'}/>
                    <NavList>
                        <NavItem to={'/menu/home'} unread={homeActive}>Home</NavItem>
                        <NavItem to={'/menu/public-rooms'}>Public Rooms</NavItem>
                        <NavItem to={'/menu/blocks'}>Blocks</NavItem>
                        <NavItem to={'/menu/player'}
                                 buttonClass={mediaButtonClass}
                                 buttonClassSelected={mediaButtonClassSelected}
                                 unread={mediaInfo.Active}
                        >

                            Media
                        </NavItem>
                        <NavItem to={'/menu/room'}>Lights & Layout</NavItem>
                        <NavItem to={'/menu/settings'}>Settings</NavItem>
                        {debug ?
                                <NavItem to={'/menu/debug'} component={DebugPage}>Debug</NavItem>
                                : ''
                        }
                    </NavList>
                    <div className={'w-full p-2'}>
                        <ExitButton/>
                    </div>
                </div>

                <div className={'bg-gray-900 z-10 w-full overflow-auto scroll-host'}>
                    <Switch>
                        <Route path={`${match.path}/home`} component={HomePage}/>
                        <Route path={`${match.path}/room`} component={YourRoom}/>
                        <Route path={`${match.path}/settings`} component={SettingsPage}/>
                        <Route path={`${match.path}/debug`} component={DebugPage}/>
                        <Route path={`${match.path}/blocks`} component={BlocksPage}/>
                        <Route path={`${match.path}/player`} component={PlayerPage}/>
                        <Route path={`${match.path}/public-rooms`} component={PublicRoomsPage}/>
                        <Route path={`${match.path}`}>Page not found</Route>
                    </Switch>
                </div>

            </div>
    );
});

export default Menu;
