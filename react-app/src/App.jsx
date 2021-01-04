import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router, Route, Switch } from "react-router-dom";
import Home from "./pages/Home";
import Test from "./pages/Test";

function App() {
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Switch>
                    <Route exact path={"/"} component={Home}/>
                    <Route path={"/test"} component={Test}/>
                </Switch>
            </div>
        </Router>
    );
}

export default App;
