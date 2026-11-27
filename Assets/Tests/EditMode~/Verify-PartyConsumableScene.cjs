const fs = require('fs');
const assert = require('assert');
const cp = require('child_process');
const scenePath = 'Assets/Project/Scenes/YDM/Lobby.unity';
const prefabPath = 'Assets/Project/PrefabsR/LobbyConsumableSlot.prefab';
const text = fs.readFileSync(scenePath, 'utf8').replace(/\r\n/g, '\n');
function parse(text) {
    const docs = new Map();
    for (const m of text.matchAll(/^--- !u!(\d+) &(\d+)[^\n]*\n([\s\S]*?)(?=^--- !u!|$(?![\s\S]))/gm)) {
        assert(!docs.has(m[2]), 'Duplicate YAML ID: ' + m[2]);
        docs.set(m[2], { type: m[1], body: m[3] });
    }
    return docs;
}
function reference(body, field) { return body.match(new RegExp('  ' + field + ': \\{fileID: (\\d+)'))?.[1]; }
function missing(docs) {
    const result = new Set();
    for (const [id, doc] of docs)
        for (const m of doc.body.matchAll(/\{fileID: (\d+)\}/g))
            if (m[1] !== '0' && !docs.has(m[1])) result.add(id + ':' + m[1]);
    return result;
}
const docs = parse(text);
const baseline = parse(cp.execFileSync('git', ['show', 'HEAD:' + scenePath], {encoding:'utf8', maxBuffer: 20*1024*1024}).replace(/\r\n/g,'\n'));
const previousMissing = missing(baseline);
assert.deepEqual([...missing(docs)].filter(id => !previousMissing.has(id)), [], 'New dangling scene references');
function path(id) {
    const transform = docs.get(id); if (!transform) return '';
    const go = docs.get(reference(transform.body, 'm_GameObject'));
    const name = go?.body.match(/  m_Name: (.*)/)?.[1];
    return path(reference(transform.body, 'm_Father')) + '/' + name;
}
assert.equal([...docs].filter(([id,d]) => d.type === '224' && /\/CharBtn_\d+\/Compound$/.test(path(id))).length, 0);
const prefab = parse(fs.readFileSync(prefabPath,'utf8').replace(/\r\n/g,'\n'));
assert.deepEqual([...missing(prefab)], [], 'Dangling prefab references');
const prefabGuid = fs.readFileSync(prefabPath+'.meta','utf8').match(/guid: (\w+)/)[1];
const instances = [...docs.values()].filter(d=>d.type === '1001' && d.body.includes('m_SourcePrefab: {fileID: 100100000, guid: '+prefabGuid));
assert.equal(instances.length,4);
for(let i=0;i<4;i++) {
    assert(instances[i].body.includes('propertyPath: slotIndex\n      value: '+i));
    assert(instances[i].body.includes('propertyPath: panel\n      value:\n      objectReference: {fileID: 880000002}'));
}
assert(prefab.get('1327598619').body.includes('m_SizeDelta: {x: 60, y: 60}'));
assert.equal(reference(docs.get('880000001').body,'m_Father'),reference(docs.get('415494669').body,'m_Father'));
const panelPosition = docs.get('880000001').body.match(/m_AnchoredPosition: \{x: ([\d.-]+), y: ([\d.-]+)\}/).slice(1).map(Number);
const playPosition = docs.get('415494669').body.match(/m_AnchoredPosition: \{x: ([\d.-]+), y: ([\d.-]+)\}/).slice(1).map(Number);
assert.equal(panelPosition[1],playPosition[1]);
assert(panelPosition[0]-108-30 > playPosition[0]+50, 'Slots must be right of PlayButton with a gap');
for (const file of ['LobbyConsumablePanelUI','LobbyConsumableSlotUI']) {
    const guid = fs.readFileSync('Assets/Project/Scripts/'+file+'.cs.meta','utf8').match(/guid: (\w+)/)[1];
    assert((text+fs.readFileSync(prefabPath,'utf8')).includes('guid: '+guid));
}
console.log('PASS: removed 6 character Compound trees; 4 prefab slots; 60x60 layout right of PlayButton; script/prefab references; no new dangling or duplicate YAML IDs');
