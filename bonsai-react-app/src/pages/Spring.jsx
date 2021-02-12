import React from 'react';
import {animated, useSpring} from 'react-spring';
import {useDrag} from 'react-use-gesture';

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

let Page = () => {
  return <Simple/>;
};

export default Page;