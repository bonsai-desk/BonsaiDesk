import React, {useContext, useEffect, useState} from 'react';
import {action, makeAutoObservable} from 'mobx';
import {observer} from 'mobx-react-lite';
import axios from 'axios';

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

let API_BASE = 'https://api.desk.link';

class Store {
  ip_address = null;
  port = null;
  network_state = null;
  loading_room_code = false;
  _refresh_room_code_handler = null;
  user_info = {};
  player_info = [];
  build = "DEVELOPMENT";

  constructor() {
    makeAutoObservable(this);
  }

  _room_open = false;

  get room_open() {
    return this._room_open;
  }

  set room_open(open) {
    this._room_open = open;
    if (!open) {
      this.room_code = '';
    }
  }

  _room_code = null;

  get room_code() {
    return this._room_code;
  }

  set room_code(code) {
    this._room_code = code;
    if (code) {
      this._refresh_room_code_handler = setInterval(() => {
            if (this.room_code) {
              this.refreshRoomCode();
            }
          }
          , 1000);
    } else {
      clearInterval(this._refresh_room_code_handler);
      this._refresh_room_code_handler = null;
    }
  }

  refreshRoomCode() {
    axios({
      method: 'post',
      url: API_BASE + `/rooms/${store.room_code}/refresh`,
    }).then(response => {
      //console.log('refresh ' + store.room_code);
    }).catch(err => {
      console.log(err);
      this.room_code = null;
    });
  }

}

const store = new Store();
Object.seal(store);
let pushStoreList = action((kvList) => {
  kvList.forEach(kv => {
    store[kv.Key] = kv.Val;
  });
});
let pushStore = action(obj => {
  for (const prop in obj) {
    store[prop] = obj[prop];
  }
});
let pushStoreSingle = action(obj => {
  store[obj.Key] = obj.Val;
});

function useListeners() {
  let [init, setInit] = useState(false);

  useEffect(() => {
    if (init) return;

    setInit(true);

    let storeListeners = event => {
      let json = JSON.parse(event.data);
      switch (json.Type) {
        case 'command':
          switch (json.Message) {
            case 'pushStore':
              pushStoreList(json.Data);
              break;
            case 'pushStoreSingle':
              pushStoreSingle(json.Data);
              break;
            default:
              console.log('message not handled ' + event.data);
              break;
          }
          break;
        default:
          break;
      }

    };

    if (window.vuplex != null) {
      console.log('bonsai: vuplex is not null -> storeListeners');
      window.vuplex.addEventListener('message', storeListeners);
    } else {
      console.log('bonsai: vuplex is null');
      window.addEventListener('vuplexready', _ => {
        console.log('bonsai: vuplexready -> storeListeners');
        window.vuplex.addEventListener('message', storeListeners);
      });
    }

  }, [init]);
}

export const StoreProvider = observer(({children}) => {

  useListeners();

  return <StoreContext.Provider
      value={{
        store,
        pushStore,
        pushStoreList,
      }}>{children}</StoreContext.Provider>;
});