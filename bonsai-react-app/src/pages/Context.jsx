import {observer} from 'mobx-react-lite';
import {HandActive, HandMode, useStore} from '../DataProvider';
import {postChangeActiveBlock, postSetHand, postSetHandMode} from '../api';
import wood1 from '../static/wood1.png';
import wood2 from '../static/wood2.png';
import wood3 from '../static/wood3.png';
import wood4 from '../static/wood4.png';
import wood6 from '../static/wood6.png';
import bearing from '../static/bearing.png';
import none from '../static/value-none.svg';

function Button({children, onClick, active}) {
    let classActive = 'p-1 bg-gray-100 rounded-lg';
    let classInactive = 'p-1 rounded-lg';
    return (<div className={active ? classActive : classInactive} onPointerDown={onClick}>
        <div className={'h-16 w-16 rounded-lg overflow-hidden'}>
            {children}
        </div>
    </div>);
}

const BlockButton = observer(({hand, blockId, children}) => {

    let {store} = useStore();
    let active = false;
    if (hand === 'left') {
        active = store.ContextInfo.LeftBlockActive === blockId;
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightBlockActive === blockId;
    }

    let onClick = () => {
        postChangeActiveBlock(hand, blockId);
        postSetHandMode(HandMode.None);
    };
    return <Button onClick={onClick} active={active}>
        {children}
    </Button>;
});

function ButtonRow({children}) {
    return <div className={'flex flex-wrap space-x-4'}>{children}</div>;
}

function ButtonContainer({children}) {
    return <div className={'space-y-4'}>{children}</div>;
}

function ButtonGrid({hand}) {
    return (
            <ButtonContainer>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={'wood1'}>
                        <img src={wood1} alt={'wood1'}/>
                    </BlockButton>
                    <BlockButton hand={hand} blockId={'wood2'}>
                        <img src={wood2} alt={'wood2'}/>
                    </BlockButton>
                    <BlockButton hand={hand} blockId={'wood3'}>
                        <img src={wood3} alt={'wood3'}/>
                    </BlockButton>
                </ButtonRow>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={'wood4'}>
                        <img src={wood4} alt={'wood4'}/>
                    </BlockButton>
                    <BlockButton hand={hand} blockId={'wood5'}>
                        <img src={wood6} alt={'wood6'}/>
                    </BlockButton>
                    <BlockButton hand={hand} blockId={'bearing'}>
                        <div className={'flex flex-wrap content-center justify-center h-full w-full bg-gray-800'}>
                            <img className={'h-9/12 w-9/12'} src={bearing} alt={'bearing'}/>
                        </div>
                    </BlockButton>
                </ButtonRow>
                <div className={'w-full flex justify-center'}>
                    <BlockButton hand={hand} blockId={''}>
                        <ClearIcon/>
                    </BlockButton>
                </div>
            </ButtonContainer>
    );
}

const BlockBreakButton = observer(({children, mode}) => {
    let {store} = useStore();

    let blockBreakOn = store.ContextInfo.HandMode === mode;

    let className = 'bg-gray-800 h-full w-full text-white flex flex-wrap content-center justify-center';

    function Inner() {
        return <div className={className}>
            {children}
        </div>;
    }

    function onClick() {
        postSetHandMode(mode);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active={blockBreakOn} onClick={onClick}><Inner/></Button>
    </div>;
});

function BlockBreakNew() {
    return <BlockBreakButton mode={HandMode.Single}>
        <div>
            <div>Delete</div>
            <div>Block</div>
        </div>
    </BlockBreakButton>;
}

function WholeBreak() {
    return <BlockBreakButton mode={HandMode.Whole}>
        <div>
            <div>Delete</div>
            <div>Chunk</div>
        </div>
    </BlockBreakButton>;
}

function Save() {
    return <BlockBreakButton mode={HandMode.Save}>
        Save
    </BlockBreakButton>;
}

function Duplicate() {
    return <BlockBreakButton mode={HandMode.Duplicate}>
        Clone
    </BlockBreakButton>;
}

function ClearIcon() {
    return <div className={'flex flex-wrap content-center justify-center h-full w-full bg-gray-800'}>
        <img className={'h-8/12 w-8/12'} src={none} alt={'none'}/>
    </div>;
}

function ClearHand() {
    return <BlockBreakButton mode={HandMode.None}>
        <ClearIcon/>
    </BlockBreakButton>;
}

const HandModes = observer(() => {
    let {store} = useStore();

    let hand = store.ContextInfo.HandActive;

    function clickLeft() {
        postSetHand('left');
    }

    function clickRight() {
        postSetHand('right');
    }

    return (
            <div className={'flex flex-wrap w-full justify-center space-x-8'}>
                <div className={'flex space-x-2'}>
                    <Button onClick={clickLeft} active={hand === HandActive.Left}>
                        <div className={'h-full w-full bg-gray-800 content-center justify-center flex flex-wrap'}>
                            <span className={'text-white text-3xl'}>L</span>
                        </div>
                    </Button>
                    <Button onClick={clickRight} active={hand === HandActive.Right}>
                        <div className={'h-full w-full bg-gray-800 content-center justify-center flex flex-wrap'}>
                            <span className={'text-white text-3xl'}>R</span>
                        </div>
                    </Button>
                </div>
                <div className={'space-x-2 flex'}>
                    <Duplicate hand={'left'}/>
                    <WholeBreak hand={'left'}/>
                    <BlockBreakNew/>
                    <Save hand={'left'}/>
                    <ClearHand hand={'left'}/>
                </div>
            </div>
    );

});

const Context = observer(() => {
    document.title = 'Context Menu';
    return (
            <div className={'bg-gray-900 flex flex-wrap content-center h-screen space-y-10'}>
                <HandModes/>
                <div className={"bg-gray-700 rounded h-4 w-full mx-6"}/>
                <div className={'flex flex-wrap justify-center space-x-20 w-full'}>
                    <ButtonGrid hand={'left'}/>
                    <ButtonGrid hand={'right'}/>
                </div>
            </div>

    );
});

export default Context;