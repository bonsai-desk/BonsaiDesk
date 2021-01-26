import React, {useState, useContext, useCallback, useEffect} from "react";

export const StoreContext = React.createContext();
export const useStore = () => useContext(StoreContext);

export const StoreProvider = ({ children }) => {
    let [init, setInit] = useState(false);
    let [store, setStore] = useState({});

    let storeListeners = useCallback(event => {
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


    return <StoreContext.Provider value={{ store, setStore }}>{children}</StoreContext.Provider>;
};