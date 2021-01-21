import React, {useState} from 'react'
import "./Menu.css"

function ListItem(props) {
    let {selected, handleClick} = props;
    let className = selected ?
        "rounded bg-blue-700 text-white px-3 py-2 cursor-pointer" :
        "rounded hover:bg-gray-800 active:bg-gray-900 hover:text-white px-3 py-2 cursor-pointer"
    return (
        <div className={className} onClick={handleClick}>
            {props.children}
        </div>
    )
}

function SettingsList(props) {
    return (
        <div className={"space-y-1 px-2 scrollhost h-full overflow-auto"}>
            {props.children}
        </div>)

}

function SettingsTitle(props) {
    return <div className={"text-white font-bold text-xl px-5 pt-5 pb-2"}>{props.children}</div>
}

function Settings(props) {
    let buttonClass = "bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded w-full p-4 cursor-pointer"
    return (
        <MenuPage name={"Settings"}>
            <div className={buttonClass}>thing</div>
            <div className={buttonClass}>thing</div>
        </MenuPage>
    )
}

function JoinDesk() {
    let [code, setCode] = useState("")
    //let [loading, setLoading] = useState(false);

    let buttonClass = "bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded-full p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center"

    let handleClick = (char) => {
        if (code.length === 4) {
            setCode(char)
        } else {
            setCode(code + char)
        }
    }

    let handleBackspace = () => {
        if (code.length > 0) {
            setCode(code.slice(0, code.length - 1))
        }
    }

    return (
        <MenuPage name={"Join Desk"}>
            <div className={"flex flex-wrap w-full content-center"}>
                <div className={"text-9xl w-1/2 flex flex-wrap content-center justify-center"}>
                    {code}
                </div>
                <div className={"p-2 rounded space-y-4 text-2xl"}>
                    <div className={"flex space-x-4"}>
                        <div onClick={()=>{handleClick("A")}} className={buttonClass}><span className={"w-full text-center"}>A</span></div>
                        <div onClick={()=>{handleClick("B")}} className={buttonClass}><span className={"w-full text-center"}>B</span></div>
                        <div onClick={()=>{handleClick("C")}} className={buttonClass}><span className={"w-full text-center"}>C</span></div>
                    </div>
                    <div className={"flex space-x-4"}>
                        <div onClick={()=>{handleClick("D")}} className={buttonClass}><span className={"w-full text-center"}>D</span></div>
                        <div onClick={()=>{handleClick("E")}} className={buttonClass}><span className={"w-full text-center"}>E</span></div>
                        <div onClick={()=>{handleClick("F")}} className={buttonClass}><span className={"w-full text-center"}>F</span></div>
                    </div>
                    <div className={"flex space-x-4"}>
                        <div onClick={()=>{handleClick("G")}} className={buttonClass}><span className={"w-full text-center"}>G</span></div>
                        <div onClick={()=>{handleClick("H")}} className={buttonClass}><span className={"w-full text-center"}>H</span></div>
                        <div onClick={()=>{handleClick("I")}} className={buttonClass}><span className={"w-full text-center"}>I</span></div>
                    </div>
                    <div className={"flex flex-wrap w-full justify-around"}>
                        <div onClick={handleBackspace} className={buttonClass}><span className={"w-full text-center"}>{"<"}</span></div>
                    </div>
                </div>
            </div>
        </MenuPage>
    )
}

function Home() {
    return <MenuPage name={"Home"}>
    </MenuPage>
}

function Contacts() {
    return <MenuPage name={"Contacts"}>
    </MenuPage>
}

function MenuPage(props) {
    let {name} = props;

    return (
        <div className={"text-white p-4 h-full"}>
            {name ?
                <div className={"pb-4 text-xl"}>
                    {name}
                </div>
                : ""}
            <div className={"flex space-x-2"}>
                {props.children}
            </div>
        </div>
    )

}

let pages = [
    {name: "Home", component: Home},
    {name: "Join Desk", component: JoinDesk},
    {name: "Contacts", component: Contacts},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Join Desk", component: JoinDesk},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Contacts", component: Contacts},
    {name: "Settings", component: Settings}
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
                <SelectedPage />
            </div>
        </div>
    )

}

export default Menu;
