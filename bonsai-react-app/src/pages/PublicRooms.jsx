import React, {useCallback, useEffect, useState} from 'react';
import {MenuContent, MenuContentFixed} from '../components/MenuContent';
import {InfoItem} from '../components/InfoItem';
import ThinkingFace from '../static/thinking-face.svg';
import {NetworkManagerMode, useStore} from '../DataProvider';
import {observer} from 'mobx-react-lite';
import {apiBase} from '../utilities';
import SleepingImg from '../static/sleeping-face.svg';
import axios from 'axios';
import {InstantButton, NormalButton} from '../components/Button';
import {grayButtonClassInert, greenButtonClass, redButtonClass} from '../cssClasses';
import {postJoinRoom, postOpenPublicRoom} from '../api';
import {handleCloseRoom, myVersionString, showVersionFromApi, versionCompare} from '../esUtils';
import DoorOpen from '../static/door-open.svg';
import {SubHeader} from '../components/SubHeader';
import HashImg from '../static/hash.svg';
import LinkImg from '../static/link.svg';

function ClientConnectedItem() {
    return <InfoItem title={'Connected'} slug={'You are connected to a host'}
                     imgSrc={LinkImg}>
    </InfoItem>;

}

let RoomInfo = observer(({full, username, network_address, version}) => {

    let {store} = useStore();

    let versionMatch = versionCompare(myVersionString(store), version);

    let NetworkAddress = store.NetworkInfo.NetworkAddress;
    let MyNetworkAddress = store.NetworkInfo.MyNetworkAddress;
    let connecting = store.NetworkInfo.Connecting;
    let yourRoom = network_address === MyNetworkAddress;
    let joined = NetworkAddress === network_address;

    let inert = joined || full || connecting || yourRoom || versionMatch !== 0;

    let inner = '1/2';

    if (full) {
        inner = '2/2';
    }

    if (joined) {
        inner = 'Joined';
    }

    if (versionMatch === -1) {
        inner = 'Host has older version';
    }

    if (versionMatch === 1) {
        inner = 'Host has newer version';
    }

    if (yourRoom) {
        inner = 'Your Room';
    }

    if (connecting) {
        //inner = <BeatLoader size={8} color={'#737373'}/>
    }

    let closeRoom = handleCloseRoom(store);

    function onClick() {
        if (!inert) {
            console.log(network_address);
            closeRoom();
            postJoinRoom({network_address: network_address});
        }
    }

    let className = inert ? grayButtonClassInert : greenButtonClass;

    return <InfoItem title={username} imgSrc={ThinkingFace} slug={showVersionFromApi(version)}>
        <NormalButton onClick={onClick} className={className}>
            {inner}
        </NormalButton>

    </InfoItem>;
});

function OpenRoomItem({onClick}) {
    return <InfoItem title={'Open Public Room'} slug={'Let people join you'} imgSrc={DoorOpen}>
        <div className={'flex space-x-4'}>
            <InstantButton className={greenButtonClass} onClick={onClick}>Open Up</InstantButton>
        </div>
    </InfoItem>;
}

const ClosePublicRoom = observer(({onClick}) => {
    return <InfoItem title={'Your Room is Open'} slug={'Ready for people to join'}
                     imgSrc={DoorOpen}>
    </InfoItem>;
});

const ClosePrivateRoom = observer(({onClick}) => {
    return <InfoItem title={'Your Room is Private'} slug={'Close and open a public room'}
                     imgSrc={HashImg}>
        <NormalButton className={redButtonClass} onClick={onClick}>
            Close
        </NormalButton>
    </InfoItem>;
});

const RoomAction = observer(({clickCloseRoom}) => {
    let {store} = useStore();

    let publicRoom = store.NetworkInfo.PublicRoom;
    let roomOpen = store.NetworkInfo.RoomOpen;

    let isClient = store.NetworkInfo.Mode === NetworkManagerMode.ClientOnly;
    let isHost = store.NetworkInfo.Mode === NetworkManagerMode.Host;

    if (isHost) {
        if (roomOpen) {
            if (publicRoom) {
                return <ClosePublicRoom onClick={clickCloseRoom}/>;
            } else {
                return <ClosePrivateRoom onClick={clickCloseRoom}/>;
            }
        } else {
            return <OpenRoomItem onClick={postOpenPublicRoom}/>;
        }
    } else if (isClient) {
        return <ClientConnectedItem/>;
    }
    return <InfoItem title={'Loading'} slug={'Setting up your room'} imgSrc={DoorOpen}/>;
});

let PublicRoomsPage = observer(() => {

    let {store} = useStore();
    let [loaded, setLoaded] = useState(false);
    let [rooms, setRooms] = useState([]);

    let roomOpen = store.NetworkInfo.RoomOpen;
    let publicRoom = store.NetworkInfo.PublicRoom;
    let closeRoom = handleCloseRoom(store);
    let clientOnly = store.NetworkInfo.Mode === NetworkManagerMode.ClientOnly;
    let offline = store.NetworkInfo.Mode === NetworkManagerMode.Offline;

    let fetchRooms = useCallback(() => {

        let url = apiBase(store) + '/rooms_public';

        axios.get(url).then(res => {
            setRooms(res.data.rooms);
            setLoaded(true);
        }).catch(err => {
            console.log(err);
            setLoaded(true);
        });

    }, [store]);

    useEffect(() => {
        fetchRooms();
        let handle = setInterval(fetchRooms, 1500);
        return () => {
            clearInterval(handle);
        };
    }, [store, fetchRooms]);

    function clickOpenRoom() {
        postOpenPublicRoom(false);
        setTimeout(() => {
            fetchRooms();
        }, 50);
    }

    function clickCloseRoom() {
        closeRoom();
        setTimeout(() => {
            fetchRooms();
        }, 50);
    }

    let ActionButton = <InstantButton className={greenButtonClass} onClick={clickOpenRoom}>
        Open One
    </InstantButton>;

    if (!publicRoom && roomOpen) {
        ActionButton = <InstantButton className={redButtonClass} onClick={clickCloseRoom}>
            Close Your Private Room
        </InstantButton>;
    }

    if (rooms.length > 0) {
        return <MenuContent name={'Public Rooms'}>
            <RoomAction clickCloseRoom={clickCloseRoom}/>
            <SubHeader>Join a Room</SubHeader>
            {rooms.map(room => {
                return <RoomInfo key={room.id} {...room}/>;
            })}
        </MenuContent>;
    } else {
        return <MenuContentFixed name={'Public Rooms'}>
            <div className={'flex flex-wrap content-center justify-center h-full'}>
                {loaded ?
                        <div className={'w-full flex flex-wrap justify-center space-y-4'}>
                            <img className={'h-20'} src={SleepingImg} alt={'thinking face'}/>
                            <div className={'w-full flex flex-wrap justify-center'}>
                                No public rooms open right now
                            </div>
                            {!clientOnly && !offline ?
                                    ActionButton : ''
                            }
                        </div> : ''
                }
            </div>
        </MenuContentFixed>;
    }

});

export default PublicRoomsPage;