import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch, useHistory} from "react-router-dom";
import YouTube from "./pages/YouTube";

let Home = () => {
    console.log("home")

    let history = useHistory();

    let addCSharpListeners = () => {

        // ping back the player time every 1/10th of a second
        window.vuplex.addEventListener('message', event => {

            let json = JSON.parse(event.data);

            if (!(json.type === "nav")) return;

            switch (json.command) {
                case "push":
                    console.log("command: nav " + json.path)
                    history.push(json.path)
                    break;
                case "goHome":
                    console.log("command: goHome pre ")
                    history.push("/")
                    window.location.reload(true)
                    console.log("command: goHome pre post")
                    break;
                default:
                    console.log("command: not handled " + JSON.stringify(json))
                    break;
            }
        })
    }

    if (window.vuplex != null) {
        addCSharpListeners();
    } else {
        window.addEventListener('vuplexready', _ => {
            addCSharpListeners()
        })
    }

    return (
        <div>
            home
        </div>
    )
}

let Ping = () => {
    console.log("ping")
    return <div className={"bg-gray-400"}>ping</div>
}

function App() {
    console.log("App")
    return <div>app</div>
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>
                    <route path={"/ping"} componenet={Ping}/>

                    <Route exact path={"/"} component={Home}/>

                    <Route exact path={"/youtube/:id/:ts"} component={YouTube}/>

                    <Route exact path={"/youtube_test/:id/:ts"} component={YouTube}/>

                </Switch>
            </div>
        </Router>
    );
}

export default App;
