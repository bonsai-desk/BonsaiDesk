import {MenuContent} from '../components/MenuContent';
import {InfoItem} from '../components/InfoItem';
import YtImg from '../static/yt-small.png';
import {Button} from '../components/Button';
import {greenButtonClass} from '../cssClasses';
import {postBrowseYouTube} from '../api';
import React from 'react';

export function VideosPage() {
  return <MenuContent name={'Videos'}>
    <InfoItem imgSrc={YtImg} title={'YouTube'}
              slug={'Find videos to watch on the big screen'}>
      <Button className={greenButtonClass} handleClick={postBrowseYouTube}>
        Browse
      </Button>
    </InfoItem>
  </MenuContent>;
}