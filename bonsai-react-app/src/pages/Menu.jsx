import React, {useState, useEffect} from 'react'
import "./Menu.css"
import {postJson} from "../utilities";
import axios from "axios"
import DoorOpen from "../static/door-open.svg"
import {useStore} from "../DataProvider"
import {BeatLoader, BounceLoader, ClipLoader, FadeLoader, PulseLoader} from "react-spinners";


let API_BASE = "https://api.desk.link"

let buttonClass = "bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center"

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

function JoinDeskButton(props) {
    let {handleClick, char} = props

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


function MenuContent(props) {
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


function HomePage() {
    let {store} = useStore()

    let [roomCode, setRoomCode] = useState("");

    let [postedInfo, setPostedInfo] = useState(false);

    useEffect(() => {
        let refreshRoom = () => {
            if (roomCode){
                axios({
                    method: 'post',
                    url: API_BASE + `/rooms/${roomCode}/refresh`,
                }).then(reponse => {
                    console.log(reponse)
                }).catch(err => {
                    console.log(err)
                    setRoomCode("")
                    setPostedInfo(false)
                })
                console.log("refresh " + roomCode)
            }
        }

        let interval = window.setInterval(refreshRoom, 5000)

        return () => {
            window.clearInterval(interval)
        }

    }, [roomCode])

    useEffect(() => {

        // remove the room code if no ip/port
        if (roomCode && !store.ip_address && !store.port) {
            setRoomCode("");
            return;
        }

        // send ip/port out for a room code
        if (!roomCode && !postedInfo && store.ip_address && store.port) {
            setPostedInfo(true)
            let url = API_BASE + "/rooms"
            axios(
                {
                    method: 'post',
                    url: url,
                    data: `ip_address=${store.ip_address}&port=${store.port}`,
                    header: {'content-type': "application/x-www-form-urlencoded"}
                }
            ).then(response => {
                setRoomCode(response.data.tag)
                setPostedInfo(false)
            }).catch(err => {
                setPostedInfo(false)
            })
        }

    }, [roomCode, postedInfo, store.ip_address, store.port])

    return (
        <MenuContent name={"Home"}>

            <InfoItem title={"Desk Code"} slug={"People who have this can join you"} imgSrc={DoorOpen}>
                <div className={"text-4xl flex flex-wrap content-center"}>
                    {roomCode ?
                        roomCode
                    : <BounceLoader size={40} color={"#737373"}/>
                }
                </div>
            </InfoItem>
        </MenuContent>
    )
}

function JoinDeskPage() {
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
        <MenuContent name={"Join Desk"}>
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
        </MenuContent>
    )
}

function ContactsPage() {
    return <MenuContent name={"Contacts"}>
    </MenuContent>
}

function SettingsPage(props) {
    let {store, setStore} = useStore();

    let addFakeIpPort = () => {
        let _store = {...store}
        setStore({..._store, ip_address: 1234, port: 4321})
    }

    let rmFakeIpPort = () => {
        let _store = {...store}
        setStore({..._store, ip_address: null, port: null})
    }

    return (
        <MenuContent name={"Settings"}>
            <div className={buttonClass} onClick={addFakeIpPort}>+fake ip/port</div>
            <div className={buttonClass} onClick={rmFakeIpPort}>-fake ip/port</div>
            <ul>
                {Object.entries(store).map(info => {
                    return <li key={info[0]}>{info[0]}{": "}{info[1]}</li>
                })}
            </ul>
        </MenuContent>
    )
}

const pages = [
    {name: "Home", component: HomePage},
    {name: "Join Desk", component: JoinDeskPage},
    {name: "Contacts", component: ContactsPage},
    {name: "Settings", component: SettingsPage},
]

function Menu() {

    let [active, setActive] = useState(0)

    let SelectedPage = pages[active].component;

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
                <SelectedPage/>
            </div>
        </div>
    )
}

export default Menu;
