import React from "react";
import {MemoryRouter as Router, Route, Switch, useHistory, Link} from "react-router-dom";
import YouTube from "./pages/YouTube";
import Spring from "./pages/Spring";
import Twitch from "./pages/Twitch";
import Menu from "./pages/Menu";

function genNavListeners (history) {

    function _navListeners (event) {

        let json = JSON.parse(event.data);

        if (json.type !== "nav") return;

        switch (json.command) {
            case "push":
                console.log("command: nav " + json.path)
                history.push(json.path)
                break;
            default:
                console.log("command: not handled (navListeners) " + JSON.stringify(json))
                break;
        }
    }

    return _navListeners
}


let Boot = () => {

    console.log("Boot")

    let history = useHistory();

    let navListeners = genNavListeners(history);

    if (window.vuplex != null) {

        console.log("bonsai: vuplex is not null -> navListeners")
        window.vuplex.addEventListener('message', navListeners)

    } else {
        console.log("bonsai: vuplex is null")
        window.addEventListener('vuplexready', _ => {

            console.log("bonsai: vuplexready -> navListeners")
            window.vuplex.addEventListener('message', navListeners)

        })
    }

    return (
        <div>
            Boot
            <ul>
                <li>
                    <Link to={"//youtube_test/qEfPBt9dU60/19.02890180001912?x=480&y=360"}>youtube_test video</Link>
                </li>
                <li>
                    <Link to={"/spring"}>spring</Link>
                </li>
                <li>
                    <Link to={"/twitch"}>twitch</Link>
                </li>
            </ul>
        </div>
    )
}

let Home = () => {
    return <div></div>
}

function App() {
    console.log("App")
    return (
        <Router>
            <div className={"bg-gray-900 h-screen text-green-400 select-none"}>
                <Switch>


                    <Route path={"/home"} component={Home}/>

                    <Route path={"/spring"} component={Spring}/>

                    <Route path={"/twitch"} component={Twitch}/>

                    <Route path={"/youtube/:id/:timeStamp"} component={YouTube}/>

                    <Route path={"/youtube_test/:id/:timeStamp"} component={YouTube}/>

                    <Route path={"/"} component={Menu}/>

                </Switch>
            </div>
        </Router>
    );
}

export default App;
