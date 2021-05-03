import React from 'react';
import axios from 'axios';
import {observer} from 'mobx-react-lite';
import {BeatLoader, BounceLoader} from 'react-spinners';
import {Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';

import DoorOpen from '../static/door-open.svg';
import HashImg from '../static/hash.svg';
import LinkImg from '../static/link.svg';
import ThinkingFace from '../static/thinking-face.svg';
import {grayButtonClass, grayButtonClassInert, greenButtonClass, redButtonClass} from '../cssClasses';
import {InfoItem} from '../components/InfoItem';
import {Button, ForwardButton} from '../components/Button';
import {MenuContent} from '../components/MenuContent';
import {apiBase} from '../utilities';
import {postCloseRoom, postKickConnectionId, postLeaveRoom, postOpenPrivateRoom, postOpenPublicRoom} from '../api';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {JoinDeskPage} from './JoinDesk';

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

function OpenRoomItem() {
    return <InfoItem title={'Open Your Room'} slug={'Let people join you'} imgSrc={DoorOpen}>
        <div className={'flex space-x-4'}>
            <Button className={grayButtonClass} handleClick={postOpenPrivateRoom}>
                Private
            </Button>
            <Button className={greenButtonClass} handleClick={postOpenPublicRoom}>
                Public
            </Button>
        </div>
    </InfoItem>;
}

function JoinDeskItem() {

    let history = useHistory();
    let match = useRouteMatch();

    function onClick() {
        history.push(`${match.path}/join-desk`);
    }

    return <InfoItem title={'Join Room'} slug={'Using a room code'} imgSrc={HashImg}>
        <ForwardButton onClick={onClick}/>
    </InfoItem>;

}

const CloseRoomItem = observer(() => {
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

    return <InfoItem title={'Room'} slug={'Ready to accept connections'}
                     imgSrc={DoorOpen}>
        <Button className={redButtonClass} handleClick={handleCloseRoom}>
            Close
        </Button>
    </InfoItem>;
});

const DeskCodeItem = observer(() => {
    let {store} = useStore();
    const roomCodeCLass = 'text-5xl ';
    return <InfoItem title={'Desk Code'}
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
    </InfoItem>;
});

const RoomInfo = observer(() => {
    let {store} = useStore();

    if (store.NetworkInfo.RoomOpen) {
        return (
                <React.Fragment>
                    <CloseRoomItem/>
                    <DeskCodeItem/>
                </React.Fragment>
        );
    } else {
        return (
                <React.Fragment>
                    <OpenRoomItem/>
                    <JoinDeskItem/>
                </React.Fragment>
        );
    }

});

export const HostHomePage = observer(() => {

    let {store} = useStore();

    return <MenuContent name={'Home'}>
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
    </MenuContent>;
});

function LoadingHomePage() {
    return <div className={'flex justify-center w-full flex-wrap content-center h-screen'}>
        <BounceLoader size={100} color={'#737373'}/>
    </div>;
}

function ClientHomePage() {
    return <MenuContent name={'Client Connected'}>
        <div className={'flex'}>
            <InfoItem title={'Connected'} slug={'You are connected to a host'}
                      imgSrc={LinkImg}>
                <Button handleClick={postLeaveRoom}
                        className={redButtonClass}>Exit</Button>
            </InfoItem>
        </div>

    </MenuContent>;
}

export const HomePage = observer(() => {
    let {store} = useStore();
    let match = useRouteMatch();

    let Inner;

    switch (store.NetworkInfo.Mode) {
        case NetworkManagerMode.Host:
            Inner = HostHomePage;
            break;
        case NetworkManagerMode.ClientOnly:
            Inner = ClientHomePage;
            break;
        default:
            Inner = LoadingHomePage;
            break;
    }

    return <Switch>
        <Route exact path={`${match.path}`} component={Inner}/>
        <Route path={`${match.path}/join-desk`} component={JoinDeskPage}/>
    </Switch>;
});
