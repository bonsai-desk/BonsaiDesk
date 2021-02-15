import React, {useState} from "react"
import {Button} from '../Components/Button';
import CloseImg from '../static/close.svg';
import BackImg from '../static/back.svg';
import ForwardImg from '../static/forward.svg';
import KeyBoardImg from '../static/keyboard.svg';
import KeyBoardDismissImg from '../static/keyboard-dismiss.svg';
import {postJson} from '../utilities';
import {KeySVG} from '../Components/Keys';

function postCommand(message) {
  console.log(message);
  postJson({Type: 'command', Message: message});
}

function KeyboardButton(props) {
  let {kbActive, handleClick} = props;

  return (
      <Button className={'w-full flex justify-center'}
              handleClick={handleClick}>
        <KeySVG imgSrc={kbActive ? KeyBoardDismissImg : KeyBoardImg}/>
      </Button>

  );

}

function WebNav() {

  let [kbActive, setKbActive] = useState(false);

  let closeButtonClass = 'bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  let handleClose = () => {
    postCommand('closeWeb');
  };

  let handleKeyboardButtonClick = () => {
    kbActive ? postCommand('dismissKeyboard') : postCommand('spawnKeyboard');
    setKbActive(!kbActive);
  };

  return (
      <div
          className={'w-full h-screen bg-black flex flex-wrap content-center justify-center'}>
        <div className={'space-y-2 mb-2'}>
          <Button className={'w-full flex justify-center'}
                  handleClick={handleClose}>
            <KeySVG className={closeButtonClass} imgSrc={CloseImg}/>
          </Button>
          <div className={'flex space-x-2'}>
            <Button handleClick={() => {
              postCommand('navBack');
            }}>
              <KeySVG imgSrc={BackImg}/>
            </Button>
            <Button handleClick={() => {
              postCommand('navForward');
            }}>
              <KeySVG imgSrc={ForwardImg}/>
            </Button>
          </div>
          <KeyboardButton kbActive={kbActive}
                          handleClick={handleKeyboardButtonClick}/>
        </div>
      </div>
  );
}

export default WebNav;