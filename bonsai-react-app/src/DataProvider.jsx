import React, {useContext, useEffect, useState} from 'react';
import {action, makeAutoObservable} from 'mobx';
import {observer} from 'mobx-react-lite';
import axios from 'axios';

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

let API_BASE = 'https://api.desk.link';

export const NetworkManagerMode = {
    Offline: 0,
    ServerOnly: 1,
    ClientOnly: 2,
    Host: 3,
};

// todo ip_address, port, network_state
class Store {
    AppInfo = {
        Build: 'PRODUCTION',
        MicrophonePermission: false,
        Version: '?',
        BuildId: -1,
    };

    _networkInfo = {
        Online: false,
        NetworkAddress: 'none',
        RoomOpen: false,
        Mode: NetworkManagerMode.Offline
    };
    MediaInfo = {
        Active: false,
        Name: 'None',
        Paused: true,
        Scrub: 0,
        Duration: 1,
        VolumeLevel: 0,
    };
    ExperimentalInfo = {
        BlockBreakEnabled: false,
        PinchPullEnabled: false,
    };
    PlayerInfos = [];
    LoadingRoomCode = false;
    _refresh_room_code_handler = null;

    constructor() {
        makeAutoObservable(this);
    }

    get NetworkInfo() {
        return this._networkInfo;
    }

    set NetworkInfo(networkInfo) {
        this._networkInfo = networkInfo;
        if (!networkInfo.RoomOpen) {
            this.RoomCode = '';
        }

    }

    _roomCode = null;

    get RoomCode() {
        return this._roomCode;
    }

    set RoomCode(code) {
        this._roomCode = code;
        if (code) {
            this._refresh_room_code_handler = setInterval(() => {
                        if (this.RoomCode) {
                            this.refreshRoomCode();
                        }
                    }
                    , 1000);
        } else {
            clearInterval(this._refresh_room_code_handler);
            this._refresh_room_code_handler = null;
        }
    }

    _room_open = false;

    get room_open() {
        return this._room_open;
    }

    set room_open(open) {
        this._room_open = open;
        if (!open) {
            this.RoomCode = '';
        }
    }

    refreshRoomCode() {
        axios({
            method: 'post',
            url: API_BASE + `/rooms/${store.RoomCode}/refresh`,
        }).catch(err => {
            console.log(err);
            this.RoomCode = null;
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