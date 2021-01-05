import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch, useHistory} from "react-router-dom";
import YouTube from "./pages/YouTube";

let Home = () => {
    console.log("home")

    let history = useHistory();

    let cSharpHomeListeners = (event) => {
        let json = JSON.parse(event.data);

        console.log(json)

        if (!(json.type === "nav")) return;

        switch (json.command) {
            case "push":
                console.log("command: nav " + json.path)
                history.push(json.path)
                break;
            default:
                console.log("command: not handled (cSharpHomeListeners) " + JSON.stringify(json))
                break;
        }
    }

    let addCSharpListeners = () => {
        window.vuplex.addEventListener('message', cSharpHomeListeners)
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
    console.log("App")
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>

                    <Route exact path={"/"} component={Home}/>

                    <Route exact path={"/youtube/:id/:timeStamp"} component={YouTube}/>

                    <Route exact path={"/youtube_test/:id/:timeStamp"} component={YouTube}/>

                </Switch>
            </div>
        </Router>
    );
}

export default App;
