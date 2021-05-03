import React from 'react';
import {useHistory} from 'react-router-dom';
import {roundButtonClass} from '../cssClasses';
import BackImg from "../static/back.svg"

function BackButton({to}) {
    let className = 'h-14 w-14 bg-gray-400 rounded-full';
    let history = useHistory();

    function onClick() {
        history.push(to);
    }

    return <div className={roundButtonClass} onClick={onClick}>
        <img src={BackImg} alt={"back"}/>
        
    </div>;
}

export function MenuContent(props) {
    let {name, back} = props;
    
    //back = ""

    return (
            <div className={'text-white p-4 h-full pr-8'}>
                <div className={'flex flex-wrap space-x-6 content-center h-20'}>
                    {back ?
                            <BackButton to={back}/> : ''
                    }
                    {name ?

                            <div className={'flex flex-wrap content-center'}>
                                <div className={'text-2xl'}>
                                    {name}
                                </div>
                            </div>

                            : ''}
                </div>
                <div className={'space-y-8 pb-8'}>
                    {props.children}
                </div>
            </div>
    );

}