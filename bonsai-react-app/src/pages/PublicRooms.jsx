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

function RoomInfo({username, network_address, inert}) {

    function onClick() {
        if (!inert) {
            console.log(network_address);
            postJoinRoom({network_address: network_address});
        }
    }

    let className = inert ? grayButtonClassInert : greenButtonClass;

    return <InfoItem title={username} imgSrc={ThinkingFace}>
        <NormalButton onClick={onClick} className={className}>
            Connect
        </NormalButton>

    </InfoItem>;
}

let PublicRoomsPage = observer(() => {

    let {store} = useStore();
    let [loaded, setLoaded] = useState(false);
    let [rooms, setRooms] = useState([]);

    useEffect(() => {
        function fetchRooms() {
            let url = apiBase(store) + '/rooms_public';

            axios.get(url).then(res => {
                setRooms(res.data.rooms);
                setLoaded(true)
            }).catch(err => {
                console.log(err)
                setLoaded(true)
            });
        }

        fetchRooms();
        let handle = setInterval(fetchRooms, 1500);

        return () => {
            clearInterval(handle);
        };
    }, [store]);
    
    if (rooms.length === 0) {
        return <MenuContentFixed name={'Public Rooms'}>
            <div className={'flex flex-wrap content-center justify-center h-full'}>
                {loaded ?
                        <div className={'w-full flex flex-wrap justify-center space-y-4'}>
                            <img className={'h-20'} src={SleepingImg} alt={'thinking face'}/>
                            <div className={'w-full flex flex-wrap justify-center'}>
                                No public rooms open right now
                            </div>
                        </div> : ""
                }
            </div>
        </MenuContentFixed>;
    }

    return <MenuContent name={'Public Rooms'}>
        {rooms.map(room => {
            let netAd = store.NetworkInfo.NetworkAddress;
            let inert = netAd === room.network_address;
            return <RoomInfo {...room} inert={inert}/>;
        })}
    </MenuContent>;
});

export default PublicRoomsPage;