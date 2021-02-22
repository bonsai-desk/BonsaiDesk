function injectScript (func) {
  let script = document.createElement ('script');
  let code = '(' + func + ')();';
  script.textContent = code;
  (document.head || document.documentElement).appendChild (script);
  script.remove ();
}

injectScript (() => {
  let lastTag;

  function postMessage (json) {
    if (window.vuplex) {
      console.log ('post json ' + JSON.stringify (json));
      window.vuplex.postMessage (json);
    } else {
      console.log ('(no vuplex) json ' + JSON.stringify (json));
    }
  }

  function stripLinks () {
    let tag = document.activeElement.tagName;

    if (tag === 'INPUT' && tag !== lastTag) {
      postMessage ({Type: 'event', Message: 'focusInput'});
    }

    if (lastTag === 'INPUT' && tag !== lastTag) {
      postMessage ({Type: 'event', Message: 'blurInput'});
    }

    lastTag = tag;

    let links = document.getElementsByTagName ('a');
    for (let i = 0; i < links.length; i++) {
      let link = links[i];
      if (link.pathname === '/watch') {
        const params = new URLSearchParams (link.search);
        const id = params.get ('v');
        if (id) {
          link.onclick = () => {
            postMessage ({Type: 'command', Message: 'spawnYT', Data: id});
          };
        }
        link.removeAttribute ('href');
      }
    }
  }

  setInterval (stripLinks, 100);
});


