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

function Button(props) {
  let {className=""} = props
  return (
      <div onMouseEnter={postHover}
           onTouchStart={postMouseDown}
           onTouchEnd={postMouseUp}
           className={className}
      >
        {props.children}
      </div>
  );
}

export default Button;