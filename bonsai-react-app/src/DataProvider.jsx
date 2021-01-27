import React, {useContext, useEffect, useState} from "react";
import {action, makeAutoObservable} from "mobx";

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

class Store {
    ip_address = null;
    port = null;
    network_state = null;

    constructor () {
        makeAutoObservable(this)
    }
}

const store = new Store()
Object.seal(store)

let pushStore = action((kvList) => {
    kvList.forEach(kv => {
        store[kv.Key] = kv.Val
    })
})

export const StoreProvider = ({children}) => {
    let [init, setInit] = useState(false);

    useEffect(() => {
        if (init) return;

        setInit(true)

        let storeListeners = event => {
            let json = JSON.parse(event.data)
            switch (json.Type) {
                case "command":
                    switch (json.Message) {
                        case "pushStore":
                            pushStore(json.Data)
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

        }

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

    }, [init])

    return <StoreContext.Provider value={{store}}>{children}</StoreContext.Provider>;
};