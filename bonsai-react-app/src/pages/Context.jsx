import {observer} from 'mobx-react-lite';
import {useStore} from '../DataProvider';
import {postChangeActiveBlock, postSetHandMode} from '../api';

function Button({children, onClick, active}) {
    let classInactive = 'h-20 w-20 bg-gray-600 rounded';
    let classActive = 'h-20 w-20 bg-gray-600 rounded border-bonsai-orange border-solid border-4 border-light-blue-500';
    return <div className={active ? classActive : classInactive} onPointerDown={onClick}>{children}</div>;
}

const BlockButton = observer(({hand, blockId}) => {

    let {store} = useStore();
    let activeBlock = '';
    let active = false;
    if (hand === 'left') {
        active = store.ContextInfo.LeftBlockActive == blockId;
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightBlockActive == blockId;
    }

    let onClick = () => {
        postChangeActiveBlock(hand, blockId);
    };
    return <Button onClick={onClick} active={active}>{blockId}</Button>;
});

function ButtonRow({children}) {
    return <div className={'flex flex-wrap space-x-4'}>{children}</div>;
}

function ButtonContainer({children}) {
    return <div className={'space-y-4'}>{children}</div>;
}

const ActiveItem = observer(({hand}) => {

    let {store} = useStore();
    let activeBlock = '';
    if (hand === 'left') {
        activeBlock = store.ContextInfo.LeftBlockActive;
    }
    if (hand === 'right') {
        activeBlock = store.ContextInfo.RightBlockActive;
    }

    return <div className={'w-full flex justify-center'}>
        <Button>{activeBlock}</Button>
    </div>;
});

const ClearBlock = observer(({hand}) => {
    let {store} = useStore();

    let switchOff = false;

    switch (hand) {
        case 'left':
            switchOff = store.ContextInfo.LeftBlockActive === '';
            break;
        case 'right':
            switchOff = store.ContextInfo.RightBlockActive === '';
            break;
        default:
            console.log(`Toggle blocks for ${hand} not handled`);
            break;
    }

    let className = switchOff ? 'bg-gray-900 h-10' : 'bg-green-400 h-10';

    let onClick = () => {
        if (hand === 'left' || hand === 'right') {
            postChangeActiveBlock(hand, '');
        }
    };

    return <div className={'w-full flex justify-center'}>
        <Button onClick={onClick}>
            <div className={className}/>
        </Button>
    </div>;

});

function ButtonGrid({hand}) {
    return (
            <ButtonContainer>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={'wood1'}/>
                    <BlockButton hand={hand} blockId={'wood2'}/>
                    <BlockButton hand={hand} blockId={'wood3'}/>
                </ButtonRow>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={'wood4'}/>
                    <BlockButton hand={hand} blockId={'wood5'}/>
                    <BlockButton hand={hand} blockId={'wood6'}/>
                </ButtonRow>
                <div className={"w-full flex justify-center"}>
                    <BlockButton hand={hand} blockId={''}/>
                </div>
            </ButtonContainer>
    );
}

const BlockBreak = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === 'blockBreak';
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === 'blockBreak';
    }

    let className = blockBreakOn ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>block break</div>;
    }

    function onClick() {
        postSetHandMode(hand, 'blockBreak');
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const WholeBreak = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === 'wholeBreak';
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === 'wholeBreak';
    }

    let className = blockBreakOn ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>whole break</div>;
    }

    function onClick() {
        postSetHandMode(hand, 'wholeBreak');
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const Save = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === 'save';
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === 'save';
    }

    let className = blockBreakOn ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>save</div>;
    }

    function onClick() {
        postSetHandMode(hand, 'save');
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const Duplicate = observer(({hand}) => {
    let {store} = useStore();

    let active = false;

    if (hand === 'left') {
        active = store.ContextInfo.LeftHandMode === 'duplicate';
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightHandMode === 'duplicate';
    }

    let className = active ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>duplicate</div>;
    }

    function onClick() {
        postSetHandMode(hand, 'duplicate');
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const ClearHand = observer(({hand}) => {
    let {store} = useStore();

    let active = false;

    if (hand === 'left') {
        active = store.ContextInfo.LeftHandMode === '';
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightHandMode === '';
    }

    let className = active ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>Clear Hand</div>;
    }

    function onClick() {
        postSetHandMode(hand, 'clear');
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const Context = observer(() => {
    return <div className={'bg-gray-900 h-screen flex flex-wrap justify-center space-x-20 content-center'}>
        <div className={'space-y-2'}>
            <BlockBreak hand={'left'}/>
            <WholeBreak hand={'left'}/>
            <Save hand={'left'}/>
            <Duplicate hand={'left'}/>
            <ClearHand hand={'left'}/>
        </div>
        <ButtonGrid hand={'left'}/>
        <ButtonGrid hand={'right'}/>
        <div className={'space-y-2'}>
            <BlockBreak hand={'right'}/>
            <WholeBreak hand={'right'}/>
            <Save hand={'right'}/>
            <Duplicate hand={'right'}/>
            <ClearHand hand={'right'}/>
        </div>
    </div>;
});

export default Context;