import React from 'react';

import {postJson} from '../utilities';

function postMouseDown() {
  postJson({Type: 'event', Message: 'mouseDown'});
}

function postMouseUp() {
  postJson({Type: 'event', Message: 'mouseUp'});
}

function postHover() {
  postJson({Type: 'event', Message: 'hover'});
}

export function Button(props) {
  let {
    handleClick,
    className = '',
    shouldPostDown = true,
    shouldPostHover = true,
    shouldPostUp = true,
  } = props;
  return (
      <div onPointerEnter={shouldPostHover ? postHover : null}
           onPointerDown={() => {
             handleClick();
             if (shouldPostDown) {
               postMouseDown();
             }
           }}
           onPointerUp={shouldPostUp ? postMouseUp : null}
      >
        <div className={className}>
          {props.children}
        </div>
      </div>
  );
}

