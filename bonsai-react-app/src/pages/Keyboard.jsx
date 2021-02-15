import React, {useState} from 'react';
import {Shift, SymbolsOrNum, NumsOrChar, Backspace, Enter, KeyChar, Space} from '../Components/Keys';

function Keyboard() {
  let [shift, setShift] = useState(false);
  let [level, setLevel] = useState(0);
  let level0 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-center'}>
          <KeyChar shift={shift} char={'q'}/>
          <KeyChar shift={shift} char={'w'}/>
          <KeyChar shift={shift} char={'e'}/>
          <KeyChar shift={shift} char={'r'}/>
          <KeyChar shift={shift} char={'t'}/>
          <KeyChar shift={shift} char={'y'}/>
          <KeyChar shift={shift} char={'u'}/>
          <KeyChar shift={shift} char={'i'}/>
          <KeyChar shift={shift} char={'o'}/>
          <KeyChar shift={shift} char={'p'}/>
          <Backspace/>
        </div>
        <div className={'flex space-x-2 justify-end'}>
          <KeyChar shift={shift} char={'a'}/>
          <KeyChar shift={shift} char={'s'}/>
          <KeyChar shift={shift} char={'d'}/>
          <KeyChar shift={shift} char={'f'}/>
          <KeyChar shift={shift} char={'g'}/>
          <KeyChar shift={shift} char={'h'}/>
          <KeyChar shift={shift} char={'j'}/>
          <KeyChar shift={shift} char={'k'}/>
          <KeyChar shift={shift} char={'l'}/>
          <Enter/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <Shift shift={shift} toggleShift={() => {
            setShift(!shift);
          }}/>
          <KeyChar shift={shift} char={'z'}/>
          <KeyChar shift={shift} char={'x'}/>
          <KeyChar shift={shift} char={'c'}/>
          <KeyChar shift={shift} char={'v'}/>
          <KeyChar shift={shift} char={'b'}/>
          <KeyChar shift={shift} char={'n'}/>
          <KeyChar shift={shift} char={'m'}/>
          <KeyChar shift={shift} char={','}/>
          <KeyChar shift={shift} char={'.'}/>
          <Shift shift={shift} toggleShift={() => {
            setShift(!shift);
          }}/>
        </div>
      </React.Fragment>
  );

  let level1 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-end'}>
          <KeyChar shift={shift} char={'@'}/>
          <KeyChar shift={shift} char={'#'}/>
          <KeyChar shift={shift} char={'$'}/>
          <KeyChar shift={shift} char={'&'}/>
          <KeyChar shift={shift} char={'*'}/>
          <KeyChar shift={shift} char={'('}/>
          <KeyChar shift={shift} char={')'}/>
          <KeyChar shift={shift} char={'\''}/>
          <KeyChar shift={shift} char={'"'}/>
          <Enter/>
        </div>
        <div className={'flex space-x-2 justify-end'}>
          <SymbolsOrNum level={level} handleClick={handleClickSymbolOrNum}/>
          <KeyChar shift={shift} char={'%'}/>
          <KeyChar shift={shift} char={'-'}/>
          <KeyChar shift={shift} char={'+'}/>
          <KeyChar shift={shift} char={'='}/>
          <KeyChar shift={shift} char={'/'}/>
          <KeyChar shift={shift} char={';'}/>
          <KeyChar shift={shift} char={':'}/>
          <KeyChar shift={shift} char={','}/>
          <KeyChar shift={shift} char={'.'}/>
          <SymbolsOrNum level={level} handleClick={handleClickSymbolOrNum}/>
        </div>
      </React.Fragment>
  );
  let level12 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-center'}>
          <KeyChar shift={shift} char={'1'}/>
          <KeyChar shift={shift} char={'2'}/>
          <KeyChar shift={shift} char={'3'}/>
          <KeyChar shift={shift} char={'4'}/>
          <KeyChar shift={shift} char={'5'}/>
          <KeyChar shift={shift} char={'6'}/>
          <KeyChar shift={shift} char={'7'}/>
          <KeyChar shift={shift} char={'8'}/>
          <KeyChar shift={shift} char={'9'}/>
          <KeyChar shift={shift} char={'0'}/>
          <Backspace/>
        </div>
      </React.Fragment>
  );
  let level2 = (
      <React.Fragment>
        <div className={'flex space-x-2 justify-end'}>
          <KeyChar shift={shift} char={'€'}/>
          <KeyChar shift={shift} char={'£'}/>
          <KeyChar shift={shift} char={'¥'}/>
          <KeyChar shift={shift} char={'_'}/>
          <KeyChar shift={shift} char={'^'}/>
          <KeyChar shift={shift} char={'['}/>
          <KeyChar shift={shift} char={']'}/>
          <KeyChar shift={shift} char={'{'}/>
          <KeyChar shift={shift} char={'}'}/>
          <Enter/>
        </div>
        <div className={'flex space-x-2 justify-center'}>
          <SymbolsOrNum level={level} handleClick={handleClickSymbolOrNum}/>
          <KeyChar shift={shift} char={'§'}/>
          <KeyChar shift={shift} char={'|'}/>
          <KeyChar shift={shift} char={'~'}/>
          <KeyChar shift={shift} char={'…'}/>
          <KeyChar shift={shift} char={'\\'}/>
          <KeyChar shift={shift} char={'<'}/>
          <KeyChar shift={shift} char={'>'}/>
          <KeyChar shift={shift} char={'!'}/>
          <KeyChar shift={shift} char={'?'}/>
          <SymbolsOrNum level={level} handleClick={handleClickSymbolOrNum}/>
        </div>
      </React.Fragment>
  );

  function handleClickSymbolOrNum() {
    switch (level) {
      case 1:
        setLevel(2);
        break;
      default:
        setLevel(1);
    }
  }

  function handleClickNumOrChar() {
    switch (level) {
      case 0:
        setLevel(1);
        break;
      default:
        setLevel(0);
        break;
    }

  }

  return (
      <div
          className={'w-full h-screen bg-black flex flex-wrap justify-center content-center'}>
        <div className={'space-y-2'}>

          {level === 0 ? level0 : ''}
          {level === 1 || level === 2 ? level12 : ''}
          {level === 1 ? level1 : ''}
          {level === 2 ? level2 : ''}
          <div className={'w-full flex space-x-2 justify-between'}>
            <NumsOrChar level={level} handleClick={handleClickNumOrChar}/>
            <Space/>
            <div className={'flex space-x-2'}>
              <NumsOrChar level={level} handleClick={handleClickNumOrChar}/>
            </div>
          </div>
        </div>

      </div>
  );
}

export default Keyboard;