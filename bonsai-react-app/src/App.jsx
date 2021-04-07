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
import Keyboard from './pages/Keyboard';
import WebNav from './pages/WebNav';
import {postJson} from './utilities';
import {observer} from 'mobx-react-lite';
import {useStore} from './DataProvider';
import {BounceLoader} from 'react-spinners';

function postListenersReady() {
    postJson(
            {Type: 'event', Message: 'listenersReady', Data: new Date().getTime()});

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

const Boot = observer(() => {
    let {store} = useStore();
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

    function handleKeyPress(e) {
        if (e.key === 'x') {
            store.build = 'DEVELOPMENT';
        }
        if (e.key === 'm') {
            store.app_info.MicrophonePermission = true;
        }

        if (e.key === 'b') {
            store.build = 'DEVELOPMENT';
            store.app_info.MicrophonePermission = true;
            store.is_internet_good = true;
        }
    }

    if (store.build === 'DEVELOPMENT') {
        return (
                <div>
                    Boot
                    <ul>
                        <li>
                            <Link
                                    to={'/youtube_test/Mr0l9iZx4no/19.02890180001912?x=480&y=360'}>youtube_test
                                video</Link>
                        </li>
                        <li>
                            <Link to={'/spring'}>spring</Link>
                        </li>
                        <li>
                            <Link to={'/twitch'}>twitch</Link>
                        </li>
                        <li>
                            <Link to={'/menu'}>menu</Link>
                        </li>
                        <li>
                            <Link to={'/home'}>home</Link>
                        </li>
                        <li>
                            <Link to={'/keyboard'}>keyboard</Link>
                        </li>
                        <li>
                            <Link to={'/webnav'}>webnav</Link>
                        </li>
                    </ul>
                </div>
        );
    } else {
        return <div
                onKeyDown={handleKeyPress}
                tabIndex={0}
                className={'h-screen bg-gray-900 flex flex-wrap content-center justify-center w-full flex-wrap'}>
            <BounceLoader size={200} color={'#737373'}/>
        </div>;
    }

});

function Home() {
    return <div className={'w-full h-full bg-gray-900'}></div>;
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

                        <Route path={'/keyboard'} component={Keyboard}/>

                        <Route path={'/webnav'} component={WebNav}/>

                        <Route path={'/youtube/:id/:timeStamp'} component={YouTube}/>

                        <Route path={'/youtube_test/:id/:timeStamp'} component={YouTube}/>

                        <Route path={'/'} component={Boot}/>

                    </Switch>
                </div>
            </Router>
    );
}

export default App;
