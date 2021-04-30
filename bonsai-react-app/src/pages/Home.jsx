import {observer} from 'mobx-react-lite';
import {NetworkManagerMode, useStore} from '../DataProvider';
import React from 'react';
import axios from 'axios';
import {apiBase} from '../utilities';
import {postCloseRoom, postKickConnectionId, postLeaveRoom, postOpenRoom} from '../api';
import {InfoItem} from '../components/InfoItem';
import DoorOpen from '../static/door-open.svg';
import {Button} from '../components/Button';
import {grayButtonClassInert, greenButtonClass, redButtonClass} from '../cssClasses';
import LinkImg from '../static/link.svg';
import {BeatLoader, BounceLoader} from 'react-spinners';
import ThinkingFace from '../static/thinking-face.svg';
import {MenuContent} from '../components/MenuContent';

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
  return <InfoItem title={'Room'} slug={'Invite others'} imgSrc={DoorOpen}>
    <Button className={greenButtonClass} handleClick={postOpenRoom}>
      Open Up
    </Button>
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