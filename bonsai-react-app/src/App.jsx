import React from 'react';
import {
  Link,
  MemoryRouter as Router,
  Route,
  Switch,
  useHistory,
} from 'react-router-dom';
import YouTube from './pages/YouTube';
import Spring from './pages/Spring';
import Twitch from './pages/Twitch';
import Menu from './pages/Menu';
import {postJson} from './utilities';

function postListenersReady() {
  postJson({Type: 'event', Message: 'listenersReady'});

}

function genNavListeners(history) {

  function _navListeners(event) {

    let json = JSON.parse(event.data);

    if (json.type !== 'nav') return;

    switch (json.command) {
      case 'push':
        console.log('command: nav ' + json.path);
        history.push(json.path);
        break;
      default:
        console.log(
            'command: not handled (navListeners) ' + JSON.stringify(json));
        break;
    }
  }

  return _navListeners;
}

function Boot() {

  console.log('Boot');

  let history = useHistory();

  let navListeners = genNavListeners(history);

  if (window.vuplex != null) {

    console.log('bonsai: vuplex is not null -> navListeners');
    window.vuplex.addEventListener('message', navListeners);
    postListenersReady();

  } else {
    console.log('bonsai: vuplex is null');
    window.addEventListener('vuplexready', _ => {

      console.log('bonsai: vuplexready -> navListeners');
      window.vuplex.addEventListener('message', navListeners);
      postListenersReady();

    });
  }

  return (
      <div>
        Boot
        <ul>
          <li>
            <Link
                to={'/youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360'}>youtube_test
              video</Link>
          </li>
          <li>
            <Link to={'/spring'}>spring</Link>
          </li>
          <li>
            <Link to={'/twitch'}>twitch</Link>
          </li>
          <li onClick={() => {
            history.push('/menu');
          }} className={'text-white'}>menu
          </li>
          <li>
            <Link to={'/menu'}>menu</Link>
          </li>
        </ul>
      </div>
  );
}

function Home() {
  return <div>home</div>;
}

function App() {
  console.log('App');
  return (
      <Router>
        <div className={'h-screen text-green-400 select-none'}>
          <Switch>

            <Route path={'/home'} component={Home}/>

            <Route path={'/spring'} component={Spring}/>

            <Route path={'/twitch'} component={Twitch}/>

            <Route path={'/menu'} component={Menu}/>

            <Route path={'/youtube/:id/:timeStamp'} component={YouTube}/>

            <Route path={'/youtube_test/:id/:timeStamp'} component={YouTube}/>

            <Route path={'/'} component={Boot}/>

          </Switch>
        </div>
      </Router>
  );
}

export default App;
