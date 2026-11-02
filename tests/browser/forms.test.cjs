// Executes the shared script against a small DOM double. No browser, HTTP or database.
const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const path = require('node:path');
const source = fs.readFileSync(path.join(__dirname, '../../wwwroot/js/forms.js'), 'utf8');

function harness() {
    const events = {}, timers = new Map(), observers = [];
    let next = 0;
    class Element {
        constructor() { this.attrs = {}; this.disabled = false; this.id = ''; this.children = []; this.events = {}; }
        getAttribute(name) { return this.attrs[name] ?? null; }
        setAttribute(name, value) { this.attrs[name] = value; }
        removeAttribute(name) { delete this.attrs[name]; }
        remove() { this.removed = true; }
        append(child) { this.children.push(child); }
        addEventListener(name, fn) { this.events[name] = fn; }
    }
    class Form extends Element {
        constructor() {
            super(); this.method = 'post'; this.action = 'https://example.test/guardar';
            this.dataset = {}; this.elements = []; this.messages = []; this.children = []; this.valid = true;
        }
        checkValidity() { return this.valid; }
        append(child) { this.children.push(child); }
        querySelectorAll() { return this.messages; }
    }
    const form = new Form(), other = new Form();
    const button = new Element(); button.type = 'submit'; button.name = 'accion'; button.value = 'guardar';
    form.elements.push(button);
    const control = new Element(); control.name = 'Nombre'; control.type = 'text';
    control.setAttribute('aria-describedby', 'ayuda'); form.elements.push(control);
    const message = new Element(); message.setAttribute('data-valmsg-for', 'Nombre');
    message.invalid = true; message.classList = { contains: () => message.invalid }; form.messages.push(message);
    const context = {
        URL, HTMLFormElement: Form, location: { href: form.action, origin: 'https://example.test' },
        setTimeout: fn => { const id = ++next; timers.set(id, fn); return id; }, clearTimeout: id => timers.delete(id),
        MutationObserver: class { constructor(fn) { observers.push(fn); } observe() {} },
        document: {
            addEventListener: (name, fn) => { events[name] = fn; }, createElement: () => new Element(),
            querySelectorAll: () => [form, other],
            getElementById: id => [message].find(element => element.id === id)
        },
        window: { addEventListener: (name, fn) => { events[name] = fn; } }
    };
    vm.runInNewContext(source, context);
    return { form, other, button, control, message, context, observers, events,
        submit(target = form, canceled = false) {
            const event = { target, submitter: button, defaultPrevented: canceled,
                preventDefault() { this.defaultPrevented = true; } };
            events.submit(event); return event;
        }, flush() { for (const fn of timers.values()) fn(); timers.clear(); }
    };
}

test('bloquea doble envío y Enter sin perder el nombre/valor del submitter', () => {
    const h = harness();
    assert.equal(h.submit().defaultPrevented, false);
    assert.equal(h.button.disabled, false); // still serialized by the native submission
    assert.equal(h.button.value, 'guardar');
    assert.equal(h.submit().defaultPrevented, true);
    h.flush(); assert.equal(h.button.disabled, true);
    assert.equal(h.form.children[0].getAttribute('role'), 'status');
    assert.equal(h.form.getAttribute('aria-busy'), 'true');
});
test('otro formulario permanece utilizable', () => {
    const h = harness(); h.submit(); assert.equal(h.submit(h.other).defaultPrevented, false);
});
test('pageshow recupera botones y estado, sin habilitar botones previamente deshabilitados', () => {
    const h = harness(); const locked = { type: 'submit', disabled: true }; h.form.elements.push(locked);
    h.form.setAttribute('aria-busy', 'false'); h.submit(); h.flush(); h.events.pageshow();
    assert.equal(h.button.disabled, false); assert.equal(locked.disabled, true);
    assert.equal(h.form.getAttribute('aria-busy'), 'false');
    assert.equal(h.form.children[0].removed, true); assert.equal(h.submit().defaultPrevented, false);
});
test('cancelación posterior de un handler restaura el formulario', () => {
    const h = harness(); const event = h.submit(); event.preventDefault(); h.flush();
    assert.equal(h.button.disabled, false); assert.equal(h.form.getAttribute('aria-busy'), null);
});
test('recuperación explícita tras interrupción no reenvía automáticamente', () => {
    const h = harness(); h.submit(); h.flush();
    const recover = h.form.children[0].children[0];
    h.context.window.confirm = () => false; recover.events.click();
    assert.equal(h.button.disabled, true);
    h.context.window.confirm = () => true; recover.events.click();
    assert.equal(h.button.disabled, false);
    assert.equal(h.form.children.length, 1); // no second submission/status
});
for (const kind of ['confirmation', 'native', 'jquery', 'get', 'ajax', 'external', 'target', 'opt-out']) {
    test(`no bloquea un envío excluido o rechazado: ${kind}`, () => {
        const h = harness();
        if (kind === 'native') h.form.valid = false;
        if (kind === 'jquery') h.context.window.jQuery = () => ({ data: () => true, valid: () => false });
        if (kind === 'get') h.form.method = 'get';
        if (kind === 'ajax') h.form.dataset.ajax = 'true';
        if (kind === 'external') h.form.action = 'https://external.test';
        if (kind === 'target') h.form.target = '_blank';
        if (kind === 'opt-out') h.form.dataset.mcSubmit = 'false';
        h.submit(h.form, kind === 'confirmation'); h.flush();
        assert.equal(h.button.disabled, false); assert.equal(h.form.children.length, 0);
    });
}
test('conecta error al control sin borrar ayuda y actualiza aria-invalid al corregir', () => {
    const h = harness();
    assert.equal(h.control.getAttribute('aria-describedby'), `ayuda ${h.message.id}`);
    assert.equal(h.control.getAttribute('aria-invalid'), 'true');
    h.message.invalid = false; h.observers.forEach(fn => fn());
    assert.equal(h.control.getAttribute('aria-invalid'), 'false');
});
