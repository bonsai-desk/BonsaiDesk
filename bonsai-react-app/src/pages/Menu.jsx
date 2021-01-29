import React, {useEffect, useState} from 'react';
import './Menu.css';
import {postJson} from '../utilities';
import axios from 'axios';
import DoorOpen from '../static/door-open.svg';
import LinkImg from '../static/link.svg';
import ThinkingFace from '../static/thinking-face.svg';
import {useStore} from '../DataProvider';
import {BeatLoader, BounceLoader} from 'react-spinners';
import {observer} from 'mobx-react-lite';
import {action, autorun} from 'mobx';

let API_BASE = 'https://api.desk.link';

const roundButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center';
const redButtonClass = 'py-4 px-8 font-bold bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer flex flex-wrap content-center';
const greenButtonClass = 'py-4 px-8 font-bold bg-green-800 active:bg-green-700 hover:bg-green-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClass = 'py-4 px-8 font-bold bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center';
const grayButtonClassInert = 'py-4 px-8 font-bold bg-gray-800 rounded flex flex-wrap content-center';

function postOpenRoom() {
  postJson({Type: 'command', Message: 'openRoom'});
}

function postCloseRoom() {
  postJson({Type: 'command', Message: 'closeRoom'});
}

function postJoinRoom(data) {
  postJson({Type: 'command', Message: 'joinRoom', data: JSON.stringify(data)});
}

function postLeaveRoom() {
  postJson({Type: 'command', Message: 'leaveRoom'});

}

function postKickConnectionId(id) {
  postJson({Type: 'command', Message: 'kickConnectionId', Data: id});
}

function postMouseDown() {
  postJson({Type: 'event', Message: 'mouseDown'});
}

function postMouseUp() {
  postJson({Type: 'event', Message: 'mouseUp'});
}

function postHover() {
  postJson({Type: 'event', Message: 'hover'});
}

function Button(props) {
  return <div onMouseDown={postMouseDown} onMouseUp={postMouseUp}
              onMouseEnter={postHover}>{props.children}</div>;
}

function ListItem(props) {
  let {selected, handleClick} = props;
  let className = selected ?
      'rounded bg-blue-700 text-white px-3 py-2 cursor-pointer' :
      'rounded hover:bg-gray-800 active:bg-gray-900 hover:text-white px-3 py-2 cursor-pointer';
  return (
      <Button>
        <div className={className} onClick={handleClick}>
          {props.children}
        </div>
      </Button>
  );
}

function SettingsList(props) {
  return (
      <div className={'space-y-1 px-2'}>
        {props.children}
      </div>);

}

function SettingsTitle(props) {
  return <div
      className={'text-white font-bold text-xl px-5 pt-5 pb-2'}>{props.children}</div>;
}

function JoinDeskButton(props) {
  let {handleClick, char, triggerMouseDown = false} = props;

  return (
      <Button>
        <div
            onMouseDown={() => {
              if (triggerMouseDown) {
                handleClick(char);
              }
            }}
            onMouseUp={() => {
              if (!triggerMouseDown) {
                handleClick(char);
              }
            }}
            className={roundButtonClass}>
            <span className={'w-full text-center'}>
                {char}
            </span>
        </div>
      </Button>
  );
}

function ConnectedClient(props) {
  let {info} = props;
  let {Name, ConnectionId} = info;
  return <InfoItem title={Name} slug={ConnectionId}
                   imgSrc={ThinkingFace}><Button>
    <div onClick={() => {
      postKickConnectionId(ConnectionId);
    }} className={redButtonClass}>Kick
    </div>
  </Button></InfoItem>;
}

function InfoItem(props) {
  return (
      <div className={'flex w-full justify-between'}>
        <div className={'flex w-auto'}>
          <div className={'flex flex-wrap content-center  p-2 mr-2'}>
            <img className={'h-9 w-9'} src={props.imgSrc} alt={''}/>
          </div>
          <div>
            <div className={'text-xl'}>
              {props.title}
            </div>
            <div className={'text-gray-400'}>
              {props.slug}
            </div>
          </div>
        </div>
        {props.children}
      </div>
  );
}

function MenuContent(props) {
  let {name} = props;

  return (
      <div className={'text-white p-4 h-full pr-8'}>
        {name ?
            <div className={'pb-8 text-xl'}>
              {name}
            </div>
            : ''}
        <div className={'space-y-8'}>
          {props.children}
        </div>
      </div>
  );

}

let ClientHomePage = () => {
  return (
      <div className={'flex'}>
        <InfoItem title={'Connected'} slug={'You are connected to a host'}
                  imgSrc={LinkImg}>
          <Button>
            <div onClick={postLeaveRoom} className={redButtonClass}>Exit</div>
          </Button>
        </InfoItem>
      </div>
  );
};

let RoomInfo = observer(() => {
  let {store, pushStore} = useStore();

  let buttonClass = 'px-2 py-1 bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer flex flex-wrap content-center';

  let OpenRoom =
      <InfoItem title={'Room'} slug={'Invite others'} imgSrc={DoorOpen}>
        <Button>
          <div className={greenButtonClass} onClick={postOpenRoom}>
            Open Up
          </div>
        </Button>
      </InfoItem>;

  let CloseRoom =
      <InfoItem title={'Room'} slug={'Ready to accept connections'}
                imgSrc={DoorOpen}>
        <Button>
          <div className={redButtonClass} onClick={postCloseRoom}>
            Close
          </div>
        </Button>
      </InfoItem>;

  if (store.room_open) {
    return (
        <React.Fragment>
          {CloseRoom}
          <InfoItem title={'Desk Code'}
                    slug={'People who have this can join you'}
                    imgSrc={LinkImg}>
            {store.room_code ?
                <Button>
                  <div onClick={() => {
                    pushStore({room_code: null});
                  }} className={grayButtonClass}>{store.room_code}</div>
                </Button>

                :
                <div className={grayButtonClassInert}><BeatLoader size={8}
                                                                  color={'#737373'}/>
                </div>
            }
          </InfoItem>
        </React.Fragment>
    );
  } else {
    return (
        <React.Fragment>
          {OpenRoom}
        </React.Fragment>
    );
  }

});

let HostHomePage = observer(() => {

  let {store, pushStore} = useStore();

  return (
      <React.Fragment>
        <RoomInfo/>
        {store.player_info.length > 0 && store.room_open ?
            <React.Fragment>
              {store.player_info.map(info => <ConnectedClient info={info}/>)}
            </React.Fragment>
            :
            ''}
      </React.Fragment>
  );

});

let LoadingHomePage = () => {
  return <div className={'flex justify-center w-full h-full'}>
    <BounceLoader size={200} color={'#737373'}/>
  </div>;
};

let HomePage = observer(() => {

  let {store} = useStore();

  let Inner;

  switch (store.network_state) {
    case 'Neutral':
    case 'HostWaiting':
    case 'Hosting':
      Inner = <HostHomePage/>;
      break;
    case 'ClientConnected':
      Inner = <ClientHomePage/>;
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

function JoinDeskPage(props) {
  let {navHome} = props;

  let [code, setCode] = useState('');
  let [loading, setLoading] = useState(false);
  let [message, setMessage] = useState('');

  useEffect(() => {
    if (loading) return;

    if (code.length === 4) {
      let url = API_BASE + `/rooms/${code}`;
      console.log(url);
      axios({
        method: 'get',
        url: url,
      }).then(response => {
        postJoinRoom(response.data);
        navHome();
        setCode('');
        setLoading(false);
      }).catch(err => {
        console.log(err);
        setMessage(`Could not find ${code} try again`);
        setCode('');
        setLoading(false);
      });
    }
  }, [loading, code]);

  function handleClick(char) {
    console.log('handleclick');
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
      <MenuContent name={'Join Desk'}>
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
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'L'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'R'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'C'}/>
            </div>
            <div className={'flex space-x-4'}>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'D'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'E'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'F'}/>
            </div>
            <div className={'flex space-x-4'}>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'G'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'H'}/>
              <JoinDeskButton triggerMouseDown={true} handleClick={handleClick}
                              char={'I'}/>
            </div>
            <div className={'flex flex-wrap w-full justify-around'}>
              <JoinDeskButton handleClick={handleBackspace} char={'<'}/>
            </div>
          </div>
        </div>
      </MenuContent>
  );
}

function ContactsPage() {
  return <MenuContent name={'Contacts'}>
  </MenuContent>;
}

let SettingsPage = observer(() => {
  let {store} = useStore();

  let buttonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer flex flex-wrap content-center';

  let addFakeIpPort = action((store) => {
    store.ip_address = 1234;
    store.port = 4321;
  });
  let rmFakeIpPort = action(store => {
    store.ip_address = null;
    store.port = null;
  });

  let setNetState = action((store, netState) => {
    store.network_state = netState;
  });

  let addFakeClient = action(store => {
    store.player_info.push({Name: 'cam', ConnectionId: 0});
  });
  let rmFakeClient = action(store => {
    store.player_info.pop();
  });

  let toggleRoomOpen = action(store => {
    store.room_open = !store.room_open;
  });

  return (
      <MenuContent name={'Settings'}>
        <div className={'flex space-x-2'}>
          <Button>
            <div onClick={() => {
              setNetState(store, 'Neutral');
            }} className={buttonClass}>Neutral
            </div>
          </Button>
          <Button>
            <div onClick={() => {
              setNetState(store, 'HostWaiting');
            }} className={buttonClass}>HostWaiting
            </div>
          </Button>
          <Button>
            <div onClick={() => {
              setNetState(store, 'Hosting');
            }} className={buttonClass}>Hosting
            </div>
          </Button>
          <Button>
            <div onClick={() => {
              setNetState(store, 'ClientConnected');
            }} className={buttonClass}>ClientConnected
            </div>
          </Button>


        </div>
        <div className={'flex space-x-2'}>
          <Button>
            <div className={buttonClass} onClick={() => {
              addFakeIpPort(store);
            }}>+ fake ip/port
            </div>
          </Button>
          <Button>
            <div className={buttonClass} onClick={() => {
              rmFakeIpPort(store);
            }}>- fake ip/port
            </div>
          </Button>
          <Button>
            <div onClick={() => {
              addFakeClient(store);
            }} className={buttonClass}>+ fake client
            </div>
          </Button>
          <Button>
            <div onClick={() => {
              rmFakeClient(store);
            }} className={buttonClass}>- fake client
            </div>
          </Button>
        </div>
        <Button>
          <div onClick={() => {
            toggleRoomOpen(store);
          }} className={buttonClass}>
            toggle room open
          </div>
        </Button>
        <div className={'flex space-x-2'}>
        </div>
        <ul>
          {Object.entries(store).map(info => {
            return <li key={info[0]}>{info[0]}{': '}{showInfo(info)}</li>;
          })}
        </ul>
      </MenuContent>
  );
});

const pages = [
  {name: 'Home', component: HomePage},
  {name: 'Join Desk', component: JoinDeskPage},
  {name: 'Contacts', component: ContactsPage},
  {name: 'Settings', component: SettingsPage},
];

function showInfo(info) {
  switch (info[0]) {
    case 'player_info':
      return showPlayerInfo(info[1]);
    default:
      return info[1] ? info[1].toString() : '';
  }
}

function showPlayerInfo(playerInfo) {
  return '[' + playerInfo.map(info => {
    return `(${info.Name}, ${info.ConnectionId})`;
  }).join(' ') + ']';
}

let Menu = () => {

  let {store, pushStore} = useStore();

  let [active, setActive] = useState(0);

  let SelectedPage = pages[active].component;

  let navHome = () => {
    setActive(0);
  };

  useEffect(() => {
    autorun(() => {
      // remove room code if
      if (store.room_code &&
          (!store.ip_address || !store.port || !store.room_open)) {
        console.log('rm room code');
        pushStore({room_code: null});
        return;
      }

      // send ip/port out for a room code
      if (store.room_open && !store.room_code && !store.loading_room_code &&
          store.ip_address &&
          store.port) {
        console.log('fetch room code');
        pushStore({loading_room_code: true});
        let url = API_BASE + '/rooms';
        axios(
            {
              method: 'post',
              url: url,
              data: `ip_address=${store.ip_address}&port=${store.port}`,
              header: {'content-type': 'application/x-www-form-urlencoded'},
            },
        ).then(response => {
          pushStore({room_code: response.data.tag, loading_room_code: false});
        }).catch(err => {
          console.log(err);
          pushStore({loading_room_code: false});
        });
      }
    });

  });

  useEffect(() => {
    return () => {
      pushStore({room_code: null});
    };
  }, [pushStore]);

  return (
      <div className={'flex text-lg text-gray-500 h-full'}>
        <div className={'w-4/12 bg-black overflow-auto scrollhost static'}>
          <div className={'w-4/12 bg-black fixed'}>
            <SettingsTitle>
              Menu
            </SettingsTitle>
          </div>
          <div className={'h-16'}/>
          <SettingsList>
            {pages.map((info, i) => {
              return <ListItem key={info.name} handleClick={() => {
                setActive(i);
              }} selected={active === i}>{info.name}</ListItem>;
            })}
          </SettingsList>
        </div>
        <div className={'bg-gray-900 z-10 w-full overflow-auto scrollhost'}>
          <SelectedPage navHome={navHome}/>
        </div>
      </div>
  );
};

export default Menu;
