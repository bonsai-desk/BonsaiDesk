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