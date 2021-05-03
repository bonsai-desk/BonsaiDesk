import {MenuContent} from '../components/MenuContent';
import {Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import {Button, ToggleButton} from '../components/Button';
import {grayButtonClass, greenButtonClass} from '../cssClasses';
import {apache, lgpl, mpl} from '../static/licenses';
import React, {useState} from 'react';
import {InfoItem} from '../components/InfoItem';
import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import {postLightsChange, postToggleBlockBreak, postTogglePinchPull} from '../api';
import LightImg from '../static/lightbulb.svg';

const Settings = observer(() => {

    let {store} = useStore();
    let history = useHistory();
    let match = useRouteMatch();

    function goToInfo() {
        history.push(`${match.path}/about`);
    }

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
                    handleClick={goToInfo}
            >
                About
            </Button>
        </InfoItem>
    </MenuContent>;

});

function AboutPage({back}) {
    let match = useRouteMatch();
    let mplUrl = `${match.path}/mpl`;
    let gplUrl = `${match.path}/gpl`;
    let aplUrl = `${match.path}/apl`;
    let history = useHistory();

    console.log(match.path);
    return <MenuContent name={'About'} back={back}>
        <InfoItem title={'GeckoView'} slug={'Mozilla Public License'}>
            <Button className={grayButtonClass} handleClick={() => {
                history.push(mplUrl);
            }}>
                View
            </Button>
        </InfoItem>
        <InfoItem title={'PDF.js'} slug={'Apache License'}>
            <Button className={grayButtonClass} handleClick={() => {
                history.push(aplUrl);
            }}>
                View
            </Button>
        </InfoItem>
        <InfoItem title={'AdGuard AdBlocker'}
                  slug={'GNU Lesser General Public License'}>
            <Button className={grayButtonClass} handleClick={() => {
                history.push(gplUrl);
            }}>
                View
            </Button>
        </InfoItem>
    </MenuContent>;
}

function About({back}) {
    let match = useRouteMatch();
    return <Switch>
        <Route path={`${match.path}/apl`}><ApacheLicense back={match.path}/></Route>
        <Route path={`${match.path}/gpl`}><LesserGlpl back={match.path}/></Route>
        <Route path={`${match.path}/mpl`}><MozillaPublicLicense back={match.path}/></Route>
        <Route><AboutPage back={back}/></Route>
    </Switch>;

}

function MozillaPublicLicense({handleClickReturn, back}) {
    return (
            <MenuContent name={'Mozilla Public License Version 2.0'} back={back}>
                <div dangerouslySetInnerHTML={{__html: mpl}}/>
            </MenuContent>
    );
}

function LesserGlpl({handleClickReturn, back}) {
    return (
            <MenuContent name={'GNU LESSER GENERAL PUBLIC LICENSE'} back={back}>
                <div className={'flex'}>
                </div>
                <div dangerouslySetInnerHTML={{__html: lgpl}}/>
            </MenuContent>
    );
}

function ApacheLicense({handleClickReturn, back}) {
    return (
            <MenuContent name={'APACHE LICENSE, VERSION 2.0'} back={back}>
                <div className={'flex'}>
                </div>
                <div dangerouslySetInnerHTML={{__html: apache}}/>
            </MenuContent>
    );
}

export const SettingsPage = observer(() => {
    let match = useRouteMatch();

    let [about, setAbout] = useState(false);

    function toggleAbout() {
        setAbout(!about);
    }

    return <Switch>
        <Route exact path={`${match.path}`} component={Settings}/>
        <Route path={`${match.path}/about`}><About back={match.path}/></Route>
    </Switch>;

});
