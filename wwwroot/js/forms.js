// Native MVC forms only: feedback is not a substitute for server authorization or idempotency.
(() => {
    'use strict';
    const pending = new Map();
    let messageId = 0;

    function restore(form) {
        const state = pending.get(form);
        if (!state) return;
        clearTimeout(state.timer);
        state.buttons.forEach(button => { button.disabled = false; });
        if (state.busy === null) form.removeAttribute('aria-busy');
        else form.setAttribute('aria-busy', state.busy);
        state.status.remove();
        pending.delete(form);
    }

    document.addEventListener('submit', event => {
        const form = event.target;
        if (!(form instanceof HTMLFormElement) || form.method.toLowerCase() !== 'post' ||
            (form.target && form.target !== '_self') || form.dataset.ajax === 'true' ||
            form.dataset.mcSubmit === 'false' || new URL(form.action, location.href).origin !== location.origin) return;
        if (pending.has(form)) { event.preventDefault(); return; }
        // Run after native validation, unobtrusive validation and local confirmation handlers.
        if (event.defaultPrevented) return;
        if (!form.noValidate && !event.submitter?.formNoValidate && !form.checkValidity()) { event.preventDefault(); return; }
        if (window.jQuery && window.jQuery(form).data('validator') && !window.jQuery(form).valid()) { event.preventDefault(); return; }

        const status = document.createElement('span');
        status.className = 'd-block mt-2';
        status.setAttribute('role', 'status');
        status.textContent = 'Procesando…';
        const recover = document.createElement('button');
        recover.type = 'button';
        recover.className = 'btn btn-link btn-sm';
        recover.textContent = 'Recuperar formulario';
        recover.addEventListener('click', () => {
            if (window.confirm('La respuesta puede haberse interrumpido aunque la operación se haya guardado. Revise el estado antes de reintentar. ¿Habilitar el formulario sin volver a enviarlo?')) restore(form);
        });
        status.append(recover);
        form.append(status);
        const buttons = Array.from(form.elements).filter(control =>
            (control.type === 'submit' || control.type === 'image') && !control.disabled);
        const state = { buttons, status, busy: form.getAttribute('aria-busy'), timer: null };
        pending.set(form, state);
        form.setAttribute('aria-busy', 'true');
        // Do not disable the submitter until the browser has serialized its name/value.
        // The pending map already rejects a second submit (including Enter).
        state.timer = setTimeout(() => {
            if (event.defaultPrevented) restore(form);
            else buttons.forEach(button => { button.disabled = true; });
        }, 0);
    });

    // Includes return from a network error via Back and the browser's back/forward cache.
    window.addEventListener('pageshow', () => Array.from(pending.keys()).forEach(restore));

    document.querySelectorAll('form').forEach(form => {
        form.querySelectorAll('[data-valmsg-for]').forEach(message => {
            const name = message.getAttribute('data-valmsg-for');
            const controls = Array.from(form.elements).filter(control => control.name === name && control.type !== 'hidden');
            if (!controls.length) return;
            if (!message.id) {
                do { message.id = `mc-field-error-${++messageId}`; } while (document.getElementById(message.id) !== message);
            }
            controls.forEach(control => {
                const ids = new Set((control.getAttribute('aria-describedby') || '').split(/\s+/).filter(Boolean));
                ids.add(message.id);
                control.setAttribute('aria-describedby', Array.from(ids).join(' '));
            });
            const update = () => controls.forEach(control => {
                control.setAttribute('aria-invalid', message.classList.contains('field-validation-error') ? 'true' : 'false');
            });
            update();
            new MutationObserver(update).observe(message, { attributes: true, attributeFilter: ['class'], childList: true, subtree: true });
        });
    });
})();
