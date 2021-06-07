import React from 'react';

export function InfoItem({imgSrc, title, slug, children, rightPad = true}) {
    let className = 'flex w-full justify-between pr-4';
    if (!rightPad) {
        className = 'flex w-full justify-between';
    }

    return (
            <div className={className}>
                <div className={'flex w-auto'}>
                    <div className={'flex flex-wrap content-center p-2 mr-2'}>
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

export function InfoItemCustom({imgSrc, title, slug, children, padded = true, leftItems}) {
    let className = 'flex w-full justify-between px-4';
    if (!padded) {
        className = 'flex w-full justify-between';
    }
    return (
            <div className={className}>
                <div className={'flex w-auto'}>
                    {leftItems}
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
