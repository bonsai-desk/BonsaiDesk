import React, {useState} from 'react';
import {animated, useSpring} from 'react-spring';
import {useDrag} from 'react-use-gesture';
import BackSpaceImgHollow from '../static/backspace-hollow.svg';
import {Button} from '../Components/Button';
import {postJson} from '../utilities';

const roundButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-20 h-20 flex flex-wrap content-center';
const stretchButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer h-20 flex flex-wrap content-center';

function Simple() {

  const [{x, y}, set] = useSpring(
      () => ({x: 0, y: 0, config: {mass: 1, tension: 400, friction: 5}}));

  const bind = useDrag(({down, movement: [mx, my]}) => {
    set({x: down ? mx : 0, y: down ? my : 0});
  });

  return (
      <div
          className={'h-screen w-full flex flex-wrap content-center justify-center'}>
        <animated.div {...bind()} className={'bg-red-400 h-10 w-10 rounded-lg'}
                      style={{x, y}}/>
      </div>
  );

}

function KeyFrames(props) {
  let {handleClick, width} = props;
  const [state, toggle] = useState(false);
  const {color} = useSpring(
      {
        reset: true,
        from: {color: 'rgba(38,38,38,0.5)'},
        color: 'rgba(38,38,38,1)',
        config: {duration: 400},
      });
  return (
      <Button handleClick={handleClick}>
        <div onClick={() => toggle(!state)} className={''}>
          <animated.div
              style={{
                width: width ? width : '5rem',
                height: '5rem',
                padding: '1rem',
                cursor: 'pointer',
                borderRadius: '0.25rem',
                background: color,
                display: 'flex',
                flexWrap: 'wrap',
                alignContent: 'center',
              }}
          >
            <div
                className={'w-full text-center text-white text-3xl'}>
              {props.children}
            </div>
          </animated.div>
        </div>
      </Button>
  );
}

function postChar(char) {
  console.log('postchar ' + char);
  postJson({Type: 'event', Message: 'keyPress', Data: char});
}

function KeyChar(props) {
  let {char, shift, stretch = false} = props;
  const _char = shift ? char.toUpperCase() : char;
  return <KeyFrames handleClick={() => {
    postChar(_char);
  }}>
    <span className={'w-full text-center text-white text-3xl'}>
      {_char}
    </span>
  </KeyFrames>;
}

function Enter() {

  return <KeyFrames handleClick={()=>{postChar("Enter")}} width={"8rem"}>Enter</KeyFrames>;

}

function Backspace () {

  return <KeySVG handleClick={()=>{postChar("Backspace")}} imgSrc={BackSpaceImgHollow}/>

}

function Key(props) {
  let {char, shift, handleClick, stretch = false, className} = props;
  const _char = shift ? char.toUpperCase() : char;
  let _className;
  if (className) {
    _className = className;
  } else {
    _className = !stretch ? roundButtonClass : stretchButtonClass;
  }
  return (
      <Button>
        <div className={_className} onMouseDown={() => {
          postChar(_char);
          if (handleClick) handleClick();

        }}>
    <span className={'w-full text-center text-white text-3xl'}>
      {_char}
    </span>
        </div>
      </Button>);
}

function KeySVG(props) {
  let {imgSrc, handleClick} = props;
  return (
      <KeyFrames handleClick={handleClick}>
        <div className={'relative w-full flex justify-center'}>
          <img className={'h-10 w-10 absolute -bottom-5 left-1'} src={imgSrc}
               alt={''}/>
        </div>
      </KeyFrames>
  );

}

const wideButtonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded p-4 cursor-pointer w-32 h-20 flex flex-wrap content-center';

let Page = () => {
  return <div className={'space-x-2 flex'}>
    <KeyChar char={'a'} shift={true}/>
    <KeyChar char={'a'} shift={false}/>
    <Backspace/>
    <Enter/>
  </div>;
};

export default Page;