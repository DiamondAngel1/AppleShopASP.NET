import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';

import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';

import { Toaster } from 'react-hot-toast';

import App from './App';
import { store } from './app/store';

import './index.css';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <Provider store={store}>
            <BrowserRouter>
                <App />

                <Toaster
                    position="top-right"
                    toastOptions={{
                        duration: 4000,

                        style: {
                            background: '#103429',
                            color: '#ffffff',
                            border: '1px solid rgba(198,244,50,.25)',
                            borderRadius: '16px',
                            fontSize: '13px',
                        },

                        success: {
                            iconTheme: {
                                primary: '#c6f432',
                                secondary: '#07201a',
                            },
                        },
                    }}
                />
            </BrowserRouter>
        </Provider>
    </StrictMode>,
);