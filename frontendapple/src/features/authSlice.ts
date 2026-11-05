import {
    createSlice,
    type PayloadAction,
} from '@reduxjs/toolkit';

import type {
    AuthResponse,
    User,
} from '../types';

interface AuthState {
    token: string | null;
    user: User | null;
}

const STORAGE_KEY = 'applestore-auth';

function readAuth(): AuthState {
    try {
        const raw = localStorage.getItem(STORAGE_KEY);

        if (!raw) {
            return { token: null, user: null };
        }

        const saved = JSON.parse(raw) as AuthResponse;

        if (
            !saved.token ||
            !saved.user ||
            new Date(saved.expiresAt).getTime() <= Date.now()
        ) {
            localStorage.removeItem(STORAGE_KEY);
            return { token: null, user: null };
        }

        return {
            token: saved.token,
            user: saved.user,
        };
    } catch {
        return { token: null, user: null };
    }
}

const authSlice = createSlice({
    name: 'auth',
    initialState: readAuth(),
    reducers: {
        setCredentials(
            state,
            action: PayloadAction<AuthResponse>,
        ) {
            state.token = action.payload.token;
            state.user = action.payload.user;
        },

        updateUser(
            state,
            action: PayloadAction<Partial<User>>,
        ) {
            if (state.user) {
                state.user = {
                    ...state.user,
                    ...action.payload,
                };
            }
        },

        logout(state) {
            state.token = null;
            state.user = null;
        },
    },
});

export const {
    setCredentials,
    updateUser,
    logout,
} = authSlice.actions;

export const authStorageKey = STORAGE_KEY;

export default authSlice.reducer;