const BASE_URL = '';   // 同源，不需要指定主機

async function apiFetch(path, options = {}) {
    const token = localStorage.getItem('token');

    const res = await fetch(`${BASE_URL}${path}`, {
        ...options,
        headers: {
            'Content-Type': 'application/json',
            ...(token ? { Authorization: `Bearer ${token}` } : {}),
            ...options.headers
        }
    });

    if (res.status === 401) {
        // token 失效就清掉；本範例沒有登入頁，正式專案通常會在此導向登入
        localStorage.removeItem('token');
    }

    if (!res.ok) {
        const errBody = await res.json().catch(() => ({}));
        const err = new Error(errBody.title ?? `HTTP ${res.status}`);
        err.status = res.status;
        err.data = errBody;
        throw err;
    }

    if (res.status === 204) return null;
    return res.json();
}

export const api = {
    get:    (path)       => apiFetch(path),
    post:   (path, body) => apiFetch(path, { method: 'POST',   body: JSON.stringify(body) }),
    put:    (path, body) => apiFetch(path, { method: 'PUT',    body: JSON.stringify(body) }),
    patch:  (path, body) => apiFetch(path, { method: 'PATCH',  body: JSON.stringify(body) }),
    delete: (path)       => apiFetch(path, { method: 'DELETE' })
};
