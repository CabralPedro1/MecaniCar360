const { test } = require('node:test');
const assert = require('node:assert/strict');
const vm = require('node:vm');
const fs = require('node:fs');
const source = fs.readFileSync(require('node:path').join(__dirname, '../../wwwroot/js/navigation.js'), 'utf8');
function setup(current = '/Turno/Detalle/3') {
    const events = {}, attrs = {}, classes = new Set();
    let hidden = 0, focused = 0, moved = 0, restored = 0;
    const group = { open: false, classList: { add() {} }, querySelector: () => ({ textContent: 'Operaciones' }), querySelectorAll: () => [link, link] };
    const link = { href: 'https://localhost/Turno/Index', textContent: 'Turnos', classList: { add: x => classes.add(x) }, setAttribute: (k,v) => attrs[k] = v, closest: () => group, addEventListener: (k,f) => events['link:'+k] = f };
    const toggle = { parentElement: { insertBefore: () => restored++ }, nextSibling: {}, setAttribute: (k,v) => attrs[k] = v, focus: () => focused++ };
    const sidebar = { querySelectorAll: q => q === 'details' ? [group] : [link], querySelector: () => ({ prepend: () => moved++ }), addEventListener: (k,f) => events[k] = f };
    vm.runInNewContext(source, { document: { getElementById: id => id === 'navigationSidebar' ? sidebar : id === 'navigationToggle' ? toggle : null }, window: { location: { pathname: current } }, URL, bootstrap: { Offcanvas: { getInstance: () => ({ hide: () => hidden++ }) } } });
    return { events, attrs, group, classes, counts: () => ({ hidden, focused, moved, restored }) };
}
test('open/close events synchronize same trigger, expanded state and focus', () => {
    const h = setup();
    h.events['show.bs.offcanvas']();
    assert.equal(h.attrs['aria-expanded'], 'true');
    h.events['hidden.bs.offcanvas']();
    assert.equal(h.attrs['aria-expanded'], 'false');
    assert.deepEqual(h.counts(), { hidden: 0, focused: 1, moved: 1, restored: 1 });
});
test('selecting a destination closes the panel', () => {
    const h = setup(); h.events['link:click']({}); assert.equal(h.counts().hidden, 1);
});
test('cancelled or new-tab navigation does not close the panel', () => {
    const h = setup(); h.events['link:click']({defaultPrevented:true}); h.events['link:click']({ctrlKey:true}); assert.equal(h.counts().hidden, 0);
});
test('detail recognizes existing module and expands its group', () => {
    const h = setup(); assert.equal(h.group.open, true); assert.equal(h.attrs['aria-current'], 'location'); assert.ok(h.classes.has('active'));
});
test('exact destination is current page', () => { assert.equal(setup('/Turno/Index').attrs['aria-current'], 'page'); });
test('unknown destination is not mislabeled', () => { assert.equal(setup('/Other/Index').attrs['aria-current'], undefined); });
