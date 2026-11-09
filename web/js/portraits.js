// Alderwick — hand-built SVG portraits. No external art, every face is shapes.

const clamp = (v, a, b) => Math.max(a, Math.min(b, v));

function hash(str) {
  let h = 2166136261;
  for (let i = 0; i < str.length; i++) {
    h ^= str.charCodeAt(i);
    h = Math.imul(h, 16777619);
  }
  return h >>> 0;
}

function rgb(hex) {
  const h = hex.replace('#', '');
  const n = parseInt(h.length === 3 ? h.split('').map(c => c + c).join('') : h, 16);
  return { r: (n >> 16) & 255, g: (n >> 8) & 255, b: n & 255 };
}

function mix(a, b, t) {
  const A = rgb(a), B = rgb(b);
  const r = Math.round(A.r + (B.r - A.r) * t);
  const g = Math.round(A.g + (B.g - A.g) * t);
  const bl = Math.round(A.b + (B.b - A.b) * t);
  return '#' + [r, g, bl].map(v => clamp(v, 0, 255).toString(16).padStart(2, '0')).join('');
}

function hairBack(style, hair, shade) {
  switch (style) {
    case 'braid':
      return `<path d="M29 48 C27 25 38 14 50 14 C62 14 73 25 71 48 L71 66 C66 58 64 46 64 34 L36 34 C36 46 34 58 29 66 Z" fill="${hair}"/>
        <path d="M69 53 C76 58 78 68 75 78 C73 86 75 91 72 98" stroke="${shade}" stroke-width="4.2" fill="none" stroke-linecap="round"/>
        <path d="M69 53 C74 58 76 67 73 76 C71 84 73 90 71 95" stroke="${hair}" stroke-width="2.6" fill="none" stroke-linecap="round"/>
        <ellipse cx="72" cy="53" rx="3.4" ry="3.6" fill="${hair}"/>`;
    case 'bun':
      return `<path d="M29 47 C28 25 38 15 50 15 C62 15 72 25 71 47 L69 58 L31 58 Z" fill="${hair}"/>
        <circle cx="50" cy="14" r="10" fill="${shade}"/>
        <circle cx="50" cy="14" r="8" fill="${hair}"/>
        <path d="M41 16 Q50 8 59 16" stroke="${mix(hair, '#000000', .25)}" stroke-width="2" fill="none"/>`;
    case 'long':
      return `<path d="M28 50 C26 26 37 13 50 13 C63 13 74 26 72 50 L73 86 L63 88 L61 44 L39 44 L37 88 L27 86 Z" fill="${hair}"/>
        <path d="M35 40 C40 34 60 34 65 40 L64 82 L59 82 L58 44 L42 44 L41 82 L36 82 Z" fill="${shade}" opacity=".45"/>`;
    case 'locs':
      return `<path d="M28 47 C27 24 38 14 50 14 C62 14 73 24 72 47 L70 56 L30 56 Z" fill="${hair}"/>
        <g stroke="${shade}" stroke-width="4" stroke-linecap="round">
          <path d="M30 48 C24 56 24 68 27 80"/><path d="M34 50 C28 60 29 72 31 84"/>
          <path d="M70 48 C76 56 76 68 73 80"/><path d="M66 50 C72 60 71 72 69 84"/>
        </g>
        <g stroke="${hair}" stroke-width="2.4" stroke-linecap="round">
          <path d="M30 48 C25 56 25 67 28 79"/><path d="M34 50 C29 60 30 71 32 83"/>
          <path d="M70 48 C75 56 75 67 72 79"/><path d="M66 50 C71 60 70 71 68 83"/>
        </g>`;
    default:
      return `<path d="M29 48 C27 25 38 14 50 14 C62 14 73 25 71 48 L68 55 L32 55 Z" fill="${hair}"/>`;
  }
}

function hairFront(style, hair, shade) {
  switch (style) {
    case 'braid':
      return `<path d="M31 45 C30 26 39 20 46 20 C42 28 40 37 40 45 C36 44 33 44 31 45 Z" fill="${shade}" opacity=".55"/>
        <path d="M31 42 C33 27 40 21 50 21 C61 21 68 28 70 43 C63 33 56 30 50 30 C43 30 36 34 31 42 Z" fill="${hair}"/>
        <path d="M32 42 C37 33 45 29 53 31" stroke="${shade}" stroke-width="1.2" fill="none" opacity=".6"/>`;
    case 'bun':
      return `<path d="M31 44 C31 29 38 22 48 21 C44 28 42 35 42 43 C38 42 34 43 31 44 Z" fill="${shade}" opacity=".5"/>
        <path d="M31 40 C35 26 44 21 55 23 C64 25 69 32 70 41 C62 32 54 29 47 31 C40 33 35 36 31 40 Z" fill="${hair}"/>`;
    case 'long':
      return `<path d="M31 43 C32 27 40 20 50 20 C60 20 68 27 69 43 C61 32 55 30 50 30 C44 30 38 33 31 43 Z" fill="${hair}"/>
        <path d="M33 39 C39 28 50 25 59 30" stroke="${shade}" stroke-width="1.4" fill="none" opacity=".55"/>`;
    case 'locs':
      return `<path d="M31 42 C33 27 40 21 50 21 C61 21 68 28 70 42 C60 32 53 30 46 32 C39 34 34 37 31 42 Z" fill="${hair}"/>
        <g stroke="${shade}" stroke-width="1.2" fill="none" opacity=".55">
          <path d="M35 35 C41 29 48 28 54 30"/><path d="M39 32 C45 27 53 27 60 32"/>
        </g>`;
    default:
      return `<path d="M31 42 C33 27 40 21 50 21 C61 21 68 28 70 42 C62 32 55 29 49 31 C41 33 35 36 31 42 Z" fill="${hair}"/>`;
  }
}

function headwearFor(look, tints) {
  const { headwear, coat, accent, hair } = look;
  const coatShade = mix(coat, '#0b1310', .35);
  switch (headwear) {
    case 'hood':
      return `<path fill="${coatShade}" fill-rule="evenodd" d="M21 98 C21 46 30 9 50 9 C70 9 79 46 79 98 L69 98 C69 47 63 22 50 22 C37 22 31 47 31 98 Z"/>
        <path d="M31 30 C38 16 61 16 69 30 C62 25 56 23 50 23 C43 23 37 25 31 30 Z" fill="${mix(coatShade, '#000000', .22)}"/>
        <path d="M26 64 C22 78 21 90 21 98" stroke="${mix(coat, '#ffffff', .12)}" stroke-width="1.6" fill="none" opacity=".6"/>`;
    case 'cap':
      return `<path d="M29 33 C31 18 41 13 51 14 C62 15 69 22 69 32 L69 36 L29 36 Z" fill="${mix(coatShade, '#3b2d22', .35)}"/>
        <path d="M27 34 C38 29 60 28 73 34 C74 38 72 40 68 40 L31 40 C27 40 26 37 27 34 Z" fill="${mix(coatShade, '#000000', .2)}"/>
        <path d="M31 28 C40 20 59 19 66 27" stroke="${mix(coatShade, '#ffffff', .18)}" stroke-width="1.2" fill="none"/>`;
    case 'bandana':
      return `<path d="M29 38 C31 25 39 20 50 20 C61 20 69 26 71 38 L71 43 L29 43 Z" fill="${accent}"/>
        <path d="M29 38 C38 33 62 33 71 38 L71 43 L29 43 Z" fill="${mix(accent, '#000000', .18)}"/>
        <path d="M70 39 C78 40 81 46 78 53 C76 49 73 47 69 47 Z" fill="${accent}"/>
        <path d="M70 40 C75 42 77 46 76 50" stroke="${mix(accent, '#000000', .28)}" stroke-width=".9" fill="none"/>
        <g fill="${mix(accent, '#ffffff', .35)}" opacity=".7"><circle cx="38" cy="36" r=".8"/><circle cx="48" cy="34" r=".8"/><circle cx="58" cy="35" r=".8"/><circle cx="65" cy="37" r=".8"/></g>`;
    case 'headscarf':
      return `<path d="M27 47 C27 27 37 16 50 16 C63 16 73 27 73 47 L70 59 C67 47 66 33 50 33 C34 33 33 47 30 59 Z" fill="${accent}"/>
        <path d="M30 37 C36 28 64 28 70 37 C61 31 39 31 30 37 Z" fill="${mix(accent, '#ffffff', .22)}"/>
        <path d="M30 55 C24 64 24 74 27 82 M70 55 C76 64 76 74 73 82" stroke="${accent}" stroke-width="4.5" fill="none" stroke-linecap="round"/>
        <g opacity=".45" fill="${mix(accent, '#000000', .38)}"><circle cx="39" cy="25" r="1.1"/><circle cx="53" cy="21" r="1.1"/><circle cx="64" cy="28" r="1.1"/></g>`;
    case 'straw':
      return `<ellipse cx="50" cy="33" rx="35" ry="8" fill="${mix(accent, '#b28b4e', .35)}"/>
        <path d="M29 33 C31 18 40 12 50 12 C60 12 69 18 71 33 Z" fill="${mix(accent, '#e2c185', .45)}"/>
        <path d="M30 30 C40 34 60 34 70 30 L70 34 C60 38 40 38 30 34 Z" fill="${mix(coatShade, '#4a3322', .4)}"/>
        <g stroke="${mix(accent, '#7a6338', .3)}" stroke-width=".7" fill="none" opacity=".6">
          <path d="M35 19 C39 27 40 31 40 33"/><path d="M47 15 C48 24 48 29 48 33"/><path d="M57 15 C56 24 56 29 56 33"/><path d="M64 19 C61 26 60 30 60 33"/>
        </g>`;
    case 'feather':
      return `<path d="M29 32 C32 19 41 14 50 14 C59 14 67 20 69 31 L69 35 L29 35 Z" fill="${mix(coatShade, '#5b3b2c', .3)}"/>
        <path d="M28 34 C39 29 61 29 73 33 C73 37 70 39 66 39 L31 39 C28 38 27 36 28 34 Z" fill="${mix(coatShade, '#22170f', .3)}"/>
        <path d="M63 15 C72 4 79 5 80 12 C76 12 71 17 67 23 Z" fill="${accent}"/>
        <path d="M66 21 C70 15 74 11 79 11" stroke="${mix(accent, '#000000', .3)}" stroke-width=".9" fill="none"/>`;
    case 'goggles':
      return `<path d="M28 35 C33 22 40 17 50 17 C61 17 68 23 71 35 L71 38 L28 38 Z" fill="${hair}"/>
        <path d="M30 34 C40 30 61 30 71 33" stroke="${mix(hair, '#000000', .35)}" stroke-width="2.2" fill="none"/>
        <g fill="none" stroke="${mix(accent, '#8a6a39', .3)}" stroke-width="2.2">
          <circle cx="38" cy="35" r="7"/><circle cx="61" cy="34" r="7"/>
          <path d="M45 35 C48 32 52 32 54 34"/>
        </g>
        <g fill="${mix(accent, '#c9d6cf', .35)}" opacity=".55"><circle cx="38" cy="35" r="4.6"/><circle cx="61" cy="34" r="4.6"/></g>
        <g fill="#ffffff" opacity=".4"><circle cx="36" cy="33" r="1.4"/><circle cx="59" cy="32" r="1.4"/></g>`;
    default:
      return '';
  }
}

function facialFor(facial, hair, skinShade) {
  switch (facial) {
    case 'stubble':
      return `<path d="M36 57 C37 68 44 73 50 73 C56 73 63 68 64 57 C61 66 56 69 50 69 C44 69 39 66 36 57 Z" fill="${mix(hair, skinShade, .45)}" opacity=".34"/>
        <g fill="${mix(hair, skinShade, .3)}" opacity=".35"><circle cx="42" cy="65" r=".55"/><circle cx="46" cy="68" r=".5"/><circle cx="54" cy="68" r=".5"/><circle cx="58" cy="65" r=".55"/><circle cx="50" cy="70" r=".5"/></g>`;
    case 'beard':
      return `<path d="M34 54 C34 68 41 78 50 79 C59 78 66 68 66 54 C63 63 60 66 58 60 C56 66 53 67 50 61 C47 67 44 66 42 60 C40 66 37 63 34 54 Z" fill="${hair}"/>
        <path d="M39 54 C40 62 43 66 47 62 C48 67 52 67 53 62 C57 66 60 62 61 54" fill="${mix(hair, '#000000', .22)}" opacity=".45"/>
        <path d="M45 59 Q50 57 55 59" stroke="${mix(hair, '#000000', .2)}" stroke-width="1.2" fill="none"/>`;
    case 'moustache':
      return `<path d="M42 62 C45 59 48 59 50 61 C52 59 55 59 58 62 C55 63 52 63 50 63 C48 63 45 63 42 62 Z" fill="${hair}"/>
        <path d="M42 62 C45 64 47 64 49 63 M58 62 C55 64 53 64 51 63" stroke="${mix(hair, '#000000', .25)}" stroke-width=".9" fill="none"/>`;
    default:
      return '';
  }
}

export function portraitSVG(look, opts = {}) {
  const seed = opts.seed || [look.skin, look.hair, look.coat, look.hairStyle, look.headwear].join('|');
  const id = 'p' + hash(seed).toString(36);
  const skin = look.skin || '#c99770';
  const skinShade = mix(skin, '#703f2d', .28);
  const skinLight = mix(skin, '#ffe9d2', .26);
  const hair = look.hair || '#4d3524';
  const hairShade = mix(hair, '#160f0b', .35);
  const coat = look.coat || '#4a5a44';
  const coatLight = mix(coat, '#f0e4c8', .16);
  const coatShade = mix(coat, '#08100c', .35);
  const shirt = look.shirt || '#d8c7a5';
  const accent = look.accent || '#c9a25b';
  const tint = opts.tint || accent;
  const bgA = mix(coat, '#0a1510', .74);
  const bgB = '#09120d';
  const bgC = mix(tint, '#0a1510', .82);

  const hairStyle = look.hairStyle || 'short';
  const apron = look.apron
    ? `<path d="M37 92 L63 92 L67 120 L33 120 Z" fill="${mix(shirt, accent, .18)}"/>
       <path d="M41 92 L41 84 M59 92 L59 84" stroke="${mix(shirt, accent, .3)}" stroke-width="2"/>
       <path d="M38 100 L62 100" stroke="${mix(shirt, '#000000', .2)}" stroke-width=".8" opacity=".5"/>`
    : '';

  return `<svg viewBox="0 0 100 120" preserveAspectRatio="xMidYMid slice" xmlns="http://www.w3.org/2000/svg" role="img" aria-label="Portrait">
  <defs>
    <linearGradient id="${id}bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${bgA}"/><stop offset=".55" stop-color="${bgB}"/><stop offset="1" stop-color="${bgC}"/>
    </linearGradient>
    <radialGradient id="${id}halo" cx=".5" cy=".36" r=".55">
      <stop offset="0" stop-color="${mix(tint, '#ffffff', .18)}" stop-opacity=".34"/>
      <stop offset=".6" stop-color="${tint}" stop-opacity=".1"/>
      <stop offset="1" stop-color="${tint}" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="${id}face" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="${skinLight}"/><stop offset=".5" stop-color="${skin}"/><stop offset="1" stop-color="${skinShade}"/>
    </linearGradient>
    <linearGradient id="${id}coat" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${coatLight}"/><stop offset=".45" stop-color="${coat}"/><stop offset="1" stop-color="${coatShade}"/>
    </linearGradient>
    <linearGradient id="${id}hair" x1="0" y1="0" x2="0" y2="1">
      <stop offset="0" stop-color="${mix(hair, '#ffffff', .16)}"/><stop offset=".5" stop-color="${hair}"/><stop offset="1" stop-color="${hairShade}"/>
    </linearGradient>
    <clipPath id="${id}clip"><rect width="100" height="120"/></clipPath>
  </defs>
  <g clip-path="url(#${id}clip)">
    <rect width="100" height="120" fill="url(#${id}bg)"/>
    <rect width="100" height="120" fill="url(#${id}halo)"/>
    <g opacity=".16" stroke="${mix(tint, '#ffffff', .35)}" stroke-width=".6">
      <path d="M18 0 V120"/><path d="M82 0 V120"/><path d="M0 102 Q50 88 100 102"/>
    </g>
    <circle cx="50" cy="48" r="39" fill="none" stroke="${tint}" stroke-width=".7" opacity=".22"/>
    <path d="M0 120 L0 101 C21 93 36 101 50 96 C66 91 81 95 100 104 L100 120 Z" fill="${mix(coat, '#0b1711', .6)}" opacity=".7"/>

    <path d="M10 120 C13 101 24 91 39 87 C44 85 45 90 50 90 C55 90 56 85 61 87 C76 91 87 101 90 120 Z" fill="url(#${id}coat)"/>
    <path d="M39 87 C42 92 46 96 50 98 C54 96 58 92 61 87 C58 91 55 93 50 94 C45 93 42 91 39 87 Z" fill="${coatShade}" opacity=".6"/>
    <path d="M39 87 L50 99 L61 87 C57 92 54 94 50 95 C46 94 43 92 39 87 Z" fill="${shirt}" opacity=".9"/>
    <path d="M34 120 C35 110 38 102 43 97 M66 120 C65 110 62 102 57 97" stroke="${coatShade}" stroke-width="1" opacity=".5" fill="none"/>
    ${apron}
    <rect x="44" y="69" width="12" height="21" rx="5.5" fill="${skinShade}"/>
    <ellipse cx="50" cy="76" rx="7" ry="3" fill="${mix(skin, '#000000', .14)}" opacity=".75"/>

    ${hairBack(hairStyle, hair, hairShade)}
    <ellipse cx="29.5" cy="50" rx="3" ry="5" fill="${skinShade}"/>
    <ellipse cx="70.5" cy="50" rx="3" ry="5" fill="${skinShade}"/>
    <path d="M31 44 C31 28 39 22 50 22 C61 22 69 28 69 44 C69 58 61 69 50 72 C39 69 31 58 31 44 Z" fill="url(#${id}face)"/>
    <path d="M34 42 C34 30 41 25 50 25 C59 25 66 30 66 42 C62 33 56 30 50 30 C44 30 38 33 34 42 Z" fill="${skinLight}" opacity=".2"/>
    <path d="M31 45 C33 59 40 68 50 72 C45 67 40 58 39 47 Z" fill="${skinShade}" opacity=".22"/>
    <ellipse cx="42" cy="55" rx="5" ry="3" fill="${mix(skin, '#c96a5e', .18)}" opacity=".2"/>
    <ellipse cx="58" cy="55" rx="5" ry="3" fill="${mix(skin, '#c96a5e', .18)}" opacity=".2"/>

    <g stroke="${mix(hair, '#1b120d', .3)}" stroke-width="1.7" stroke-linecap="round">
      <path d="M38 44.5 C40 43.1 43 43 44.8 44.2"/>
      <path d="M55.2 44.2 C57 43 60 43.1 62 44.5"/>
    </g>
    <g>
      <ellipse cx="41.5" cy="48.4" rx="2.7" ry="1.8" fill="#231b16"/>
      <ellipse cx="58.5" cy="48.4" rx="2.7" ry="1.8" fill="#231b16"/>
      <circle cx="42.4" cy="47.8" r=".65" fill="#f7f1e4" opacity=".85"/>
      <circle cx="59.4" cy="47.8" r=".65" fill="#f7f1e4" opacity=".85"/>
      <path d="M38.8 48.2 C40.5 47 43 47 44.4 48.4" stroke="${hairShade}" stroke-width=".8" fill="none" opacity=".55"/>
      <path d="M55.6 48.4 C57 47 59.5 47 61.2 48.2" stroke="${hairShade}" stroke-width=".8" fill="none" opacity=".55"/>
    </g>
    <path d="M49.7 49 C50.8 53.4 51.4 56.4 49.2 57.2" stroke="${skinShade}" stroke-width="1" fill="none" stroke-linecap="round"/>
    <path d="M45 62.2 Q50 65.4 55 62.2" stroke="${mix(skin, '#5d2d25', .5)}" stroke-width="1.15" fill="none" stroke-linecap="round"/>
    <path d="M46 63.3 Q50 65 54 63.3" stroke="${mix(skin, '#f4ddc4', .35)}" stroke-width=".7" fill="none" opacity=".6"/>
    ${facialFor(look.facial, hair, skinShade)}
    ${hairFront(hairStyle, hair, hairShade)}
    ${headwearFor(look, { mix, accent, hair })}
    <path d="M30 52 C32 63 38 70 45 73" stroke="${mix(skin, '#fff1dc', .4)}" stroke-width="1" fill="none" opacity=".22"/>
  </g>
  <rect x=".75" y=".75" width="98.5" height="118.5" rx="6" fill="none" stroke="${mix(tint, '#ffffff', .25)}" stroke-opacity=".28" stroke-width="1.2"/>
</svg>`;
}
