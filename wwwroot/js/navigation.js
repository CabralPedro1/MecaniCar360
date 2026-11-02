(() => {
    const sidebar = document.getElementById('navigationSidebar');
    if (!sidebar) return;
    const toggle = document.getElementById('navigationToggle');
    const home = toggle.parentElement;
    const next = toggle.nextSibling;
    const hide = () => bootstrap.Offcanvas.getInstance(sidebar)?.hide();
    // Keep the same trigger inside the modal focus trap while open.
    sidebar.addEventListener('show.bs.offcanvas', () => {
        sidebar.querySelector('.offcanvas-header').prepend(toggle);
        toggle.setAttribute('aria-expanded', 'true');
        toggle.setAttribute('aria-label', 'Cerrar men\u00fa de navegaci\u00f3n');
    });
    sidebar.addEventListener('hidden.bs.offcanvas', () => {
        home.insertBefore(toggle, next);
        toggle.setAttribute('aria-expanded', 'false');
        toggle.setAttribute('aria-label', 'Abrir men\u00fa de navegaci\u00f3n');
        toggle.focus();
    });
    const normalize = path => path.replace(/\/$/, '').toLowerCase();
    const current = normalize(window.location.pathname);
    const links = [...sidebar.querySelectorAll('a.nav-link')];
    const pathOf = link => normalize(new URL(link.href).pathname);
    const exact = links.find(link => pathOf(link) === current);
    const active = exact || links.find(link => {
        const module = pathOf(link).split('/')[1];
        return module && current.split('/')[1] === module;
    });
    sidebar.querySelectorAll('details').forEach(group => {
        const options = group.querySelectorAll('a.nav-link');
        group.hidden = options.length === 0;
        if (options.length === 1) {
            group.classList.add('mc-single');
            group.open = true;
        }
    });
    if (active) {
        active.classList.add('active');
        active.setAttribute('aria-current', exact ? 'page' : 'location');
        const group = active.closest('details');
        group.open = true;
        const breadcrumb = document.getElementById('navigationBreadcrumb');
        if (breadcrumb && !exact) {
            const list = breadcrumb.querySelector('ol');
            const section = document.createElement('li');
            section.className = 'breadcrumb-item';
            section.textContent = group.querySelector('summary').textContent;
            const item = document.createElement('li');
            item.className = 'breadcrumb-item';
            const link = document.createElement('a');
            link.href = active.href;
            link.textContent = active.textContent;
            item.append(link);
            list.append(section, item);
            breadcrumb.hidden = false;
        }
    }
    links.forEach(link => link.addEventListener('click', event => {
        if (!event.defaultPrevented && !event.ctrlKey && !event.metaKey && !event.shiftKey)
            hide();
    }));
    // Bootstrap owns Escape, backdrop, focus trapping and native trigger toggling.
})();
