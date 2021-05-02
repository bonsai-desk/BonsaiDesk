import React from 'react';

export function InfoItem({imgSrc, title, slug, children}) {
  return (
      <div className={'flex w-full justify-between'}>
        <div className={'flex w-auto'}>
          <div className={'flex flex-wrap content-center  p-2 mr-2'}>
            {imgSrc ?
                <img className={'h-9 w-9'} src={imgSrc} alt={''}/>
                : ''
            }
          </div>
          <div className={'my-auto'}>
            <div className={'text-xl'}>
              {title}
            </div>
            <div className={'text-gray-400'}>
              {slug}
            </div>
          </div>
        </div>
        {children}
      </div>
  );
}