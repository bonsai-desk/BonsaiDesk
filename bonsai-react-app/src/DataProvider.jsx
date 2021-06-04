import React, {useContext, useEffect, useState} from 'react';
import {action, makeAutoObservable} from 'mobx';
import {observer} from 'mobx-react-lite';
import axios from 'axios';
import {apiBase} from './utilities';
import jwt from "jsonwebtoken";

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

export const NetworkManagerMode = {
    Offline: 0,
    ServerOnly: 1,
    ClientOnly: 2,
    Host: 3,
};

export const HandMode = {
    None: 0,
    Single: 1,
    Whole: 2,
    Duplicate: 3,
    Save: 4,
};

class Store {
    SocialInfo = {
        UserName: 'NoName',
    };
    AppInfo = {
        Build: 'DEVELOPMENT',
        MicrophonePermission: true,
        Version: '0.1.6',
        BuildId: 54,
    };
    _networkInfo = {
        Online: true,
        NetworkAddress: 'none',
        MyNetworkAddress: 'none',
        RoomOpen: false,
        Mode: NetworkManagerMode.Offline,
        PublicRoom: false,
        Full: false,
        Connecting: false,
    };
    ContextInfo = {
        LeftHandMode: HandMode.None,
        RightHandMode: HandMode.None,
        LeftBlockActive: '',
        RightBlockActive: '',
    };
    MediaInfo = {
        Active: false,
        Name: 'None',
        Paused: true,
        Scrub: 0,
        Duration: 1,
        VolumeLevel: 0,
        VolumeMax: 1,
    };
    ExperimentalInfo = {
        BlockBreakEnabled: false,
        PinchPullEnabled: false,
    };
    PlayerInfos = [];
    LoadingRoomCode = false;
    _refresh_room_code_handler = null;
    RoomSecret = '';
    _roomCode = null;
    BonsaiToken = "";
    BonsaiTokenInfo = {
        userId: -1,
        orgScopedId: ""
    }

    _authInfo = {
        UserId: null,
        Nonce: '',
        Build: '',
    };

    constructor() {
        makeAutoObservable(this);
    }
    
    set AuthInfo(authInfo) {
        this._authInfo = authInfo;

        let auth_params = [
            `user_id=${this._authInfo.UserId}`,
            `nonce=${this._authInfo.Nonce}`,
            `build=${this._authInfo.Build}`,
        ].join('&');

        let url = apiBase(this) + `/blocks/login?` + auth_params;

        console.log("POST: " + url)
        
        axios.post(url).then(response => {
            console.log(response)
            this.BonsaiToken = response.data.token;
            const decoded = jwt.decode(this.BonsaiToken);
            this.BonsaiTokenInfo = {
                userId: decoded.user_id,
                orgScopedId: decoded.org_scoped_id
            }
        }).catch(error => {
            console.log(error)
        })

    }

    get FullVersion() {
        return this.AppInfo.Version + 'b' + this.AppInfo.BuildId;
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
            this.RoomSecret = '';
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
        let url = apiBase(this) + `/rooms/${store.RoomCode}/refresh`;
        console.log(url);
        axios({
            method: 'post',
            url: url,
            data: `secret=${this.RoomSecret}&full=${this.NetworkInfo.Full ? 1 : 0}`,
            header: {'content-type': 'application/x-www-form-urlencoded'},
        }).catch(err => {
            console.log(err);
            this.RoomCode = null;
        });
    }
    
    get ApiBase () {
        let API_BASE = 'https://api.desk.link:1776/v1';
        if (this.AppInfo.Build === 'DEVELOPMENT') {
            API_BASE = 'https://api.desk.link:8080/v1';
        }
        return API_BASE;
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