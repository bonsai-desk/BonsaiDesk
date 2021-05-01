import {observer} from 'mobx-react-lite';
import {Blocks, showBlock, useStore} from '../DataProvider';
import {postChangeActiveBlock, postToggleBlockActive, postToggleBlockBreakHand} from '../api';

function Button({children, onClick}) {
    return <div className={'h-20 w-20 bg-gray-600 rounded'} onPointerDown={onClick}>{children}</div>;
}

function WoodButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Wood);
    };
    return <Button onClick={onClick}>wood</Button>;
}

function OrangeButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Orange);
    };
    return <Button onClick={onClick}>orange</Button>;
}

function GreenButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Green);
    };
    return <Button onClick={onClick}>green</Button>;
}

function PinkButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Pink);
    };
    return <Button onClick={onClick}>pink</Button>;
}

function VioletButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Violet);
    };
    return <Button onClick={onClick}>violet</Button>;
}

function DarkNeutralButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.DarkNeutral);
    };
    return <Button onClick={onClick}>dark neutral</Button>;
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

    let className = 'bg-green-400 h-10';

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

    if (switchOff) {
        className = 'bg-gray-900 h-10';
    }

    function Inner() {
        return <div className={className}/>;
    }

    let onClick = () => {
        if (hand === 'left' || hand === 'right') {
            postToggleBlockActive(hand);
        }
    };

    return <div className={'w-full flex justify-center'}>
        <Button onClick={onClick}><Inner/></Button>
    </div>;

});

function ButtonGrid({hand}) {
    return (
            <ButtonContainer>
                <ActiveItem hand={hand}/>
                <ButtonRow>
                    <WoodButton hand={hand}/>
                    <OrangeButton hand={hand}/>
                    <GreenButton hand={hand}/>
                </ButtonRow>
                <ButtonRow>
                    <PinkButton hand={hand}/>
                    <VioletButton hand={hand}/>
                    <DarkNeutralButton hand={hand}/>
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