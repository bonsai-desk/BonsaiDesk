let postJson = (json) => {
    //console.log('post json ' + JSON.stringify(json));
    if (window.vuplex != null) {
        window.vuplex.postMessage(json);
    }
};

function apiBase(store) {
    let API_BASE = 'https://api.desk.link:1776/v1';
    if (store.AppInfo.Build === 'DEVELOPMENT') {
        API_BASE = 'https://api.desk.link:8080/v1';
    }
    return API_BASE;
}

function apiBaseManual(release) {
    let API_BASE = 'https://api.desk.link:1776/v1';
    if (release === 'DEVELOPMENT') {
        API_BASE = 'https://api.desk.link:8080/v1';
    }
    return API_BASE;
    
}

module.exports = {postJson, apiBase, apiBaseManual};