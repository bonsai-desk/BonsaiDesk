import React, {useEffect, useState} from 'react';
import axios from 'axios';
import {observer} from 'mobx-react-lite';
import {BeatLoader, BounceLoader} from 'react-spinners';
import {useHistory, useRouteMatch} from 'react-router-dom';

import DoorOpen from '../static/door-open.svg';
import ForwardImg from "../static/forward.svg"
import LinkImg from '../static/link.svg';
import ThinkingFace from '../static/thinking-face.svg';
import {grayButtonClass, grayButtonClassInert, greenButtonClass, redButtonClass, roundButtonClass} from '../cssClasses';
import {InfoItem} from '../components/InfoItem';
import {Button} from '../components/Button';
import {MenuContent} from '../components/MenuContent';
import {apiBase} from '../utilities';
import {
    postCloseRoom,
    postJoinRoom,
    postKickConnectionId,
    postLeaveRoom, postOpenPrivateRoom,
    postOpenPublicRoom,
} from '../api';
import {NetworkManagerMode, useStore} from '../DataProvider';

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
        <div className={"flex space-x-4"}>
            <Button className={grayButtonClass} handleClick={postOpenPrivateRoom}>
                Private
            </Button>
            <Button className={greenButtonClass} handleClick={postOpenPublicRoom}>
                Public
            </Button>
        </div>
    </InfoItem>;
}

function JoinDeskItem () {

    let history = useHistory();
    
    function onClick () {
        
        history.push("/menu/join-desk")
    }
    
    return <InfoItem title={"Join Room"} slug={"Using a room code"} imgSrc={ForwardImg}>
        <Button className={grayButtonClass} handleClick={onClick}>
            Enter Room Code
        </Button>
    </InfoItem>

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

export let JoinDeskPage = observer(() => {
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
            <MenuContent name={'Join Desk'} back={"/menu/home"}>
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

export const HomePage = observer(() => {

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
