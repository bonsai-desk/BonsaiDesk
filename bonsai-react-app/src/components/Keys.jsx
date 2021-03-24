import React, {useState} from 'react';
import {animated, useSpring} from 'react-spring';
import {Button} from './Button';
import {postJson} from '../utilities';
import BackSpaceImgHollow from '../static/backspace-hollow.svg';
import CaretSquareUp from '../static/caret-square-up.svg';
import CaretSquareUpHollow from '../static/caret-square-up-hollow.svg';
import PlayImg from '../static/play.svg';

export function Shift(props) {
  let {shift, toggleShift} = props;

  const imgHidden = 'hidden h-10 w-10 absolute bottom-0 left-0';
  const imgVisible = 'h-10 w-10 absolute -bottom-5 left-1';

  return <KeyFrames handleClick={toggleShift}>
    <div className={'relative w-full flex justify-center'}>
      <img className={shift ? imgVisible : imgHidden} src={CaretSquareUp}
           alt={''}/>
      <img className={shift ? imgHidden : imgVisible}
           src={shift ? CaretSquareUp : CaretSquareUpHollow} alt={''}/>
    </div>
  </KeyFrames>;
}

export function KeyFrames({handleClick, width, children}) {
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
      <Button handleClick={handleClick} shouldPostDown={false}
              shouldPostUp={false} shouldPostHover={false}>
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
              {children}
            </div>
          </animated.div>
        </div>
      </Button>
  );
}

export function NumsOrChar(props) {
  let {handleClick, level} = props;

  let shift;
  shift = level === 0;

  return <KeyFrames handleClick={handleClick} width={'7em'}>
    {shift ? '.?123' : 'ABC'}
  </KeyFrames>;
}

export function SymbolsOrNum(props) {
  let {handleClick, level} = props;

  let shift;
  shift = level !== 1;

  return <KeyFrames handleClick={handleClick} width={'5em'}>
    <span className={'w-full -m-1'}>
    {shift ? '123' : '#+='}
    </span>
  </KeyFrames>;
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

export function KeySVG(props) {
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