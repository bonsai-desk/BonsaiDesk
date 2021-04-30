import {MenuContent} from '../components/MenuContent';
import {Button, ToggleButton} from '../components/Button';
import {grayButtonClass, greenButtonClass} from '../cssClasses';
import {apache, lgpl, mpl} from '../static/licenses';
import React, {useState} from 'react';
import {InfoItem} from '../components/InfoItem';
import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import {
  postLightsChange,
  postToggleBlockBreak,
  postTogglePinchPull,
} from '../api';
import LightImg from '../static/lightbulb.svg';

export const SettingsPage = observer(() => {
  let {store} = useStore();

  let [about, setAbout] = useState(false);

  function handleClickVibes() {
    postLightsChange('vibes');
  }

  function handleClickBright() {
    postLightsChange('bright');
  }

  function handleClickPinchPull() {
    postTogglePinchPull();
  }

  function handleClickBlockBreak() {
    postToggleBlockBreak();

  }

  function toggleAbout() {
    setAbout(!about);
  }

  if (about) {
    return <AboutPage handleClickReturn={toggleAbout}/>;
  }

  return <MenuContent name={'Settings'}>
    <InfoItem title={'Lights'} slug={'Set the mood'}
              imgSrc={LightImg}>
      <div className={'flex space-x-2'}>
        <Button handleClick={handleClickVibes}
                className={grayButtonClass}>Vibes</Button>
        <Button handleClick={handleClickBright}
                className={grayButtonClass}>Bright</Button>

      </div>
    </InfoItem>
    <div className={'text-xl'}>
      Experimental
    </div>
    <InfoItem title={'Pinch Pull'}
              slug={'Point at object with pinched fingers'}
    >
      <ToggleButton
          classEnabled={greenButtonClass}
          classDisabled={grayButtonClass}
          enabled={store.ExperimentalInfo.PinchPullEnabled}
          handleClick={handleClickPinchPull}
      >
        Toggle
      </ToggleButton>
    </InfoItem>
    <InfoItem title={'Block Break'}
              slug={'Delete blocks by touching them (right index finger)'}>
      <ToggleButton
          classEnabled={greenButtonClass}
          classDisabled={grayButtonClass}
          enabled={store.ExperimentalInfo.BlockBreakEnabled}
          handleClick={handleClickBlockBreak}
      >
        Toggle
      </ToggleButton>
    </InfoItem>
    <div className={'text-xl'}>
      Information
    </div>
    <InfoItem title={'Version'}
              slug={store.AppInfo.Version + 'b' + store.AppInfo.BuildId}>
      <Button
          className={grayButtonClass}
          handleClick={toggleAbout}
      >
        About
      </Button>
    </InfoItem>
  </MenuContent>;
});

function AboutPage({handleClickReturn}) {
  // 0 : main
  // 1 : MPL
  // 2 : LGPL
  let [view, setView] = useState(0);

  function viewMain() {
    setView(0);
  }

  function viewMpl() {
    setView(1);
  }

  function viewLgpl() {
    setView(2);
  }

  function viewApache() {
    setView(3);
  }

  if (view === 1) {
    return <MozillaPublicLicense handleClickReturn={viewMain}/>;
  }

  if (view === 2) {
    return <LesserGlpl handleClickReturn={viewMain}/>;
  }

  if (view === 3) {
    return <ApacheLicense handleClickReturn={viewMain}/>;
  }

  return (
      <MenuContent name={'About'}>
        <div className={'flex'}>
          <Button className={grayButtonClass} handleClick={handleClickReturn}>
            Return to Settings
          </Button>
        </div>
        <div className={'text-xl'}>
          Credits
        </div>
        <InfoItem title={'GeckoView'} slug={'Mozilla Public License'}>
          <Button className={grayButtonClass} handleClick={viewMpl}>
            View
          </Button>
        </InfoItem>
        <InfoItem title={'PDF.js'} slug={'Apache License'}>
          <Button className={grayButtonClass} handleClick={viewApache}>
            View
          </Button>
        </InfoItem>
        <InfoItem title={'AdGuard AdBlocker'}
                  slug={'GNU Lesser General Public License'}>
          <Button className={grayButtonClass} handleClick={viewLgpl}>
            View
          </Button>
        </InfoItem>
      </MenuContent>
  );
}

function MozillaPublicLicense({handleClickReturn}) {
  return (
      <MenuContent name={'Mozilla Public License Version 2.0'}>
        <div className={'flex'}>
          <Button className={grayButtonClass} handleClick={handleClickReturn}>
            Return
          </Button>
        </div>
        <div dangerouslySetInnerHTML={{__html: mpl}}/>
      </MenuContent>
  );
}

function LesserGlpl({handleClickReturn}) {
  return (
      <MenuContent name={'GNU LESSER GENERAL PUBLIC LICENSE'}>
        <div className={'flex'}>
          <Button className={grayButtonClass} handleClick={handleClickReturn}>
            Return
          </Button>
        </div>
        <div dangerouslySetInnerHTML={{__html: lgpl}}/>
      </MenuContent>
  );
}

function ApacheLicense({handleClickReturn}) {
  return (
      <MenuContent name={'APACHE LICENSE, VERSION 2.0'}>
        <div className={'flex'}>
          <Button className={grayButtonClass} handleClick={handleClickReturn}>
            Return
          </Button>
        </div>
        <div dangerouslySetInnerHTML={{__html: apache}}/>
      </MenuContent>
  );
}