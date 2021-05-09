import {MenuContent} from '../components/MenuContent';
import {InfoItem} from '../components/InfoItem';
import YtImg from '../static/yt-small.png';
import {NormalButton} from '../components/Button';
import {greenButtonClass} from '../cssClasses';
import {postBrowseYouTube} from '../api';
import React from 'react';

export function VideosPage() {
  return <MenuContent name={'Media'}>
    <InfoItem imgSrc={YtImg} title={'YouTube'}
              slug={'Find videos to watch on the big screen'}>
      <NormalButton className={greenButtonClass} onClick={postBrowseYouTube}>
        Browse
      </NormalButton>
    </InfoItem>
  </MenuContent>;
}