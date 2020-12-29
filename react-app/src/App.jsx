import React from "react";
import "./assets/main.css";
import {BrowserRouter as Router } from "react-router-dom";
import Home from "./pages/Home";

function App() {
    return (
        <Router>
            <div className={"bg-gray-800 h-screen text-green-400"}>
                <Home/>
            </div>
        </Router>
    );
}

export default App;
