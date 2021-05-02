import {observer} from 'mobx-react-lite';
import {Blocks, showBlock, useStore} from '../DataProvider';
import {postChangeActiveBlock, postToggleBlockActive, postToggleBlockBreakHand} from '../api';

function Button({children, onClick}) {
    return <div className={'h-20 w-20 bg-gray-600 rounded'} onPointerDown={onClick}>{children}</div>;
}

function BlockButton({hand, blockId}) {
    let onClick = () => {
        postChangeActiveBlock(hand, blockId);
    };
    return <Button onClick={onClick}>{showBlock(blockId)}</Button>;
}

function ButtonRow({children}) {
    return <div className={'flex flex-wrap space-x-4'}>{children}</div>;
}

function ButtonContainer({children}) {
    return <div className={'space-y-4'}>{children}</div>;
}

const ActiveItem = observer(({hand}) => {

    let {store} = useStore();
    let activeBlock = Blocks.None;
    if (hand === 'left') {
        activeBlock = store.ContextInfo.LeftBlockActive;
    }
    if (hand === 'right') {
        activeBlock = store.ContextInfo.RightBlockActive;
    }

    return <div className={'w-full flex justify-center'}>
        <Button>{showBlock(activeBlock)}</Button>
    </div>;
});

const ToggleBlocks = observer(({hand}) => {
    let {store} = useStore();

    let switchOff = false;

    switch (hand) {
        case 'left':
            switchOff = store.ContextInfo.LeftBlockActive === Blocks.None;
            break;
        case 'right':
            switchOff = store.ContextInfo.RightBlockActive === Blocks.None;
            break;
        default:
            console.log(`Toggle blocks for ${hand} not handled`);
            break;
    }

    let className = switchOff ? 'bg-gray-900 h-10' : 'bg-green-400 h-10';

    let onClick = () => {
        if (hand === 'left' || hand === 'right') {
            postToggleBlockActive(hand);
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
                <ActiveItem hand={hand}/>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={Blocks.Wood}/>
                    <BlockButton hand={hand} blockId={Blocks.Orange}/>
                    <BlockButton hand={hand} blockId={Blocks.Green}/>
                </ButtonRow>
                <ButtonRow>
                    <BlockButton hand={hand} blockId={Blocks.Pink}/>
                    <BlockButton hand={hand} blockId={Blocks.Violet}/>
                    <BlockButton hand={hand} blockId={Blocks.DarkNeutral}/>
                </ButtonRow>
                <ToggleBlocks hand={hand}/>
            </ButtonContainer>
    );
}

const HandButton = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftBlockBreak;
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightBlockBreak;
    }

    let className = blockBreakOn ? 'bg-red-400 h-10' : 'bg-gray-900 h-10';

    function Inner() {
        return <div className={className}>block break</div>;
    }

    function onClick() {
        postToggleBlockBreakHand(hand);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

const Context = observer(() => {
    return <div className={'bg-gray-900 h-screen flex flex-wrap justify-center space-x-20 content-center'}>
        <HandButton hand={'left'}/>
        <ButtonGrid hand={'left'}/>
        <ButtonGrid hand={'right'}/>
        <HandButton hand={'right'}/>
    </div>;
});

export default Context;