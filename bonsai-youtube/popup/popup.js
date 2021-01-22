(function() {
    const MSG_PERFORM_REMOVING = 'perform_iframe_removing';
    const OPTIONS_AUTO_ENABLED = 'enabled_auto_removing_doms';

    function sendRemovingMsg () {
        browser.tabs.query({
            currentWindow: true,
            active: true
        }).then((tabs) => {
            tabs.forEach((tab) => {
                browser.tabs.sendMessage(tab.id, MSG_PERFORM_REMOVING);
            });
        });
    }

    // send remove-iframe message when onClick
    document.getElementById('button').addEventListener('click', () => {
        sendRemovingMsg();
    });


    // sync option UI from preference
    let checkbox = document.getElementById('checkbox');
    browser.storage.local.get(OPTIONS_AUTO_ENABLED).then((r) => {
        checkbox.checked = !!r[OPTIONS_AUTO_ENABLED];
    });

    // update preference from popup
    checkbox.addEventListener('change', () => {
        var obj = {};
        obj[OPTIONS_AUTO_ENABLED] = checkbox.checked;
        browser.storage.local.set(obj);

        // if user enable auto-removing, we send event to remove iframes as well.
        if (checkbox.checked) {
            sendRemovingMsg();
        }
    });

})();
