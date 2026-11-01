(() => {
    const sidebar = document.getElementById('navigationSidebar');
    if (!sidebar) return;
    const normalize = path => path.replace(/\/$/, '').toLowerCase();
    const current = normalize(window.location.pathname);
    const links = [...sidebar.querySelectorAll('a.nav-link')];
    const exact = links.find(link => normalize(new URL(link.href).pathname) === current);
    const active = exact || links.find(link => {
        const path = normalize(new URL(link.href).pathname);
        const base = path.endsWith('/index') ? path.slice(0, -6) : path;
        return base.length > 0 && current.startsWith(base + '/');
    });
    if (active) {
        active.classList.add('active');
        active.setAttribute('aria-current', 'page');
        active.closest('details').open = true;
    }
    sidebar.querySelectorAll('details').forEach(group => {
        group.hidden = !group.querySelector('a');
    });
    // Bootstrap 5.1 has no responsive offcanvas variant. Release its backdrop
    // and focus trap when moving to the persistent desktop navigation.
    const desktop = window.matchMedia('(min-width: 992px)');
    desktop.addEventListener('change', event => {
        if (event.matches) bootstrap.Offcanvas.getInstance(sidebar)?.hide();
    });
})();
