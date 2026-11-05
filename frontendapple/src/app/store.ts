import {
    configureStore,
    createListenerMiddleware,
    isAnyOf,
} from '@reduxjs/toolkit';

import { setupListeners } from '@reduxjs/toolkit/query';

import authReducer, {
    authStorageKey,
    logout,
    setCredentials,
    updateUser,
} from '../features/authSlice';

import { api } from '../api/api';

const listener = createListenerMiddleware();

listener.startListening({
    actionCreator: setCredentials,
    effect: (action) => {
        localStorage.setItem(
            authStorageKey,
            JSON.stringify(action.payload),
        );
    },
});

listener.startListening({
    actionCreator: updateUser,
    effect: (action) => {
        const raw = localStorage.getItem(authStorageKey);
        if (!raw) return;

        try {
            const saved = JSON.parse(raw);

            localStorage.setItem(
                authStorageKey,
                JSON.stringify({
                    ...saved,
                    user: {
                        ...saved.user,
                        ...action.payload,
                    },
                }),
            );
        } catch {
            localStorage.removeItem(authStorageKey);
        }
    },
});

listener.startListening({
    matcher: isAnyOf(logout, setCredentials),
    effect: (action, listenerApi) => {
        if (logout.match(action)) {
            localStorage.removeItem(authStorageKey);
        }

        listenerApi.dispatch(api.util.resetApiState());
    },
});

export const store = configureStore({
    reducer: {
        auth: authReducer,
        [api.reducerPath]: api.reducer,
    },

    middleware: (getDefaultMiddleware) =>
        getDefaultMiddleware()
            .prepend(listener.middleware)
            .concat(api.middleware),
});

setupListeners(store.dispatch);

export type RootState =
    ReturnType<typeof store.getState>;

export type AppDispatch = typeof store.dispatch;