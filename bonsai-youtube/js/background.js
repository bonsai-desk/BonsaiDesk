const MSG_PERFORM_REMOVING = 'perform_iframe_removing';
const MSG_ANIMATE_ICON = 'animate_browser_action_icon';
let iconTimeout = null;

browser.browserAction.onClicked.addListener((tab) => {
    browser.tabs.sendMessage(tab.id, MSG_PERFORM_REMOVING);
});

browser.runtime.onMessage.addListener((msg, sender, sendResponse) => {
    if (msg === MSG_ANIMATE_ICON) {
        console.log('Got event from tab');

        // set an endless svg and clear it later, to get animation effect
        browser.browserAction.setIcon({path: 'imgs/rotate.svg'});
        iconTimeout = setTimeout(() => {
            browser.browserAction.setIcon({});
            if (!!iconTimeout) {
                window.clearTimeout(timeout);
                iconTimeout = null;
            }
        }, 1000);
    }
});

browser.commands.onCommand.addListener((cmd) => {
    // refer: https://developer.chrome.com/extensions/tabs
    // cannot user tabs.getCurrent() since this script
    // in running in background
    if (cmd === 'shortcut_rm_iframes') {
        browser.tabs.query({active: true}, (tabs) => {
            tabs.forEach((tab) => {
                browser.tabs.sendMessage(tab.id, cmd);
            });
        });
    }
});

