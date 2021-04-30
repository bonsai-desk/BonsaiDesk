import React, {useEffect} from 'react';
import {Link, Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import './Menu.css';
import {apiBase} from '../utilities';
import {Button} from '../components/Button';
import axios from 'axios';

import DotsImg from '../static/dots-vertical.svg';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {observer} from 'mobx-react-lite';
import {autorun} from 'mobx';
import {postCloseMenu, postRequestMicrophone} from '../api';
import {DebugPage} from './Debug';
import {VideosPage} from './Videos';
import {SettingsPage} from './Settings';
import {HomePage, JoinDeskPage} from './Home';
import {PlayerPage} from './Player';

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

function ExitButton() {

    let buttonClass = 'rounded h-16 py-4 px-8 bg-red-800 hover:bg-red-700 active:bg-red-600 hover:text-white cursor-pointer flex flex-wrap content-center';

    return (
            <NavItem buttonClass={buttonClass} handleClick={postCloseMenu}
                     className={'text-white'}>
                <span className={'text-white'}>Close Menu</span>
            </NavItem>
    );

}

function NavItem(props) {
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
                        <NavTitle>
                            Menu
                        </NavTitle>
                    </div>

                    <div className={'h-16'}/>
                    <NavList>
                        <NavItem to={'/menu/home'}>Home</NavItem>
                        {store.MediaInfo.Active ?
                                <NavItem to={'/menu/player'}
                                          buttonClass={playerButtonClass}
                                          buttonClassSelected={playerButtonClassSelected}>
                                    Player
                                </NavItem> : ''}
                        <NavItem to={'/menu/join-desk'} inactive={!joinDeskActive}>Join Desk</NavItem>
                        <NavItem to={'/menu/videos'}>Videos</NavItem>
                        <NavItem to={'/menu/settings'}>Settings</NavItem>

                        <NavItem to={'/menu/debug'} component={DebugPage}>Debug</NavItem>
                    </NavList>
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

export default Menu;
