import React, {useState} from 'react';
import Button from '../Components/Button';
import {postJson} from '../utilities';
import CaretSquareUpHollow from '../static/caret-square-up-hollow.svg';
import CaretSquareUp from '../static/caret-square-up.svg';
import BackSpaceImg from '../static/backspace.svg';
import BackSpaceImgHollow from '../static/backspace-hollow.svg';
import KeyBoardImg from '../static/keyboard-dismiss.svg';

const roundButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center';

function postChar(char) {
  console.log(char);
  postJson({Type: 'event', Message: 'keyPress', Data: char});
}

function postKeyEvent(event) {
  console.log(event);
  postJson({Type: 'event', Message: 'keyEvent', Data: event});
}

function Key(props) {
  let {char, shift, handleClick} = props;
  const _char = shift ? char.toUpperCase() : char;
  return (
      <Button>
        <div className={roundButtonClass} onMouseDown={() => {
          postChar(_char);
          if (handleClick) handleClick();

        }}>
    <span className={'w-full text-center text-white text-3xl'}>
      {_char}
    </span>
        </div>
      </Button>);
}

function KeyBoardDismiss() {
  const shiftButtonClass = 'bg-gray-900 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  const imgVisible = 'h-10 w-10';

  return <Button>
    <div className={shiftButtonClass}>
      <div onClick={()=>{postKeyEvent("dismiss")}} className={'w-full flex justify-center'}>
        <img className={imgVisible}
             src={KeyBoardImg} alt={''}/>
      </div>
    </div>
  </Button>;

}

function BackSpace() {
  let [pressed, setPressed] = useState(false);

  const shiftButtonClass = 'bg-gray-900 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  const imgHidden = 'hidden h-10 w-10 absolute bottom-0 left-0';
  const imgVisible = 'h-10 w-10 absolute -bottom-5 left-5';

  return <Button>
    <div
        onMouseDown={() => {
          setPressed(true);
          postKeyEvent('backspace');
        }}
        onMouseUp={() => {
          setPressed(false);
        }}
        className={shiftButtonClass}>
      <div className={'relative w-full flex justify-center'}>
        <img className={pressed ? imgVisible : imgHidden} src={BackSpaceImg}
             alt={''}/>
        <img className={pressed ? imgHidden : imgVisible}
             src={BackSpaceImgHollow} alt={''}/>
      </div>
    </div>
  </Button>;

}

function Shift(props) {
  let {shift, toggleShift} = props;

  const shiftButtonClass = 'bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';
  const shiftButtonClassActive = 'bg-gray-600 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  const imgHidden = 'hidden h-10 w-10 absolute bottom-0 left-0';
  const imgVisible = 'h-10 w-10 absolute -bottom-5 left-5';

  return <Button>
    <div onMouseDown={toggleShift}
         className={shift ? shiftButtonClassActive : shiftButtonClass}>
      <div className={'relative w-full flex justify-center'}>
        <img className={shift ? imgVisible : imgHidden} src={CaretSquareUp}
             alt={''}/>
        <img className={shift ? imgHidden : imgVisible}
             src={shift ? CaretSquareUp : CaretSquareUpHollow} alt={''}/>
      </div>
    </div>
  </Button>;
}

function NumsOrChar(props) {
  let {handleClick} = props;
  let [shift, setShift] = useState(false);

  const shiftButtonClass = 'bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  return <Button>
    <div onMouseDown={() => {
      handleClick();
      setShift(!shift);
    }}
         className={shiftButtonClass}>
      <div
          className={'relative w-full flex justify-center text-white text-3xl'}>
        {shift ? '123' : 'ABC'}
      </div>
    </div>
  </Button>;
}

function SymbolsOrNum(props) {
  let {handleClick} = props;
  let [shift, setShift] = useState(false);

  const shiftButtonClass = 'bg-gray-900 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';

  return <Button>
    <div onMouseDown={() => {
      handleClick();
      setShift(!shift);
    }}
         className={shiftButtonClass}>
      <div
          className={'relative w-full flex justify-center text-white text-3xl'}>
        {shift ? '123' : '#+='}
      </div>
    </div>
  </Button>;
}

function Space(props) {
  let {char, shift} = props;
  const _char = shift ? char.toUpperCase() : char;

  const buttonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-full h-20 flex flex-wrap content-center';

  return (
      <Button>
        <div className={buttonClass} onMouseDown={() => {
          postKeyEvent('space');
        }}>
    <span className={'w-80 text-center text-white text-3xl'}>
      {_char}
    </span>
        </div>
      </Button>);
}

function Keyboard() {
  let [shift, setShift] = useState(false);
  let [level, setLevel] = useState(0);
  let level0 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'q'}/>
          <Key shift={shift} char={'w'}/>
          <Key shift={shift} char={'e'}/>
          <Key shift={shift} char={'r'}/>
          <Key shift={shift} char={'t'}/>
          <Key shift={shift} char={'y'}/>
          <Key shift={shift} char={'u'}/>
          <Key shift={shift} char={'i'}/>
          <Key shift={shift} char={'o'}/>
          <Key shift={shift} char={'p'}/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'a'}/>
          <Key shift={shift} char={'s'}/>
          <Key shift={shift} char={'d'}/>
          <Key shift={shift} char={'f'}/>
          <Key shift={shift} char={'g'}/>
          <Key shift={shift} char={'h'}/>
          <Key shift={shift} char={'j'}/>
          <Key shift={shift} char={'k'}/>
          <Key shift={shift} char={'l'}/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <Shift shift={shift} toggleShift={() => {
            setShift(!shift);
          }}/>
          <Key shift={shift} char={'z'}/>
          <Key shift={shift} char={'x'}/>
          <Key shift={shift} char={'c'}/>
          <Key shift={shift} char={'v'}/>
          <Key shift={shift} char={'b'}/>
          <Key shift={shift} char={'n'}/>
          <Key shift={shift} char={'m'}/>
          <BackSpace/>
        </div>
      </React.Fragment>
  );

  let level1 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'1'}/>
          <Key shift={shift} char={'2'}/>
          <Key shift={shift} char={'3'}/>
          <Key shift={shift} char={'4'}/>
          <Key shift={shift} char={'5'}/>
          <Key shift={shift} char={'6'}/>
          <Key shift={shift} char={'7'}/>
          <Key shift={shift} char={'8'}/>
          <Key shift={shift} char={'9'}/>
          <Key shift={shift} char={'10'}/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'-'}/>
          <Key shift={shift} char={'/'}/>
          <Key shift={shift} char={':'}/>
          <Key shift={shift} char={';'}/>
          <Key shift={shift} char={'('}/>
          <Key shift={shift} char={')'}/>
          <Key shift={shift} char={'$'}/>
          <Key shift={shift} char={'&'}/>
          <Key shift={shift} char={'@'}/>
          <Key shift={shift} char={'"'}/>
        </div>
      </React.Fragment>
  );
  let level2 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'['}/>
          <Key shift={shift} char={']'}/>
          <Key shift={shift} char={'{'}/>
          <Key shift={shift} char={'}'}/>
          <Key shift={shift} char={'#'}/>
          <Key shift={shift} char={'%'}/>
          <Key shift={shift} char={'^'}/>
          <Key shift={shift} char={'*'}/>
          <Key shift={shift} char={'+'}/>
          <Key shift={shift} char={'='}/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <Key shift={shift} char={'_'}/>
          <Key shift={shift} char={'\\'}/>
          <Key shift={shift} char={'|'}/>
          <Key shift={shift} char={'~'}/>
          <Key shift={shift} char={'<'}/>
          <Key shift={shift} char={'>'}/>
          <Key shift={shift} char={'€'}/>
          <Key shift={shift} char={'£'}/>
          <Key shift={shift} char={'¥'}/>
          <Key shift={shift} char={'•'}/>
        </div>
      </React.Fragment>
  );
  let level12 = (
      <div className={'flex space-x-2 justify-center'}>
        <SymbolsOrNum handleClick={handleClickSymbolOrNum}/>
        <Key shift={shift} char={'.'}/>
        <Key shift={shift} char={','}/>
        <Key shift={shift} char={'?'}/>
        <Key shift={shift} char={'!'}/>
        <Key handleClick={() => {
          setLevel(0);
        }} shift={shift} char={'\''}/>
        <BackSpace/>
      </div>
  );

  function handleClickSymbolOrNum() {
    switch (level) {
      case 1:
        setLevel(2);
        break;
      default:
        setLevel(1);
    }
  }

  function handleClickNumOrChar() {
    switch (level) {
      case 0:
        setLevel(1);
        break;
      default:
        setLevel(0);
        break;
    }

  }

  return (
      <div className={'w-full h-screen bg-black space-y-2'}>
        {level === 0 ? level0 : ''}
        {level === 1 ? level1 : ''}
        {level === 2 ? level2 : ''}
        {level === 1 || level === 2 ? level12 : ''}
        <div className={'w-full flex space-x-2 justify-center'}>
          <NumsOrChar handleClick={handleClickNumOrChar}/>
          <Space/>
          <KeyBoardDismiss/>
        </div>

      </div>
  );
}

export default Keyboard;