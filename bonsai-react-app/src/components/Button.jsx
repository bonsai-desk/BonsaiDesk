import React from 'react';

import {postJson} from '../utilities';
import {smallRoundButtonClass} from '../cssClasses';
import ForwardImg from "../static/forward.svg"
import BackImg from "../static/back.svg"

function postMouseDown() {
    postJson({Type: 'event', Message: 'mouseDown'});
}

function postMouseUp() {
    postJson({Type: 'event', Message: 'mouseUp'});
}

function postHover() {
    postJson({Type: 'event', Message: 'hover'});
}

export function ToggleButton(props) {
    let {
        handleClick,
        classDisabled = '',
        classEnabled = '',
        shouldPostDown = true,
        shouldPostHover = true,
        shouldPostUp = true,
        enabled = false,
    } = props;
    return (
            <div onPointerEnter={shouldPostHover ? postHover : null}
                 onPointerDown={() => {
                     handleClick();
                     if (shouldPostDown) {
                         postMouseDown();
                     }
                 }}
                 onPointerUp={shouldPostUp ? postMouseUp : null}
            >
                <div className={enabled ? classEnabled : classDisabled}>
                    {props.children}
                </div>
            </div>
    );
}

export function NormalButton (props) {
    let {onClick, children} = props;
    return <Button {...props} handleClick={delay(onClick)}>{children}</Button>
}

export function InstantButton(props){
    let {onClick, children} = props;
    return <Button {...props} handleClick={onClick}>{children}</Button>
}

function Button(props) {
    let {
        handleClick,
        className = '',
        shouldPostDown = true,
        shouldPostHover = true,
        shouldPostUp = true,
    } = props;
    return (
            <div onPointerEnter={shouldPostHover ? postHover : null}
                 onPointerDown={() => {
                     handleClick();
                     if (shouldPostDown) {
                         postMouseDown();
                     }
                 }}
                 onPointerUp={shouldPostUp ? postMouseUp : null}
            >
                <div className={className}>
                    {props.children}
                </div>
            </div>
    );
}

function delay(onClick) {
    return () => {
        setTimeout(onClick, 175)
    }
}

export function ForwardButton ({onClick}) {
    return <Button className={smallRoundButtonClass} handleClick={delay(onClick)}>
        <img src={ForwardImg} alt={"forward"}/>
    </Button>
}

export function BackButton ({onClick}) {
    return <Button className={smallRoundButtonClass} handleClick={delay(onClick)}>
        <img src={BackImg} alt={"back"}/>
    </Button>
}


export function UpButton(props) {
    let {
        handleClick,
        className = '',
        shouldPostDown = true,
        shouldPostHover = true,
        shouldPostUp = true,
    } = props;
    return (
            <div onPointerEnter={shouldPostHover ? postHover : null}
                 onPointerDown={() => {
                     if (shouldPostDown) {
                         postMouseDown();
                     }
                 }}
                 onPointerUp={() => {
                     handleClick();
                     if (shouldPostUp) {
                         postMouseUp();
                     }
                 }}
            >
                <div className={className}>
                    {props.children}
                </div>
            </div>
    );
}

