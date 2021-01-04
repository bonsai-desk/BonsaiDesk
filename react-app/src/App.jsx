import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch } from "react-router-dom";
import YouTube from "./pages/YouTube";
import YoutTubeTest from "./pages/YoutTubeTest";

function App() {
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>
                    <Route path={"/youtube/:id"} component={YouTube}/>
                    <Route path={"/youtube_test/:id"} component={YoutTubeTest}/>
                </Switch>
            </div>
        </Router>
    );
}

export default App;
