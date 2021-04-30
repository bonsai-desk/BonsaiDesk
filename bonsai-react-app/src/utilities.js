let postJson = (json) => {
    console.log('post json ' + JSON.stringify(json));
    if (window.vuplex != null) {
        window.vuplex.postMessage(json);
    }
};

function apiBase(store) {
    let API_BASE = 'https://api.desk.link';
    if (store.AppInfo.Build === 'DEVELOPMENT') {
        API_BASE = 'https://api.desk.link:8080';
    }
    return API_BASE;
}

module.exports = {postJson, apiBase};