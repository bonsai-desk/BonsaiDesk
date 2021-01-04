import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch, useHistory} from "react-router-dom";
import YouTube from "./pages/YouTube";

let Home = () => {

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

function App() {
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>

                    <Route exact path={"/"} component={Home}/>

                    <Route exact path={"/youtube/:id"} component={YouTube}/>
                    <Route exact path={"/youtube/:id/:ts"} component={YouTube}/>

                    <Route exact path={"/youtube_test/:id"} component={YouTube}/>
                    <Route exact path={"/youtube_test/:id/:ts"} component={YouTube}/>

                </Switch>
            </div>
        </Router>
    );
}

export default App;
