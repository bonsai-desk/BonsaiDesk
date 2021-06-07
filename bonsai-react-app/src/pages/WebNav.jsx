import React from 'react';
import CloseImg from '../static/close.svg';
import BackImg from '../static/back.svg';
import ForwardImg from '../static/forward.svg';
import KeyBoardImg from '../static/keyboard.svg';
import KeyBoardDismissImg from '../static/keyboard-dismiss.svg';
import {postJson} from '../utilities';
import {KeySVG} from '../components/Keys';
import {postToggleKeyboard} from '../api';

function postCommand(message) {
    postJson({Type: 'command', Message: message});
}

function KeyboardButton(props) {
    let {kbActive, handleClick} = props;

    return (
            <div className={'w-full flex justify-center'}>
                <KeySVG handleClick={handleClick}
                        imgSrc={kbActive ? KeyBoardDismissImg : KeyBoardImg}/>
            </div>

    );

}

function WebNav() {
    
    document.title = "Web Navigator"

    let closeButtonClass = 'bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

    let handleClose = () => {
        postCommand('closeWeb');
    };

    let handleKeyboardButtonClick = () => {
        postToggleKeyboard()
    };

    return (
            <div
                    className={'w-full h-screen bg-black flex flex-wrap content-center justify-center'}>
                <div className={'space-y-2 mb-2'}>
                    <div className={'w-full flex justify-center'}>
                        <KeySVG handleClick={handleClose} className={closeButtonClass}
                                imgSrc={CloseImg}/>
                    </div>
                    <div className={'flex space-x-2'}>
                        <KeySVG imgSrc={BackImg} handleClick={
                            () => {
                                postCommand('navBack');
                            }
                        }/>
                        <KeySVG imgSrc={ForwardImg} handleClick={() => {
                            postCommand('navForward');
                        }}/>
                    </div>
                    <KeyboardButton kbActive={false}
                                    handleClick={handleKeyboardButtonClick}/>
                </div>
            </div>
    );
}

export default WebNav;