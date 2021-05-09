import React, {useEffect, useState} from 'react';
import {MenuContent, MenuContentFixed} from '../components/MenuContent';
import {InfoItem} from '../components/InfoItem';
import ThinkingFace from '../static/thinking-face.svg';
import {useStore} from '../DataProvider';
import {observer} from 'mobx-react-lite';
import {apiBase} from '../utilities';
import SleepingImg from '../static/sleeping-face.svg';
import axios from 'axios';
import {NormalButton} from '../components/Button';
import {grayButtonClassInert, greenButtonClass} from '../cssClasses';
import {postJoinRoom} from '../api';
import {handleCloseRoom} from '../esUtils';

let RoomInfo = observer(({full, username, network_address}) => {

    let {store} = useStore();

    let NetworkAddress = store.NetworkInfo.NetworkAddress;
    let MyNetworkAddress = store.NetworkInfo.MyNetworkAddress;
    let connecting = store.NetworkInfo.Connecting;
    let yourRoom = network_address === MyNetworkAddress;
    let joined = NetworkAddress === network_address;

    let inert = joined || full || connecting || yourRoom;

    let inner = '1/2';

    if (full) {
        inner = '2/2';
    }
   
    if (joined) {
        inner = 'Joined';
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

    return <InfoItem title={username} imgSrc={ThinkingFace}>
        <NormalButton onClick={onClick} className={className}>
            {inner}
        </NormalButton>

    </InfoItem>;
});

let PublicRoomsPage = observer(() => {

    let {store} = useStore();
    let [loaded, setLoaded] = useState(false);
    let [rooms, setRooms] = useState([]);

    useEffect(() => {
        function fetchRooms() {
            let url = apiBase(store) + '/rooms_public';

            axios.get(url).then(res => {
                setRooms(res.data.rooms);
                setLoaded(true);
            }).catch(err => {
                console.log(err);
                setLoaded(true);
            });
        }

        fetchRooms();
        let handle = setInterval(fetchRooms, 1500);

        return () => {
            clearInterval(handle);
        };
    }, [store]);

    if (rooms.length > 0) {
        return <MenuContent name={'Public Rooms'}>
            {rooms.map(room => {
                return <RoomInfo {...room}/>;
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
                        </div> : ''
                }
            </div>
        </MenuContentFixed>;

    }

});

export default PublicRoomsPage;