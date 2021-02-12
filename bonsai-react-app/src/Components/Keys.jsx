import React, {useState} from 'react';
import {animated, useSpring} from 'react-spring';
import {Button} from './Button';
import {postJson} from '../utilities';
import BackSpaceImgHollow from '../static/backspace-hollow.svg';

export function KeyFrames(props) {
  let {handleClick, width} = props;
  const [state, toggle] = useState(false);
  const level = 150;
  const {color} = useSpring(
      {
        reset: true,
        from: {color: `rgba(${level},${level},${level},1)`},
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
  postJson({Type: 'event', Message: 'keyPress', Data: char});
}

export function KeyChar(props) {
  let {char, shift} = props;
  const _char = shift ? char.toUpperCase() : char;
  return <KeyFrames handleClick={() => {
    postChar(_char);
  }}>
    <span className={'w-full text-center text-white text-3xl'}>
      {_char}
    </span>
  </KeyFrames>;
}

export function Enter() {

  return <KeyFrames handleClick={() => {
    postChar('Enter');
  }} width={'8rem'}>Enter</KeyFrames>;

}

export function Backspace() {

  return <KeySVG handleClick={() => {
    postChar('Backspace');
  }} imgSrc={BackSpaceImgHollow}/>;

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

export function Space() {
  return <KeyFrames handleClick={() => {
    postChar(' ');
  }} width={'24rem'}/>;
}