// Alderwick — static world, character, and story data.

export const WORLD = {
  width: 2400,
  height: 1600,
  startX: 1152,
  startY: 1310,
  startMinutes: 7 * 60 + 20,
  minutesPerSecond: 2.5,
  dayLengthMinutes: 1440
};

export const RIVER = {
  centerY: 725,
  halfWidth: 66,
  amp: 25,
  freq: 260,
  amp2: 10,
  freq2: 90,
  bridge: { x: 1138, y: 610, w: 96, h: 205 },
  dock: { x: 1648, y: 688, w: 74, h: 132 },
  boat: { x: 1812, y: 742 },
  name: 'The River Fenn'
};

export const FACTIONS = {
  riverfolk: {
    name: 'The Riverfolk',
    short: 'Riverfolk',
    color: '#6ba9ab',
    pale: '#c1e0dc',
    dwell: 'east bank, docks and smokehouse',
    creed: 'The river gives, the river keeps.',
    desc: 'Ferryfolk and fishers who read the Fenn the way farmers read soil. They are first awake, last to gossip, and keep a tally of every favour owed across the water.',
  },
  hearthfolk: {
    name: 'The Hearthfolk',
    short: 'Hearthfolk',
    color: '#c9a05f',
    pale: '#efdcb6',
    dwell: 'bakehouse, fields and the Bent Hook',
    creed: 'Keep the hearth burning and the door unlatched.',
    desc: 'The Marles feed the village. Their days begin before the sun and end after the last cup is cleared. Bread, barley and a warm bench are their currency of trust.',
  },
  wardens: {
    name: 'The Thornwardens',
    short: 'Wardens',
    color: '#89a66d',
    pale: '#d2e0bd',
    dwell: 'Elderwood edge, lodge and herb hut',
    creed: 'Walk quietly in the green dark.',
    desc: 'Rangers, a herbalist and a woodcutter who share the forest’s patience. They mark the boundary stones, mend what the wind breaks, and take a dim view of axes used carelessly.',
  },
  emberwrights: {
    name: 'The Emberwrights',
    short: 'Emberwrights',
    color: '#c38660',
    pale: '#efd3bd',
    dwell: 'forge, market and the painted wagon',
    creed: 'Honest fire, honest price.',
    desc: 'Smiths, pedlars and tinkers. They argue over coin as eagerly as they share a table, and have made the forge the loudest, warmest corner of Alderwick.',
  }
};

export const LOCATIONS = {
  tavern: { name: 'The Bent Hook', x: 1258, y: 1006, radius: 78 },
  market: { name: 'Market Square', x: 986, y: 1002, radius: 48 },
  green: { name: 'The Green', x: 1138, y: 1112, radius: 92 },
  bakehouse: { name: 'Marle Bakehouse', x: 812, y: 962, radius: 32 },
  forge: { name: 'Ember Forge', x: 1432, y: 1008, radius: 36 },
  fields: { name: 'Marle Fields', x: 414, y: 1192, radius: 108 },
  mill: { name: 'The Windmill', x: 770, y: 1168, radius: 34 },
  sawmill: { name: 'Lowbank Sawmill', x: 704, y: 982, radius: 30 },
  woodlot: { name: 'The Woodlot', x: 746, y: 556, radius: 72 },
  forestEdge: { name: 'Elderwood Edge', x: 986, y: 600, radius: 48 },
  glade: { name: 'Gatherer’s Glade', x: 1034, y: 336, radius: 86 },
  stones: { name: 'The Old Stones', x: 1448, y: 322, radius: 70 },
  herbHut: { name: 'Ysolde’s Herb Hut', x: 1074, y: 592, radius: 30 },
  herbGarden: { name: 'Physic Garden', x: 1138, y: 598, radius: 26 },
  riverbank: { name: 'The Riverbank', x: 1068, y: 832, radius: 42 },
  dock: { name: 'Fenn Dock', x: 1682, y: 800, radius: 26 },
  smokehouse: { name: 'The Smokehouse', x: 1626, y: 958, radius: 30 },
  gate: { name: 'South Gate', x: 1152, y: 1334, radius: 30 },
  workshop: { name: 'Quill Workshop', x: 1386, y: 1174, radius: 32 }
};

export const BUILDINGS = [
  { id: 'tavern', name: 'The Bent Hook', x: 1090, y: 808, w: 220, h: 120, style: 'tavern', roof: '#52604a', wall: '#a68d64', trim: '#5e4730', door: { x: 1200, y: 932 }, sign: 'THE BENT HOOK', smoke: true },
  { id: 'bakehouse', name: 'Marle Bakehouse', x: 728, y: 810, w: 164, h: 100, style: 'house', roof: '#8a5a3e', wall: '#ccb591', trim: '#6a4c34', door: { x: 810, y: 914 }, sign: 'BAKEHOUSE', smoke: true },
  { id: 'forge', name: 'Ember Forge', x: 1350, y: 824, w: 172, h: 110, style: 'forge', roof: '#4f4b46', wall: '#9a846a', trim: '#5a4532', door: { x: 1436, y: 938 }, sign: 'EMBER FORGE', smoke: true },
  { id: 'lodge', name: 'Warden Lodge', x: 810, y: 414, w: 154, h: 100, style: 'lodge', roof: '#39543e', wall: '#8f7857', trim: '#4d3b2b', door: { x: 887, y: 518 }, sign: 'WARDEN LODGE', smoke: true },
  { id: 'herbHut', name: 'Herb Hut', x: 1006, y: 442, w: 128, h: 96, style: 'house', roof: '#5e6d40', wall: '#b5a17d', trim: '#655136', door: { x: 1070, y: 542 }, sign: 'HERBS', smoke: true },
  { id: 'sawmill', name: 'Lowbank Sawmill', x: 618, y: 826, w: 166, h: 98, style: 'mill', roof: '#66503a', wall: '#9c8563', trim: '#4c3a29', door: { x: 701, y: 928 }, woodpile: true },
  { id: 'cabin', name: 'Woodcutter’s Cabin', x: 448, y: 824, w: 126, h: 92, style: 'cabin', roof: '#514435', wall: '#8d785a', trim: '#423225', door: { x: 511, y: 920 }, smoke: true },
  { id: 'mill', name: 'The Windmill', x: 700, y: 1002, w: 126, h: 118, style: 'windmill', roof: '#7c5c40', wall: '#c8b08a', trim: '#5b432e', door: { x: 763, y: 1124 }, sails: true },
  { id: 'farmhouse', name: 'Marle Farmhouse', x: 502, y: 1148, w: 164, h: 102, style: 'house', roof: '#965f42', wall: '#d0bda2', trim: '#6f4e34', door: { x: 584, y: 1254 }, smoke: true },
  { id: 'marenCottage', name: 'Tull Cottage', x: 1528, y: 926, w: 134, h: 96, style: 'house', roof: '#4d6163', wall: '#bcb095', trim: '#5d4b38', door: { x: 1595, y: 1026 }, smoke: true },
  { id: 'osprenCottage', name: 'Boathouse', x: 1642, y: 1044, w: 132, h: 96, style: 'house', roof: '#5a5341', wall: '#ab9c82', trim: '#51412f', door: { x: 1708, y: 1144 }, smoke: true },
  { id: 'lioraCottage', name: 'Fen Cottage', x: 1488, y: 1114, w: 130, h: 96, style: 'house', roof: '#5d5a4c', wall: '#c3b598', trim: '#5f4c37', door: { x: 1553, y: 1214 }, smoke: true },
  { id: 'workshop', name: 'Quill Workshop', x: 1310, y: 1104, w: 152, h: 106, style: 'workshop', roof: '#5b4b40', wall: '#ad9875', trim: '#4c3a2d', door: { x: 1386, y: 1214 }, smoke: true },
  { id: 'smokehouse', name: 'The Smokehouse', x: 1550, y: 814, w: 154, h: 96, style: 'smokehouse', roof: '#46423a', wall: '#8e795d', trim: '#453629', door: { x: 1626, y: 914 }, smoke: true }
];

export const PATHS = [
  { width: 38, points: [[1152, 1470], [1150, 1340], [1168, 1240], [1182, 1140], [1178, 1040], [1178, 960], [1180, 860], [1180, 800], [1180, 620], [1168, 566], [1120, 516], [1072, 460], [1048, 386], [1044, 336], [1100, 272], [1166, 214], [1212, 152]] },
  { width: 24, points: [[1182, 1132], [1080, 1116], [984, 1104], [884, 1112], [796, 1094], [736, 1042], [704, 978]] },
  { width: 22, points: [[746, 1048], [664, 1104], [614, 1150], [584, 1172]] },
  { width: 22, points: [[704, 978], [648, 1040], [632, 1120], [544, 1188], [438, 1226], [366, 1274]] },
  { width: 20, points: [[802, 1012], [758, 1060], [700, 1126], [628, 1188]] },
  { width: 26, points: [[1182, 1012], [1272, 1012], [1354, 1000], [1440, 1000], [1528, 1018], [1604, 1038], [1660, 1076], [1694, 1126]] },
  { width: 26, points: [[1272, 1012], [1408, 1024], [1528, 1004], [1616, 968], [1662, 906], [1682, 820]] },
  { width: 24, points: [[1386, 1012], [1392, 1080], [1388, 1174], [1420, 1240], [1502, 1262]] },
  { width: 18, points: [[1120, 516], [1010, 522], [912, 526], [887, 530]] },
  { width: 18, points: [[1072, 460], [1068, 524], [1074, 580], [1122, 594]] },
  { width: 18, points: [[1048, 386], [978, 356], [948, 328]] },
  { width: 16, points: [[1100, 272], [1236, 268], [1340, 284], [1404, 322], [1448, 370]] },
  { width: 16, points: [[1166, 214], [1200, 184], [1212, 156]] },
  { width: 22, points: [[1178, 960], [1110, 968], [1052, 986], [986, 1004]] },
  { width: 18, points: [[986, 1004], [940, 1076], [930, 1120]] },
  { width: 20, points: [[1152, 1340], [1078, 1306], [970, 1302], [846, 1268], [762, 1240], [680, 1224]] },
  { width: 20, points: [[1694, 1126], [1600, 1180], [1552, 1240], [1470, 1300]] }
];

export const TREES_HINT = {
  forestTop: 625,
  westGroveX: 210,
  eastGroveX: 2230
};

export const STALLS = [
  { id: 'osricStall', seller: 'osric', name: 'The Tin Peddler', x: 926, y: 948, awning: '#9c5a43', accent: '#d9b26f' },
  { id: 'fishStall', seller: 'maren', name: 'Fenn Fish', x: 1022, y: 962, awning: '#3f7373', accent: '#b9d8cf' }
];

export const INTERACTABLES = [
  { id: 'sign', type: 'sign', x: 1168, y: 1300, label: 'Read the gate sign' },
  { id: 'tavernDoor', type: 'tavern', x: 1200, y: 950, label: 'Step into the Bent Hook' },
  { id: 'well', type: 'well', x: 1088, y: 1168, label: 'Draw from the well' },
  { id: 'oldOak', type: 'oak', x: 1216, y: 1152, label: 'Rest beneath the old oak', radius: 46 }
];

export const QUEST_OBJECTS = [
  { id: 'lostNet', type: 'net', x: 1946, y: 846, label: 'Pull the gillnet from the reeds' },
  { id: 'track1', type: 'tracks', x: 1392, y: 362, label: 'Study the tracks by the old stones' },
  { id: 'track2', type: 'tracks', x: 1482, y: 330, label: 'Study the tracks at the fallen stone' },
  { id: 'track3', type: 'tracks', x: 1444, y: 246, label: 'Study the tracks beneath the thorn tree' },
  { id: 'page1', type: 'page', x: 1040, y: 972, label: 'Pick up the windblown ledger page' },
  { id: 'page2', type: 'page', x: 1716, y: 886, label: 'Pick up the ledger page by the dock' },
  { id: 'page3', type: 'page', x: 1176, y: 1122, label: 'Pick up the ledger page on the green' }
];

export const GATHER_NODES = [
  { id: 'herb1', type: 'herb', x: 1124, y: 612, label: 'Gather wild herbs' },
  { id: 'herb2', type: 'herb', x: 990, y: 356, label: 'Gather wild herbs' },
  { id: 'herb3', type: 'herb', x: 1284, y: 258, label: 'Gather wild herbs' },
  { id: 'herb4', type: 'herb', x: 784, y: 492, label: 'Gather wild herbs' },
  { id: 'herb5', type: 'herb', x: 1512, y: 434, label: 'Gather wild herbs' },
  { id: 'mush1', type: 'mushroom', x: 904, y: 300, label: 'Forage mooncap mushrooms' },
  { id: 'mush2', type: 'mushroom', x: 1196, y: 226, label: 'Forage mooncap mushrooms' },
  { id: 'mush3', type: 'mushroom', x: 1378, y: 404, label: 'Forage mooncap mushrooms' },
  { id: 'mush4', type: 'mushroom', x: 694, y: 356, label: 'Forage mooncap mushrooms' },
  { id: 'wood1', type: 'firewood', x: 672, y: 986, label: 'Gather fallen firewood' },
  { id: 'wood2', type: 'firewood', x: 812, y: 574, label: 'Gather fallen firewood' },
  { id: 'wood3', type: 'firewood', x: 556, y: 1082, label: 'Gather fallen firewood' },
  { id: 'wood4', type: 'firewood', x: 1540, y: 786, label: 'Gather fallen firewood' }
];

export const FISH_SPOTS = [
  { id: 'fishBridge', name: 'The bridge pool', x: 1078, y: 832, bonus: 0 },
  { id: 'fishReeds', name: 'The west reeds', x: 678, y: 838, bonus: 0.04 },
  { id: 'fishBend', name: 'The east bend', x: 1832, y: 852, bonus: 0.08 }
];

export const ITEMS = {
  herb: { name: 'Wild Herb', glyph: 'H', sell: 4, desc: 'Bitter yarrow and sweet elderflower. Ysolde will trade for it.' },
  mushroom: { name: 'Mooncap', glyph: 'M', sell: 6, desc: 'A pale forest mushroom. Good in pies, better in tinctures.' },
  fish: { name: 'Fenn Trout', glyph: 'F', sell: 9, desc: 'Silver-fleshed and river-cold. Maren pays best for these.' },
  firewood: { name: 'Firewood Bundle', glyph: 'W', sell: 3, desc: 'Dry ash and hazel, split small for a bakehouse oven.' },
  pelt: { name: 'Wolf Pelt', glyph: 'P', sell: 16, desc: 'Thick grey winter fur. Osric will not ask too many questions.' },
  bread: { name: 'Hearth Loaf', glyph: 'B', buy: 7, sell: 3, use: { hp: 28 }, desc: 'Still warm. Restores 28 health.' },
  tonic: { name: 'Yarrow Tonic', glyph: 'T', buy: 24, sell: 10, use: { hp: 999, stamina: 999 }, desc: 'A green, astringent draft. Restores all health and stamina.' },
  bait: { name: 'River Bait', glyph: 'R', buy: 4, sell: 1, desc: 'Fat grubs in a twist of cloth. Widens the strike window when fishing.' },
  boots: { name: 'Fennwalker Boots', glyph: 'N', buy: 70, sell: 25, unique: true, passive: 'speed', desc: 'Steeped in tallow and river mud. Walk 12% faster.' },
  charm: { name: 'Lucky Charm', glyph: 'C', buy: 55, sell: 20, unique: true, passive: 'luck', desc: 'A fox tooth on a leather cord. Sometimes finds a little more than you did.' },
  oil: { name: 'Wolfbane Oil', glyph: 'O', buy: 34, sell: 12, unique: true, passive: 'wolfbane', desc: 'Dagna’s sharp-smelling tincture. Your strikes bite 25% deeper into wolves.' },
  wardenCharm: { name: 'Warden’s Charm', glyph: 'A', sell: 0, unique: true, passive: 'stamina', desc: 'A wren-feather and rowan-berry knot. Ivor’s trust, worn at the collar. +15 stamina.' }
};

export const SELLERS = {
  osric: {
    title: 'Peddler of the Tin Wagon',
    shopPlaces: ['market'],
    buys: ['herb', 'mushroom', 'fish', 'firewood', 'pelt'],
    stock: ['tonic', 'bread', 'bait', 'charm'],
    line: 'Everything has a price, friend. Some of it is even fair.'
  },
  hesta: {
    title: 'Baker and Hearthkeeper',
    shopPlaces: ['market', 'bakehouse'],
    buys: ['herb', 'mushroom'],
    stock: ['bread', 'tonic'],
    line: 'Two loaves and a kind word. That is how a village stays standing.'
  },
  maren: {
    title: 'Ferrywoman and Fishmonger',
    shopPlaces: ['market'],
    buys: ['fish'],
    stock: ['fish', 'bait'],
    line: 'Fresh off the Fenn. I do not sell yesterday’s catch.'
  },
  ysolde: {
    title: 'Herbalist',
    shopPlaces: ['market', 'herbHut'],
    buys: ['herb', 'mushroom'],
    stock: ['tonic', 'bait'],
    line: 'Root, leaf and patience. Most ailments only need two of the three.'
  },
  dagna: {
    title: 'Smith of Ember Forge',
    shopPlaces: ['forge'],
    buys: ['firewood', 'pelt'],
    stock: ['boots', 'oil'],
    line: 'Good steel. No rust, no flattery, no credit.'
  }
};

export const HEROES = [
  {
    id: 'rowan',
    name: 'Rowan Vale',
    title: 'The Wayfarer',
    className: 'Wayfarer',
    pronouns: 'he/him',
    tagline: 'A road-walker with a hunter’s patience and a green thumb.',
    bio: 'Rowan followed the forest roads south chasing rumours of a wolf that walks like a man. He carries a short bow, a good knife, and an unshakable habit of noticing things other people miss.',
    stats: { might: 2, wit: 3, grace: 4 },
    maxHp: 100,
    maxStamina: 115,
    speed: 190,
    passive: { name: 'Greenstep', text: 'Walks 8% faster and sometimes finds an extra herb in the undergrowth.' },
    ability: { name: 'Keen Eye', text: 'Reveal every gatherable and animal nearby for 18 seconds.', type: 'keen', cooldown: 50 },
    startingItems: { bread: 1, herb: 2 },
    look: { skin: '#c38e6a', hair: '#7a3f2a', hairStyle: 'short', facial: 'stubble', headwear: 'hood', coat: '#486750', shirt: '#d7c6a2', accent: '#c9a25b', apron: false }
  },
  {
    id: 'ilsa',
    name: 'Ilsa Varn',
    title: 'The Bulwark',
    className: 'Shieldbearer',
    pronouns: 'she/her',
    tagline: 'A stubborn shield between the village and the dark.',
    bio: 'Ilsa served three years on the eastern road guard before the road took more from her than she was willing to give. She came to Alderwick for the quiet, and stayed because the quiet kept needing a shield.',
    stats: { might: 5, wit: 2, grace: 2 },
    maxHp: 150,
    maxStamina: 95,
    speed: 170,
    passive: { name: 'Stubborn as Stone', text: 'Takes 20% less damage from beasts.' },
    ability: { name: 'Second Wind', text: 'Recover 40% health and all stamina. A long breath, a planted shield.', type: 'secondwind', cooldown: 70 },
    startingItems: { bread: 2, tonic: 0 },
    look: { skin: '#7f5038', hair: '#2c2420', hairStyle: 'braid', facial: 'none', headwear: null, coat: '#673b34', shirt: '#c9bca4', accent: '#b7a277', apron: false }
  },
  {
    id: 'corvin',
    name: 'Corvin Hale',
    title: 'The Emberkith',
    className: 'Emberkith',
    pronouns: 'he/him',
    tagline: 'A gentle fire that refuses to go out.',
    bio: 'Corvin studied ember-lore in the cold northern schools, where they teach you to carry a flame without burning yourself. He arrived in Alderwick with soot on his sleeves and a lantern that never needs a wick.',
    stats: { might: 1, wit: 5, grace: 3 },
    maxHp: 110,
    maxStamina: 120,
    speed: 178,
    passive: { name: 'Emberlight', text: 'A soft personal lantern glows around him after dusk.' },
    ability: { name: 'Ember Ward', text: 'A ring of warm fire keeps wolves at bay for 22 seconds.', type: 'emberward', cooldown: 60 },
    startingItems: { bread: 1, tonic: 1 },
    look: { skin: '#d5ad8a', hair: '#2e2725', hairStyle: 'long', facial: 'none', headwear: null, coat: '#4c3c60', shirt: '#cdc1ad', accent: '#d29d5a', apron: false }
  }
];

export const NPCS = [
  {
    id: 'maren', name: 'Maren Tull', faction: 'riverfolk', role: 'Ferrywoman', age: 38, pronouns: 'she/her',
    homeName: 'Tull Cottage, east bank', tool: 'pole',
    look: { skin: '#b98260', hair: '#3d2a21', hairStyle: 'braid', facial: 'none', headwear: 'bandana', coat: '#315f68', shirt: '#d8c7a5', accent: '#d3a75f', apron: false },
    quote: 'A ferry is just a promise that you can come back.',
    relationships: 'Ospren’s older sister. Trusts Ivor with river business and no one with boat repairs.',
    hello: 'Mind the wet boards. The Fenn is in a generous mood, and generous rivers have long fingers.',
    chats: [
      'My grandmother rowed this crossing with a broken oar for forty years. I have better oars and a better temper.',
      'If you need the far bank, call out. I will not leave a stranger stranded for want of a coin.'
    ],
    schedule: [
      { from: 330, to: 480, place: 'dock', state: 'work', activity: 'Checking the drift nets' },
      { from: 480, to: 690, place: 'dock', state: 'work', activity: 'Running the ferry crossing' },
      { from: 690, to: 750, place: 'tavern', state: 'meal', activity: 'Taking a late lunch' },
      { from: 750, to: 990, place: 'market', state: 'work', activity: 'Selling the morning catch' },
      { from: 990, to: 1080, place: 'dock', state: 'work', activity: 'Sorting cargo crates' },
      { from: 1080, to: 1290, place: 'tavern', state: 'social', activity: 'Trading dockside gossip' },
      { from: 1290, to: 1350, place: 'riverbank', state: 'leisure', activity: 'Walking the bank before bed' },
      { from: 1350, to: 330, place: 'home', state: 'sleep', activity: 'Asleep at the Tull cottage' }
    ]
  },
  {
    id: 'ospren', name: 'Ospren Tull', faction: 'riverfolk', role: 'Fisher', age: 29, pronouns: 'he/him',
    homeName: 'Boathouse loft, east bank', tool: 'rod',
    look: { skin: '#c08d67', hair: '#5b3826', hairStyle: 'short', facial: 'stubble', headwear: 'cap', coat: '#3c6b6f', shirt: '#c7bda1', accent: '#7fa39b', apron: false },
    quote: 'Fish are honest. They tell you exactly how good you are.',
    relationships: 'Maren’s younger brother. Owes Dagna eleven coins and a new pair of tongs.',
    hello: 'Quiet as you like by the water. The trout can hear a heavy boot from here to the mill.',
    chats: [
      'The big pool under the bridge is a liar. Looks empty, holds half the river’s wisdom.',
      'Maren says I overwork the nets. Maren has never had to buy net twine on a fisher’s wage.'
    ],
    schedule: [
      { from: 300, to: 600, place: 'riverbank', state: 'work', activity: 'Fly-fishing the shallows' },
      { from: 600, to: 720, place: 'dock', state: 'work', activity: 'Mending gillnets' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'Lunch and a cold cider' },
      { from: 780, to: 1020, place: 'riverbank', state: 'work', activity: 'Working the west pools' },
      { from: 1020, to: 1110, place: 'dock', state: 'work', activity: 'Unloading the skiff' },
      { from: 1110, to: 1320, place: 'tavern', state: 'social', activity: 'Losing at cards to Nell' },
      { from: 1320, to: 300, place: 'home', state: 'sleep', activity: 'Asleep in the boathouse loft' }
    ],
    quest: 'gillnet'
  },
  {
    id: 'liora', name: 'Liora Fen', faction: 'riverfolk', role: 'Fish smoker', age: 34, pronouns: 'she/her',
    homeName: 'Fen Cottage, east bank', tool: 'basket',
    look: { skin: '#e0b591', hair: '#a45c32', hairStyle: 'bun', facial: 'none', headwear: null, coat: '#4a6f67', shirt: '#d8c6a8', accent: '#d88f5f', apron: true },
    quote: 'Smoke, salt and patience. Same recipe as a good marriage.',
    relationships: 'Childhood friend of Hesta. Keeps a card game with Nell every Thursday.',
    hello: 'You will smell like oak smoke for an hour. Do not stand downwind of Dagna if you mind being sniffed.',
    chats: [
      'The smokehouse catches the whole village on its clothes. You can tell a good morning by the smell of it.',
      'The heron at the west reeds steals more fish than any otter. I respect the heron.'
    ],
    schedule: [
      { from: 390, to: 600, place: 'smokehouse', state: 'work', activity: 'Smoking the morning catch' },
      { from: 600, to: 720, place: 'market', state: 'work', activity: 'Selling smoked trout' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'A quick pie at the tavern' },
      { from: 780, to: 1020, place: 'smokehouse', state: 'work', activity: 'Tending the smoke fires' },
      { from: 1020, to: 1110, place: 'riverbank', state: 'leisure', activity: 'Feeding the ducks' },
      { from: 1110, to: 1320, place: 'tavern', state: 'social', activity: 'Playing cards with Nell' },
      { from: 1320, to: 390, place: 'home', state: 'sleep', activity: 'Asleep at Fen Cottage' }
    ]
  },
  {
    id: 'hesta', name: 'Hesta Marle', faction: 'hearthfolk', role: 'Baker', age: 41, pronouns: 'she/her',
    homeName: 'Rooms above the bakehouse', tool: 'basket',
    look: { skin: '#d6a47e', hair: '#7a4a2d', hairStyle: 'bun', facial: 'none', headwear: 'headscarf', coat: '#9b6b43', shirt: '#e0d3b6', accent: '#c6a468', apron: true },
    quote: 'A village is only as warm as its second loaf.',
    relationships: 'Bran’s sister. Aunt to Tam. Liora’s oldest friend.',
    hello: 'Take a heel of bread before you go gallivanting. You look like a strong wind would take you.',
    chats: [
      'I know every soul in Alderwick by their order: two rye, one white, extra crust for the ferryfolk.',
      'Bran says I fuss. I say a village fed on time is a village less likely to quarrel.'
    ],
    schedule: [
      { from: 270, to: 510, place: 'bakehouse', state: 'work', activity: 'Pulling the dawn loaves' },
      { from: 510, to: 600, place: 'market', state: 'work', activity: 'Selling warm bread' },
      { from: 600, to: 750, place: 'bakehouse', state: 'work', activity: 'Kneading the noon batch' },
      { from: 750, to: 810, place: 'tavern', state: 'meal', activity: 'Lunch with Bran' },
      { from: 810, to: 990, place: 'fields', state: 'work', activity: 'Taking bread to the fieldhands' },
      { from: 990, to: 1080, place: 'bakehouse', state: 'work', activity: 'Sweeping out the ovens' },
      { from: 1080, to: 1260, place: 'tavern', state: 'social', activity: 'Gossip and a small ale' },
      { from: 1260, to: 270, place: 'home', state: 'sleep', activity: 'Asleep above the bakehouse' }
    ],
    quest: 'loaf'
  },
  {
    id: 'bran', name: 'Bran Marle', faction: 'hearthfolk', role: 'Farmer', age: 44, pronouns: 'he/him',
    homeName: 'Marle Farmhouse, south fields', tool: 'hoe',
    look: { skin: '#b77e56', hair: '#4c3724', hairStyle: 'short', facial: 'stubble', headwear: 'straw', coat: '#6e6a3e', shirt: '#bfb18e', accent: '#a08a55', apron: false },
    quote: 'Barley does not care for your worries. That is why I like barley.',
    relationships: 'Hesta’s brother, Nell’s son, Tam’s father.',
    hello: 'Mind the fence line. The ewes are cleverer than they look and twice as stubborn.',
    chats: [
      'The south fields give us barley, beans and a fair amount of backache. Good honest arithmetic.',
      'Tam wants to be a ferryman now. Last week it was a smith. I tell him to learn the soil first.'
    ],
    schedule: [
      { from: 330, to: 690, place: 'fields', state: 'work', activity: 'Turning the barley rows' },
      { from: 690, to: 750, place: 'tavern', state: 'meal', activity: 'A ploughman’s lunch' },
      { from: 750, to: 1050, place: 'fields', state: 'work', activity: 'Repairing the field fence' },
      { from: 1050, to: 1110, place: 'mill', state: 'work', activity: 'Hauling grain to the mill' },
      { from: 1110, to: 1290, place: 'tavern', state: 'social', activity: 'Telling tall tales' },
      { from: 1290, to: 330, place: 'home', state: 'sleep', activity: 'Asleep at Marle Farmhouse' }
    ]
  },
  {
    id: 'tam', name: 'Tam Marle', faction: 'hearthfolk', role: 'River lad', age: 10, pronouns: 'he/him',
    homeName: 'Marle Farmhouse, south fields', tool: null,
    look: { skin: '#d2a078', hair: '#8b5a32', hairStyle: 'short', facial: 'none', headwear: null, coat: '#7a8f6b', shirt: '#d9ccb0', accent: '#c58b5c', apron: false },
    quote: 'When I grow up I am going to ride the ferry all the way to the sea.',
    relationships: 'Bran’s son. Hesta’s nephew. Nell’s grandson. Follows Wren around asking how things work.',
    hello: 'Have you seen the big pike under the bridge? Ospren says it is older than the mill.',
    chats: [
      'Wren is building a clock that runs on river water. Wren says it will work. Wren says that a lot.',
      'If you see the white deer in the Elderwood, you are not allowed to say so. It is bad luck to name her.'
    ],
    schedule: [
      { from: 450, to: 540, place: 'bakehouse', state: 'work', activity: 'Sweeping flour for Hesta' },
      { from: 540, to: 660, place: 'green', state: 'leisure', activity: 'Racing the village dogs' },
      { from: 660, to: 780, place: 'bakehouse', state: 'meal', activity: 'Bread and honey' },
      { from: 780, to: 930, place: 'green', state: 'leisure', activity: 'Playing knucklebones with the ferry kids' },
      { from: 930, to: 1020, place: 'riverbank', state: 'leisure', activity: 'Skipping stones by the bridge' },
      { from: 1020, to: 1110, place: 'green', state: 'leisure', activity: 'Chasing geese off the green' },
      { from: 1110, to: 1170, place: 'tavern', state: 'meal', activity: 'Supper before bedtime' },
      { from: 1170, to: 450, place: 'home', state: 'sleep', activity: 'Fast asleep in the loft' }
    ]
  },
  {
    id: 'nell', name: 'Nell Marle', faction: 'hearthfolk', role: 'Keeper of the Bent Hook', age: 58, pronouns: 'she/her',
    homeName: 'Rooms above the Bent Hook', tool: null,
    look: { skin: '#c89a76', hair: '#9a9488', hairStyle: 'bun', facial: 'none', headwear: null, coat: '#6b4e3e', shirt: '#d8c8ab', accent: '#c39658', apron: true },
    quote: 'A tavern is a village with the roof taken off.',
    relationships: 'Mother of Hesta and Bran, grandmother of Tam. Knows every secret in Alderwick and keeps most of them.',
    hello: 'Come in out of the weather. Even when the weather is fine, come in out of it.',
    chats: [
      'The Bent Hook has stood through three floods and one very determined goat. It will outlast us all.',
      'You can judge a village by how it treats its quietest table. Ours is by the fire and always filled.'
    ],
    schedule: [
      { from: 360, to: 600, place: 'tavern', state: 'work', activity: 'Baking pies and mending chairs' },
      { from: 600, to: 660, place: 'market', state: 'work', activity: 'Buying the day’s vegetables' },
      { from: 660, to: 900, place: 'tavern', state: 'work', activity: 'Keeping the hearth and the peace' },
      { from: 900, to: 960, place: 'green', state: 'leisure', activity: 'A pipe on the green' },
      { from: 960, to: 1380, place: 'tavern', state: 'work', activity: 'Pouring for the evening crowd' },
      { from: 1380, to: 360, place: 'home', state: 'sleep', activity: 'Asleep above the tavern' }
    ]
  },
  {
    id: 'ivor', name: 'Ivor Ash', faction: 'wardens', role: 'Forest warden', age: 47, pronouns: 'he/him',
    homeName: 'Warden Lodge, Elderwood edge', tool: 'staff',
    look: { skin: '#a9714c', hair: '#4a3f30', hairStyle: 'short', facial: 'beard', headwear: 'hood', coat: '#3d5a45', shirt: '#b8ac8e', accent: '#8f9b69', apron: false },
    quote: 'The forest is not empty. It is merely polite.',
    relationships: 'Old friend of Ysolde. Trusts Kestrel with an axe and no one with the boundary stones.',
    hello: 'Keep your voice low and your eyes on the ground. The pines here keep a long memory.',
    chats: [
      'The boundary stones at the north edge are older than the village name. Something marked this place before us.',
      'At dusk, stay on the path. The wolves do not hunt people by habit, but a hungry winter makes new habits.'
    ],
    schedule: [
      { from: 270, to: 540, place: 'forestEdge', state: 'work', activity: 'Walking the dawn boundary' },
      { from: 540, to: 720, place: 'glade', state: 'work', activity: 'Checking snares at the glade' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'A bowl of stew' },
      { from: 780, to: 1080, place: 'stones', state: 'work', activity: 'Tending the old boundary stones' },
      { from: 1080, to: 1140, place: 'tavern', state: 'social', activity: 'Listening more than talking' },
      { from: 1140, to: 1410, place: 'gate', state: 'work', activity: 'Night watch by the south gate' },
      { from: 1410, to: 270, place: 'home', state: 'sleep', activity: 'Asleep at the warden lodge' }
    ],
    quest: 'tracks'
  },
  {
    id: 'ysolde', name: 'Ysolde Reed', faction: 'wardens', role: 'Herbalist', age: 36, pronouns: 'she/her',
    homeName: 'The Herb Hut, north bank', tool: 'basket',
    look: { skin: '#e2b795', hair: '#b0784c', hairStyle: 'long', facial: 'none', headwear: null, coat: '#6d7f55', shirt: '#d6c9ac', accent: '#a9b57d', apron: true },
    quote: 'Most poisons are only medicine that lost its manners.',
    relationships: 'Ivor’s oldest friend. Sends tinctures downriver and treats Dagna’s burnt knuckles without comment.',
    hello: 'Wipe your boots, or the feverfew will take offence and refuse to flower.',
    chats: [
      'The Elderwood gives me yarrow, elderflower and three headaches a week. A fair trade, I think.',
      'If a wolf bites you, come to me before the forge. Dagna means well, but her remedy is a rag and a strong word.'
    ],
    schedule: [
      { from: 360, to: 540, place: 'herbGarden', state: 'work', activity: 'Weeding the physic beds' },
      { from: 540, to: 750, place: 'glade', state: 'work', activity: 'Gathering yarrow and elderflower' },
      { from: 750, to: 810, place: 'tavern', state: 'meal', activity: 'Tea and a cheese roll' },
      { from: 810, to: 990, place: 'herbHut', state: 'work', activity: 'Mixing salves and tinctures' },
      { from: 990, to: 1080, place: 'market', state: 'work', activity: 'Selling herbs and remedies' },
      { from: 1080, to: 1230, place: 'tavern', state: 'social', activity: 'Trading news over tea' },
      { from: 1230, to: 1320, place: 'herbGarden', state: 'leisure', activity: 'Watering by moonlight' },
      { from: 1320, to: 360, place: 'home', state: 'sleep', activity: 'Asleep in the herb hut' }
    ]
  },
  {
    id: 'kestrel', name: 'Kestrel Voss', faction: 'wardens', role: 'Woodcutter', age: 31, pronouns: 'they/them',
    homeName: 'Woodcutter’s cabin, west bank', tool: 'axe',
    look: { skin: '#8d5c3d', hair: '#2f2f2a', hairStyle: 'locs', facial: 'none', headwear: null, coat: '#7d5b3c', shirt: '#b9a988', accent: '#9d8756', apron: false },
    quote: 'I take the trees that have already chosen to fall.',
    relationships: 'Close friends with Wren. Sits with Ivor at the end of the bar in comfortable silence.',
    hello: 'Watch the chips. Green wood throws them further than you expect.',
    chats: [
      'The windfall in the west lot will keep the village warm until spring. I counted it twice.',
      'Wren wants me to test a wood-splitter with gears. I told Wren the axe has never once demanded a spare part.'
    ],
    schedule: [
      { from: 330, to: 630, place: 'woodlot', state: 'work', activity: 'Felling windfall in the woodlot' },
      { from: 630, to: 720, place: 'sawmill', state: 'work', activity: 'Stacking planks by the water' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'Bread, cheese and silence' },
      { from: 780, to: 1020, place: 'woodlot', state: 'work', activity: 'Hauling timber to the bank' },
      { from: 1020, to: 1140, place: 'sawmill', state: 'work', activity: 'Sharpening the crosscut saws' },
      { from: 1140, to: 1320, place: 'tavern', state: 'social', activity: 'A quiet corner and a cider' },
      { from: 1320, to: 330, place: 'home', state: 'sleep', activity: 'Asleep in the woodcutter’s cabin' }
    ]
  },
  {
    id: 'dagna', name: 'Dagna Ember', faction: 'emberwrights', role: 'Blacksmith', age: 45, pronouns: 'she/her',
    homeName: 'Rooms above the forge', tool: 'hammer',
    look: { skin: '#c1845c', hair: '#b5542c', hairStyle: 'braid', facial: 'none', headwear: null, coat: '#5a4638', shirt: '#c9b28e', accent: '#d98c52', apron: true },
    quote: 'A blade is a promise you can hold. Keep it sharp.',
    relationships: 'Osric’s old friend and longest-running argument. Ospren owes Dagna eleven coins.',
    hello: 'Stand clear of the sparks unless you want a new set of freckles.',
    chats: [
      'The old forge belonged to my mother. She could shoe a horse and insult a duke in the same breath.',
      'In the spring I will make a proper sign for the forge. Until then, the smoke does the advertising.'
    ],
    schedule: [
      { from: 300, to: 480, place: 'forge', state: 'work', activity: 'Rousing the forge fire' },
      { from: 480, to: 540, place: 'market', state: 'work', activity: 'Buying charcoal and nails' },
      { from: 540, to: 750, place: 'forge', state: 'work', activity: 'Shoeing the miller’s mare' },
      { from: 750, to: 810, place: 'tavern', state: 'meal', activity: 'Meat pie and small beer' },
      { from: 810, to: 1110, place: 'forge', state: 'work', activity: 'Beating out a new ploughshare' },
      { from: 1110, to: 1260, place: 'tavern', state: 'social', activity: 'Arm-wrestling Ospren again' },
      { from: 1260, to: 300, place: 'home', state: 'sleep', activity: 'Asleep above the forge' }
    ]
  },
  {
    id: 'osric', name: 'Osric Vane', faction: 'emberwrights', role: 'Peddler', age: 39, pronouns: 'he/him',
    homeName: 'The painted wagon, by the green', tool: null,
    look: { skin: '#caa07b', hair: '#4a3326', hairStyle: 'short', facial: 'moustache', headwear: 'feather', coat: '#7a4e3a', shirt: '#cbb892', accent: '#d9b26f', apron: false },
    quote: 'A fair price is whatever both of us can live with.',
    relationships: 'Dagna’s oldest friend. Buys Kestrel’s timber and sells half of it back as furniture.',
    hello: 'Ah, a new face with an old road on their boots. Come and see what the wagon brought in.',
    chats: [
      'I came to Alderwick for one market day and stayed four years. The food is good, and the arguments are better.',
      'My ledger has gone missing pages again. The wind or a magpie, I cannot say which is worse.'
    ],
    schedule: [
      { from: 420, to: 720, place: 'market', state: 'work', activity: 'Pedlaring ribbons and tinware' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'Lunch with whoever listens' },
      { from: 780, to: 1050, place: 'market', state: 'work', activity: 'Haggling over copper and salt' },
      { from: 1050, to: 1110, place: 'market', state: 'work', activity: 'Packing the wagon before dusk' },
      { from: 1110, to: 1320, place: 'tavern', state: 'social', activity: 'Buying the first round' },
      { from: 1320, to: 420, place: 'home', state: 'sleep', activity: 'Asleep in the painted wagon' }
    ],
    quest: 'ledger'
  },
  {
    id: 'wren', name: 'Wren Quill', faction: 'emberwrights', role: 'Tinker', age: 24, pronouns: 'she/her',
    homeName: 'The workshop loft', tool: 'wrench',
    look: { skin: '#dbb794', hair: '#2e2828', hairStyle: 'short', facial: 'none', headwear: 'goggles', coat: '#4c5350', shirt: '#c4b494', accent: '#c89b5b', apron: true },
    quote: 'Everything is broken. The interesting part is how.',
    relationships: 'Apprentice to nobody. Friend of Kestrel and Tam’s favourite nuisance.',
    hello: 'Do not touch the brass bowl. It is either a clock or a very slow explosive. I am still deciding.',
    chats: [
      'I am building a water clock for the green. It will keep time better than the sun, if the eels cooperate.',
      'Tam asks better questions than half the guild. I will make that child an engineer yet.'
    ],
    schedule: [
      { from: 450, to: 630, place: 'workshop', state: 'work', activity: 'Tuning the water clock' },
      { from: 630, to: 720, place: 'riverbank', state: 'work', activity: 'Testing a paddle prototype' },
      { from: 720, to: 780, place: 'tavern', state: 'meal', activity: 'A hurried bowl of stew' },
      { from: 780, to: 1050, place: 'workshop', state: 'work', activity: 'Soldering a weathervane' },
      { from: 1050, to: 1110, place: 'market', state: 'leisure', activity: 'Staring at Osric’s brass fittings' },
      { from: 1110, to: 1350, place: 'tavern', state: 'social', activity: 'Sketches over a cider' },
      { from: 1350, to: 1440, place: 'workshop', state: 'work', activity: 'Midnight tinkering' },
      { from: 0, to: 450, place: 'home', state: 'sleep', activity: 'Asleep in the workshop loft' }
    ]
  }
];

export const QUESTS = {
  welcome: {
    id: 'welcome', title: 'The Road to Alderwick', auto: true, giver: null, faction: null,
    summary: 'You have walked through the south gate with dust on your boots. Get to know the people who keep this bend of the river alive.',
    tasks: [{ label: 'Speak with anyone in Alderwick', type: 'auto', done: false }],
    reward: { gold: 10, xp: 15, items: [] },
    offerText: '', activeText: '', completeText: ''
  },
  gillnet: {
    id: 'gillnet', title: 'The Lost Gillnet', giver: 'ospren', faction: 'riverfolk',
    summary: 'A gillnet tore loose in last night’s current and drifted downriver. Ospren cannot afford to lose the twine — or the catch tangled in it.',
    tasks: [
      { label: 'Recover the gillnet from the reeds at the east bend', type: 'visit', target: 'lostNet' },
      { label: 'Return the gillnet to Ospren', type: 'return' }
    ],
    reward: { gold: 30, xp: 45, items: [{ id: 'fish', qty: 2 }] },
    offerText: 'The current took one of my gillnets in the night. It will be snagged in the reeds at the east bend, past the dock. My knees are bad and my pride is worse — would you fetch it?',
    activeText: 'The east bend, past the smokehouse. Follow the bank until the water slows. Mind the mud; it has ambitions.',
    completeText: 'That is the one. Twine’s sound, floats still tied. You have saved me a week of mending and a fair bit of pride. Take these trout — they were in it when it went.'
  },
  loaf: {
    id: 'loaf', title: 'Flour for the Morning', giver: 'hesta', faction: 'hearthfolk',
    summary: 'The bakehouse oven is hungry and the woodpile is low. Hesta needs a pair of strong hands and two dry bundles of firewood before the evening batch.',
    tasks: [
      { label: 'Gather 2 bundles of firewood', type: 'have', item: 'firewood', qty: 2, consume: true },
      { label: 'Bring the firewood to Hesta', type: 'return' }
    ],
    reward: { gold: 25, xp: 40, items: [{ id: 'bread', qty: 2 }] },
    offerText: 'The oven eats more than Bran does, and Bran eats for two. Two dry bundles of firewood and I will have the evening batch in. There is always fallen wood by the sawmill.',
    activeText: 'Two bundles, dry as a sermon. The sawmill pile is picked over; try the woodlot or the old field edge.',
    completeText: 'Dry as a sermon indeed. Set them by the oven — careful now. These two loaves are for your satchel, and do not tell Bran I gave you the good crust.'
  },
  tracks: {
    id: 'tracks', title: 'Tracks at the Treeline', giver: 'ivor', faction: 'wardens',
    summary: 'Something large has been circling the Old Stones. Ivor wants the tracks read before dark, while the ground still holds their shape.',
    tasks: [
      { label: 'Study the tracks at the Old Stones', type: 'visit', target: 'track1' },
      { label: 'Study the tracks at the fallen stone', type: 'visit', target: 'track2' },
      { label: 'Study the tracks beneath the thorn tree', type: 'visit', target: 'track3' },
      { label: 'Bring word to Ivor', type: 'return' }
    ],
    reward: { gold: 40, xp: 55, items: [{ id: 'wardenCharm', qty: 1 }] },
    offerText: 'Three nights running, something has walked the circle at the Old Stones. Not deer. Not boar. I would go myself, but the gate needs a warden after dusk. Read the ground for me — three signs, then bring me word.',
    activeText: 'The tracks lie around the Old Stones, north and east of the forest path. Look for the bent grass and the deep prints.',
    completeText: 'Wolf sign. A big one, walking alone, and too bold by half. You read the ground well. Take this charm — rowan berry and wren feather. It will not stop a wolf, but the old stories say it helps you keep your feet.'
  },
  ledger: {
    id: 'ledger', title: 'The Peddler’s Ledger', giver: 'osric', faction: 'emberwrights',
    summary: 'Three pages of Osric’s ledger have blown loose across the village. Without them, half the village will discover he has been extending credit rather more generously than he admits.',
    tasks: [
      { label: 'Find the ledger page by the market stalls', type: 'visit', target: 'page1' },
      { label: 'Find the ledger page near the dock', type: 'visit', target: 'page2' },
      { label: 'Find the ledger page on the green', type: 'visit', target: 'page3' },
      { label: 'Return the pages to Osric', type: 'return' }
    ],
    reward: { gold: 35, xp: 50, items: [{ id: 'tonic', qty: 2 }] },
    offerText: 'Do not look at me like that. The wind took three pages of my ledger — market, dock and green, most likely. I cannot have half the village knowing what the other half owes. Fetch them quietly?',
    activeText: 'Market square, the dock, and somewhere on the green. Wind is a cruel clerk, but it does not read.',
    completeText: 'All three! You are a treasure and a discreet one, which is rarer. Here — two tonics from the wagon. And if Dagna asks, you found nothing at all.'
  }
};

export const ANIMAL_SITES = [
  { id: 'deer1', species: 'deer', x: 944, y: 356, roam: 96 },
  { id: 'deer2', species: 'deer', x: 1338, y: 440, roam: 88 },
  { id: 'deer3', species: 'deer', x: 760, y: 322, roam: 80 },
  { id: 'rabbit1', species: 'rabbit', x: 858, y: 568, roam: 58 },
  { id: 'rabbit2', species: 'rabbit', x: 548, y: 1052, roam: 52 },
  { id: 'rabbit3', species: 'rabbit', x: 1454, y: 1262, roam: 66 },
  { id: 'fox1', species: 'fox', x: 1292, y: 540, roam: 110 },
  { id: 'duck1', species: 'duck', x: 1310, y: 702, roam: 90 },
  { id: 'duck2', species: 'duck', x: 1392, y: 718, roam: 70 },
  { id: 'duck3', species: 'duck', x: 934, y: 724, roam: 60 },
  { id: 'duck4', species: 'duck', x: 1752, y: 736, roam: 80 },
  { id: 'heron', species: 'heron', x: 624, y: 712, roam: 18 },
  { id: 'otter', species: 'otter', x: 1868, y: 740, roam: 86 },
  { id: 'sheep1', species: 'sheep', x: 559, y: 1312, roam: 40 },
  { id: 'sheep2', species: 'sheep', x: 614, y: 1348, roam: 36 },
  { id: 'sheep3', species: 'sheep', x: 666, y: 1302, roam: 34 },
  { id: 'sheep4', species: 'sheep', x: 586, y: 1368, roam: 30 },
  { id: 'cat', species: 'cat', x: 1246, y: 968, roam: 168 },
  { id: 'bird1', species: 'bird', x: 1040, y: 870, roam: 240, fly: true },
  { id: 'bird2', species: 'bird', x: 1420, y: 760, roam: 200, fly: true },
  { id: 'bird3', species: 'bird', x: 780, y: 950, roam: 190, fly: true }
];

export const WOLF_SITES = [
  { x: 980, y: 552 },
  { x: 764, y: 1048 }
];

export const SPECIES = {
  deer: { label: 'Red deer', behavior: 'graze', speed: 24, flee: 145, color: '#9a6f48', accent: '#e4d2b0' },
  rabbit: { label: 'Brown hare', behavior: 'hop', speed: 36, flee: 120, color: '#9a8468', accent: '#d4c3a6' },
  fox: { label: 'Red fox', behavior: 'prowl', speed: 30, flee: 160, color: '#a95b35', accent: '#e4c9a4' },
  duck: { label: 'Mallard duck', behavior: 'swim', speed: 16, flee: 70, color: '#6f7355', accent: '#c9a35f' },
  heron: { label: 'Grey heron', behavior: 'still', speed: 6, flee: 90, color: '#8e99a0', accent: '#d9d8cd' },
  otter: { label: 'River otter', behavior: 'swim', speed: 30, flee: 90, color: '#6b4d38', accent: '#c8a77f' },
  sheep: { label: 'Meadow ewe', behavior: 'sheep', speed: 12, flee: 50, color: '#d7ccb5', accent: '#5d5243' },
  cat: { label: 'Pudding the cat', behavior: 'cat', speed: 22, flee: 60, color: '#7a5b40', accent: '#e0cbaa' },
  bird: { label: 'River swift', behavior: 'fly', speed: 70, flee: 0, color: '#4c4a42', accent: '#cfc8b5' },
  wolf: { label: 'Grey wolf', behavior: 'wolf', speed: 66, flee: 0, color: '#5d605c', accent: '#b7bcb1' }
};

export const PLUS = {
  START_GOLD: 20,
  LEVEL_XP: (level) => Math.round(50 * Math.pow(1.45, level - 1)),
  GATHER_XP: 4,
  FISH_XP: 8,
  WOLF_XP: 22,
  HERB_REGROW_HOUR: 6
};
