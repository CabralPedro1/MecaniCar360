(async () => {
    const token = location.hash.slice(1);
    history.replaceState(null, '', location.pathname);
    const estado = document.getElementById('estado');
    if (!/^[A-Za-z0-9_-]{43}$/.test(token)) {
        estado.textContent = 'El enlace no está disponible. Solicite uno nuevo.';
        return;
    }
    try {
        const response = await fetch(location.pathname, {
            headers: { 'X-Password-Token': token }, cache: 'no-store', credentials: 'same-origin',
            redirect: 'error', referrerPolicy: 'no-referrer'
        });
        if (!response.ok) throw new Error();
        const html = await response.text();
        document.open(); document.write(html); document.close();
    } catch {
        estado.textContent = 'No se pudo abrir el enlace. Intente nuevamente desde el correo.';
    }
})();
