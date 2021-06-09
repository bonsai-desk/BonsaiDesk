import {MenuContent} from '../components/MenuContent';
import {Route, Switch, useHistory, useRouteMatch} from 'react-router-dom';
import {ForwardButton, ToggleButton} from '../components/Button';
import {grayButtonClass, greenButtonClass} from '../cssClasses';
import {apache, lgpl, mpl} from '../static/licenses';
import React from 'react';
import {InfoItem} from '../components/InfoItem';
import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import {postTogglePinchPull} from '../api';
import {showVersionFromStore} from '../esUtils';

const Settings = observer(() => {

    let {store} = useStore();
    let history = useHistory();
    let match = useRouteMatch();

    function goToInfo() {
        history.push(`${match.path}/about`);
    }

    function handleClickPinchPull() {
        postTogglePinchPull();
    }

    return <MenuContent name={'Settings'}>
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
        <div className={'text-xl'}>
            Info
        </div>
        <InfoItem title={'About'}
                  slug={showVersionFromStore(store)}>
            <ForwardButton onClick={goToInfo}/>
        </InfoItem>
    </MenuContent>;

});

function AboutPage({back}) {
    let match = useRouteMatch();
    let mplUrl = `${match.path}/mpl`;
    let gplUrl = `${match.path}/gpl`;
    let aplUrl = `${match.path}/apl`;
    let history = useHistory();

    return <MenuContent name={'About'} back={back}>
        <InfoItem title={'GeckoView'} slug={'Mozilla Public License'}>
            <ForwardButton onClick={()=>{history.push(mplUrl)}}/>
        </InfoItem>
        <InfoItem title={'PDF.js'} slug={'Apache License'}>
            <ForwardButton onClick={()=>{history.push(aplUrl)}}/>
        </InfoItem>
        <InfoItem title={'AdGuard AdBlocker'}
                  slug={'GNU Lesser General Public License'}>
            <ForwardButton onClick={()=>{history.push(gplUrl)}}/>
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

    return <Switch>
        <Route exact path={`${match.path}`} component={Settings}/>
        <Route path={`${match.path}/about`}><About back={match.path}/></Route>
    </Switch>;

});
