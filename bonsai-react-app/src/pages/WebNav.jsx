import {Button} from '../Components/Button';
import CloseImg from '../static/close.svg';
import BackImg from '../static/back.svg';
import ForwardImg from '../static/forward.svg';
import KeyBoardImg from '../static/keyboard.svg';
import {postJson} from '../utilities';
import KeySVG from '../Components/KeySVG';

function postCommand(message) {
  console.log(message);
  postJson({Type: 'command', Message: message});
}

function WebNav(props) {

  let closeButtonClass = 'bg-red-800 active:bg-red-700 hover:bg-red-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  let handleClose = () => {
    postCommand('closeWeb');
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
          <Button className={'w-full flex justify-center'}
                  handleClick={() => {
                    postCommand('spawnKeyboard');
                  }}>
            <KeySVG imgSrc={KeyBoardImg}/>
          </Button>
        </div>
      </div>
  );
}

export default WebNav;