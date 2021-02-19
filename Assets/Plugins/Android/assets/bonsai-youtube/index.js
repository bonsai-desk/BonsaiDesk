function injectScript (func) {
  let script = document.createElement ('script');
  let code = '(' + func + ')();';
  console.log ('injecting ' + code);
  script.textContent = code;
  (document.head || document.documentElement).appendChild (script);
  script.remove ();
}

injectScript (() => {
  function stripLinks () {
    let links = document.getElementsByTagName ('a');
    for (let i = 0; i < links.length; i++) {
      let link = links[i];
      if (link.pathname === '/watch') {
        const params = new URLSearchParams (link.search);
        const id = params.get ('v');
        if (id) {
          link.onclick = () => {
            console.log (id);
            if (window.vuplex) {
              const json = {Type: 'command', Message: 'spawnYT', Data: id};
              console.log ('post json ' + JSON.stringify (json));
              window.vuplex.postMessage (json);
            }
          };
        }
        link.removeAttribute ('href');
      }
    }
  }

  setInterval (stripLinks, 100);
});


