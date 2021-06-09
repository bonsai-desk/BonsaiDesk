import {InfoItem} from './InfoItem';
import LinkImg from '../static/link.svg';
import {NormalButton} from './Button';
import {postLeaveRoom} from '../api';
import {redButtonClass} from '../cssClasses';
import React from 'react';

export function ClientConnectedItem() {
    return <InfoItem title={'Connected'} slug={'You are connected to a host'}
                     imgSrc={LinkImg}>
        <NormalButton onClick={postLeaveRoom}
                      className={redButtonClass}>Exit</NormalButton>
    </InfoItem>;

}