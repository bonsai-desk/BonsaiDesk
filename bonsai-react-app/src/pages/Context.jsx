import {observer} from 'mobx-react-lite';
import {Blocks, useStore} from '../DataProvider';
import {postChangeActiveBlock} from '../api';

function Button({children, onClick}) {
    return <div className={'h-20 w-20 bg-gray-600 rounded'} onPointerDown={onClick}>{children}</div>;
}

function NoneButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.None);
    };
    return <Button onClick={onClick}>none</Button>;
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

function PurpleButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Purple);
    };
    return <Button onClick={onClick}>purple</Button>;
}

function RedButton({hand}) {
    let onClick = () => {
        postChangeActiveBlock(hand, Blocks.Red);
    };
    return <Button onClick={onClick}>red</Button>;
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
        <Button>{activeBlock}</Button>
    </div>;
});

function ButtonGrid({hand}) {
    return (
            <ButtonContainer>
                <ActiveItem hand={hand}/>
                <ButtonRow>
                    <NoneButton hand={hand}/>
                    <WoodButton hand={hand}/>
                    <OrangeButton hand={hand}/>
                </ButtonRow>
                <ButtonRow>
                    <GreenButton hand={hand}/>
                    <PurpleButton hand={hand}/>
                    <RedButton hand={hand}/>
                </ButtonRow>
            </ButtonContainer>
    );
}

const Context = observer(() => {
    return <div className={'bg-gray-900 h-screen flex justify-center space-x-20'}>
        <ButtonGrid hand={'left'}/>
        <ButtonGrid hand={'right'}/>
    </div>;
});

export default Context;