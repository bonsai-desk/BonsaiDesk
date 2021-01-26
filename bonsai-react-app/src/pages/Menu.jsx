import React, {useState, useEffect, useCallback} from 'react'
import "./Menu.css"
import {postJson} from "../utilities";
import axios from "axios"
import DoorOpen from "../static/door-open.svg"


let API_BASE = "https://api.desk.link"

function postJoinRoom(data) {
    postJson({Type: "command", Message: "joinRoom", data: JSON.stringify(data)})
}

function postMouseDown() {
    postJson({Type: "event", Message: "mouseDown"})
}

function postMouseUp() {
    postJson({Type: "event", Message: "mouseUp"})
}

function postHover() {
    postJson({Type: "event", Message: "hover"})
}

function Button(props) {
    return <div onMouseDown={postMouseDown} onMouseUp={postMouseUp} onMouseEnter={postHover}>{props.children}</div>
}

function ListItem(props) {
    let {selected, handleClick} = props;
    let className = selected ?
        "rounded bg-blue-700 text-white px-3 py-2 cursor-pointer" :
        "rounded hover:bg-gray-800 active:bg-gray-900 hover:text-white px-3 py-2 cursor-pointer"
    return (
        <Button>
            <div className={className} onClick={handleClick}>
                {props.children}
            </div>
        </Button>
    )
}

function SettingsList(props) {
    return (
        <div className={"space-y-1 px-2 h-full overflow-auto"}>
            {props.children}
        </div>)

}

function SettingsTitle(props) {
    return <div className={"text-white font-bold text-xl px-5 pt-5 pb-2"}>{props.children}</div>
}

function Settings(props) {
    let {store} = props;
    return (
        <MenuPage name={"Settings"}>
            <ul>
                {Object.entries(store).map(info => {
                    return <li>{info[0]}{": "}{info[1]}</li>
                })}
            </ul>
        </MenuPage>
    )
}

function JoinDeskButton(props) {
    let {handleClick, char} = props
    let buttonClass = "bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center"

    return (
        <Button>
            <div onClick={() => {
                handleClick(char)
            }} className={buttonClass}>
            <span className={"w-full text-center"}>
                {char}
            </span>
            </div>
        </Button>
    )
}

function JoinDesk() {
    let [code, setCode] = useState("")
    let [loading, setLoading] = useState(false);
    let [message, setMessage] = useState("")

    useEffect(() => {
        if (loading) return;

        if (code.length === 4) {
            let url = API_BASE + `/rooms/${code}`
            console.log(url)
            axios({
                method: "get",
                url: url
            }).then(response => {
                postJoinRoom(response.data)
                setCode("")
                setLoading(false)
            }).catch(err => {
                setCode("")
                setLoading(false)
                setMessage("Could not find room, try again")
            })
        }
    }, [loading, code])

    function handleClick(char) {
        setMessage("")
        switch (code.length) {
            case 4:
                setCode(char)
                break;
            default:
                setCode(code + char)
                break;
        }
    }

    function handleBackspace() {
        if (code.length > 0) {
            setCode(code.slice(0, code.length - 1))
        }
    }

    return (
        <MenuPage name={"Join Desk"}>
            <div className={"flex flex-wrap w-full content-center"}>
                <div className={" w-1/2"}>
                    <div className={"text-xl"}>
                        {message}
                    </div>
                    <div className={"text-9xl h-full flex flex-wrap content-center justify-center"}>
                        {code}
                    </div>
                </div>
                <div className={"p-2 rounded space-y-4 text-2xl"}>
                    <div className={"flex space-x-4"}>
                        <JoinDeskButton handleClick={handleClick} char={"L"}/>
                        <JoinDeskButton handleClick={handleClick} char={"R"}/>
                        <JoinDeskButton handleClick={handleClick} char={"C"}/>
                    </div>
                    <div className={"flex space-x-4"}>
                        <JoinDeskButton handleClick={handleClick} char={"D"}/>
                        <JoinDeskButton handleClick={handleClick} char={"E"}/>
                        <JoinDeskButton handleClick={handleClick} char={"F"}/>
                    </div>
                    <div className={"flex space-x-4"}>
                        <JoinDeskButton handleClick={handleClick} char={"G"}/>
                        <JoinDeskButton handleClick={handleClick} char={"H"}/>
                        <JoinDeskButton handleClick={handleClick} char={"I"}/>
                    </div>
                    <div className={"flex flex-wrap w-full justify-around"}>
                        <JoinDeskButton handleClick={handleBackspace} char={"<"}/>
                    </div>
                </div>
            </div>
        </MenuPage>
    )
}

function InfoItem(props) {
    return (
        <div className={"flex w-full"}>
            <div className={"flex w-full"}>
                <div className={"flex flex-wrap content-center  p-2 mr-2"}>
                    <img className={"h-9"} src={props.imgSrc} alt={""}/>
                </div>
                <div>
                    <div className={"text-xl"}>
                        {props.title}
                    </div>
                    <div className={"text-gray-400"}>
                        {props.slug}
                    </div>
                </div>
            </div>
            {props.children}
        </div>
    )
}

function Contacts() {
    return <MenuPage name={"Contacts"}>
    </MenuPage>
}

function MenuPage(props) {
    let {name} = props;

    return (
        <div className={"text-white p-4 h-full pr-8"}>
            {name ?
                <div className={"pb-8 text-xl"}>
                    {name}
                </div>
                : ""}
            <div className={"space-y-8"}>
                {props.children}
            </div>
        </div>
    )

}

function Home(props) {
    let {store} = props;
    return (
        <MenuPage name={"Home"}>
            <InfoItem title={"Desk Code"} slug={"People who have this can join you"} imgSrc={DoorOpen}>
                {store.roomCode ?
                    <div className={"text-4xl flex flex-wrap content-center"}>
                        {store.roomCode}
                    </div>
                    : ""
                }
            </InfoItem>
        </MenuPage>
    )
}

const pages = [
    {name: "Home", component: Home},
    {name: "Join Desk", component: JoinDesk},
    {name: "Contacts", component: Contacts},
    {name: "Settings", component: Settings},
]

function Menu() {

    let [active, setActive] = useState(0)
    let [store, setStore] = useState({app: "Bonsai Desk"})

    let SelectedPage = pages[active].component;

    let menuListeners = useCallback(event => {
        let json = JSON.parse(event.data)
        switch (json.Type) {
            case "command":
                switch (json.Message) {
                    case "pushStore":
                        let _store = {...store}
                        let kvs = json.Data;
                        kvs.map(kv => {
                            _store[kv.Key] = kv.Val;
                        })
                        setStore(_store)
                        break;
                    default:
                        console.log("message not handled " + event.data)
                        break;
                }
                break;
            default:
                console.log("command not handled " + event.data)
                console.log(json)
                break;
        }

    }, [store])

    useEffect(() => {
        if (window.vuplex != null) {
            console.log("bonsai: vuplex is not null -> menuListeners")
            window.vuplex.addEventListener('message', menuListeners)
        } else {
            console.log("bonsai: vuplex is null")
            window.addEventListener('vuplexready', _ => {
                console.log("bonsai: vuplexready -> menuListeners")
                window.vuplex.addEventListener('message', menuListeners)
            })
        }
        return () => {
            if (window.vuplex) {
                console.log("bonsai: remove menuListeners")
                window.vuplex.removeEventListener("message", menuListeners)
            }
        }
    }, [menuListeners])

    return (
        <div className={"flex text-lg text-gray-500 h-full"}>
            <div className={"w-4/12 bg-black h-full overflow-hidden"}>
                <SettingsTitle>
                    Menu
                </SettingsTitle>
                <SettingsList>
                    {pages.map((info, i) => {
                        return <ListItem key={info.name} handleClick={() => {
                            setActive(i)
                        }} selected={active === i}>{info.name}</ListItem>
                    })}
                </SettingsList>
            </div>
            <div className={"w-full"}>
                <SelectedPage store={store}/>
            </div>
        </div>
    )

}

export default Menu;
