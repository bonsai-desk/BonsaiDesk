import axios from 'axios';
import {apiBase} from './utilities';
import {postCloseRoom} from './api';

export function showInfo(info) {
    switch (info[0]) {
        case 'PlayerInfos':
            return showPlayerInfo(info[1]);
        case 'user_info':
            return JSON.stringify(info);
        default:
            return info[1] ? JSON.stringify(info[1], null, 2) : '';
    }
}

function showPlayerInfo(playerInfo) {
    return '[' + playerInfo.map(info => {
        return `(${info.Name}, ${info.ConnectionId})`;
    }).join(' ') + ']';
}

export let handleCloseRoom = (store) => {
    let secret = store.RoomSecret;
    return () => {
        if (store.RoomCode) {
            console.log('secret ', secret);
            axios({
                method: 'delete',
                url: apiBase(store) + '/rooms/' + store.RoomCode + `?secret=${secret}`,
            }).then(r => {
                if (r.status === 200) {
                    console.log(`deleted room ${store.RoomCode}`);
                }
            }).catch(console.log);
        }
        postCloseRoom();
    };
};

export function versionCompare(verA, verB) {
    let aBuild = parseInt(verA.split('b')[1]);
    let bBuild = parseInt(verB.split('b')[1]);
    if (aBuild > bBuild) {
        return -1
    }
    if (aBuild < bBuild) {
        return 1
    }
    return 0
}

export function myVersionString(store) {
    return store.AppInfo.Version + 'b' + store.AppInfo.BuildId;
}

export function showVersionFromApi(apiVersionString){
    let data = apiVersionString.split("b")
    return `v${data[0]} b${data[1]}`
}

export function showVersionFromStore(store){
    return `v${store.AppInfo.Version} b${store.AppInfo.BuildId}`
}