import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch} from "react-router-dom";
import YouTube from "./pages/YouTube";

function App() {
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>
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
