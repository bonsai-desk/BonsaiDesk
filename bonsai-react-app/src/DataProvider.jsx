import React, {useState, useContext, useCallback, useEffect} from "react";
import axios from "axios";

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

let API_BASE = "https://api.desk.link"

export const StoreProvider = ({ children }) => {
    let [init, setInit] = useState(false);

    // setup store
    let [store, setStore] = useState({});
    const pushStore = useCallback(ob => {
        const _store = {...store};
        setStore({..._store, ...ob})
    }, [store])
    const storeListeners = useCallback(event => {
        let json = JSON.parse(event.data)
        switch (json.Type) {
            case "command":
                switch (json.Message) {
                    case "pushStore":
                        let _store = {...store}
                        json.Data.map(kv => {
                            _store[kv.Key] = kv.Val;
                            return 0;
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
        if (init) return;

        setInit(true)

        if (window.vuplex != null) {
            console.log("bonsai: vuplex is not null -> storeListeners")
            window.vuplex.addEventListener('message', storeListeners)
        } else {
            console.log("bonsai: vuplex is null")
            window.addEventListener('vuplexready', _ => {
                console.log("bonsai: vuplexready -> storeListeners")
                window.vuplex.addEventListener('message', storeListeners)
            })
        }

    }, [init, storeListeners])

    // setup room code
    let [postedInfo, setPostedInfo] = useState(false);
    useEffect(() => {
        let refreshRoom = () => {
            if (store.roomCode){
                axios({
                    method: 'post',
                    url: API_BASE + `/rooms/${store.roomCode}/refresh`,
                }).then(reponse => {
                }).catch(err => {
                    console.log(err)
                    pushStore({roomCode: ""})
                    setPostedInfo(false)
                })
            }
        }

        let interval = window.setInterval(refreshRoom, 5000)

        return () => {
            window.clearInterval(interval)
            if (store.roomCode) {
                let url = API_BASE + `/rooms/${store.roomCode}`
                console.log(url)
                axios({
                    method: "delete",
                    url: url
                }).then(response => {
                    console.log("deleted " + store.roomCode)
                }).catch(console.log)
            }
        }

    }, [store.roomCode, pushStore])
    useEffect(() => {

        // remove the room code if no ip/port
        if (store.roomCode && !store.ip_address && !store.port) {
            pushStore({roomCode: ""})
            return;
        }

        // send ip/port out for a room code
        if (!store.roomCode && !postedInfo && store.ip_address && store.port) {
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
                pushStore({roomCode: response.data.tag})
                setPostedInfo(false)
            }).catch(err => {
                setPostedInfo(false)
            })
        }

    }, [store.roomCode, postedInfo, store.ip_address, store.port, pushStore])

    return <StoreContext.Provider value={{ store, setStore, pushStore }}>{children}</StoreContext.Provider>;

};