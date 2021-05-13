import {MenuContent} from '../components/MenuContent';
import {InfoItem} from '../components/InfoItem';
import tableImg from '../static/table-regular.svg';
import {InstantButton} from '../components/Button';
import {grayButtonClass} from '../cssClasses';
import {Layout, postLightsChange, postSetLayout} from '../api';
import React from 'react';
import LightImg from '../static/lightbulb.svg';

export function YourRoom() {

    let setLayoutAcross = () => {
        postSetLayout(Layout.Across)
    }

    let setLayoutSideBySide = () => {
        postSetLayout(Layout.SideBySide)
    }

    function handleClickVibes() {
        postLightsChange('vibes');
    }

    function handleClickBright() {
        postLightsChange('bright');
    }

    return <MenuContent name={'Lights & Layout'}>


        <InfoItem title={'Lights'} slug={'Set the mood'}
                  imgSrc={LightImg}>
            <div className={'flex space-x-2'}>
                <InstantButton onClick={handleClickVibes}
                               className={grayButtonClass}>Vibes</InstantButton>
                <InstantButton onClick={handleClickBright}
                               className={grayButtonClass}>Bright</InstantButton>

            </div>
        </InfoItem>

        <InfoItem title={"Layout"} slug={"Only when someone is in your room"} imgSrc={tableImg}>
            <div className={"flex flex-wrap space-x-2"}>
                <InstantButton onClick={setLayoutSideBySide} className={grayButtonClass}>Side by Side</InstantButton>
                <InstantButton onClick={setLayoutAcross} className={grayButtonClass}>Opposite</InstantButton>
            </div>
        </InfoItem>

    </MenuContent>;
}
