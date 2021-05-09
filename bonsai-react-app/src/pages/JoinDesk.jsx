import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import React, {useEffect, useState} from 'react';
import {useHistory} from 'react-router-dom';
import {apiBase} from '../utilities';
import axios from 'axios';
import {postJoinRoom} from '../api';
import {MenuContentFixed} from '../components/MenuContent';
import {InstantButton} from '../components/Button';
import {roundButtonClass} from '../cssClasses';

function JoinDeskButton(props) {
    let {handleClick, char} = props;

    return (
            <InstantButton className={roundButtonClass} onClick={() => {
                handleClick(char);
            }}>
            <span className={'w-full text-center'}>
                {char}
            </span>
            </InstantButton>
    );
}

export let JoinDeskPage = observer(() => {
    let {store} = useStore();

    let [code, setCode] = useState('');
    let [posting, setPosting] = useState(false);
    let [message, setMessage] = useState('');

    let history = useHistory();

    let url = apiBase(store) + `/rooms/${code}`;

    useEffect(() => {

        function navHome() {
            history.push('/menu/home');
        }

        if (posting) return;

        if (code.length === 4) {
            setPosting(true);
            axios({
                method: 'get',
                url: url,
            }).then(response => {

                let networkAddressResponse = response.data.network_address.toString();
                let networkAddressStore = store.NetworkInfo.NetworkAddress;

                let {
                    version,
                } = response.data;

                if (networkAddressResponse === networkAddressStore) {
                    // trying to join your own room
                    setMessage(`You can't join your own room`);
                    setCode('');
                    setPosting(false);
                } else if (store.FullVersion !== version) {
                    setMessage(
                            `Your version (${store.FullVersion}) mismatch host (${version})`);
                    setCode('');
                    setPosting(false);
                } else {
                    postJoinRoom(response.data);
                    setCode('');
                    setPosting(false);
                    navHome();
                }
            }).catch(err => {
                console.log("Join room error")
                console.log(err);
                setMessage(`Could not find ${code} try again`);
                setCode('');
                setPosting(false);
            });
        }
    }, [
        history,
        code,
        posting,
        url,
        store.NetworkInfo.NetworkAddress,
        store.FullVersion]);

    function handleClick(char) {
        setMessage('');
        switch (code.length) {
            case 4:
                setCode(char);
                break;
            default:
                setCode(code + char);
                break;
        }
    }

    function handleBackspace() {
        if (code.length > 0) {
            setCode(code.slice(0, code.length - 1));
        }
    }

    return (
            <MenuContentFixed name={'Join Desk'} back={'/menu/home'}>
                <div className={'h-20'}/>
                <div className={'h-2'}/>
                <div className={'px-8 justify-between flex flex-wrap w-full content-center'}>
                    <div className={'w-1/2'}>
                        {code.length > 0 && code.length < 4 ?
                                <div className={'text-9xl h-full flex flex-wrap content-center justify-center'}>
                                    {code}
                                </div>
                                :
                                <div className={'text-2xl h-full flex flex-wrap content-center'}>
                                    {message}
                                </div>
                        }
                    </div>
                    <div className={'p-2 rounded space-y-4 text-2xl'}>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'1'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'2'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'3'}/>
                        </div>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'4'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'5'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'6'}/>
                        </div>
                        <div className={'flex space-x-4'}>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'7'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'8'}/>
                            <JoinDeskButton handleClick={handleClick}
                                            char={'9'}/>
                        </div>
                        <div className={'flex flex-wrap w-full justify-around'}>
                            <JoinDeskButton
                                    handleClick={handleBackspace} char={'<'}/>
                        </div>
                    </div>
                </div>
            </MenuContentFixed>
    );
});