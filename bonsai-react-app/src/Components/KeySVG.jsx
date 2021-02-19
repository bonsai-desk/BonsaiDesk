import React from 'react';

function KeySVG(props) {
  let {imgSrc, className} = props;

  let buttonClass;
  if (className) {
    buttonClass = className;
  } else {
    buttonClass = 'bg-gray-800 active:bg-gray-700 hover:bg-gray-600 rounded cursor-pointer w-20 h-20 flex flex-wrap content-center';
  }

  const imgVisible = 'h-10 w-10 absolute -bottom-5 left-5';

  return (
      <div className={buttonClass}>
        <div className={'relative w-full flex justify-center'}>
          <img className={imgVisible}
               src={imgSrc} alt={''}/>
        </div>
      </div>
  );
}

export default KeySVG;