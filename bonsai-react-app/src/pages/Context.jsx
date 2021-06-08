import {useState} from"react"
import {observer} from 'mobx-react-lite';
import {HandMode, useStore} from '../DataProvider';
import {postChangeActiveBlock, postSetHandMode} from '../api';
import wood1 from '../static/wood1.png';
import wood2 from '../static/wood2.png';
import wood3 from '../static/wood3.png';
import wood4 from '../static/wood4.png';
import wood6 from '../static/wood6.png';

let buttonClass = 'bg-gray-800 h-full w-full text-white flex flex-wrap content-center justify-center';

function Button({children, onClick, active}) {
    //let classInactive = ""
    //let classActive ='p-2 h-20 w-20 bg-gray-600 rounded border-bonsai-orange border-solid border-4 border-light-blue-500';
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
                        <img src={wood1} alt={'wood1'}/>
                    </BlockButton>
                </ButtonRow>
                <div className={'w-full flex justify-center'}>
                    <BlockButton hand={hand} blockId={''}>
                        <div className={'h-full w-full bg-gray-800'}/>
                    </BlockButton>
                </div>
            </ButtonContainer>
    );
}

const BlockBreak = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === HandMode.Single;
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === HandMode.Single;
    }

    let className = 'bg-gray-800 h-full w-full text-white flex flex-wrap content-center justify-center';

    function Inner() {
        return <div className={className}>
            <div>
                <div>Delete</div>
                <div>Block</div>
            </div>
        </div>;
    }

    function onClick() {
        postSetHandMode(hand, HandMode.Single);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active={blockBreakOn} onClick={onClick}><Inner/></Button>
    </div>;

});

const WholeBreak = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === HandMode.Whole;
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === HandMode.Whole;
    }

    let className = 'bg-gray-800 h-full w-full text-white flex flex-wrap content-center justify-center';

    function Inner() {
        return <div className={className}>
            <div>
                <div>Delete</div>
                <div>Chunk</div>
            </div>
        </div>;
    }

    function onClick() {
        postSetHandMode(hand, HandMode.Whole);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active={blockBreakOn} onClick={onClick}><Inner/></Button>
    </div>;

});

const Save = observer(({hand}) => {
    let {store} = useStore();

    let blockBreakOn = false;

    if (hand === 'left') {
        blockBreakOn = store.ContextInfo.LeftHandMode === HandMode.Save;
    }
    if (hand === 'right') {
        blockBreakOn = store.ContextInfo.RightHandMode === HandMode.Save;
    }

    let className = 'bg-gray-800 h-full w-full text-white flex flex-wrap content-center justify-center';

    function Inner() {
        return <div className={className}>Save</div>;
    }

    function onClick() {
        postSetHandMode(hand, HandMode.Save);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active={blockBreakOn} onClick={onClick}><Inner/></Button>
    </div>;

});

const Duplicate = observer(({hand}) => {
    let {store} = useStore();

    let active = false;

    if (hand === 'left') {
        active = store.ContextInfo.LeftHandMode === HandMode.Duplicate;
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightHandMode === HandMode.Duplicate;
    }

    let className = buttonClass;

    function Inner() {
        return <div className={className}>Clone</div>;
    }

    function onClick() {
        postSetHandMode(hand, HandMode.Duplicate);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active ={active} onClick={onClick}><Inner/></Button>
    </div>;

});

const ClearHand = observer(({hand}) => {
    let {store} = useStore();

    let active = false;

    if (hand === 'left') {
        active = store.ContextInfo.LeftHandMode === HandMode.None;
    }
    if (hand === 'right') {
        active = store.ContextInfo.RightHandMode === HandMode.None;
    }

    let className = buttonClass;

    function Inner() {
        return <div className={className}></div>;
    }

    function onClick() {
        postSetHandMode(hand, HandMode.None);
    }

    return <div className={'flex flex-wrap content-center'}>
        <Button active={active} onClick={onClick}><Inner/></Button>
    </div>;

});

const HandModes = observer(() => {
    let [hand, setHand] = useState("right")
    
    return (
            <div className={'flex flex-wrap w-full justify-center space-x-8'}>
                <div className={'flex space-x-2'}>
                    <Button active={hand === "left"}>
                        <div className={'h-full w-full bg-gray-800 content-center justify-center flex flex-wrap'}>
                            <span className={'text-white text-3xl'}>L</span>
                        </div>
                    </Button>
                    <Button active={hand === "right"}>
                        <div className={'h-full w-full bg-gray-800 content-center justify-center flex flex-wrap'}>
                            <span className={'text-white text-3xl'}>R</span>
                        </div>
                    </Button>
                </div>
                <div className={'space-x-2 flex'}>
                    <BlockBreak hand={'left'}/>
                    <WholeBreak hand={'left'}/>
                    <Save hand={'left'}/>
                    <Duplicate hand={'left'}/>
                    <ClearHand hand={'left'}/>
                </div>
            </div>
    );

});

const Context = observer(() => {
    document.title = 'Context Menu';
    return (
            <div className={'bg-gray-900 flex flex-wrap content-center h-screen space-y-8'}>
                <div className={'flex flex-wrap justify-center space-x-20 w-full'}>
                    <ButtonGrid hand={'left'}/>
                    <ButtonGrid hand={'right'}/>
                </div>
                ;
                <HandModes/>
            </div>

    );
});

export default Context;