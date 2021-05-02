import React from 'react';
import ReactDOM from 'react-dom';
import './index.css';
import App from './App';
import {StoreProvider} from './DataProvider';

ReactDOM.render(
        <React.StrictMode>
            <StoreProvider>
                <App/>
            </StoreProvider>
        </React.StrictMode>,
        document.getElementById('root'),
);
