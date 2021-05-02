import React from 'react';

export function MenuContent(props) {
  let {name} = props;

  return (
      <div className={'text-white p-4 h-full pr-8'}>
        {name ?
            <div className={'pb-8 text-xl'}>
              {name}
            </div>
            : ''}
        <div className={'space-y-8 pb-8'}>
          {props.children}
        </div>
      </div>
  );

}