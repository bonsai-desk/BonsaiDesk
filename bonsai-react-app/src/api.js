import {postJson} from './utilities';

export function postRequestMicrophone() {
    postJson({Type: 'command', Message: 'requestMicrophone'});
}

export function postTogglePinchPull() {
    postJson({Type: 'command', Message: 'togglePinchPull'});
}

export function postToggleBlockBreak() {
    postJson({Type: 'command', Message: 'toggleBlockBreak'});
}

export function postCloseMenu() {
    postJson({Type: 'command', Message: 'closeMenu'});
}

export function postBrowseYouTube() {
    postJson({Type: 'command', Message: 'browseYouTube'});
}

export function postOpenPrivateRoom() {
    postJson({Type: 'command', Message: 'openPrivateRoom'});
}

export function postOpenPublicRoom(resetNav = true) {
    postJson({Type: 'command', Message: 'openPublicRoom', Data: resetNav});
}

export function postCloseRoom() {
    postJson({Type: 'command', Message: 'closeRoom'});
}

export function postJoinRoom(data) {
    postJson({Type: 'command', Message: 'joinRoom', data: JSON.stringify(data)});
}

export function postLeaveRoom() {
    postJson({Type: 'command', Message: 'leaveRoom'});
}

export function postKickConnectionId(id) {
    postJson({Type: 'command', Message: 'kickConnectionId', Data: id});
}

export function postSeekPlayer(ts) {
    postJson({Type: 'command', Message: 'seekPlayer', Data: ts});
}

export function postSetVolume(level) {
    // [0,1]
    postJson({Type: 'command', Message: 'setVolume', Data: level});
}

export function postVideoPlay() {
    postJson({Type: 'command', Message: 'playVideo'});
}

export function postVideoPause() {
    postJson({Type: 'command', Message: 'pauseVideo'});
}

export function postVideoEject() {
    postJson({Type: 'command', Message: 'ejectVideo'});
}

export function postVideoRestart() {
    postJson({Type: 'command', Message: 'restartVideo'});
}

export function postLightsChange(level) {
    postJson({Type: 'command', Message: 'lightsChange', Data: level});
}

export function postChangeActiveBlock(hand, blockId) {
    postJson({
        Type: 'command',
        Message: 'changeActiveBlock',
        Data: JSON.stringify({Hand: hand, BlockName: blockId}),
    });
}

export function postSetHandMode(hand, mode) {
    postJson({
        Type: 'command',
        Message: 'setHandMode',
        Data: JSON.stringify({Hand: hand, Mode: mode}),
    });
}

export function postDismissKeyboard() {
    postJson({Type: "command", Message: "dismissKeyboard"})
}

export function postToggleKeyboard() {
    postJson({Type: "command", Message: "toggleKeyboard"})
}

// blocks

export function postStageBuild(buildId) {
    postJson({Type: "command", Message: "stageBuild", Data: buildId})
}

export function postBuildsRefresh () {
    postJson({Type: "command", Message: "buildsRefresh"})
}

export function postSaveBuild(name){
    postJson({Type: "command", Message: "saveBuild", Data: name})
}

export function postDeleteBuild(buildId){
    postJson({Type: "command", Message: "deleteBuild", Data: buildId})
}

export function postSpawnBuild(data) {
    postJson({Type: "command", Message: "spawnBuild", Data: data})
}

export const Layout = {
    Across: 0, SideBySide: 1,
};

export function postSetLayout(layout) {
    let layoutStr;
    switch (layout) {
        case Layout.Across:
            layoutStr = 'across';
            break;
        case Layout.SideBySide:
            layoutStr = 'sideBySide';
            break;
        default:
            layoutStr = 'across';
            break;
    }
    postJson({
        Type: 'command',
        Message: 'layoutChange',
        Data: layoutStr,
    });
}