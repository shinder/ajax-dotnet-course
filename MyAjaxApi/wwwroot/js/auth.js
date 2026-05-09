import { api } from './api.js';

export async function login(username, password) {
    const data = await api.post('/api/auth/login', { username, password });
    localStorage.setItem('token', data.token);
}

export function logout() {
    localStorage.removeItem('token');
    location.href = '/login.html';
}

export function isLoggedIn() {
    return Boolean(localStorage.getItem('token'));
}
