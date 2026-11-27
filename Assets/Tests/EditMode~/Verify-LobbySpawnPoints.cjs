const fs = require('node:fs');
const assert = require('node:assert/strict');
const scenePath = process.argv[2] || 'Assets/Project/Scenes/YDM/Lobby.unity';
const read = p => fs.readFileSync(p, 'utf8').replace(/\r\n/g, '\n');
const scene = read(scenePath);
function blocks(text) {
  const result = new Map();
  for (const match of text.matchAll(/^--- !u!\d+ &(-?\d+)[^\n]*\n[\s\S]*?(?=^--- !u!|(?![\s\S]))/gm)) {
    assert(!result.has(match[1]), `Duplicate fileID ${match[1]}`);
    result.set(match[1], match[0]);
  }
  return result;
}
const parts = blocks(scene);
const owner = parts.get('695555850');
assert(owner.includes('  spawnPoints:\n'), 'Owner must reference scene spawn points');
assert(!owner.includes('spawnDefinitions:'), 'Legacy definitions must be removed');
const pointGuid = read('Assets/Project/Scripts/Gameplay/Scene/Lobby/LobbyCharacterSpawnPoint.cs.meta').match(/guid: (\w+)/)[1];
const dbGuid = read('Assets/DB/CharacterPrefabDB.asset.meta').match(/guid: (\w+)/)[1];
const db = read('Assets/DB/CharacterPrefabDB.asset');
const expectedPositions = ['{x: 2.07, y: -4.16, z: 0}', '{x: -5.7, y: -1.03, z: 0}', '{x: -2.3, y: -4.15, z: 0}', '{x: 4.05, y: -5.81, z: 0}'];
for (let i = 0; i < 4; i++) {
  const base = 910270000 + i * 10;
  const point = parts.get(String(base+2));
  const transform = parts.get(String(base+1));
  assert(point, `Point ${i} must exist`);
  assert(owner.includes(`  - {fileID: ${base+2}}`));
  assert(parts.get('695555849').includes(`  - {fileID: ${base+1}}`));
  assert(transform.includes('m_Father: {fileID: 695555849}'));
  assert(transform.includes('m_LocalPosition: '+expectedPositions[i]));
  assert(transform.includes('m_LocalScale: '+(i === 2 ? '{x: -0.5, y: 0.5, z: 0.5}' : '{x: 0.5, y: 0.5, z: 0.5}')));
  assert(point.includes(`guid: ${pointGuid}, type: 3}`));
  assert(point.includes(`guid: ${dbGuid}, type: 2}`));
  const id = 'Char_0'+(i+1);
  assert(point.includes('characterId: '+id));
  assert(db.includes('CharacterId: '+id+'\n'));
  assert(point.includes('flipX: '+(i === 1 ? 0 : 1)));
  assert(point.includes('sortingLayerId: -2107577011'));
  assert(point.includes('frontSortingOrder: '+[2,5,3,2][i]));
  assert(point.includes('backSortingOrder: '+[1,1,2,1][i]));
  assert(point.includes('overrideLayer: 0'));
  assert(point.includes('hoverNameType: '+(5+i)));
}
console.log('PASS: 4 scene points, transforms, DB/script links, presentation values and parent references');
const beforePath = process.argv[3];
if (beforePath) {
  const before = read(beforePath), oldParts = blocks(before);
  for (const [id, block] of oldParts) {
    if (id !== '695555849' && id !== '695555850')
      assert.equal(parts.get(id), block, `Unrelated scene object changed: ${id}`);
  }
  assert.equal(parts.size, oldParts.size + 12);
  const strippedParent = parts.get('695555849').replace(/  m_Children:\n(?:  - \{fileID: \d+\}\n){4}/, '  m_Children: []\n');
  assert.equal(strippedParent, oldParts.get('695555849'));
  console.log('PASS: all unrelated scene objects unchanged from pre-migration snapshot');
}
