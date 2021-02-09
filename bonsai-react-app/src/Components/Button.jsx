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

function SoundButton(props) {
  let {className = ''} = props;
  return (
      <div onPointerEnter={postHover}
           onPointerDown={postMouseDown}
           onPointerUp={postMouseUp}
           className={className}
      >
        {props.children}
      </div>
  );
}

export function Button(props) {
  let {handleClick, className = ''} = props;
  return (
      <SoundButton>
        <div className={className} onPointerDown={handleClick}>
          {props.children}
        </div>
      </SoundButton>

  );
}

