import React from 'react';
import {useHistory} from 'react-router-dom';
import {BackButton} from './Button';

export function MenuContentTabbed(props) {
    let {name, back, navBar} = props;
    let history = useHistory();

    return (
            <div className={'text-white h-full'}>
                <div className={'w-full fixed flex flex-wrap content-center h-24 bg-gray-900 z-10'}>
                    <div className={'flex flex-wrap px-4 w-9/12'}>
                        {navBar}
                    </div>
                </div>
                <div className={back ? 'h-24' : 'h-20'}/>
                <div className={'space-y-8 p-4 pb-8'}>
                    {props.children}
                </div>
            </div>
    );
}

export function MenuContent(props) {
    let {name, back} = props;
    let history = useHistory();

    return (
            <div className={'text-white h-full'}>
                <div className={'w-full fixed flex flex-wrap content-center h-24 bg-gray-900'}>
                    <div className={'flex flex-wrap pl-4 space-x-6'}>
                        {back ?
                                <BackButton onClick={() => {
                                    history.push(back);
                                }}/> : ''

                        }
                        {name ?

                                <div className={'flex flex-wrap content-center'}>
                                    <div className={'text-2xl'}>
                                        {name}
                                    </div>
                                </div>

                                : ''}
                    </div>
                </div>
                <div className={back ? 'h-24' : 'h-20'}/>
                <div className={'space-y-8 p-4 pb-8'}>
                    {props.children}
                </div>
            </div>
    );
}

export function MenuContentFixed(props) {
    let {name, back} = props;
    let history = useHistory();
    return (
            <div className={'text-white h-full'}>
                <div className={'w-full fixed flex flex-wrap content-center h-24 bg-gray-900'}>
                    <div className={'flex flex-wrap pl-4 space-x-6'}>
                        {back ?
                                <BackButton onClick={() => {
                                    history.push(back);
                                }}/> : ''

                        }
                        {name ?

                                <div className={'flex flex-wrap content-center'}>
                                    <div className={'text-2xl'}>
                                        {name}
                                    </div>
                                </div>

                                : ''}
                    </div>
                </div>
                <div className={'h-full'}>
                    {props.children}
                </div>
            </div>
    );
}