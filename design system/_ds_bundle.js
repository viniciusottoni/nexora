/* @ds-bundle: {"format":4,"namespace":"NexoraDesignSystem_aa692a","components":[{"name":"Badge","sourcePath":"components/core/Badge.jsx"},{"name":"NexoraLogo","sourcePath":"components/core/NexoraLogo.jsx"},{"name":"BrandMark","sourcePath":"components/core/BrandMark.jsx"},{"name":"NexoraLoader","sourcePath":"components/core/NexoraLoader.jsx"},{"name":"NexoraSplash","sourcePath":"components/core/NexoraLoader.jsx"},{"name":"Button","sourcePath":"components/core/Button.jsx"},{"name":"Card","sourcePath":"components/core/Card.jsx"},{"name":"Icon","sourcePath":"components/core/Icon.jsx"},{"name":"IconButton","sourcePath":"components/core/IconButton.jsx"},{"name":"DataTable","sourcePath":"components/data/DataTable.jsx"},{"name":"ProgressMeter","sourcePath":"components/data/ProgressMeter.jsx"},{"name":"StatTile","sourcePath":"components/data/StatTile.jsx"},{"name":"AlertBanner","sourcePath":"components/feedback/AlertBanner.jsx"},{"name":"EmptyState","sourcePath":"components/feedback/EmptyState.jsx"},{"name":"OrderTimer","sourcePath":"components/feedback/OrderTimer.jsx"},{"name":"StatusPill","sourcePath":"components/feedback/StatusPill.jsx"},{"name":"SyncStatus","sourcePath":"components/feedback/SyncStatus.jsx"},{"name":"Checkbox","sourcePath":"components/forms/Checkbox.jsx"},{"name":"Field","sourcePath":"components/forms/Field.jsx"},{"name":"Input","sourcePath":"components/forms/Input.jsx"},{"name":"NumericKeypad","sourcePath":"components/forms/NumericKeypad.jsx"},{"name":"QuantityStepper","sourcePath":"components/forms/QuantityStepper.jsx"},{"name":"Select","sourcePath":"components/forms/Select.jsx"},{"name":"Switch","sourcePath":"components/forms/Switch.jsx"},{"name":"SegmentedControl","sourcePath":"components/navigation/SegmentedControl.jsx"},{"name":"SideNav","sourcePath":"components/navigation/SideNav.jsx"},{"name":"TopBar","sourcePath":"components/navigation/TopBar.jsx"},{"name":"MenuItemCard","sourcePath":"components/operacao/MenuItemCard.jsx"},{"name":"OrderLine","sourcePath":"components/operacao/OrderLine.jsx"},{"name":"OrderTicket","sourcePath":"components/operacao/OrderTicket.jsx"},{"name":"TableCard","sourcePath":"components/operacao/TableCard.jsx"}],"sourceHashes":{"components/core/Badge.jsx":"d4a614d0d340","components/core/BrandMark.jsx":"0672c0b19271","components/core/Button.jsx":"8f046e14b9de","components/core/Card.jsx":"3ebf9a379d7c","components/core/Icon.jsx":"f680f8a1b767","components/core/IconButton.jsx":"98495f9f1d64","components/data/DataTable.jsx":"332b397a9de6","components/data/ProgressMeter.jsx":"973fcf51e6da","components/data/StatTile.jsx":"b387e78cccc0","components/feedback/AlertBanner.jsx":"f7153f8371f7","components/feedback/EmptyState.jsx":"2b0fb42f4d6c","components/feedback/OrderTimer.jsx":"1b3ec38eabcd","components/feedback/StatusPill.jsx":"446a780e8780","components/feedback/SyncStatus.jsx":"962fecda4786","components/forms/Checkbox.jsx":"124ef2c59569","components/forms/Field.jsx":"9ca9f144669b","components/forms/Input.jsx":"477828d8220e","components/forms/NumericKeypad.jsx":"1e780e600ad9","components/forms/QuantityStepper.jsx":"2b699d86d95b","components/forms/Select.jsx":"b9e5daa46b3c","components/forms/Switch.jsx":"399127bada2e","components/navigation/SegmentedControl.jsx":"9d84a94d3578","components/navigation/SideNav.jsx":"9958bfc1dc3f","components/navigation/TopBar.jsx":"5a267171a611","components/nx-css.js":"86e455660675","components/operacao/MenuItemCard.jsx":"c0861f1c6975","components/operacao/OrderLine.jsx":"d0b23ec02d1e","components/operacao/OrderTicket.jsx":"8e9e806c27d1","components/operacao/TableCard.jsx":"38be36506835","ui_kits/admin-nexora/AdminApp.jsx":"182f9ea81077","ui_kits/admin-nexora/data.jsx":"bdb86b85834c","ui_kits/caixa/CaixaApp.jsx":"ee1ab5de662c","ui_kits/caixa/data.jsx":"1afc1aa8326f","ui_kits/garcom/GarcomApp.jsx":"5de3ba1c5695","ui_kits/garcom/data.jsx":"d3c6809da509","ui_kits/kds/KdsApp.jsx":"a9c4f6452468","ui_kits/kds/data.jsx":"d3749c92c5e3","ui_kits/mesa/MesaApp.jsx":"1f464f058ff1","ui_kits/mesa/data.jsx":"82551df521d5","ui_kits/painel-dono/PainelApp.jsx":"ac619fa45d99","ui_kits/painel-dono/data.jsx":"d1cfc0f1f230"},"inlinedExternals":[],"unexposedExports":[{"name":"injectCss","sourcePath":"components/nx-css.js"}]} */

(() => {

const __ds_ns = (window.NexoraDesignSystem_aa692a = window.NexoraDesignSystem_aa692a || {});

const __ds_scope = {};

(__ds_ns.__errors = __ds_ns.__errors || []);

// components/core/Icon.jsx
try { (() => {
/* Iconografia Nexora = Material Symbols Rounded (CDN). Ver readme.md › ICONOGRAPHY. */
function Icon({
  name,
  size = 20,
  fill = false,
  weight = 400,
  color,
  style,
  label,
  ...rest
}) {
  return React.createElement('span', {
    className: 'material-symbols-rounded',
    'aria-hidden': label ? undefined : 'true',
    'aria-label': label,
    role: label ? 'img' : undefined,
    style: {
      fontSize: size + 'px',
      lineHeight: 1,
      color: color || 'inherit',
      flex: '0 0 auto',
      fontVariationSettings: `'FILL' ${fill ? 1 : 0},'wght' ${weight},'GRAD' 0,'opsz' ${size}`,
      userSelect: 'none',
      ...style
    },
    ...rest
  }, name);
}
Object.assign(__ds_scope, { Icon });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Icon.jsx", error: String((e && e.message) || e) }); }

// components/nx-css.js
try { (() => {
const done = new Set();
function injectCss(id, css) {
  if (typeof document === 'undefined' || done.has(id)) return;
  done.add(id);
  const s = document.createElement('style');
  s.setAttribute('data-nx', id);
  s.textContent = css;
  document.head.appendChild(s);
}
Object.assign(__ds_scope, { injectCss });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/nx-css.js", error: String((e && e.message) || e) }); }

// components/core/Badge.jsx
try { (() => {
__ds_scope.injectCss('badge', `
.nxBadge{display:inline-flex;align-items:center;gap:var(--sp-2);border-radius:var(--radius-pill);font-family:var(--font-sans);font-weight:var(--fw-semibold);white-space:nowrap;border:var(--border-1) solid transparent}
.nxBadge--sm{height:20px;padding:0 var(--sp-3);font-size:var(--fs-11)}
.nxBadge--md{height:26px;padding:0 var(--sp-4);font-size:var(--fs-12)}
.nxBadge--lg{height:32px;padding:0 var(--sp-5);font-size:var(--fs-14)}
.nxBadge--neutral{background:var(--surface-sunken);color:var(--text-secondary);border-color:var(--border-subtle)}
.nxBadge--brand{background:var(--surface-brand-subtle);color:var(--nx-navy-700)}
.nxBadge--info{background:var(--nx-blue-100);color:var(--nx-blue-600)}
.nxBadge--success{background:var(--nx-success-100);color:var(--nx-success-600)}
.nxBadge--warning{background:var(--nx-warning-100);color:var(--nx-warning-600)}
.nxBadge--danger{background:var(--nx-danger-100);color:var(--nx-danger-600)}
.nxBadge--accent{background:var(--nx-teal-100);color:var(--nx-teal-600)}
.nxBadge--solid{background:var(--nx-navy-800);color:#fff}
.nxBadge--square{border-radius:var(--radius-sm)}
`);
function Badge({
  children,
  tone = 'neutral',
  size = 'md',
  icon,
  square = false,
  ...rest
}) {
  return React.createElement('span', {
    className: ['nxBadge', 'nxBadge--' + tone, 'nxBadge--' + size, square ? 'nxBadge--square' : ''].filter(Boolean).join(' '),
    ...rest
  }, icon ? React.createElement(__ds_scope.Icon, {
    name: icon,
    size: size === 'sm' ? 12 : 14
  }) : null, children);
}
Object.assign(__ds_scope, { Badge });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Badge.jsx", error: String((e && e.message) || e) }); }

// components/core/NexoraLogo.jsx
try { (() => {
__ds_scope.injectCss('nxLogo', `
.nxLogo{display:block;max-width:100%}
/* branco literal de proposito: e a marca da plataforma, nao do tenant */
.nxLogo--white{fill:#fff}
.nxLogo__maskShape{fill:#fff}
.nxLogo__shineStop{stop-color:#fff}
.nxLogo__shine{transform:translateX(calc(var(--nxLogoShineW) * -1));pointer-events:none}
`);
/* Marca Nexora vetorizada de Assets/logo.jpeg (a unica arte fornecida). Os mesmos
   caminhos estao em assets/logo-nexora-*.svg — aqui vao inline, e nao via <img src>,
   para o componente funcionar em qualquer profundidade de pasta e em HTML copiado
   para fora do kit. tone 'color' sobre fundo claro, tone 'white' sobre navy/azul. */
const NX_GRADIENTS=[
{ id: 'nxg1', x1: 43.54, y1: 217.63, x2: 270.82, y2: 8.76, stops: [[7.1,'#032559'],[21.4,'#0E3984'],[35.7,'#175FC9'],[50.0,'#1297C5'],[64.3,'#28BDA0'],[78.6,'#6CD05B'],[92.9,'#8BD73E']] },
{ id: 'nxg2', x1: 614.87, y1: 90.28, x2: 658.14, y2: 45.01, stops: [[7.1,'#0CA7B6'],[21.4,'#10B3B4'],[35.7,'#21BDA5'],[50.0,'#3DC48E'],[64.3,'#5FCD6F'],[78.6,'#73D45A'],[92.9,'#83DB4F']] },
{ id: 'nxg3', x1: 539.54, y1: 143.24, x2: 605.69, y2: 91.74, stops: [[7.1,'#022151'],[21.4,'#022357'],[35.7,'#072F6F'],[50.0,'#0E489C'],[64.3,'#135DB9'],[78.6,'#1481CA'],[92.9,'#1494CB']] },
{ id: 'nxg4', x1: 614.34, y1: 143.12, x2: 650.13, y2: 136.33, stops: [[7.1,'#021F52'],[21.4,'#012359'],[35.7,'#012359'],[50.0,'#012358'],[64.3,'#012358'],[78.6,'#012255'],[92.9,'#021B3F']] },
];
const NX_SYMBOL=[
['M255.97,0.0 262.55,0.0 272.84,2.28 281.72,8.0 285.44,11.73 290.3,19.16 293.16,30.61 292.87,39.22 290.02,48.94 285.16,56.37 275.54,63.84 274.65,65.62 274.07,172.81 268.36,190.53 259.23,203.94 245.8,214.8 229.25,221.08 218.96,222.22 210.95,221.65 196.36,217.65 190.07,214.5 179.82,206.53 91.29,125.07 79.77,179.04 87.15,186.94 91.73,196.39 92.88,207.57 90.58,217.03 87.15,223.32 82.58,228.46 70.56,235.04 64.55,236.18 53.94,235.32 42.49,229.32 38.2,225.03 34.2,218.74 31.91,211.87 31.34,206.43 31.91,198.95 33.63,192.94 38.77,184.66 43.87,179.65 41.01,137.67 39.44,136.75 32.95,139.03 24.06,139.32 13.74,135.88 5.15,128.16 2.29,123.01 0.0,115.58 0.29,105.51 4.57,95.22 10.3,88.92 16.88,84.91 25.45,82.63 31.21,82.62 36.4,83.77 44.64,87.74 81.26,62.69 87.87,60.68 97.63,61.26 109.93,67.26 153.34,106.82 154.89,107.12 163.34,103.71 169.96,103.42 174.01,104.29 181.44,108.29 186.01,113.15 189.74,120.61 190.6,129.22 189.17,135.77 190.06,137.56 214.86,160.94 219.72,164.37 226.59,166.66 231.5,166.67 237.82,165.23 244.12,161.8 250.12,155.8 252.98,151.22 256.13,141.19 256.41,72.2 255.2,68.67 250.44,68.16 234.17,130.7 231.86,133.88 228.71,135.6 222.89,135.89 215.71,132.17 115.34,37.06 107.36,31.08 102.78,29.07 97.31,27.64 87.3,27.35 80.69,28.79 73.83,32.22 68.4,36.8 63.83,43.66 61.54,50.52 60.37,61.21 43.35,72.34 43.3,54.54 45.02,43.97 49.59,32.55 56.44,23.41 61.58,18.55 70.44,13.12 77.87,10.26 87.84,8.27 103.33,9.41 116.17,14.54 127.88,23.39 216.57,108.52 219.72,111.1 221.24,111.11 234.47,59.99 233.6,56.48 229.07,50.08 226.22,41.5 225.93,28.9 227.64,21.75 232.79,12.87 238.79,6.86 248.21,1.72ZM88.76,79.49 80.67,83.22 54.71,101.8 61.01,174.05 63.47,174.06 64.38,172.56 76.94,107.47 80.69,102.58 85.28,100.86 89.33,100.85 94.83,103.45 190.07,192.0 199.21,198.85 205.51,201.7 213.51,203.42 227.54,202.56 235.26,199.42 240.98,195.7 246.41,190.55 250.69,184.57 253.15,177.78 244.95,182.32 236.66,184.9 225.77,185.47 216.59,183.18 204.89,175.76 177.43,149.59 166.51,151.28 160.49,150.42 155.32,147.84 149.6,142.69 145.88,136.97 144.16,129.5 143.85,121.41 99.96,82.08 95.35,79.78ZM257.14,18.52 251.63,20.24 246.47,24.55 243.59,31.15 243.59,36.94 245.88,43.24 249.9,47.55 257.36,50.42 263.15,50.14 266.91,48.69 271.77,44.4 274.92,37.23 274.92,31.43 273.19,26.54 268.62,21.39 262.3,18.52ZM60.84,191.17 55.62,192.61 50.17,197.48 48.44,202.09 48.43,207.85 50.74,213.35 53.61,216.21 57.64,218.51 63.41,219.09 69.16,217.08 72.34,214.49 75.21,209.59 75.78,202.97 74.06,198.07 70.61,194.04 65.15,191.46ZM25.52,99.72 21.41,101.45 17.41,106.03 16.24,109.81 16.53,114.44 18.83,118.76 21.72,121.35 27.48,123.08 31.82,122.5 35.85,120.2 38.73,116.17 39.6,112.41 39.03,107.22 37.01,103.46 33.84,100.87 30.93,99.72ZM165.12,118.52 160.14,121.41 158.42,124.28 157.84,127.83 159.57,131.86 162.72,135.3 168.85,136.46 171.75,135.3 174.62,132.43 176.35,128.11 176.06,124.88 174.9,122.27 171.16,119.1Z', 'url(#nxg1)'],
];
const NX_WORD=[
['M656.82,41.03 658.66,41.07 659.25,41.97 659.26,44.32 658.69,45.96 658.4,49.19 656.98,53.65 656.98,55.75 656.41,56.5 656.41,57.74 655.84,59.35 655.84,61.16 654.71,65.62 653.53,74.6 652.92,75.49 651.92,75.49 646.66,70.95 644.57,70.66 643.92,70.97 641.85,74.03 639.34,76.68 638.15,78.87 637.63,78.96 636.73,80.58 633.36,84.38 632.17,86.57 630.8,87.79 630.72,88.29 630.23,88.36 630.15,88.86 629.66,88.93 628.46,91.12 627.96,91.19 627.89,91.69 627.39,91.76 627.32,92.26 626.24,93.21 623.9,96.82 622.18,98.55 621.41,98.55 619.4,96.82 617.07,93.21 612.85,88.56 612.26,87.37 612.25,85.59 612.56,84.66 613.93,83.43 613.99,82.95 615.93,80.87 616.84,79.25 617.35,79.16 617.41,78.68 618.21,78.02 618.83,76.68 620.77,74.6 621.68,72.98 623.05,71.75 625.1,68.71 625.61,68.62 628.23,64.72 629.89,63.2 629.96,62.7 630.46,62.63 632.75,58.91 632.74,58.16 629.02,54.44 627.38,53.52 626.52,51.9 629.13,50.44 630.96,50.14 631.69,49.59 632.67,49.57 633.4,49.02 634.38,49.0 636.85,47.87 637.82,47.85 639.95,46.74 640.93,46.72 643.94,45.31 645.8,45.0 652.49,42.18Z', 'url(#nxg2)'],
['M712.66,49.29 724.09,49.57 734.38,51.86 743.81,55.57 752.38,61.28 760.09,68.99 765.51,76.99 768.36,83.26 769.8,87.84 772.08,99.84 772.08,113.27 769.8,125.26 767.79,131.27 763.52,139.53 762.7,140.22 762.37,141.27 760.66,143.55 757.29,147.05 757.24,147.53 752.66,151.54 744.09,157.25 743.37,157.28 742.95,157.82 740.24,158.7 738.39,159.82 732.67,161.82 723.52,163.53 717.5,163.53 717.25,163.82 708.1,163.53 699.52,162.1 695.92,160.69 694.94,160.67 686.39,156.68 678.11,150.97 672.11,144.97 666.4,136.69 661.54,125.26 659.54,114.12 659.54,110.38 659.26,110.13 659.54,100.12 661.54,88.7 662.98,84.4 666.11,77.27 671.25,69.56 679.53,61.28 686.65,56.44 691.23,54.14 702.94,50.43 707.82,49.57 712.41,49.57ZM710.99,69.8 705.79,70.66 701.21,72.38 700.79,72.92 698.64,73.8 695.49,75.81 692.26,78.89 691.78,78.94 688.06,83.24 684.06,90.67 682.06,97.24 681.48,101.26 681.48,112.41 682.63,118.99 684.91,125.0 688.06,130.44 692.35,135.3 698.64,139.87 704.08,142.16 710.38,143.59 718.96,143.87 727.54,142.16 733.55,139.59 737.56,136.73 742.14,132.15 743.85,129.87 747.28,123.58 749.0,118.14 750.14,112.13 750.14,107.82 750.43,107.57 750.14,100.69 748.71,93.83 747.56,90.38 745.29,85.82 742.42,81.53 736.71,76.1 730.99,72.66 724.69,70.37 720.96,69.8Z', '#012456'],
['M942.29,50.14 946.35,50.43 949.79,51.3 953.52,53.3 956.1,55.88 957.25,57.62 962.38,69.3 965.26,77.14 968.08,82.97 969.81,87.97 971.21,90.38 973.23,95.94 976.62,102.92 984.06,121.59 985.74,124.57 988.62,132.13 990.87,136.53 990.9,137.26 991.44,137.67 994.89,146.66 999.13,155.91 1000.0,158.21 1000.0,160.27 999.08,161.24 997.88,161.54 980.18,161.54 978.96,161.23 977.52,159.78 976.08,156.91 975.49,154.77 972.66,149.22 968.08,137.39 966.83,135.33 917.47,135.33 916.54,136.25 914.8,141.53 910.55,150.78 908.82,156.06 906.81,159.78 905.37,161.23 904.15,161.54 886.73,161.54 884.91,160.62 884.9,158.5 885.77,156.19 888.31,151.22 891.18,143.66 892.86,140.67 900.3,122.57 900.84,122.16 900.87,121.43 902.84,117.6 904.86,111.75 906.54,108.77 909.42,101.49 911.67,97.08 913.69,91.52 916.8,85.12 918.53,80.12 921.07,75.15 923.38,68.73 928.79,56.76 931.95,53.3 935.11,51.58 937.99,50.72ZM942.04,74.93 941.04,76.42 938.16,85.12 935.63,90.38 933.89,95.66 931.35,100.92 928.76,108.2 925.94,114.03 925.37,116.46 958.28,116.52 959.19,116.8 958.96,114.6 957.56,111.9 956.11,107.47 953.86,103.07 952.12,97.79 948.16,88.82 945.29,80.69 944.74,80.28 942.72,75.0Z', '#012456'],
['M559.67,51.0 572.24,51.0 574.29,51.58 575.45,52.45 579.46,56.45 580.65,58.64 582.93,61.49 583.45,61.58 585.78,65.48 586.87,66.14 588.06,68.33 589.72,70.13 589.77,70.9 592.0,72.98 593.19,75.17 594.85,76.68 594.9,77.45 595.4,77.52 595.47,78.02 596.84,79.25 597.46,80.87 598.83,82.1 598.9,82.59 599.4,82.67 600.88,85.14 601.97,86.08 602.31,87.13 608.52,94.92 608.57,95.68 609.09,95.77 613.7,101.95 614.79,102.61 615.95,104.94 615.95,106.43 612.85,110.02 612.51,111.07 612.01,111.14 611.94,111.64 610.28,113.15 610.23,113.92 609.71,114.0 605.67,119.61 604.3,120.84 603.96,121.89 603.45,121.98 599.97,127.02 598.6,128.25 596.55,131.58 596.04,131.67 594.28,134.43 593.76,134.52 592.0,137.28 590.34,138.79 583.45,148.1 579.79,152.2 579.21,153.37 579.23,154.66 583.43,157.86 586.01,160.44 586.01,161.21 578.82,163.53 576.73,163.82 576.0,164.37 568.0,166.95 566.76,166.96 554.61,170.94 553.4,170.94 553.85,166.19 556.98,152.61 558.98,140.55 559.86,137.94 560.92,138.2 566.14,142.99 567.48,142.71 570.11,138.79 573.19,135.28 573.82,133.95 576.33,131.29 579.23,127.11 581.45,124.74 581.79,123.69 585.16,119.9 589.2,114.0 590.86,112.49 594.02,107.85 594.0,106.62 592.0,103.46 588.35,99.39 588.01,98.34 586.92,97.39 584.3,93.49 582.93,92.26 577.18,84.09 575.53,82.58 573.19,78.96 571.54,77.45 569.2,73.83 566.98,71.47 564.36,67.57 562.13,65.2 558.95,60.73 557.29,59.21 557.24,58.45 555.58,56.65 554.42,54.01 554.42,52.52 555.01,51.61Z', 'url(#nxg3)'],
['M345.99,51.28 356.0,51.28 357.74,51.57 362.61,53.57 365.79,56.17 365.84,56.65 367.49,58.16 368.97,60.64 369.77,61.3 372.68,66.05 373.48,66.71 380.37,77.16 384.02,81.81 386.35,85.71 387.15,86.37 391.48,93.12 394.28,96.63 397.18,101.38 397.98,102.04 400.6,106.22 402.82,108.88 403.16,109.93 404.53,111.44 406.86,115.34 411.65,121.41 412.85,123.6 413.66,124.28 413.99,125.31 416.08,127.64 416.81,127.51 416.81,54.54 417.1,53.08 418.56,51.59 419.5,51.28 435.49,51.28 437.3,52.47 438.18,55.11 438.18,151.16 437.02,155.77 435.59,158.08 434.14,159.52 430.96,161.25 426.34,161.54 426.09,161.82 421.21,161.82 415.45,160.67 413.15,159.52 412.21,158.43 410.59,157.52 405.15,150.67 401.68,145.06 400.88,144.4 400.54,143.35 399.46,142.41 397.69,139.36 396.04,137.56 389.72,127.96 388.92,127.31 388.58,126.26 385.5,122.46 384.3,120.27 382.08,117.62 381.74,116.57 379.23,113.63 377.75,110.87 376.1,109.07 372.05,102.61 370.4,100.81 363.22,90.07 361.28,87.99 360.37,86.08 359.76,85.48 359.1,85.47 358.97,158.88 357.51,160.94 355.72,161.54 340.58,161.54 338.81,160.96 337.92,160.07 337.32,158.85 337.32,59.38 337.91,57.05 340.22,53.59 343.37,51.87Z', '#012456'],
['M469.36,51.28 538.91,51.28 540.71,52.75 541.31,54.54 541.31,67.68 540.71,69.47 538.96,70.93 534.6,70.94 534.35,71.23 529.75,71.23 529.5,70.94 523.49,70.94 523.24,71.23 480.47,70.94 479.23,71.84 478.63,73.91 478.63,85.92 478.92,86.16 478.92,95.89 479.27,96.28 533.21,96.3 535.03,97.79 535.61,100.69 535.33,113.27 533.52,115.38 529.47,115.38 529.22,115.67 526.91,115.67 526.66,115.38 479.25,115.41 478.63,116.08 478.63,118.68 478.92,118.93 478.94,140.7 479.82,141.57 480.75,141.88 538.65,141.88 540.43,143.06 541.31,145.14 541.31,158.56 540.71,160.07 539.84,160.94 538.05,161.54 469.36,161.54 466.45,160.96 462.72,158.95 459.86,156.08 458.72,154.37 457.56,151.79 456.98,149.16 456.98,65.65 457.26,65.4 457.26,62.52 458.7,58.47 461.58,54.73 465.88,52.15Z', '#012456'],
['M798.99,51.28 841.76,51.28 841.97,51.56 844.89,51.57 851.47,52.71 858.05,54.71 863.77,57.86 866.91,60.14 872.34,66.14 873.25,68.05 874.35,69.3 875.77,72.43 877.49,78.44 877.78,81.96 878.06,82.18 878.06,93.32 876.35,101.33 873.78,107.06 870.06,112.21 864.34,117.35 858.62,120.5 856.76,121.1 856.41,122.1 859.29,127.31 860.39,128.56 863.55,134.69 865.51,137.39 868.67,143.52 872.92,150.21 873.52,151.79 876.34,155.91 877.49,158.53 877.49,160.02 876.59,161.23 874.8,161.54 857.96,161.54 855.6,160.66 852.73,156.94 849.56,150.78 845.03,143.52 844.15,141.38 843.62,140.98 839.3,133.12 838.76,132.7 837.88,130.55 836.48,128.71 835.6,126.56 834.06,124.79 809.16,124.81 808.83,126.62 808.83,128.65 809.12,128.9 809.12,156.57 808.53,160.05 807.34,161.24 805.86,161.54 790.72,161.54 789.24,161.24 788.06,160.07 787.46,158.28 787.46,62.23 788.04,59.64 789.19,57.05 792.07,53.87 794.37,52.44ZM809.56,70.94 809.12,71.35 809.12,102.15 808.83,102.4 808.84,105.03 809.47,105.97 836.63,105.98 836.88,105.7 842.64,105.41 844.95,104.83 848.96,103.11 852.09,100.83 854.69,97.65 855.84,95.35 856.98,90.47 856.98,85.31 856.41,82.43 854.1,77.54 851.52,74.96 849.79,73.8 844.92,71.8 842.04,71.23Z', '#012456'],
['M620.92,111.97 621.32,111.99 623.05,113.72 623.39,114.77 626.18,117.99 626.81,119.33 629.6,122.55 630.23,123.89 632.45,126.26 632.51,126.74 633.31,127.39 635.92,131.29 637.01,132.24 639.63,136.14 640.71,137.08 643.9,141.84 648.41,147.05 649.03,148.39 650.12,149.33 650.74,150.67 651.83,151.61 654.16,155.23 655.53,156.45 656.98,159.1 656.68,160.33 655.8,161.23 654.51,161.26 654.29,161.54 637.16,161.54 634.52,160.66 631.94,158.08 630.46,155.6 628.52,153.52 628.46,153.04 627.66,152.38 627.04,151.04 626.24,150.38 623.62,146.48 622.53,145.54 619.92,141.64 617.69,139.27 617.64,138.79 610.0,129.02 609.67,127.99 609.12,127.54 609.14,126.26 613.08,121.61 613.13,121.13 614.22,120.18 614.27,119.7 616.78,117.05 616.84,116.57 617.92,115.62 619.97,112.58Z', 'url(#nxg4)'],
];
const NX_VIEWBOX={lockup:'0.0 0.0 1000.0 236.18',symbol:'0.0 0.0 293.16 236.18'};
const NX_RATIO={lockup:4.2341,symbol:1.2413};

let nxLogoSeq = 0;

function NexoraLogo({variant='lockup',tone='color',height=28,shine=false,className='',...rest}){
  const scope=React.useMemo(()=>'nxl'+(++nxLogoSeq),[]);
  const paths=variant==='symbol'?NX_SYMBOL:NX_SYMBOL.concat(NX_WORD);
  const white=tone==='white';
  const used=new Set(paths.map(p=>p[1]).filter(f=>f.indexOf('url(')===0));
  const [vbX,vbY,vbW,vbH]=NX_VIEWBOX[variant].split(' ').map(Number);
  const maskId=scope+'-shine-mask';
  const shineGradId=scope+'-shine-grad';
  return React.createElement('svg',{
    xmlns:'http://www.w3.org/2000/svg',viewBox:NX_VIEWBOX[variant],height:height,
    width:Math.round(height*NX_RATIO[variant]*100)/100,role:'img','aria-label':'Nexora',
    className:('nxLogo'+(white?' nxLogo--white':'')+' '+className).trim(),...rest},
    (white&&!shine)?null:React.createElement('defs',null,
      white?null:NX_GRADIENTS.filter(g=>used.has('url(#'+g.id+')')).map(g=>
        React.createElement('linearGradient',{key:g.id,id:scope+'-'+g.id,gradientUnits:'userSpaceOnUse',
          x1:g.x1,y1:g.y1,x2:g.x2,y2:g.y2},
          g.stops.map(s=>React.createElement('stop',{key:s[0],offset:s[0]+'%',stopColor:s[1]})))),
      shine?[
        React.createElement('mask',{key:'m',id:maskId,maskUnits:'userSpaceOnUse'},
          paths.map(p=>React.createElement('path',{key:p[0].slice(0,24),d:p[0],className:'nxLogo__maskShape'}))),
        React.createElement('linearGradient',{key:'g',id:shineGradId,x1:'0',y1:'0',x2:'1',y2:'0'},
          React.createElement('stop',{key:'0',offset:'0%',className:'nxLogo__shineStop',stopOpacity:0}),
          React.createElement('stop',{key:'42',offset:'42%',className:'nxLogo__shineStop',stopOpacity:0}),
          React.createElement('stop',{key:'50',offset:'50%',className:'nxLogo__shineStop',stopOpacity:.9}),
          React.createElement('stop',{key:'58',offset:'58%',className:'nxLogo__shineStop',stopOpacity:0}),
          React.createElement('stop',{key:'100',offset:'100%',className:'nxLogo__shineStop',stopOpacity:0})),
      ]:null),
    paths.map(p=>React.createElement('path',{key:p[0].slice(0,24),d:p[0],
      ...(white?{}:{fill:p[1].indexOf('url(')===0?'url(#'+scope+'-'+p[1].slice(5,-1)+')':p[1]})})),
    shine?React.createElement('rect',{className:'nxLogo__shine',x:vbX,y:vbY,width:vbW,height:vbH,
      fill:'url(#'+shineGradId+')',mask:'url(#'+maskId+')',
      style:{'--nxLogoShineW':vbW+'px'}}):null);
}
Object.assign(__ds_scope, { NexoraLogo });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/NexoraLogo.jsx", error: String((e && e.message) || e) }); }

// components/core/BrandMark.jsx
try { (() => {
__ds_scope.injectCss('brand', `
.nxBrand{display:inline-flex;align-items:center;gap:var(--sp-4);min-width:0}
.nxBrand__img{display:block;height:100%;width:auto}
.nxBrand__word{font-family:var(--font-display);font-weight:var(--fw-black);letter-spacing:var(--ls-tight);color:var(--text-brand);line-height:1}
.nxBrand__sub{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:var(--text-muted);margin-top:3px}
.nxBrand--inverse .nxBrand__word{color:#fff}
.nxBrand--inverse .nxBrand__sub{color:rgba(255,255,255,.7)}
.nxBrand__tenant{width:var(--nxBrandSize);height:var(--nxBrandSize);border-radius:var(--radius-md);background:var(--brand-primary);color:var(--brand-on-primary);display:flex;align-items:center;justify-content:center;font-family:var(--font-display);font-weight:var(--fw-black);flex:0 0 auto}
.nxBrand--center{flex-direction:column;justify-content:center;text-align:center;gap:var(--sp-3)}
`);
/* Assinatura de marca. Sem logoSrc nem tenantName desenha a marca Nexora
   (NexoraLogo): colorida sobre fundo claro, branca com inverse. center empilha e
   centraliza — e o arranjo de cartao de login e de primeiro acesso. */

function BrandMark({logoSrc,tenantName,subtitle,size=28,inverse=false,center=false,...rest}){
  const inner=logoSrc
    ?React.createElement('img',{src:logoSrc,alt:tenantName||'Nexora',className:'nxBrand__img',style:{height:size+'px'}})
    :tenantName
      ?[React.createElement('span',{key:'i',className:'nxBrand__tenant',style:{'--nxBrandSize':size+'px',fontSize:size*.46+'px'}},tenantName.trim().charAt(0).toUpperCase()),
        React.createElement('span',{key:'w'},
          React.createElement('span',{className:'nxBrand__word',style:{fontSize:size*.62+'px'}},tenantName),
          subtitle?React.createElement('span',{className:'nxBrand__sub',style:{display:'block'}},subtitle):null)]
      :React.createElement('span',null,
          React.createElement(__ds_scope.NexoraLogo,{variant:'lockup',tone:inverse?'white':'color',height:size}),
          subtitle?React.createElement('span',{className:'nxBrand__sub',style:{display:'block'}},subtitle):null);
  return React.createElement('span',{className:'nxBrand'+(inverse?' nxBrand--inverse':'')+(center?' nxBrand--center':''),...rest},inner);
}
Object.assign(__ds_scope, { BrandMark });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/BrandMark.jsx", error: String((e && e.message) || e) }); }

// components/core/NexoraLoader.jsx
try { (() => {
__ds_scope.injectCss('nxLoader', `
.nxLoader{--nxLoaderSize:88px;--nxLoaderBounces:infinite;--nxLoaderCycle:calc(var(--dur-slower) + var(--dur-slow));display:grid;justify-items:center;gap:var(--sp-5)}
.nxLoader__stage{position:relative;display:grid;place-items:end center;width:var(--nxLoaderSize);height:calc(var(--nxLoaderSize) * 1.4)}
.nxLoader__shadow{position:absolute;bottom:0;left:14%;width:72%;height:6px;border-radius:50%;background:color-mix(in srgb, var(--nx-navy-900) 24%, transparent);filter:blur(2px);animation:nx-brand-bounce-shadow var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__coin{position:relative;display:grid;place-items:center;width:var(--nxLoaderSize);height:var(--nxLoaderSize);margin-bottom:9px;border-radius:50%;background:var(--surface-card);box-shadow:0 0 0 1px color-mix(in srgb, var(--brand-primary) 18%, transparent),0 12px 26px -14px color-mix(in srgb, var(--nx-navy-900) 65%, transparent);transform-origin:50% 100%;perspective:32rem;animation:nx-brand-bounce var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__flip{display:grid;place-items:center;transform-style:preserve-3d;animation:nx-brand-flip var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__label{margin:0;font:var(--type-caption);color:var(--text-muted);text-align:center}
.nxLoader--inverse .nxLoader__label{color:rgba(255,255,255,.72)}
.nxSplash{display:grid;width:100%;justify-items:center}
.nxSplash>*{grid-area:1 / 1}
.nxSplash__intro{display:grid;place-items:center;align-self:center}
.nxSplash__intro.is-leaving{pointer-events:none;animation:nx-brand-exit var(--dur-slow) var(--ease-in-out) both}
.nxSplash__content{align-self:center;width:100%;display:grid;justify-items:center}
.nxSplash__content.is-waiting{visibility:hidden}
.nx-anim-open-x{transform-origin:center center;animation:nx-open-x var(--dur-slower) var(--ease-out) both}
.nxSplash__content.is-open .nxLogo__shine{animation:nx-logo-shine calc(var(--dur-slower) + var(--dur-base)) var(--ease-in-out) 1 both}
`);
/* Carregamento padrao da plataforma: o simbolo da Nexora numa moeda que quica e gira
   360 graus no eixo Y, vista de frente, com o rotulo do que esta acontecendo embaixo.
   NexoraSplash e o uso padrao antes de cartao de login e de primeiro acesso: quica duas
   vezes e some enquanto o cartao abre do centro para os lados (esquerda/direita). Com
   prefers-reduced-motion os --dur-* zeram, o animationend dispara na hora e a tela cai
   direto no conteudo. Depois que o cartao termina de abrir, um NexoraLogo com shine
   dentro dele brilha uma vez da esquerda para a direita (gatilho: classe .is-open). */

const NX_SPLASH_FALLBACK_MS = 4000;

function NexoraLoader({label='Carregando',size=88,bounces,inverse=false,onSettled,className='',style,...rest}){
  return React.createElement('div',{
    className:('nxLoader'+(inverse?' nxLoader--inverse':'')+' '+className).trim(),
    style:{'--nxLoaderSize':size+'px',...(bounces===undefined?{}:{'--nxLoaderBounces':String(bounces)}),...style},
    ...rest},
    React.createElement('div',{className:'nxLoader__stage'},
      React.createElement('span',{className:'nxLoader__shadow','aria-hidden':'true'}),
      React.createElement('span',{className:'nxLoader__coin',
        onAnimationEnd:e=>{if(e.target===e.currentTarget&&onSettled)onSettled();}},
        React.createElement('span',{className:'nxLoader__flip'},
          React.createElement(__ds_scope.NexoraLogo,{variant:'symbol',height:Math.round(size*.5)})))),
    React.createElement('p',{className:'nxLoader__label',role:'status'},label));
}

function NexoraSplash({label='Carregando',bounces=2,onOpened,children}){
  const [phase,setPhase]=React.useState('bouncing');
  const [opened,setOpened]=React.useState(false);
  React.useEffect(()=>{
    if(phase==='done')return undefined;
    const t=setTimeout(()=>setPhase('done'),NX_SPLASH_FALLBACK_MS);
    return ()=>clearTimeout(t);
  },[phase]);
  const contentClassName=['nxSplash__content',phase==='bouncing'?'is-waiting':'nx-anim-open-x',
    opened?'is-open':''].filter(Boolean).join(' ');
  return React.createElement('div',{className:'nxSplash'},
    phase==='done'?null:React.createElement('div',{
      className:'nxSplash__intro'+(phase==='leaving'?' is-leaving':''),
      onAnimationEnd:e=>{if(e.target===e.currentTarget)setPhase('done');}},
      React.createElement(NexoraLoader,{label:label,bounces:bounces,onSettled:()=>setPhase('leaving')})),
    React.createElement('div',{className:contentClassName,
      onAnimationEnd:e=>{if(e.target===e.currentTarget&&phase!=='bouncing'){setOpened(true);if(onOpened)onOpened();}}},children));
}
Object.assign(__ds_scope, { NexoraLoader, NexoraSplash });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/NexoraLoader.jsx", error: String((e && e.message) || e) }); }

// components/core/Button.jsx
try { (() => {
__ds_scope.injectCss('btn', `
.nxBtn{display:inline-flex;align-items:center;justify-content:center;gap:var(--sp-3);font-family:var(--font-sans);font-weight:var(--fw-semibold);border:var(--border-1) solid transparent;border-radius:var(--brand-radius);cursor:pointer;transition:var(--transition-control);text-decoration:none;white-space:nowrap}
.nxBtn:focus-visible{outline:none;box-shadow:var(--focus-ring)}
.nxBtn[disabled]{cursor:not-allowed;opacity:.45}
.nxBtn--sm{height:var(--density-desk-control);padding:0 var(--sp-5);font-size:var(--fs-13)}
.nxBtn--md{height:40px;padding:0 var(--sp-6);font-size:var(--fs-14)}
.nxBtn--lg{height:var(--density-touch-min);padding:0 var(--sp-7);font-size:var(--fs-16)}
.nxBtn--touch{height:var(--density-touch-lg);padding:0 var(--sp-8);font-size:var(--fs-18)}
.nxBtn--block{width:100%}
.nxBtn--primary{background:var(--brand-primary);color:var(--brand-on-primary)}
.nxBtn--primary:hover:not([disabled]){background:var(--brand-primary-hover,var(--action-primary-hover))}
.nxBtn--primary:active:not([disabled]){background:var(--brand-primary-active,var(--action-primary-active))}
.nxBtn--accent{background:var(--action-accent);color:var(--action-accent-text)}
.nxBtn--accent:hover:not([disabled]){background:var(--action-accent-hover)}
.nxBtn--secondary{background:var(--surface-card);color:var(--text-primary);border-color:var(--border-default);box-shadow:var(--shadow-subtle)}
.nxBtn--secondary:hover:not([disabled]){background:var(--surface-sunken);border-color:var(--border-strong)}
.nxBtn--ghost{background:transparent;color:var(--text-secondary)}
.nxBtn--ghost:hover:not([disabled]){background:var(--surface-sunken);color:var(--text-primary)}
.nxBtn--danger{background:var(--nx-danger-600);color:#fff}
.nxBtn--danger:hover:not([disabled]){background:var(--nx-danger-700)}
.nxBtn--danger:focus-visible{box-shadow:var(--focus-ring-danger)}
.nxBtn:active:not([disabled]){transform:translateY(1px)}
`);
function Button({
  children,
  variant = 'primary',
  size = 'md',
  iconLeft,
  iconRight,
  block = false,
  as = 'button',
  ...rest
}) {
  const cls = ['nxBtn', 'nxBtn--' + variant, 'nxBtn--' + size, block ? 'nxBtn--block' : ''].filter(Boolean).join(' ');
  const g = size === 'touch' ? 24 : size === 'lg' ? 22 : 18;
  return React.createElement(as, {
    className: cls,
    ...rest
  }, iconLeft ? React.createElement(__ds_scope.Icon, {
    name: iconLeft,
    size: g
  }) : null, children, iconRight ? React.createElement(__ds_scope.Icon, {
    name: iconRight,
    size: g
  }) : null);
}
Object.assign(__ds_scope, { Button });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Button.jsx", error: String((e && e.message) || e) }); }

// components/core/Card.jsx
try { (() => {
__ds_scope.injectCss('card', `
.nxCard{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);box-shadow:var(--shadow-card);display:flex;flex-direction:column;min-width:0}
.nxCard--flat{box-shadow:none}
.nxCard--raised{box-shadow:var(--shadow-raised)}
.nxCard--interactive{cursor:pointer;transition:box-shadow var(--dur-fast) var(--ease-standard),border-color var(--dur-fast) var(--ease-standard)}
.nxCard--interactive:hover{box-shadow:var(--shadow-raised);border-color:var(--border-default)}
.nxCard__head{display:flex;align-items:center;justify-content:space-between;gap:var(--sp-5);padding:var(--sp-6) var(--sp-6) 0}
.nxCard__t{font:var(--type-h3);color:var(--text-primary)}
.nxCard__s{font:var(--type-caption);color:var(--text-muted);margin-top:var(--sp-1)}
.nxCard__body{padding:var(--sp-6);min-width:0}
.nxCard__body--tight{padding:var(--sp-5)}
.nxCard__body--none{padding:0}
.nxCard__foot{padding:var(--sp-5) var(--sp-6);border-top:var(--border-1) solid var(--border-subtle);display:flex;align-items:center;justify-content:flex-end;gap:var(--sp-4)}
`);
function Card({
  title,
  subtitle,
  actions,
  footer,
  children,
  elevation = 'card',
  interactive = false,
  padding = 'default',
  ...rest
}) {
  return React.createElement('section', {
    className: ['nxCard', elevation === 'flat' ? 'nxCard--flat' : '', elevation === 'raised' ? 'nxCard--raised' : '', interactive ? 'nxCard--interactive' : ''].filter(Boolean).join(' '),
    ...rest
  }, title || actions ? React.createElement('header', {
    className: 'nxCard__head'
  }, React.createElement('div', null, React.createElement('div', {
    className: 'nxCard__t'
  }, title), subtitle ? React.createElement('div', {
    className: 'nxCard__s'
  }, subtitle) : null), actions ? React.createElement('div', {
    style: {
      display: 'flex',
      gap: 'var(--sp-3)',
      alignItems: 'center'
    }
  }, actions) : null) : null, React.createElement('div', {
    className: 'nxCard__body' + (padding === 'tight' ? ' nxCard__body--tight' : padding === 'none' ? ' nxCard__body--none' : '')
  }, children), footer ? React.createElement('footer', {
    className: 'nxCard__foot'
  }, footer) : null);
}
Object.assign(__ds_scope, { Card });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/Card.jsx", error: String((e && e.message) || e) }); }

// components/core/IconButton.jsx
try { (() => {
__ds_scope.injectCss('iconbtn', `
.nxIB{display:inline-flex;align-items:center;justify-content:center;border:var(--border-1) solid transparent;border-radius:var(--radius-md);background:transparent;color:var(--text-secondary);cursor:pointer;transition:var(--transition-control);position:relative}
.nxIB:hover{background:var(--surface-sunken);color:var(--text-primary)}
.nxIB:active{transform:translateY(1px)}
.nxIB[disabled]{opacity:.4;cursor:not-allowed}
.nxIB--sm{width:32px;height:32px}.nxIB--md{width:40px;height:40px}.nxIB--lg{width:48px;height:48px}
.nxIB--solid{background:var(--brand-primary);color:var(--brand-on-primary)}
.nxIB--solid:hover{background:var(--brand-primary-hover,var(--action-primary-hover));color:var(--brand-on-primary)}
.nxIB--outline{border-color:var(--border-default);background:var(--surface-card)}
.nxIB__dot{position:absolute;top:5px;right:5px;min-width:16px;height:16px;padding:0 4px;border-radius:var(--radius-pill);background:var(--nx-danger-500);color:#fff;font:var(--type-overline);display:flex;align-items:center;justify-content:center}
`);
function IconButton({
  icon,
  size = 'md',
  variant = 'ghost',
  badge,
  label,
  ...rest
}) {
  const g = size === 'lg' ? 24 : size === 'sm' ? 18 : 20;
  return React.createElement('button', {
    type: 'button',
    'aria-label': label,
    title: label,
    className: ['nxIB', 'nxIB--' + size, 'nxIB--' + variant].join(' '),
    ...rest
  }, React.createElement(__ds_scope.Icon, {
    name: icon,
    size: g
  }), badge ? React.createElement('span', {
    className: 'nxIB__dot'
  }, badge) : null);
}
Object.assign(__ds_scope, { IconButton });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/core/IconButton.jsx", error: String((e && e.message) || e) }); }

// components/data/DataTable.jsx
try { (() => {
__ds_scope.injectCss('table', `
.nxTbWrap{width:100%;overflow-x:auto}
.nxTb{width:100%;border-collapse:collapse;font:var(--type-body)}
.nxTb th{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:var(--text-muted);text-align:left;padding:var(--sp-4) var(--sp-6);border-bottom:var(--border-1) solid var(--border-subtle);white-space:nowrap;background:var(--surface-card);position:sticky;top:0}
.nxTb td{padding:0 var(--sp-6);height:var(--density-desk-row);border-bottom:var(--border-1) solid var(--border-subtle);color:var(--text-primary);vertical-align:middle}
.nxTb tbody tr:last-child td{border-bottom:0}
.nxTb tbody tr:hover td{background:var(--surface-page)}
.nxTb--clickable tbody tr{cursor:pointer}
.nxTb__num{text-align:right;font-family:var(--font-mono);font-variant-numeric:tabular-nums}
.nxTb--compact td{height:32px;font-size:var(--fs-13)}
.nxTb tfoot td{height:var(--density-desk-row);font-weight:var(--fw-semibold);background:var(--surface-sunken);border-top:var(--border-2) solid var(--border-default)}
`);
function DataTable({
  columns = [],
  rows = [],
  footer,
  compact = false,
  onRowClick,
  rowKey,
  ...rest
}) {
  return React.createElement('div', {
    className: 'nxTbWrap',
    ...rest
  }, React.createElement('table', {
    className: ['nxTb', compact ? 'nxTb--compact' : '', onRowClick ? 'nxTb--clickable' : ''].filter(Boolean).join(' ')
  }, React.createElement('thead', null, React.createElement('tr', null, columns.map(c => React.createElement('th', {
    key: c.key,
    style: {
      textAlign: c.align === 'right' ? 'right' : c.align === 'center' ? 'center' : 'left',
      width: c.width
    }
  }, c.header)))), React.createElement('tbody', null, rows.map((r, i) => React.createElement('tr', {
    key: rowKey ? r[rowKey] : i,
    onClick: onRowClick ? () => onRowClick(r) : undefined
  }, columns.map(c => React.createElement('td', {
    key: c.key,
    className: c.numeric ? 'nxTb__num' : undefined,
    style: {
      textAlign: c.align === 'center' ? 'center' : undefined
    }
  }, c.render ? c.render(r) : r[c.key]))))), footer ? React.createElement('tfoot', null, footer) : null));
}
Object.assign(__ds_scope, { DataTable });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/DataTable.jsx", error: String((e && e.message) || e) }); }

// components/data/ProgressMeter.jsx
try { (() => {
__ds_scope.injectCss('meter', `
.nxMt{display:flex;flex-direction:column;gap:var(--sp-3);min-width:0}
.nxMt__top{display:flex;justify-content:space-between;align-items:baseline;gap:var(--sp-4)}
.nxMt__lab{font:var(--type-label);color:var(--text-secondary)}
.nxMt__val{font:var(--fw-semibold) var(--fs-14)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxMt__track{height:8px;border-radius:var(--radius-pill);background:var(--surface-sunken);overflow:hidden;position:relative}
.nxMt--lg .nxMt__track{height:14px}
.nxMt__fill{height:100%;border-radius:var(--radius-pill);transition:width var(--dur-slow) var(--ease-standard)}
.nxMt__fill--brand{background:var(--brand-primary)}
.nxMt__fill--success{background:var(--nx-success-500)}
.nxMt__fill--warning{background:var(--nx-warning-500)}
.nxMt__fill--danger{background:var(--nx-danger-500)}
.nxMt__fill--accent{background:var(--nx-teal-500)}
.nxMt__mark{position:absolute;top:-3px;bottom:-3px;width:2px;background:var(--text-primary);opacity:.55}
.nxMt__cap{font:var(--type-caption);color:var(--text-muted)}
`);
function ProgressMeter({
  label,
  value = 0,
  max = 100,
  display,
  tone = 'brand',
  target,
  caption,
  size = 'md',
  ...rest
}) {
  const pct = Math.max(0, Math.min(100, value / max * 100));
  return React.createElement('div', {
    className: 'nxMt' + (size === 'lg' ? ' nxMt--lg' : ''),
    ...rest
  }, label || display ? React.createElement('div', {
    className: 'nxMt__top'
  }, React.createElement('span', {
    className: 'nxMt__lab'
  }, label), display ? React.createElement('span', {
    className: 'nxMt__val'
  }, display) : null) : null, React.createElement('div', {
    className: 'nxMt__track',
    role: 'meter',
    'aria-valuenow': value,
    'aria-valuemax': max
  }, React.createElement('div', {
    className: 'nxMt__fill nxMt__fill--' + tone,
    style: {
      width: pct + '%'
    }
  }), target != null ? React.createElement('span', {
    className: 'nxMt__mark',
    style: {
      left: Math.max(0, Math.min(100, target / max * 100)) + '%'
    }
  }) : null), caption ? React.createElement('span', {
    className: 'nxMt__cap'
  }, caption) : null);
}
Object.assign(__ds_scope, { ProgressMeter });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/ProgressMeter.jsx", error: String((e && e.message) || e) }); }

// components/data/StatTile.jsx
try { (() => {
__ds_scope.injectCss('stat', `
.nxStat{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);padding:var(--sp-6);display:flex;flex-direction:column;gap:var(--sp-3);min-width:0;box-shadow:var(--shadow-card)}
.nxStat--flat{box-shadow:none}
.nxStat--pulse{background:var(--surface-inverse);border-color:transparent}
.nxStat__lab{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:var(--text-muted);display:flex;align-items:center;gap:var(--sp-3)}
.nxStat--pulse .nxStat__lab{color:rgba(255,255,255,.62)}
.nxStat__v{font:var(--type-metric);color:var(--text-primary);font-variant-numeric:tabular-nums;display:flex;align-items:baseline;gap:var(--sp-3)}
.nxStat--pulse .nxStat__v{color:#fff}
.nxStat--lg .nxStat__v{font:var(--type-metric-lg)}
.nxStat__u{font:var(--fw-medium) var(--fs-16)/1 var(--font-sans);color:var(--text-muted)}
.nxStat--pulse .nxStat__u{color:rgba(255,255,255,.6)}
.nxStat__foot{display:flex;align-items:center;gap:var(--sp-4);flex-wrap:wrap}
.nxStat__d{display:inline-flex;align-items:center;gap:2px;font:var(--fw-semibold) var(--fs-13)/1 var(--font-sans);font-variant-numeric:tabular-nums;padding:3px var(--sp-3) 3px var(--sp-2);border-radius:var(--radius-pill)}
.nxStat__d--up{color:var(--nx-success-600);background:var(--nx-success-100)}
.nxStat__d--down{color:var(--nx-danger-600);background:var(--nx-danger-100)}
.nxStat__d--flat{color:var(--text-muted);background:var(--surface-sunken)}
.nxStat__cmp{font:var(--type-caption);color:var(--text-muted)}
.nxStat--pulse .nxStat__cmp{color:rgba(255,255,255,.55)}
.nxStat__tgt{font:var(--type-caption);color:var(--text-muted)}
`);
function StatTile({
  label,
  value,
  unit,
  delta,
  deltaDirection,
  comparison,
  target,
  icon,
  size = 'md',
  variant = 'card',
  ...rest
}) {
  const dir = deltaDirection || (delta == null ? 'flat' : String(delta).trim().startsWith('-') ? 'down' : 'up');
  return React.createElement('div', {
    className: ['nxStat', 'nxStat--' + size, variant === 'pulse' ? 'nxStat--pulse' : variant === 'flat' ? 'nxStat--flat' : ''].filter(Boolean).join(' '),
    ...rest
  }, React.createElement('div', {
    className: 'nxStat__lab'
  }, icon ? React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 14
  }) : null, label), React.createElement('div', {
    className: 'nxStat__v'
  }, value, unit ? React.createElement('span', {
    className: 'nxStat__u'
  }, unit) : null), delta != null || comparison || target ? React.createElement('div', {
    className: 'nxStat__foot'
  }, delta != null ? React.createElement('span', {
    className: 'nxStat__d nxStat__d--' + dir
  }, React.createElement(__ds_scope.Icon, {
    name: dir === 'up' ? 'arrow_upward' : dir === 'down' ? 'arrow_downward' : 'remove',
    size: 14
  }), delta) : null, comparison ? React.createElement('span', {
    className: 'nxStat__cmp'
  }, comparison) : null, target ? React.createElement('span', {
    className: 'nxStat__tgt'
  }, 'meta ', target) : null) : null);
}
Object.assign(__ds_scope, { StatTile });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/data/StatTile.jsx", error: String((e && e.message) || e) }); }

// components/feedback/AlertBanner.jsx
try { (() => {
__ds_scope.injectCss('alert', `
.nxAl{display:flex;gap:var(--sp-5);align-items:flex-start;padding:var(--sp-5) var(--sp-6);border-radius:var(--brand-radius);border:var(--border-1) solid transparent;font:var(--type-body)}
.nxAl__t{font:var(--type-h3);margin-bottom:2px}
.nxAl__b{color:inherit;opacity:.82}
.nxAl__a{margin-left:auto;display:flex;gap:var(--sp-4);align-items:center;flex:0 0 auto}
.nxAl--info{background:var(--nx-blue-50);border-color:var(--nx-blue-100);color:var(--nx-navy-800)}
.nxAl--success{background:var(--nx-success-100);border-color:#BFE6CE;color:var(--nx-success-600)}
.nxAl--warning{background:var(--nx-warning-100);border-color:#F3DFA6;color:var(--nx-warning-600)}
.nxAl--danger{background:var(--nx-danger-100);border-color:#F3C4C6;color:var(--nx-danger-600)}
.nxAl--neutral{background:var(--surface-sunken);border-color:var(--border-subtle);color:var(--text-primary)}
`);
const IC = {
  info: 'info',
  success: 'check_circle',
  warning: 'warning',
  danger: 'error',
  neutral: 'notifications'
};
function AlertBanner({
  tone = 'info',
  title,
  children,
  actions,
  icon,
  ...rest
}) {
  return React.createElement('div', {
    role: 'status',
    className: 'nxAl nxAl--' + tone,
    ...rest
  }, React.createElement(__ds_scope.Icon, {
    name: icon || IC[tone],
    size: 22,
    fill: true
  }), React.createElement('div', {
    style: {
      minWidth: 0
    }
  }, title ? React.createElement('div', {
    className: 'nxAl__t'
  }, title) : null, children ? React.createElement('div', {
    className: 'nxAl__b'
  }, children) : null), actions ? React.createElement('div', {
    className: 'nxAl__a'
  }, actions) : null);
}
Object.assign(__ds_scope, { AlertBanner });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/AlertBanner.jsx", error: String((e && e.message) || e) }); }

// components/feedback/EmptyState.jsx
try { (() => {
__ds_scope.injectCss('empty', `
.nxEm{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:var(--sp-4);padding:var(--sp-11) var(--sp-8);text-align:center;color:var(--text-muted)}
.nxEm__ic{width:56px;height:56px;border-radius:var(--radius-pill);background:var(--surface-sunken);display:flex;align-items:center;justify-content:center;color:var(--text-muted)}
.nxEm__t{font:var(--type-h3);color:var(--text-primary)}
.nxEm__b{font:var(--type-body);color:var(--text-muted);max-width:46ch;text-wrap:pretty}
`);
function EmptyState({
  icon = 'inbox',
  title,
  children,
  action,
  ...rest
}) {
  return React.createElement('div', {
    className: 'nxEm',
    ...rest
  }, React.createElement('span', {
    className: 'nxEm__ic'
  }, React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 28
  })), title ? React.createElement('div', {
    className: 'nxEm__t'
  }, title) : null, children ? React.createElement('div', {
    className: 'nxEm__b'
  }, children) : null, action ? React.createElement('div', {
    style: {
      marginTop: 'var(--sp-3)'
    }
  }, action) : null);
}
Object.assign(__ds_scope, { EmptyState });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/EmptyState.jsx", error: String((e && e.message) || e) }); }

// components/feedback/OrderTimer.jsx
try { (() => {
__ds_scope.injectCss('timer', `
.nxTm{display:inline-flex;align-items:center;gap:var(--sp-3);font-family:var(--font-mono);font-variant-numeric:tabular-nums;font-weight:var(--fw-bold);border-radius:var(--radius-md);line-height:1}
.nxTm--sm{font-size:var(--fs-14);padding:var(--sp-2) var(--sp-3)}
.nxTm--md{font-size:var(--fs-20);padding:var(--sp-3) var(--sp-4)}
.nxTm--lg{font-size:var(--fs-42);padding:var(--sp-3) var(--sp-5)}
.nxTm--late{animation:nx-pulse-alert 1.2s var(--ease-in-out) infinite}
`);
function fmt(s) {
  const m = Math.floor(Math.abs(s) / 60),
    r = Math.abs(s) % 60;
  return (s < 0 ? '-' : '') + m + ':' + String(r).padStart(2, '0');
}
function OrderTimer({
  seconds = 0,
  warnAt = 300,
  lateAt = 600,
  size = 'md',
  showIcon = false,
  onDark = false,
  ...rest
}) {
  const state = seconds >= lateAt ? 'late' : seconds >= warnAt ? 'warn' : 'ok';
  const fg = {
    ok: 'var(--nx-time-ok)',
    warn: 'var(--nx-time-warn)',
    late: 'var(--nx-time-late)'
  }[state];
  const bg = onDark ? {
    ok: 'var(--nx-time-ok-bg)',
    warn: 'var(--nx-time-warn-bg)',
    late: 'var(--nx-time-late-bg)'
  }[state] : {
    ok: 'var(--nx-success-100)',
    warn: 'var(--nx-warning-100)',
    late: 'var(--nx-danger-100)'
  }[state];
  return React.createElement('span', {
    className: ['nxTm', 'nxTm--' + size, state === 'late' ? 'nxTm--late' : ''].filter(Boolean).join(' '),
    style: {
      color: fg,
      background: bg
    },
    ...rest
  }, showIcon ? React.createElement(__ds_scope.Icon, {
    name: 'timer',
    size: size === 'lg' ? 32 : size === 'sm' ? 14 : 18
  }) : null, fmt(seconds));
}
Object.assign(__ds_scope, { OrderTimer });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/OrderTimer.jsx", error: String((e && e.message) || e) }); }

// components/feedback/StatusPill.jsx
try { (() => {
__ds_scope.injectCss('status', `
.nxSt{display:inline-flex;align-items:center;gap:var(--sp-3);height:26px;padding:0 var(--sp-4) 0 var(--sp-3);border-radius:var(--radius-pill);font:var(--fw-semibold) var(--fs-12)/1 var(--font-sans);white-space:nowrap}
.nxSt--lg{height:34px;font-size:var(--fs-14);padding:0 var(--sp-5) 0 var(--sp-4)}
.nxSt__d{width:8px;height:8px;border-radius:50%;background:currentColor;flex:0 0 auto}
.nxSt--live .nxSt__d{animation:nx-pulse-alert 1.6s var(--ease-in-out) infinite}
`);
const MAP = {
  FREE: ['Livre', 'var(--text-secondary)', 'var(--surface-sunken)'],
  OPEN: ['Ocupada', 'var(--nx-blue-600)', 'var(--nx-blue-100)'],
  QUEUED: ['Na fila', 'var(--text-secondary)', 'var(--surface-sunken)'],
  FIRED: ['Em produção', 'var(--nx-warning-600)', 'var(--nx-warning-100)'],
  IN_OVEN: ['No forno', 'var(--nx-warning-600)', 'var(--nx-warning-100)'],
  OUT_OF_OVEN: ['Fora do forno', 'var(--nx-cyan-600)', 'var(--nx-cyan-100)'],
  READY: ['Pronto', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  SERVED: ['Entregue', 'var(--nx-teal-600)', 'var(--nx-teal-100)'],
  BILL_REQUESTED: ['Conta pedida', 'var(--nx-navy-700)', 'var(--surface-brand-subtle)'],
  PAID: ['Pago', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  CLOSED: ['Fechada', 'var(--text-secondary)', 'var(--surface-sunken)'],
  DISPATCHED: ['Em rota', 'var(--nx-cyan-600)', 'var(--nx-cyan-100)'],
  DELIVERED: ['Entregue', 'var(--nx-success-600)', 'var(--nx-success-100)'],
  CANCELLED: ['Cancelado', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
  LATE: ['Atrasado', 'var(--nx-danger-600)', 'var(--nx-danger-100)'],
  UNAVAILABLE: ['Em falta', 'var(--nx-danger-600)', 'var(--nx-danger-100)']
};
function StatusPill({
  status,
  label,
  size = 'md',
  live = false,
  ...rest
}) {
  const m = MAP[status] || ['—', 'var(--text-secondary)', 'var(--surface-sunken)'];
  return React.createElement('span', {
    className: ['nxSt', size === 'lg' ? 'nxSt--lg' : '', live ? 'nxSt--live' : ''].filter(Boolean).join(' '),
    style: {
      color: m[1],
      background: m[2]
    },
    ...rest
  }, React.createElement('span', {
    className: 'nxSt__d'
  }), label || m[0]);
}
Object.assign(__ds_scope, { StatusPill });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/StatusPill.jsx", error: String((e && e.message) || e) }); }

// components/feedback/SyncStatus.jsx
try { (() => {
__ds_scope.injectCss('sync', `
.nxSy{display:inline-flex;align-items:center;gap:var(--sp-3);height:28px;padding:0 var(--sp-4);border-radius:var(--radius-pill);font:var(--fw-medium) var(--fs-12)/1 var(--font-sans);border:var(--border-1) solid transparent;white-space:nowrap}
.nxSy--online{background:var(--nx-success-100);color:var(--nx-success-600)}
.nxSy--local{background:var(--nx-warning-100);color:var(--nx-warning-600)}
.nxSy--delayed{background:var(--nx-danger-100);color:var(--nx-danger-600)}
.nxSy__q{font-family:var(--font-mono);opacity:.85}
`);
const TXT = {
  online: ['cloud_done', 'Sincronizado'],
  local: ['wifi_off', 'Modo local'],
  delayed: ['sync_problem', 'Sync atrasada']
};
function SyncStatus({
  state = 'online',
  lastSync,
  queued,
  ...rest
}) {
  const t = TXT[state] || TXT.online;
  return React.createElement('span', {
    className: 'nxSy nxSy--' + state,
    title: lastSync ? 'Última sincronização ' + lastSync : undefined,
    ...rest
  }, React.createElement(__ds_scope.Icon, {
    name: t[0],
    size: 16
  }), t[1], lastSync ? React.createElement('span', {
    className: 'nxSy__q'
  }, '· ' + lastSync) : null, queued ? React.createElement('span', {
    className: 'nxSy__q'
  }, '· ' + queued + ' na fila') : null);
}
Object.assign(__ds_scope, { SyncStatus });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/feedback/SyncStatus.jsx", error: String((e && e.message) || e) }); }

// components/forms/Checkbox.jsx
try { (() => {
__ds_scope.injectCss('check', `
.nxCk{display:inline-flex;align-items:center;gap:var(--sp-4);cursor:pointer;font:var(--type-body);color:var(--text-primary);min-height:var(--density-touch-min);user-select:none}
.nxCk--compact{min-height:auto}
.nxCk input{appearance:none;margin:0;width:22px;height:22px;flex:0 0 auto;border:var(--border-2) solid var(--border-strong);border-radius:var(--radius-sm);background:var(--surface-card);cursor:pointer;transition:var(--transition-control);position:relative}
.nxCk input:checked{background:var(--brand-primary);border-color:var(--brand-primary)}
.nxCk input:checked::after{content:"";position:absolute;left:6px;top:2px;width:6px;height:11px;border:solid var(--brand-on-primary);border-width:0 2px 2px 0;transform:rotate(45deg)}
.nxCk input:focus-visible{box-shadow:var(--focus-ring)}
.nxCk--radio input{border-radius:var(--radius-pill)}
.nxCk--radio input:checked::after{left:5px;top:5px;width:8px;height:8px;border:0;border-radius:50%;background:var(--brand-on-primary);transform:none}
.nxCk__price{margin-left:auto;font:var(--type-numeric);color:var(--text-secondary)}
`);
function Checkbox({
  label,
  type = 'checkbox',
  price,
  compact = false,
  ...rest
}) {
  return React.createElement('label', {
    className: ['nxCk', type === 'radio' ? 'nxCk--radio' : '', compact ? 'nxCk--compact' : ''].filter(Boolean).join(' ')
  }, React.createElement('input', {
    type,
    ...rest
  }), React.createElement('span', null, label), price ? React.createElement('span', {
    className: 'nxCk__price'
  }, price) : null);
}
Object.assign(__ds_scope, { Checkbox });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Checkbox.jsx", error: String((e && e.message) || e) }); }

// components/forms/Field.jsx
try { (() => {
__ds_scope.injectCss('field', `
.nxField{display:flex;flex-direction:column;gap:var(--sp-3);min-width:0}
.nxField__lab{font:var(--type-label);color:var(--text-secondary);display:flex;gap:var(--sp-2);align-items:baseline}
.nxField__req{color:var(--nx-danger-500)}
.nxField__hint{font:var(--type-caption);color:var(--text-muted)}
.nxField__err{font:var(--type-caption);color:var(--text-danger);display:flex;gap:var(--sp-2);align-items:center}
`);
function Field({
  label,
  hint,
  error,
  required,
  htmlFor,
  children,
  ...rest
}) {
  return React.createElement('div', {
    className: 'nxField',
    ...rest
  }, label ? React.createElement('label', {
    className: 'nxField__lab',
    htmlFor
  }, label, required ? React.createElement('span', {
    className: 'nxField__req'
  }, '*') : null) : null, children, error ? React.createElement('span', {
    className: 'nxField__err'
  }, error) : hint ? React.createElement('span', {
    className: 'nxField__hint'
  }, hint) : null);
}
Object.assign(__ds_scope, { Field });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Field.jsx", error: String((e && e.message) || e) }); }

// components/forms/Input.jsx
try { (() => {
__ds_scope.injectCss('input', `
.nxIn{display:flex;align-items:center;gap:var(--sp-4);background:var(--surface-card);border:var(--border-1) solid var(--border-default);border-radius:var(--radius-md);padding:0 var(--sp-5);transition:var(--transition-control);min-width:0}
.nxIn:focus-within{border-color:var(--border-brand);box-shadow:var(--focus-ring)}
.nxIn--md{height:var(--density-desk-control)}
.nxIn--lg{height:var(--density-touch-min)}
.nxIn--invalid{border-color:var(--border-danger)}
.nxIn--invalid:focus-within{box-shadow:var(--focus-ring-danger)}
.nxIn--disabled{background:var(--surface-sunken);color:var(--text-disabled)}
.nxIn__el{flex:1 1 auto;min-width:0;border:0;background:transparent;outline:none;font:var(--type-body);color:var(--text-primary)}
.nxIn--lg .nxIn__el{font:var(--type-body-lg)}
.nxIn__el::placeholder{color:var(--text-disabled)}
.nxIn__af{font:var(--type-caption);color:var(--text-muted);flex:0 0 auto}
.nxIn--numeric .nxIn__el{font-family:var(--font-mono);font-variant-numeric:tabular-nums;text-align:right}
`);
function Input({
  size = 'md',
  icon,
  suffix,
  prefix,
  invalid = false,
  numeric = false,
  disabled,
  ...rest
}) {
  return React.createElement('div', {
    className: ['nxIn', 'nxIn--' + size, invalid ? 'nxIn--invalid' : '', disabled ? 'nxIn--disabled' : '', numeric ? 'nxIn--numeric' : ''].filter(Boolean).join(' ')
  }, icon ? React.createElement(__ds_scope.Icon, {
    name: icon,
    size: 18,
    color: 'var(--text-muted)'
  }) : null, prefix ? React.createElement('span', {
    className: 'nxIn__af'
  }, prefix) : null, React.createElement('input', {
    className: 'nxIn__el',
    disabled,
    ...rest
  }), suffix ? React.createElement('span', {
    className: 'nxIn__af'
  }, suffix) : null);
}
Object.assign(__ds_scope, { Input });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Input.jsx", error: String((e && e.message) || e) }); }

// components/forms/NumericKeypad.jsx
try { (() => {
__ds_scope.injectCss('keypad', `
.nxKp{display:grid;grid-template-columns:repeat(3,1fr);gap:var(--sp-4);width:100%}
.nxKp button{height:var(--density-touch-lg);border:var(--border-1) solid var(--border-default);border-radius:var(--brand-radius);background:var(--surface-card);color:var(--text-primary);font:var(--fw-semibold) var(--fs-24)/1 var(--font-mono);cursor:pointer;transition:var(--transition-control);display:flex;align-items:center;justify-content:center}
.nxKp button:hover{background:var(--surface-sunken)}
.nxKp button:active{transform:translateY(1px);background:var(--surface-brand-subtle)}
.nxKp button.nxKp--ok{background:var(--nx-success-500);border-color:var(--nx-success-500);color:#fff}
.nxKp button.nxKp--ok:hover{background:var(--nx-success-600)}
.nxKp--dark button{background:var(--surface-raised);border-color:var(--border-default);color:var(--text-primary)}
.nxKp__dots{display:flex;gap:var(--sp-4);justify-content:center;margin-bottom:var(--sp-7)}
.nxKp__dot{width:14px;height:14px;border-radius:50%;background:var(--nx-gray-300);transition:background var(--dur-fast) var(--ease-standard)}
.nxKp__dot--on{background:var(--brand-primary)}
.nxKp__dots--dark .nxKp__dot{background:rgba(255,255,255,.24)}
.nxKp__dots--dark .nxKp__dot--on{background:var(--nx-green-400)}
`);
function NumericKeypad({
  value = '',
  onChange,
  onSubmit,
  length,
  showDots = false,
  dark = false,
  ...rest
}) {
  const push = k => {
    if (length && value.length >= length) return;
    onChange && onChange(value + k);
  };
  const keys = ['1', '2', '3', '4', '5', '6', '7', '8', '9'];
  return React.createElement('div', {
    ...rest
  }, showDots ? React.createElement('div', {
    className: 'nxKp__dots' + (dark ? ' nxKp__dots--dark' : '')
  }, Array.from({
    length: length || 4
  }).map((_, i) => React.createElement('span', {
    key: i,
    className: 'nxKp__dot' + (i < value.length ? ' nxKp__dot--on' : '')
  }))) : null, React.createElement('div', {
    className: 'nxKp' + (dark ? ' nxKp--dark' : '')
  }, keys.map(k => React.createElement('button', {
    key: k,
    type: 'button',
    onClick: () => push(k)
  }, k)), React.createElement('button', {
    type: 'button',
    'aria-label': 'Apagar',
    onClick: () => onChange && onChange(value.slice(0, -1))
  }, React.createElement(__ds_scope.Icon, {
    name: 'backspace',
    size: 24
  })), React.createElement('button', {
    type: 'button',
    onClick: () => push('0')
  }, '0'), React.createElement('button', {
    type: 'button',
    className: 'nxKp--ok',
    'aria-label': 'Confirmar',
    onClick: () => onSubmit && onSubmit(value)
  }, React.createElement(__ds_scope.Icon, {
    name: 'check',
    size: 28
  }))));
}
Object.assign(__ds_scope, { NumericKeypad });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/NumericKeypad.jsx", error: String((e && e.message) || e) }); }

// components/forms/QuantityStepper.jsx
try { (() => {
__ds_scope.injectCss('qty', `
.nxQty{display:inline-flex;align-items:center;border:var(--border-1) solid var(--border-default);border-radius:var(--radius-pill);background:var(--surface-card);overflow:hidden}
.nxQty button{width:44px;height:44px;border:0;background:transparent;color:var(--brand-primary);display:flex;align-items:center;justify-content:center;cursor:pointer;transition:var(--transition-control)}
.nxQty button:hover{background:var(--surface-sunken)}
.nxQty button[disabled]{color:var(--text-disabled);cursor:not-allowed}
.nxQty__v{min-width:36px;text-align:center;font:var(--fw-semibold) var(--fs-16)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxQty--sm button{width:32px;height:32px}.nxQty--sm .nxQty__v{min-width:26px;font-size:var(--fs-14)}
`);
function QuantityStepper({
  value = 1,
  min = 0,
  max = 99,
  onChange,
  size = 'md',
  ...rest
}) {
  const set = v => onChange && onChange(Math.min(max, Math.max(min, v)));
  return React.createElement('div', {
    className: 'nxQty' + (size === 'sm' ? ' nxQty--sm' : ''),
    ...rest
  }, React.createElement('button', {
    type: 'button',
    'aria-label': 'Diminuir',
    disabled: value <= min,
    onClick: () => set(value - 1)
  }, React.createElement(__ds_scope.Icon, {
    name: 'remove',
    size: size === 'sm' ? 16 : 20
  })), React.createElement('span', {
    className: 'nxQty__v'
  }, value), React.createElement('button', {
    type: 'button',
    'aria-label': 'Aumentar',
    disabled: value >= max,
    onClick: () => set(value + 1)
  }, React.createElement(__ds_scope.Icon, {
    name: 'add',
    size: size === 'sm' ? 16 : 20
  })));
}
Object.assign(__ds_scope, { QuantityStepper });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/QuantityStepper.jsx", error: String((e && e.message) || e) }); }

// components/forms/Select.jsx
try { (() => {
__ds_scope.injectCss('select', `
.nxSel{position:relative;display:flex;align-items:center;background:var(--surface-card);border:var(--border-1) solid var(--border-default);border-radius:var(--radius-md);transition:var(--transition-control)}
.nxSel:focus-within{border-color:var(--border-brand);box-shadow:var(--focus-ring)}
.nxSel--md{height:var(--density-desk-control)}.nxSel--lg{height:var(--density-touch-min)}
.nxSel__el{appearance:none;border:0;background:transparent;outline:none;font:var(--type-body);color:var(--text-primary);padding:0 var(--sp-9) 0 var(--sp-5);width:100%;height:100%;cursor:pointer}
.nxSel__ch{position:absolute;right:var(--sp-4);pointer-events:none;color:var(--text-muted)}
`);
function Select({
  size = 'md',
  options = [],
  children,
  ...rest
}) {
  return React.createElement('div', {
    className: 'nxSel nxSel--' + size
  }, React.createElement('select', {
    className: 'nxSel__el',
    ...rest
  }, children || options.map(o => {
    const v = typeof o === 'string' ? o : o.value,
      l = typeof o === 'string' ? o : o.label;
    return React.createElement('option', {
      key: v,
      value: v
    }, l);
  })), React.createElement('span', {
    className: 'nxSel__ch'
  }, React.createElement(__ds_scope.Icon, {
    name: 'expand_more',
    size: 20
  })));
}
Object.assign(__ds_scope, { Select });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Select.jsx", error: String((e && e.message) || e) }); }

// components/forms/Switch.jsx
try { (() => {
__ds_scope.injectCss('switch', `
.nxSw{display:inline-flex;align-items:center;gap:var(--sp-5);cursor:pointer;font:var(--type-body);color:var(--text-primary);user-select:none}
.nxSw input{appearance:none;margin:0;width:44px;height:26px;flex:0 0 auto;border-radius:var(--radius-pill);background:var(--nx-gray-300);position:relative;cursor:pointer;transition:background var(--dur-fast) var(--ease-standard)}
.nxSw input::after{content:"";position:absolute;top:3px;left:3px;width:20px;height:20px;border-radius:50%;background:#fff;box-shadow:var(--shadow-subtle);transition:transform var(--dur-fast) var(--ease-standard)}
.nxSw input:checked{background:var(--nx-success-500)}
.nxSw input:checked::after{transform:translateX(18px)}
.nxSw input:focus-visible{box-shadow:var(--focus-ring)}
.nxSw__d{font:var(--type-caption);color:var(--text-muted);display:block;margin-top:2px}
`);
function Switch({
  label,
  description,
  ...rest
}) {
  return React.createElement('label', {
    className: 'nxSw'
  }, React.createElement('input', {
    type: 'checkbox',
    role: 'switch',
    ...rest
  }), label ? React.createElement('span', null, label, description ? React.createElement('span', {
    className: 'nxSw__d'
  }, description) : null) : null);
}
Object.assign(__ds_scope, { Switch });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/forms/Switch.jsx", error: String((e && e.message) || e) }); }

// components/navigation/SegmentedControl.jsx
try { (() => {
__ds_scope.injectCss('seg', `
.nxSg{display:inline-flex;background:var(--surface-sunken);border-radius:var(--radius-md);padding:3px;gap:2px}
.nxSg button{border:0;background:transparent;color:var(--text-secondary);font:var(--type-label);padding:0 var(--sp-5);height:30px;border-radius:var(--radius-sm);cursor:pointer;transition:var(--transition-control);display:inline-flex;align-items:center;gap:var(--sp-3);white-space:nowrap}
.nxSg button:hover{color:var(--text-primary)}
.nxSg button[aria-pressed="true"]{background:var(--surface-card);color:var(--text-primary);font-weight:var(--fw-semibold);box-shadow:var(--shadow-subtle)}
.nxSg--lg button{height:42px;font-size:var(--fs-16);padding:0 var(--sp-7)}
.nxSg--block{display:flex}.nxSg--block button{flex:1 1 0;justify-content:center}
`);
function SegmentedControl({
  options = [],
  value,
  onChange,
  size = 'md',
  block = false,
  ...rest
}) {
  return React.createElement('div', {
    role: 'group',
    className: ['nxSg', size === 'lg' ? 'nxSg--lg' : '', block ? 'nxSg--block' : ''].filter(Boolean).join(' '),
    ...rest
  }, options.map(o => {
    const v = typeof o === 'string' ? o : o.value,
      l = typeof o === 'string' ? o : o.label;
    return React.createElement('button', {
      key: v,
      type: 'button',
      'aria-pressed': v === value,
      onClick: () => onChange && onChange(v)
    }, o.icon ? React.createElement(__ds_scope.Icon, {
      key: 'i',
      name: o.icon,
      size: 18
    }) : null, l);
  }));
}
Object.assign(__ds_scope, { SegmentedControl });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/SegmentedControl.jsx", error: String((e && e.message) || e) }); }

// components/navigation/SideNav.jsx
try { (() => {
__ds_scope.injectCss('sidenav', `
.nxNav{width:var(--sidebar-w);flex:0 0 auto;background:var(--surface-inverse);color:#fff;display:flex;flex-direction:column;height:100%;min-height:0}
.nxNav--light{background:var(--surface-card);color:var(--text-primary);border-right:var(--border-1) solid var(--border-subtle)}
.nxNav__brand{padding:var(--sp-7) var(--sp-6);display:flex;align-items:center;gap:var(--sp-4)}
.nxNav__scroll{flex:1 1 auto;overflow-y:auto;padding:var(--sp-3) var(--sp-4) var(--sp-8)}
.nxNav__grp{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:rgba(255,255,255,.42);padding:var(--sp-6) var(--sp-4) var(--sp-3)}
.nxNav--light .nxNav__grp{color:var(--text-muted)}
.nxNav__i{display:flex;align-items:center;gap:var(--sp-5);height:42px;padding:0 var(--sp-4);border-radius:var(--radius-md);color:rgba(255,255,255,.76);font:var(--type-label);cursor:pointer;transition:var(--transition-control);border:0;background:transparent;width:100%;text-align:left}
.nxNav--light .nxNav__i{color:var(--text-secondary)}
.nxNav__i:hover{background:rgba(255,255,255,.08);color:#fff}
.nxNav--light .nxNav__i:hover{background:var(--surface-sunken);color:var(--text-primary)}
.nxNav__i--on{background:rgba(255,255,255,.14);color:#fff;font-weight:var(--fw-semibold)}
.nxNav--light .nxNav__i--on{background:var(--surface-brand-subtle);color:var(--nx-navy-800)}
.nxNav__c{margin-left:auto;font:var(--type-overline);background:var(--nx-danger-500);color:#fff;min-width:18px;height:18px;border-radius:var(--radius-pill);display:flex;align-items:center;justify-content:center;padding:0 5px}
.nxNav__foot{padding:var(--sp-5) var(--sp-6);border-top:var(--border-1) solid rgba(255,255,255,.1)}
.nxNav--light .nxNav__foot{border-color:var(--border-subtle)}
`);
function SideNav({
  brand,
  items = [],
  activeId,
  onSelect,
  footer,
  variant = 'dark',
  ...rest
}) {
  return React.createElement('nav', {
    className: 'nxNav' + (variant === 'light' ? ' nxNav--light' : ''),
    ...rest
  }, brand ? React.createElement('div', {
    className: 'nxNav__brand'
  }, brand) : null, React.createElement('div', {
    className: 'nxNav__scroll'
  }, items.map((it, i) => it.group ? React.createElement('div', {
    key: 'g' + i,
    className: 'nxNav__grp'
  }, it.group) : React.createElement('button', {
    key: it.id,
    type: 'button',
    className: 'nxNav__i' + (it.id === activeId ? ' nxNav__i--on' : ''),
    onClick: () => onSelect && onSelect(it.id)
  }, React.createElement(__ds_scope.Icon, {
    name: it.icon,
    size: 20,
    fill: it.id === activeId
  }), React.createElement('span', null, it.label), it.count ? React.createElement('span', {
    className: 'nxNav__c'
  }, it.count) : null))), footer ? React.createElement('div', {
    className: 'nxNav__foot'
  }, footer) : null);
}
Object.assign(__ds_scope, { SideNav });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/SideNav.jsx", error: String((e && e.message) || e) }); }

// components/navigation/TopBar.jsx
try { (() => {
__ds_scope.injectCss('topbar', `
.nxTop{height:var(--topbar-h);flex:0 0 auto;display:flex;align-items:center;gap:var(--sp-6);padding:0 var(--gutter-page);background:var(--surface-card);border-bottom:var(--border-1) solid var(--border-subtle)}
.nxTop--sunken{background:var(--surface-page)}
.nxTop--brand{background:var(--brand-primary);border-bottom-color:transparent;color:var(--brand-on-primary)}
.nxTop__t{font:var(--type-h2);color:inherit;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.nxTop__s{font:var(--type-caption);color:var(--text-muted);margin-top:1px}
.nxTop--brand .nxTop__s{color:rgba(255,255,255,.72)}
.nxTop__sp{flex:1 1 auto}
.nxTop__r{display:flex;align-items:center;gap:var(--sp-4);flex:0 0 auto}
`);
function TopBar({
  title,
  subtitle,
  left,
  right,
  variant = 'card',
  ...rest
}) {
  return React.createElement('header', {
    className: 'nxTop' + (variant === 'sunken' ? ' nxTop--sunken' : variant === 'brand' ? ' nxTop--brand' : ''),
    ...rest
  }, left, title || subtitle ? React.createElement('div', {
    style: {
      minWidth: 0
    }
  }, React.createElement('div', {
    className: 'nxTop__t'
  }, title), subtitle ? React.createElement('div', {
    className: 'nxTop__s'
  }, subtitle) : null) : null, React.createElement('div', {
    className: 'nxTop__sp'
  }), right ? React.createElement('div', {
    className: 'nxTop__r'
  }, right) : null);
}
Object.assign(__ds_scope, { TopBar });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/navigation/TopBar.jsx", error: String((e && e.message) || e) }); }

// components/operacao/MenuItemCard.jsx
try { (() => {
__ds_scope.injectCss('menuitem', `
.nxMi{display:flex;gap:var(--sp-5);padding:var(--sp-5);background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);text-align:left;cursor:pointer;transition:var(--transition-control),box-shadow var(--dur-fast) var(--ease-standard);width:100%;align-items:flex-start}
.nxMi:hover{box-shadow:var(--shadow-raised);border-color:var(--border-default)}
.nxMi__ph{width:88px;height:88px;flex:0 0 auto;border-radius:var(--radius-md);background:var(--surface-sunken);display:flex;align-items:center;justify-content:center;color:var(--text-disabled);overflow:hidden}
.nxMi__ph img{width:100%;height:100%;object-fit:cover}
.nxMi__b{min-width:0;flex:1 1 auto;display:flex;flex-direction:column;gap:var(--sp-2)}
.nxMi__n{font:var(--fw-semibold) var(--fs-16)/1.25 var(--font-sans);color:var(--text-primary)}
.nxMi__d{font:var(--type-caption);color:var(--text-muted);display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;text-wrap:pretty}
.nxMi__f{display:flex;align-items:center;gap:var(--sp-5);margin-top:var(--sp-2)}
.nxMi__p{font:var(--fw-bold) var(--fs-16)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxMi__t{font:var(--type-caption);color:var(--text-muted);display:inline-flex;align-items:center;gap:3px}
.nxMi--out{opacity:.55;cursor:not-allowed}
.nxMi--out .nxMi__p{text-decoration:line-through}
`);
function MenuItemCard({
  name,
  description,
  price,
  prepMinutes,
  imageSrc,
  unavailable = false,
  badge,
  ...rest
}) {
  return React.createElement('button', {
    type: 'button',
    disabled: unavailable,
    className: 'nxMi' + (unavailable ? ' nxMi--out' : ''),
    ...rest
  }, React.createElement('span', {
    className: 'nxMi__ph'
  }, imageSrc ? React.createElement('img', {
    src: imageSrc,
    alt: ''
  }) : React.createElement(__ds_scope.Icon, {
    name: 'local_pizza',
    size: 28
  })), React.createElement('span', {
    className: 'nxMi__b'
  }, React.createElement('span', {
    className: 'nxMi__n'
  }, name), description ? React.createElement('span', {
    className: 'nxMi__d'
  }, description) : null, React.createElement('span', {
    className: 'nxMi__f'
  }, React.createElement('span', {
    className: 'nxMi__p'
  }, price), prepMinutes ? React.createElement('span', {
    className: 'nxMi__t'
  }, React.createElement(__ds_scope.Icon, {
    name: 'schedule',
    size: 14
  }), prepMinutes + ' min') : null, unavailable ? React.createElement('span', {
    className: 'nxMi__t',
    style: {
      color: 'var(--text-danger)'
    }
  }, React.createElement(__ds_scope.Icon, {
    name: 'block',
    size: 14
  }), 'Esgotado') : null, badge)));
}
Object.assign(__ds_scope, { MenuItemCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/operacao/MenuItemCard.jsx", error: String((e && e.message) || e) }); }

// components/operacao/OrderLine.jsx
try { (() => {
__ds_scope.injectCss('orderline', `
.nxOl{display:flex;gap:var(--sp-5);align-items:flex-start;padding:var(--sp-5) 0;border-bottom:var(--border-1) solid var(--border-subtle)}
.nxOl:last-child{border-bottom:0}
.nxOl__q{font:var(--fw-semibold) var(--fs-14)/1.4 var(--font-mono);color:var(--text-secondary);min-width:26px;flex:0 0 auto}
.nxOl__b{flex:1 1 auto;min-width:0}
.nxOl__n{font:var(--fw-medium) var(--fs-14)/1.35 var(--font-sans);color:var(--text-primary)}
.nxOl__m{font:var(--type-caption);color:var(--text-muted);margin-top:2px}
.nxOl__st{margin-top:var(--sp-3);display:flex;gap:var(--sp-4);align-items:center}
.nxOl__p{font:var(--fw-semibold) var(--fs-14)/1.4 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary);flex:0 0 auto;text-align:right}
.nxOl__a{flex:0 0 auto}
.nxOl--void .nxOl__n,.nxOl--void .nxOl__p{text-decoration:line-through;color:var(--text-muted)}
`);
function OrderLine({
  qty,
  name,
  modifiers,
  note,
  price,
  status,
  actions,
  cancelled = false,
  ...rest
}) {
  return React.createElement('div', {
    className: 'nxOl' + (cancelled ? ' nxOl--void' : ''),
    ...rest
  }, React.createElement('span', {
    className: 'nxOl__q'
  }, qty + '×'), React.createElement('span', {
    className: 'nxOl__b'
  }, React.createElement('span', {
    className: 'nxOl__n'
  }, name), modifiers ? React.createElement('span', {
    className: 'nxOl__m',
    style: {
      display: 'block'
    }
  }, modifiers) : null, note ? React.createElement('span', {
    className: 'nxOl__m',
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: '3px'
    }
  }, React.createElement(__ds_scope.Icon, {
    name: 'edit_note',
    size: 14
  }), note) : null, status ? React.createElement('span', {
    className: 'nxOl__st'
  }, status) : null), price ? React.createElement('span', {
    className: 'nxOl__p'
  }, price) : null, actions ? React.createElement('span', {
    className: 'nxOl__a'
  }, actions) : null);
}
Object.assign(__ds_scope, { OrderLine });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/operacao/OrderLine.jsx", error: String((e && e.message) || e) }); }

// components/operacao/OrderTicket.jsx
try { (() => {
__ds_scope.injectCss('ticket', `
.nxTk{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);display:flex;flex-direction:column;overflow:hidden;min-width:0}
.nxTk__h{display:flex;align-items:center;justify-content:space-between;gap:var(--sp-4);padding:var(--sp-5) var(--sp-5);border-bottom:var(--border-1) solid var(--border-subtle)}
.nxTk__id{display:flex;align-items:baseline;gap:var(--sp-4);min-width:0}
.nxTk__code{font:var(--fw-black) var(--fs-28)/1 var(--font-mono);color:var(--text-primary)}
.nxTk__where{font:var(--fw-semibold) var(--fs-14)/1 var(--font-sans);color:var(--text-secondary);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.nxTk__items{list-style:none;margin:0;padding:var(--sp-4) 0;flex:1 1 auto}
.nxTk__it{display:flex;gap:var(--sp-4);padding:var(--sp-3) var(--sp-5);align-items:flex-start}
.nxTk__q{font:var(--fw-black) var(--fs-24)/1.1 var(--font-mono);color:var(--nx-navy-700);flex:0 0 auto;min-width:32px}
[data-surface="kds"] .nxTk__q{color:var(--nx-cyan-400)}
.nxTk__nm{font:var(--type-kds-item);color:var(--text-primary)}
.nxTk__mod{font:var(--fw-medium) var(--fs-14)/1.35 var(--font-sans);color:var(--nx-warning-500);margin-top:2px}
.nxTk__it--done .nxTk__nm{text-decoration:line-through;opacity:.45}
.nxTk__it--done .nxTk__q{opacity:.45}
.nxTk__f{display:flex;align-items:center;gap:var(--sp-4);padding:var(--sp-4) var(--sp-5);border-top:var(--border-1) solid var(--border-subtle);background:var(--surface-sunken)}
.nxTk__fire{margin-left:auto;font:var(--type-caption);color:var(--text-muted);display:inline-flex;align-items:center;gap:3px}
.nxTk--late{border-color:var(--nx-time-late);box-shadow:0 0 0 2px var(--nx-time-late) inset}
`);
function OrderTicket({
  code,
  where,
  channel,
  seconds = 0,
  warnAt = 300,
  lateAt = 600,
  items = [],
  fireAt,
  footer,
  onDark = true,
  ...rest
}) {
  return React.createElement('article', {
    className: 'nxTk' + (seconds >= lateAt ? ' nxTk--late' : ''),
    ...rest
  }, React.createElement('div', {
    className: 'nxTk__h'
  }, React.createElement('div', {
    className: 'nxTk__id'
  }, React.createElement('span', {
    className: 'nxTk__code'
  }, code), React.createElement('span', {
    className: 'nxTk__where'
  }, where)), React.createElement(__ds_scope.OrderTimer, {
    seconds,
    warnAt,
    lateAt,
    size: 'md',
    onDark
  })), React.createElement('ul', {
    className: 'nxTk__items'
  }, items.map((it, i) => React.createElement('li', {
    key: i,
    className: 'nxTk__it' + (it.done ? ' nxTk__it--done' : '')
  }, React.createElement('span', {
    className: 'nxTk__q'
  }, it.qty + '×'), React.createElement('span', null, React.createElement('div', {
    className: 'nxTk__nm'
  }, it.name), it.modifiers ? React.createElement('div', {
    className: 'nxTk__mod'
  }, it.modifiers) : null)))), footer || channel || fireAt ? React.createElement('div', {
    className: 'nxTk__f'
  }, channel ? React.createElement('span', {
    style: {
      display: 'inline-flex',
      alignItems: 'center',
      gap: '4px',
      font: 'var(--type-caption)',
      color: 'var(--text-secondary)'
    }
  }, React.createElement(__ds_scope.Icon, {
    name: channel === 'DELIVERY' ? 'delivery_dining' : channel === 'COUNTER' ? 'takeout_dining' : 'table_restaurant',
    size: 16
  }), channel === 'DELIVERY' ? 'Delivery' : channel === 'COUNTER' ? 'Balcão' : 'Salão') : null, footer, fireAt ? React.createElement('span', {
    className: 'nxTk__fire'
  }, React.createElement(__ds_scope.Icon, {
    name: 'local_fire_department',
    size: 16
  }), 'montar ' + fireAt) : null) : null);
}
Object.assign(__ds_scope, { OrderTicket });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/operacao/OrderTicket.jsx", error: String((e && e.message) || e) }); }

// components/operacao/TableCard.jsx
try { (() => {
__ds_scope.injectCss('tablecard', `
.nxTc{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);padding:var(--sp-5);display:flex;flex-direction:column;gap:var(--sp-4);cursor:pointer;transition:var(--transition-control),box-shadow var(--dur-fast) var(--ease-standard);text-align:left;min-height:132px;box-shadow:var(--shadow-subtle)}
.nxTc:hover{box-shadow:var(--shadow-raised);border-color:var(--border-default)}
.nxTc__top{display:flex;align-items:center;justify-content:space-between;gap:var(--sp-4)}
.nxTc__n{font:var(--fw-bold) var(--fs-20)/1 var(--font-display);color:var(--text-primary)}
.nxTc__meta{display:flex;align-items:center;gap:var(--sp-5);font:var(--type-caption);color:var(--text-muted)}
.nxTc__meta span{display:inline-flex;align-items:center;gap:3px}
.nxTc__v{margin-top:auto;font:var(--fw-bold) var(--fs-18)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxTc--attention{border-color:var(--nx-danger-500);box-shadow:0 0 0 1px var(--nx-danger-500)}
.nxTc--free{background:var(--surface-page);border-style:dashed;box-shadow:none}
.nxTc--free .nxTc__n{color:var(--text-muted)}
`);
function TableCard({
  name,
  status = 'FREE',
  elapsed,
  guests,
  total,
  waiter,
  attention = false,
  ...rest
}) {
  return React.createElement('button', {
    type: 'button',
    className: ['nxTc', status === 'FREE' ? 'nxTc--free' : '', attention ? 'nxTc--attention' : ''].filter(Boolean).join(' '),
    ...rest
  }, React.createElement('div', {
    className: 'nxTc__top'
  }, React.createElement('span', {
    className: 'nxTc__n'
  }, name), React.createElement(__ds_scope.StatusPill, {
    status,
    live: attention
  })), React.createElement('div', {
    className: 'nxTc__meta'
  }, guests ? React.createElement('span', null, React.createElement(__ds_scope.Icon, {
    name: 'group',
    size: 14
  }), guests) : null, elapsed ? React.createElement('span', null, React.createElement(__ds_scope.Icon, {
    name: 'schedule',
    size: 14
  }), elapsed) : null, waiter ? React.createElement('span', null, React.createElement(__ds_scope.Icon, {
    name: 'room_service',
    size: 14
  }), waiter) : null), total ? React.createElement('div', {
    className: 'nxTc__v'
  }, total) : null);
}
Object.assign(__ds_scope, { TableCard });
})(); } catch (e) { __ds_ns.__errors.push({ path: "components/operacao/TableCard.jsx", error: String((e && e.message) || e) }); }

// ui_kits/admin-nexora/AdminApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  Card,
  Input,
  Field,
  Select,
  Switch,
  Checkbox,
  SideNav,
  TopBar,
  SegmentedControl,
  StatTile,
  ProgressMeter,
  DataTable,
  StatusPill,
  AlertBanner,
  SyncStatus,
  BrandMark,
  EmptyState
} = window.NexoraDesignSystem_aa692a;
const SAUDE = {
  ok: ['success', 'Saudável'],
  atencao: ['warning', 'Atenção'],
  implantando: ['info', 'Implantando']
};
function Instancias({
  onOpen
}) {
  const cols = [{
    key: 'n',
    header: 'Estabelecimento',
    render: r => /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
      style: {
        font: 'var(--fw-semibold) 14px/1.3 var(--font-sans)'
      }
    }, r.n), /*#__PURE__*/React.createElement("div", {
      style: {
        font: 'var(--type-caption)',
        color: 'var(--text-muted)'
      }
    }, r.t))
  }, {
    key: 'pl',
    header: 'Plano'
  }, {
    key: 'st',
    header: 'Status',
    render: r => /*#__PURE__*/React.createElement(Badge, {
      tone: r.st === 'Ativa' ? 'success' : r.st === 'Piloto' ? 'info' : 'warning',
      size: "sm"
    }, r.st)
  }, {
    key: 'ver',
    header: 'Versão',
    numeric: true
  }, {
    key: 'sync',
    header: 'Sync',
    render: r => r.sync === '—' ? /*#__PURE__*/React.createElement("span", {
      style: {
        color: 'var(--text-muted)'
      }
    }, "\u2014") : /*#__PURE__*/React.createElement(SyncStatus, {
      state: r.saude === 'atencao' ? 'delayed' : 'online',
      lastSync: r.sync
    })
  }, {
    key: 'ped',
    header: 'Volume',
    numeric: true
  }, {
    key: 'saude',
    header: 'Saúde',
    render: r => /*#__PURE__*/React.createElement(Badge, {
      tone: SAUDE[r.saude][0],
      size: "sm"
    }, SAUDE[r.saude][1])
  }];
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(5,1fr)',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    label: "Inst\xE2ncias ativas",
    value: "4",
    icon: "storefront",
    comparison: "1 em implanta\xE7\xE3o"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Sync atrasada",
    value: "1",
    icon: "sync_problem"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Parque na \xFAltima vers\xE3o",
    value: "80",
    unit: "%",
    icon: "upgrade",
    target: "100%"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Tempo m\xE9dio de implanta\xE7\xE3o",
    value: "4,2",
    unit: "dias",
    icon: "rocket_launch",
    target: "\u2264 5 dias"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Chamados abertos",
    value: "3",
    icon: "support_agent",
    delta: "-2",
    comparison: "vs. semana anterior"
  })), /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "warning",
    title: "Cantina Bella \xB7 sincroniza\xE7\xE3o atrasada h\xE1 18 min",
    actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Diagn\xF3stico"), /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "primary"
    }, "Solicitar acesso"))
  }, "862 eventos na fila local. A opera\xE7\xE3o da loja continua; o painel do dono est\xE1 defasado."), /*#__PURE__*/React.createElement(Card, {
    title: "Inst\xE2ncias",
    subtitle: "Isolamento de dados por tenant \u2014 nenhuma consulta cruza fronteira (RN-015)",
    padding: "none",
    actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Input, {
      size: "md",
      icon: "search",
      placeholder: "Buscar"
    }), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "sm",
      iconLeft: "add"
    }, "Provisionar"))
  }, /*#__PURE__*/React.createElement(DataTable, {
    columns: cols,
    rows: TENANTS,
    onRowClick: onOpen
  })));
}
function Provisionar() {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 380px',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Nova inst\xE2ncia",
    subtitle: "Sem altera\xE7\xE3o de c\xF3digo \u2014 s\xF3 configura\xE7\xE3o (RF-PLT-05)",
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      variant: "ghost"
    }, "Cancelar"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      iconLeft: "rocket_launch"
    }, "Provisionar e gerar install.sh"))
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(Field, {
    label: "Nome do estabelecimento",
    required: true
  }, /*#__PURE__*/React.createElement(Input, {
    defaultValue: "Sabor Mineiro"
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Slug / subdom\xEDnio",
    required: true,
    hint: "cardapio.<slug>.nexora.app"
  }, /*#__PURE__*/React.createElement(Input, {
    defaultValue: "sabor-mineiro"
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Modelo de neg\xF3cio",
    hint: "Traz card\xE1pio e configura\xE7\xE3o pr\xE9-montados"
  }, /*#__PURE__*/React.createElement(Select, {
    options: ['Pizzaria', 'Hamburgueria', 'Restaurante', 'Lanchonete'],
    defaultValue: "Restaurante"
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Plano"
  }, /*#__PURE__*/React.createElement(Select, {
    options: ['Operação', 'Operação + Gestão', 'Completo'],
    defaultValue: "Completo"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 20,
      paddingTop: 16,
      borderTop: '1px solid var(--border-subtle)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)',
      marginBottom: 12
    }
  }, "Identidade visual"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr 1fr',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(Field, {
    label: "Cor prim\xE1ria"
  }, /*#__PURE__*/React.createElement(Input, {
    prefix: "#",
    defaultValue: "C1121F"
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Cor secund\xE1ria"
  }, /*#__PURE__*/React.createElement(Input, {
    prefix: "#",
    defaultValue: "669BBC"
  })), /*#__PURE__*/React.createElement(Field, {
    label: "Raio de borda"
  }, /*#__PURE__*/React.createElement(Input, {
    numeric: true,
    suffix: "px",
    defaultValue: "12"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 16,
      padding: 16,
      border: '1px dashed var(--border-default)',
      borderRadius: 'var(--brand-radius)',
      textAlign: 'center',
      color: 'var(--text-muted)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "upload_file",
    size: 26
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      marginTop: 6
    }
  }, "Logo claro e escuro \xB7 favicon \xB7 \xEDcone do PWA"))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 20,
      paddingTop: 16,
      borderTop: '1px solid var(--border-subtle)',
      display: 'flex',
      flexDirection: 'column',
      gap: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "M\xF3dulos ativos"), /*#__PURE__*/React.createElement(Switch, {
    label: "KDS de cozinha",
    defaultChecked: true
  }), /*#__PURE__*/React.createElement(Switch, {
    label: "Delivery pr\xF3prio",
    defaultChecked: true
  }), /*#__PURE__*/React.createElement(Switch, {
    label: "Estoque e ficha t\xE9cnica",
    defaultChecked: true
  }), /*#__PURE__*/React.createElement(Switch, {
    label: "Financeiro de gest\xE3o"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Pr\xE9via do tenant",
    subtitle: "Tokens aplicados em runtime",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    "data-tenant": "dona-betinha",
    style: {
      background: 'var(--brand-surface)',
      borderRadius: 'var(--brand-radius)',
      padding: 16,
      display: 'flex',
      flexDirection: 'column',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(BrandMark, {
    tenantName: "Sabor Mineiro",
    subtitle: "Restaurante",
    size: 32
  }), /*#__PURE__*/React.createElement(Button, {
    variant: "primary",
    size: "lg",
    block: true,
    iconLeft: "send"
  }, "Enviar pedido"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement(Badge, {
    tone: "neutral"
  }, "Sal\xE3o"), /*#__PURE__*/React.createElement(Badge, {
    tone: "neutral"
  }, "Delivery")))), /*#__PURE__*/React.createElement(Card, {
    title: "Checklist de implanta\xE7\xE3o",
    padding: "tight"
  }, [['Instância e domínio', 1], ['Identidade visual', 1], ['Cardápio e fichas', 0], ['Mesas, perfis e regras', 0], ['Servidor local + rede', 0], ['Meios de pagamento', 0], ['Treinamento e piloto', 0]].map(([n, ok]) => /*#__PURE__*/React.createElement("div", {
    key: n,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      padding: '8px 0',
      font: 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: ok ? 'check_circle' : 'radio_button_unchecked',
    size: 18,
    color: ok ? 'var(--nx-success-500)' : 'var(--text-disabled)',
    fill: !!ok
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      color: ok ? 'var(--text-primary)' : 'var(--text-muted)'
    }
  }, n)))), /*#__PURE__*/React.createElement(Card, {
    title: "Importar",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    size: "md",
    block: true,
    iconLeft: "table_view"
  }, "Card\xE1pio e ficha por planilha"))));
}
function Auditoria() {
  return /*#__PURE__*/React.createElement(Card, {
    title: "Trilha da plataforma",
    subtitle: "Imut\xE1vel \u2014 nenhum usu\xE1rio altera ou apaga (RF-AUD-04)",
    padding: "none",
    actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Select, {
      options: ['Todas as instâncias', 'Dona Betinha', 'Cantina Bella']
    }), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "sm",
      iconLeft: "download"
    }, "Exportar"))
  }, /*#__PURE__*/React.createElement(DataTable, {
    columns: [{
      key: 0,
      header: 'Hora',
      render: r => /*#__PURE__*/React.createElement("span", {
        style: {
          fontFamily: 'var(--font-mono)'
        }
      }, r[0])
    }, {
      key: 1,
      header: 'Instância'
    }, {
      key: 2,
      header: 'Evento',
      render: r => /*#__PURE__*/React.createElement(Badge, {
        tone: "neutral",
        size: "sm",
        square: true
      }, r[2])
    }, {
      key: 3,
      header: 'Detalhe'
    }],
    rows: EVENTOS
  }));
}
function AdminApp() {
  const [view, setView] = React.useState('inst');
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      height: '100vh',
      background: 'var(--surface-page)'
    }
  }, /*#__PURE__*/React.createElement(SideNav, {
    brand: /*#__PURE__*/React.createElement(BrandMark, {
      inverse: true,
      size: 24,
      subtitle: "Plataforma"
    }),
    variant: "dark",
    activeId: view,
    onSelect: setView,
    items: [{
      group: 'Plataforma'
    }, {
      id: 'inst',
      label: 'Instâncias',
      icon: 'storefront',
      count: 5
    }, {
      id: 'prov',
      label: 'Provisionar',
      icon: 'add_business'
    }, {
      id: 'saude',
      label: 'Saúde do parque',
      icon: 'health_and_safety',
      count: 1
    }, {
      group: 'Produto'
    }, {
      id: 'mod',
      label: 'Modelos de negócio',
      icon: 'category'
    }, {
      id: 'ver',
      label: 'Versões e rollout',
      icon: 'upgrade'
    }, {
      group: 'Governança'
    }, {
      id: 'aud',
      label: 'Auditoria',
      icon: 'history'
    }, {
      id: 'sup',
      label: 'Suporte',
      icon: 'support_agent',
      count: 3
    }],
    footer: /*#__PURE__*/React.createElement("div", {
      style: {
        font: 'var(--type-caption)',
        color: 'rgba(255,255,255,.5)'
      }
    }, "Replay Studio \xB7 admin")
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      display: 'flex',
      flexDirection: 'column',
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement(TopBar, {
    title: view === 'prov' ? 'Provisionar instância' : view === 'aud' ? 'Auditoria da plataforma' : 'Instâncias',
    subtitle: "Nexora \xB7 plataforma de gest\xE3o inteligente",
    right: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(SegmentedControl, {
      options: ['Todas', 'Ativas', 'Piloto'],
      value: "Todas",
      onChange: () => {}
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "notifications",
      label: "Alertas",
      badge: 1
    }))
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: 24
    }
  }, view === 'prov' ? /*#__PURE__*/React.createElement(Provisionar, null) : view === 'aud' ? /*#__PURE__*/React.createElement(Auditoria, null) : /*#__PURE__*/React.createElement(Instancias, {
    onOpen: () => setView('prov')
  }))));
}
window.AdminApp = AdminApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/admin-nexora/AdminApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/admin-nexora/data.jsx
try { (() => {
const TENANTS = [{
  n: 'Dona Betinha',
  t: 'Pizzaria',
  pl: 'Completo',
  st: 'Ativa',
  ver: '1.8.2',
  sync: 'há 4 s',
  ped: '1.284/mês',
  saude: 'ok'
}, {
  n: 'Burger do Vale',
  t: 'Hamburgueria',
  pl: 'Operação + Gestão',
  st: 'Ativa',
  ver: '1.8.2',
  sync: 'há 12 s',
  ped: '2.140/mês',
  saude: 'ok'
}, {
  n: 'Cantina Bella',
  t: 'Restaurante',
  pl: 'Operação',
  st: 'Ativa',
  ver: '1.7.4',
  sync: 'há 18 min',
  ped: '860/mês',
  saude: 'atencao'
}, {
  n: 'Pastel da Feira',
  t: 'Lanchonete',
  pl: 'Operação',
  st: 'Piloto',
  ver: '1.8.2',
  sync: 'há 6 s',
  ped: '214/mês',
  saude: 'ok'
}, {
  n: 'Sabor Mineiro',
  t: 'Restaurante',
  pl: 'Completo',
  st: 'Implantação',
  ver: '—',
  sync: '—',
  ped: '—',
  saude: 'implantando'
}];
const EVENTOS = [['22:41', 'Dona Betinha', 'support.access_granted', 'Acesso de suporte por 60 min — autorizado por Sáskia'], ['22:18', 'Cantina Bella', 'sync.delayed', 'Atraso de sincronização acima de 5 min (18 min)'], ['21:52', 'Pastel da Feira', 'tenant.config_changed', 'Taxa de serviço 10% → 12%'], ['20:30', 'Burger do Vale', 'install.updated', 'Edge server 1.7.4 → 1.8.2, rollback disponível'], ['19:04', 'Sabor Mineiro', 'tenant.provisioned', 'Instância criada a partir do modelo RESTAURANTE']];
Object.assign(window, {
  TENANTS,
  EVENTOS
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/admin-nexora/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/caixa/CaixaApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  Card,
  Input,
  Field,
  Select,
  Checkbox,
  SideNav,
  TopBar,
  SegmentedControl,
  StatusPill,
  OrderTimer,
  TableCard,
  OrderLine,
  SyncStatus,
  BrandMark,
  AlertBanner,
  DataTable,
  StatTile,
  NumericKeypad
} = window.NexoraDesignSystem_aa692a;
function Conta({
  mesa,
  onPagar
}) {
  const sub = CONTA.filter(i => !i.cancel).reduce((s, i) => s + i.preco, 0);
  const [taxa, setTaxa] = React.useState(true);
  const total = sub + (taxa ? sub * .1 : 0);
  return /*#__PURE__*/React.createElement(Card, {
    title: 'Conta · ' + mesa.n,
    subtitle: mesa.g + ' pessoas · ' + mesa.t + ' · garçom ' + mesa.w,
    actions: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(StatusPill, {
      status: mesa.s,
      live: mesa.att
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "print",
      label: "Imprimir",
      size: "sm"
    })),
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      iconLeft: "call_split"
    }, "Dividir"), /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      iconLeft: "percent"
    }, "Desconto"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      iconLeft: "point_of_sale",
      onClick: onPagar
    }, "Receber ", brl(total)))
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      maxHeight: 300,
      overflowY: 'auto'
    }
  }, CONTA.map((i, x) => /*#__PURE__*/React.createElement(OrderLine, {
    key: x,
    qty: i.qty,
    name: i.nome,
    modifiers: i.mods,
    note: i.obs,
    price: brl(i.preco),
    cancelled: i.cancel,
    status: /*#__PURE__*/React.createElement(StatusPill, {
      status: i.status
    })
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 14,
      paddingTop: 14,
      borderTop: '1px solid var(--border-subtle)',
      display: 'flex',
      flexDirection: 'column',
      gap: 8
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Subtotal"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(sub))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center'
    }
  }, /*#__PURE__*/React.createElement(Checkbox, {
    compact: true,
    label: "Taxa de servi\xE7o 10%",
    checked: taxa,
    onChange: e => setTaxa(e.target.checked)
  }), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum",
    style: {
      font: 'var(--type-numeric)'
    }
  }, brl(sub * .1))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      paddingTop: 10,
      borderTop: '2px solid var(--border-default)',
      font: 'var(--fw-bold) 24px/1.1 var(--font-sans)'
    }
  }, /*#__PURE__*/React.createElement("span", null, "Total"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(total)))));
}
function Pagamento({
  mesa,
  onVoltar
}) {
  const sub = CONTA.filter(i => !i.cancel).reduce((s, i) => s + i.preco, 0),
    total = sub * 1.1;
  const [pagos, setPagos] = React.useState([{
    f: 'Débito',
    v: 100
  }]);
  const [valor, setValor] = React.useState('');
  const pago = pagos.reduce((s, p) => s + p.v, 0);
  const falta = Math.max(0, total - pago);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 380px',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: 'Recebimento · ' + mesa.n,
    subtitle: "M\xFAltiplas formas na mesma conta (RF-CXA-03)",
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      variant: "ghost",
      onClick: onVoltar
    }, "Voltar"), /*#__PURE__*/React.createElement(Button, {
      variant: "accent",
      iconLeft: "check_circle",
      disabled: falta > 0,
      onClick: onVoltar
    }, "Fechar conta"))
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(5,1fr)',
      gap: 10
    }
  }, FORMAS.map(([n, ic]) => /*#__PURE__*/React.createElement("button", {
    key: n,
    onClick: () => {
      const v = parseFloat(valor.replace(',', '.')) || falta;
      setPagos(p => [...p, {
        f: n,
        v
      }]);
      setValor('');
    },
    style: {
      minHeight: 80,
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      gap: 6,
      borderRadius: 'var(--brand-radius)',
      border: '1px solid var(--border-default)',
      background: 'var(--surface-card)',
      cursor: 'pointer',
      boxShadow: 'var(--shadow-subtle)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: ic,
    size: 24,
    color: "var(--nx-navy-700)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-semibold) 12px/1.2 var(--font-sans)',
      textAlign: 'center'
    }
  }, n)))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 18,
      display: 'flex',
      flexDirection: 'column',
      gap: 10
    }
  }, pagos.map((p, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      padding: '10px 12px',
      borderRadius: 'var(--radius-md)',
      background: 'var(--surface-sunken)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "check_circle",
    size: 18,
    color: "var(--nx-success-500)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--type-body)'
    }
  }, p.f), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum",
    style: {
      marginLeft: 'auto',
      font: 'var(--type-numeric)'
    }
  }, brl(p.v)), /*#__PURE__*/React.createElement(IconButton, {
    icon: "close",
    label: "Remover",
    size: "sm",
    onClick: () => setPagos(x => x.filter((_, j) => j !== i))
  })))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 18,
      paddingTop: 14,
      borderTop: '1px solid var(--border-subtle)',
      display: 'flex',
      gap: 24
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "Total"), /*#__PURE__*/React.createElement("div", {
    className: "nx-tnum",
    style: {
      font: 'var(--fw-bold) 22px/1.2 var(--font-mono)'
    }
  }, brl(total))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "Recebido"), /*#__PURE__*/React.createElement("div", {
    className: "nx-tnum",
    style: {
      font: 'var(--fw-bold) 22px/1.2 var(--font-mono)',
      color: 'var(--nx-success-600)'
    }
  }, brl(pago))), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "Falta"), /*#__PURE__*/React.createElement("div", {
    className: "nx-tnum",
    style: {
      font: 'var(--fw-bold) 22px/1.2 var(--font-mono)',
      color: falta ? 'var(--nx-danger-600)' : 'var(--text-muted)'
    }
  }, brl(falta))))), /*#__PURE__*/React.createElement(Card, {
    title: "Valor",
    subtitle: "Vazio = recebe o restante",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: 56,
      borderRadius: 'var(--radius-md)',
      background: 'var(--surface-sunken)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'flex-end',
      padding: '0 14px',
      font: 'var(--fw-bold) 26px/1 var(--font-mono)',
      color: valor ? 'var(--text-primary)' : 'var(--text-disabled)',
      marginBottom: 12
    }
  }, valor || brl(falta)), /*#__PURE__*/React.createElement(NumericKeypad, {
    value: valor,
    onChange: setValor,
    onSubmit: () => {}
  })));
}
function Fechamento() {
  const cols = [{
    key: 'f',
    header: 'Forma'
  }, {
    key: 'sis',
    header: 'Sistema',
    numeric: true
  }, {
    key: 'con',
    header: 'Conferido',
    numeric: true
  }, {
    key: 'div',
    header: 'Divergência',
    numeric: true,
    render: r => /*#__PURE__*/React.createElement("span", {
      style: {
        color: r.div === '—' ? 'var(--text-muted)' : 'var(--nx-danger-600)'
      }
    }, r.div)
  }];
  const rows = [{
    f: 'Dinheiro',
    sis: 'R$ 486,00',
    con: 'R$ 474,00',
    div: '− R$ 12,00'
  }, {
    f: 'Débito',
    sis: 'R$ 1.204,50',
    con: 'R$ 1.204,50',
    div: '—'
  }, {
    f: 'Crédito',
    sis: 'R$ 1.680,00',
    con: 'R$ 1.680,00',
    div: '—'
  }, {
    f: 'PIX',
    sis: 'R$ 812,40',
    con: 'R$ 812,40',
    div: '—'
  }];
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 340px',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Fechamento de caixa",
    subtitle: "Turno de 18:02 \xB7 operador Marcos",
    padding: "none",
    footer: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      iconLeft: "download"
    }, "Exportar"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      iconLeft: "lock"
    }, "Fechar caixa"))
  }, /*#__PURE__*/React.createElement(DataTable, {
    columns: cols,
    rows: rows,
    footer: /*#__PURE__*/React.createElement("tr", null, /*#__PURE__*/React.createElement("td", null, "Total"), /*#__PURE__*/React.createElement("td", {
      className: "nxTb__num"
    }, "R$ 4.182,90"), /*#__PURE__*/React.createElement("td", {
      className: "nxTb__num"
    }, "R$ 4.170,90"), /*#__PURE__*/React.createElement("td", {
      className: "nxTb__num",
      style: {
        color: 'var(--nx-danger-600)'
      }
    }, "\u2212 R$ 12,00"))
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "warning",
    title: "Diverg\xEAncia de R$ 12,00 em dinheiro",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Justificar")
  }, "Acima do limite configurado \u2014 exige justificativa e vai para a trilha de auditoria."), /*#__PURE__*/React.createElement(Card, {
    title: "Movimentos",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 10,
      font: 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Abertura"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, "R$ 200,00")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Suprimento 20:14"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, "+ R$ 100,00")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Sangria 22:40"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, "\u2212 R$ 800,00")))), /*#__PURE__*/React.createElement(Card, {
    title: "Taxa de cart\xE3o",
    subtitle: "Despesa normalmente invis\xEDvel",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "D\xE9bito 1,49% + Cr\xE9dito 3,19%"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, "R$ 71,55")))));
}
function CaixaApp() {
  const [view, setView] = React.useState('mesas');
  const [mesa, setMesa] = React.useState(MESAS_CX[3]);
  const abertas = MESAS_CX.filter(m => m.s !== 'PAID');
  const total = abertas.reduce((s, m) => s + m.v, 0);
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      height: '100vh',
      background: 'var(--surface-page)'
    }
  }, /*#__PURE__*/React.createElement(SideNav, {
    brand: /*#__PURE__*/React.createElement(BrandMark, {
      inverse: true,
      size: 22,
      subtitle: "Caixa \xB7 Terminal 1"
    }),
    activeId: view,
    onSelect: setView,
    items: [{
      group: 'Operação'
    }, {
      id: 'mesas',
      label: 'Mesas e comandas',
      icon: 'table_restaurant',
      count: abertas.length
    }, {
      id: 'pagamento',
      label: 'Recebimento',
      icon: 'point_of_sale'
    }, {
      id: 'fechamento',
      label: 'Fechamento de caixa',
      icon: 'lock_clock'
    }, {
      group: 'Consulta'
    }, {
      id: 'hist',
      label: 'Contas do turno',
      icon: 'receipt_long'
    }, {
      id: 'aud',
      label: 'Auditoria',
      icon: 'history'
    }],
    footer: /*#__PURE__*/React.createElement(SyncStatus, {
      state: "local",
      queued: 12
    })
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      display: 'flex',
      flexDirection: 'column',
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement(TopBar, {
    title: view === 'fechamento' ? 'Fechamento de caixa' : view === 'pagamento' ? 'Recebimento' : 'Mesas e comandas abertas',
    subtitle: "Dona Betinha \xB7 ter\xE7a, 22:48 \xB7 turno aberto \xE0s 18:02",
    right: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(SegmentedControl, {
      options: ['Salão', 'Delivery', 'Balcão'],
      value: "Sal\xE3o",
      onChange: () => {}
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "notifications",
      label: "Alertas",
      badge: 2
    }), /*#__PURE__*/React.createElement(SyncStatus, {
      state: "local",
      queued: 12
    }))
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: 24
    }
  }, view === 'fechamento' ? /*#__PURE__*/React.createElement(Fechamento, null) : view === 'pagamento' ? /*#__PURE__*/React.createElement(Pagamento, {
    mesa: mesa,
    onVoltar: () => setView('mesas')
  }) : /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 460px',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(4,1fr)',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    label: "Em aberto",
    value: brl(total),
    icon: "hourglass_top"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Recebido no turno",
    value: "R$ 4.182",
    icon: "payments",
    delta: "+8,1%",
    comparison: "vs. mesma ter\xE7a"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Ticket m\xE9dio",
    value: "R$ 96",
    icon: "receipt",
    comparison: "m\xE9dia 89"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Contas fechadas",
    value: "42",
    icon: "task_alt"
  })), /*#__PURE__*/React.createElement(Card, {
    title: "Mesas abertas",
    subtitle: "Valor e tempo de cada uma (RF-CXA-01)",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fill,minmax(196px,1fr))',
      gap: 12
    }
  }, MESAS_CX.map(m => /*#__PURE__*/React.createElement(TableCard, {
    key: m.n,
    name: m.n,
    status: m.s,
    elapsed: m.t,
    guests: m.g,
    total: brl(m.v),
    waiter: m.w,
    attention: m.att,
    onClick: () => setMesa(m)
  }))))), /*#__PURE__*/React.createElement(Conta, {
    mesa: mesa,
    onPagar: () => setView('pagamento')
  })))));
}
window.CaixaApp = CaixaApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/caixa/CaixaApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/caixa/data.jsx
try { (() => {
const MESAS_CX = [{
  n: 'Mesa 01',
  s: 'OPEN',
  t: '12 min',
  g: 2,
  v: 58.0,
  w: 'Jonas'
}, {
  n: 'Mesa 03',
  s: 'READY',
  t: '26 min',
  g: 4,
  v: 164.8,
  w: 'Jonas'
}, {
  n: 'Mesa 07',
  s: 'OPEN',
  t: '42 min',
  g: 4,
  v: 120.9,
  w: 'Jonas'
}, {
  n: 'Mesa 08',
  s: 'BILL_REQUESTED',
  t: '1h 04',
  g: 3,
  v: 186.4,
  w: 'Rita',
  att: true
}, {
  n: 'Mesa 11',
  s: 'OPEN',
  t: '8 min',
  g: 2,
  v: 34.0,
  w: 'Rita'
}, {
  n: 'Mesa 12',
  s: 'PAID',
  t: '1h 18',
  g: 6,
  v: 312.0,
  w: 'Jonas'
}];
const CONTA = [{
  qty: 1,
  nome: 'Pizza G · Calabresa / Mussarela',
  mods: 'borda catupiry',
  obs: 'sem cebola',
  preco: 72.9,
  status: 'SERVED'
}, {
  qty: 1,
  nome: 'Pizza G · Frango com catupiry',
  preco: 69.9,
  status: 'SERVED'
}, {
  qty: 3,
  nome: 'Refrigerante lata',
  preco: 21.0,
  status: 'SERVED'
}, {
  qty: 1,
  nome: 'Fritas com cheddar',
  preco: 34.0,
  status: 'SERVED'
}, {
  qty: 1,
  nome: 'Porção de azeitona',
  preco: 12.0,
  status: 'CANCELLED',
  cancel: true
}];
const FORMAS = [['Dinheiro', 'payments'], ['Débito', 'credit_card'], ['Crédito', 'credit_card'], ['PIX', 'qr_code_2'], ['Mercado Pago', 'smartphone']];
const brl = v => 'R$ ' + v.toFixed(2).replace('.', ',');
Object.assign(window, {
  MESAS_CX,
  CONTA,
  FORMAS,
  brl
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/caixa/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/garcom/GarcomApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  Card,
  Input,
  SegmentedControl,
  StatusPill,
  OrderTimer,
  TableCard,
  OrderLine,
  SyncStatus,
  BrandMark,
  AlertBanner,
  NumericKeypad,
  StatTile,
  QuantityStepper,
  EmptyState
} = window.NexoraDesignSystem_aa692a;
function Shell({
  title,
  sub,
  onBack,
  right,
  children,
  footer,
  pad = true
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      height: '100%',
      background: 'var(--surface-page)'
    }
  }, /*#__PURE__*/React.createElement("header", {
    style: {
      flex: '0 0 auto',
      background: 'var(--nx-navy-900)',
      color: '#fff',
      padding: '12px 14px',
      display: 'flex',
      alignItems: 'center',
      gap: 10
    }
  }, onBack ? /*#__PURE__*/React.createElement("button", {
    onClick: onBack,
    "aria-label": "Voltar",
    style: {
      border: 0,
      background: 'rgba(255,255,255,.14)',
      color: '#fff',
      width: 36,
      height: 36,
      borderRadius: 10,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "arrow_back",
    size: 20
  })) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0,
      flex: '1 1 auto'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 17px/1.2 var(--font-sans)'
    }
  }, title), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      color: 'rgba(255,255,255,.66)',
      marginTop: 1
    }
  }, sub)), right), /*#__PURE__*/React.createElement("main", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: pad ? 14 : 0,
      display: 'flex',
      flexDirection: 'column',
      gap: 12
    }
  }, children), footer ? /*#__PURE__*/React.createElement("footer", {
    style: {
      flex: '0 0 auto',
      padding: '12px 14px 16px',
      background: 'var(--surface-card)',
      borderTop: '1px solid var(--border-subtle)'
    }
  }, footer) : null);
}
function Login({
  onEnter
}) {
  const [pin, setPin] = React.useState('');
  return /*#__PURE__*/React.createElement("div", {
    style: {
      height: '100%',
      background: 'var(--nx-navy-900)',
      display: 'flex',
      flexDirection: 'column',
      padding: '40px 24px 28px',
      color: '#fff'
    }
  }, /*#__PURE__*/React.createElement(BrandMark, {
    inverse: true,
    size: 26,
    subtitle: "Sal\xE3o \xB7 Terminal do gar\xE7om"
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 44,
      textAlign: 'center'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 22px/1.3 var(--font-sans)'
    }
  }, "Jonas Ribeiro"), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      color: 'rgba(255,255,255,.6)',
      marginTop: 4
    }
  }, "Dispositivo registrado \xB7 PIN de 4 d\xEDgitos")), /*#__PURE__*/React.createElement("div", {
    "data-surface": "kds",
    style: {
      marginTop: 32,
      background: 'transparent'
    }
  }, /*#__PURE__*/React.createElement(NumericKeypad, {
    dark: true,
    value: pin,
    onChange: setPin,
    onSubmit: onEnter,
    length: 4,
    showDots: true
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 'auto',
      display: 'flex',
      justifyContent: 'center'
    }
  }, /*#__PURE__*/React.createElement(SyncStatus, {
    state: "local",
    queued: 0
  })));
}
function Mapa({
  onMesa
}) {
  const [amb, setAmb] = React.useState('Salão');
  const att = MESAS.filter(m => m.att).length;
  return /*#__PURE__*/React.createElement(Shell, {
    title: "Mapa de mesas",
    sub: "Jonas \xB7 turno das 18:00",
    right: /*#__PURE__*/React.createElement(SyncStatus, {
      state: "local",
      queued: 4
    }),
    footer: /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      block: true,
      iconLeft: "qr_code_scanner"
    }, "Ler QR"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      block: true,
      iconLeft: "add"
    }, "Abrir mesa"))
  }, att ? /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "warning",
    title: att + ' mesas exigem ação agora',
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Ver")
  }, "Mesa 03 com item pronto na janela \xB7 Mesa 08 pediu a conta.") : null, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 10,
      alignItems: 'center'
    }
  }, /*#__PURE__*/React.createElement(SegmentedControl, {
    options: ['Salão', 'Varanda', 'Balcão'],
    value: amb,
    onChange: setAmb
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto',
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, "6 de 8 ocupadas")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 10
    }
  }, MESAS.map(m => /*#__PURE__*/React.createElement(TableCard, {
    key: m.n,
    name: m.n,
    status: m.s,
    elapsed: m.t,
    guests: m.g,
    total: m.v,
    waiter: m.w,
    attention: m.att,
    onClick: () => onMesa(m)
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    label: "Meu ticket m\xE9dio",
    value: "R$ 78",
    icon: "receipt",
    delta: "+6,2%",
    comparison: "vs. turno anterior"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Mesas no turno",
    value: "11",
    icon: "table_restaurant",
    comparison: "m\xE9dia 9"
  })));
}
function Mesa({
  mesa,
  onBack,
  onLancar
}) {
  const sub = COMANDA.reduce((s, i) => s + i.preco, 0);
  return /*#__PURE__*/React.createElement(Shell, {
    title: mesa.n,
    sub: (mesa.g || 0) + ' pessoas · aberta há ' + (mesa.t || '—'),
    onBack: onBack,
    right: /*#__PURE__*/React.createElement(StatusPill, {
      status: mesa.s,
      size: "lg"
    }),
    footer: /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      block: true,
      iconLeft: "request_quote"
    }, "Pedir conta"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      block: true,
      iconLeft: "add",
      onClick: onLancar
    }, "Lan\xE7ar item"))
  }, /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "success",
    title: "1 item pronto na janela h\xE1 2 min",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary",
      iconLeft: "check"
    }, "Entreguei")
  }, "Fritas com cheddar \u2014 comida esperando \xE9 qualidade perdida."), /*#__PURE__*/React.createElement(Card, {
    title: "Comanda",
    subtitle: COMANDA.length + ' itens',
    padding: "tight",
    actions: /*#__PURE__*/React.createElement(IconButton, {
      icon: "swap_horiz",
      label: "Transferir item",
      size: "sm"
    })
  }, COMANDA.map((i, x) => /*#__PURE__*/React.createElement(OrderLine, {
    key: x,
    qty: i.qty,
    name: i.nome,
    modifiers: i.mods,
    note: i.obs,
    price: brl(i.preco),
    status: /*#__PURE__*/React.createElement(StatusPill, {
      status: i.status
    }),
    actions: i.status === 'READY' ? /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "accent",
      iconLeft: "check"
    }, "Entregar") : null
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      paddingTop: 12,
      marginTop: 6,
      borderTop: '2px solid var(--border-default)',
      font: 'var(--fw-bold) 18px/1.2 var(--font-sans)'
    }
  }, /*#__PURE__*/React.createElement("span", null, "Consumo"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(sub)))), /*#__PURE__*/React.createElement(Card, {
    title: "Tempos desta mesa",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 16,
      flexWrap: 'wrap'
    }
  }, /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      textTransform: 'uppercase',
      letterSpacing: 'var(--ls-caps)',
      color: 'var(--text-muted)'
    }
  }, "Na fila"), /*#__PURE__*/React.createElement(OrderTimer, {
    seconds: 92,
    size: "sm"
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      textTransform: 'uppercase',
      letterSpacing: 'var(--ls-caps)',
      color: 'var(--text-muted)'
    }
  }, "Produ\xE7\xE3o"), /*#__PURE__*/React.createElement(OrderTimer, {
    seconds: 318,
    size: "sm"
  })), /*#__PURE__*/React.createElement("div", null, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      textTransform: 'uppercase',
      letterSpacing: 'var(--ls-caps)',
      color: 'var(--text-muted)'
    }
  }, "Na janela"), /*#__PURE__*/React.createElement(OrderTimer, {
    seconds: 132,
    warnAt: 120,
    lateAt: 240,
    size: "sm"
  })))));
}
function Lancamento({
  mesa,
  onBack
}) {
  const [sel, setSel] = React.useState([]);
  const total = sel.reduce((s, i) => s + i.preco * i.qty, 0);
  const add = p => setSel(s => {
    const i = s.findIndex(x => x.nome === p.nome);
    return i >= 0 ? s.map((x, j) => j === i ? {
      ...x,
      qty: x.qty + 1
    } : x) : [...s, {
      ...p,
      qty: 1
    }];
  });
  return /*#__PURE__*/React.createElement(Shell, {
    title: 'Lançar · ' + mesa.n,
    sub: "Favoritos = 8 itens mais vendidos",
    onBack: onBack,
    footer: /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "touch",
      block: true,
      iconLeft: "send",
      disabled: !sel.length,
      onClick: onBack
    }, "Enviar ", sel.length ? '· ' + brl(total) : '')
  }, /*#__PURE__*/React.createElement(Input, {
    size: "lg",
    icon: "search",
    placeholder: "Buscar produto ou c\xF3digo"
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 10
    }
  }, FAVORITOS.map(p => /*#__PURE__*/React.createElement("button", {
    key: p.nome,
    onClick: () => add(p),
    style: {
      minHeight: 72,
      textAlign: 'left',
      padding: '12px 14px',
      borderRadius: 'var(--brand-radius)',
      border: '1px solid var(--border-subtle)',
      background: 'var(--surface-card)',
      cursor: 'pointer',
      boxShadow: 'var(--shadow-subtle)'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-semibold) 15px/1.25 var(--font-sans)',
      color: 'var(--text-primary)'
    }
  }, p.nome), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-numeric)',
      color: 'var(--text-secondary)',
      marginTop: 4
    }
  }, brl(p.preco))))), sel.length ? /*#__PURE__*/React.createElement(Card, {
    title: "A enviar",
    padding: "tight"
  }, sel.map((i, x) => /*#__PURE__*/React.createElement(OrderLine, {
    key: x,
    qty: i.qty,
    name: i.nome,
    price: brl(i.preco * i.qty),
    actions: /*#__PURE__*/React.createElement(QuantityStepper, {
      size: "sm",
      value: i.qty,
      onChange: v => setSel(s => v <= 0 ? s.filter((_, j) => j !== x) : s.map((y, j) => j === x ? {
        ...y,
        qty: v
      } : y))
    })
  }))) : /*#__PURE__*/React.createElement(EmptyState, {
    icon: "touch_app",
    title: "Toque num favorito"
  }, "Dois toques por item \u2014 sem digita\xE7\xE3o em ambiente de press\xE3o."));
}
function GarcomApp() {
  const [tela, setTela] = React.useState('login');
  const [mesa, setMesa] = React.useState(null);
  if (tela === 'login') return /*#__PURE__*/React.createElement(Login, {
    onEnter: () => setTela('mapa')
  });
  if (tela === 'mesa') return /*#__PURE__*/React.createElement(Mesa, {
    mesa: mesa,
    onBack: () => setTela('mapa'),
    onLancar: () => setTela('lancar')
  });
  if (tela === 'lancar') return /*#__PURE__*/React.createElement(Lancamento, {
    mesa: mesa,
    onBack: () => setTela('mesa')
  });
  return /*#__PURE__*/React.createElement(Mapa, {
    onMesa: m => {
      setMesa(m);
      setTela(m.s === 'FREE' ? 'mapa' : 'mesa');
    }
  });
}
window.GarcomApp = GarcomApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/garcom/GarcomApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/garcom/data.jsx
try { (() => {
const MESAS = [{
  n: 'Mesa 01',
  s: 'OPEN',
  t: '12 min',
  g: 2,
  v: 'R$ 58,00',
  w: 'Jonas'
}, {
  n: 'Mesa 03',
  s: 'READY',
  t: '26 min',
  g: 4,
  v: 'R$ 164,80',
  w: 'Jonas',
  att: true
}, {
  n: 'Mesa 05',
  s: 'FREE'
}, {
  n: 'Mesa 07',
  s: 'OPEN',
  t: '42 min',
  g: 4,
  v: 'R$ 120,90',
  w: 'Jonas'
}, {
  n: 'Mesa 08',
  s: 'BILL_REQUESTED',
  t: '1h 04',
  g: 3,
  v: 'R$ 186,40',
  w: 'Rita',
  att: true
}, {
  n: 'Mesa 09',
  s: 'FREE'
}, {
  n: 'Mesa 11',
  s: 'OPEN',
  t: '8 min',
  g: 2,
  v: 'R$ 34,00',
  w: 'Rita'
}, {
  n: 'Mesa 12',
  s: 'PAID',
  t: '1h 18',
  g: 6,
  v: 'R$ 312,00',
  w: 'Jonas'
}];
const FAVORITOS = [{
  nome: 'Calabresa G',
  preco: 64.9
}, {
  nome: 'Mussarela G',
  preco: 58
}, {
  nome: 'Frango c/ catupiry G',
  preco: 69.9
}, {
  nome: 'Refri lata',
  preco: 7
}, {
  nome: 'Suco laranja',
  preco: 12
}, {
  nome: 'Fritas cheddar',
  preco: 34
}, {
  nome: 'Cerveja 600ml',
  preco: 16
}, {
  nome: 'Água 500ml',
  preco: 5
}];
const COMANDA = [{
  qty: 1,
  nome: 'Pizza G · Calabresa / Mussarela',
  mods: 'borda catupiry',
  obs: 'sem cebola',
  preco: 72.9,
  status: 'IN_OVEN'
}, {
  qty: 2,
  nome: 'Refrigerante lata',
  preco: 14,
  status: 'SERVED'
}, {
  qty: 1,
  nome: 'Fritas com cheddar',
  preco: 34,
  status: 'READY'
}];
const brl = v => 'R$ ' + v.toFixed(2).replace('.', ',');
Object.assign(window, {
  MESAS,
  FAVORITOS,
  COMANDA,
  brl
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/garcom/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kds/KdsApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  OrderTicket,
  OrderTimer,
  StatusPill,
  SyncStatus,
  BrandMark,
  SegmentedControl,
  StatTile,
  EmptyState,
  AlertBanner
} = window.NexoraDesignSystem_aa692a;
function Forno() {
  const ocupadas = FORNO.filter(Boolean).length;
  return /*#__PURE__*/React.createElement("div", {
    style: {
      background: 'var(--surface-card)',
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--brand-radius)',
      padding: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      marginBottom: 12
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "local_fire_department",
    size: 20,
    color: "var(--nx-warning-500)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-semibold) 15px/1 var(--font-sans)',
      color: 'var(--text-primary)'
    }
  }, "Forno"), /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto',
      font: 'var(--type-numeric)',
      color: 'var(--text-secondary)'
    }
  }, ocupadas, "/5")), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(5,1fr)',
      gap: 6
    }
  }, FORNO.map((p, i) => /*#__PURE__*/React.createElement("div", {
    key: i,
    style: {
      height: 58,
      borderRadius: 'var(--radius-md)',
      display: 'flex',
      flexDirection: 'column',
      alignItems: 'center',
      justifyContent: 'center',
      gap: 2,
      background: p ? 'var(--nx-time-warn-bg)' : 'var(--surface-sunken)',
      border: '1px solid ' + (p ? 'var(--nx-warning-500)' : 'var(--border-subtle)')
    }
  }, p ? /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-black) 18px/1 var(--font-mono)',
      color: 'var(--nx-warning-500)'
    }
  }, p.c), /*#__PURE__*/React.createElement("span", {
    style: {
      font: '400 11px/1 var(--font-mono)',
      color: 'var(--text-secondary)'
    }
  }, p.left)) : /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, "livre")))), ocupadas < 5 ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 12
    }
  }, /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "danger",
    title: "2 posi\xE7\xF5es livres com fila esperando"
  }, "Perda irrecuper\xE1vel de capacidade \u2014 carregue o forno agora.")) : null);
}
function AllDay() {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      background: 'var(--surface-card)',
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--brand-radius)',
      padding: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)',
      marginBottom: 10
    }
  }, "Contagem all-day"), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 8
    }
  }, ALLDAY.map(([n, q]) => /*#__PURE__*/React.createElement("div", {
    key: n,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-black) 22px/1 var(--font-mono)',
      color: 'var(--nx-cyan-400)',
      minWidth: 28,
      textAlign: 'right'
    }
  }, q), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-medium) 15px/1.2 var(--font-sans)',
      color: 'var(--text-primary)'
    }
  }, n)))));
}
function KdsApp() {
  const [praca, setPraca] = React.useState('Todas');
  const [feitos, setFeitos] = React.useState([]);
  const [cmd, setCmd] = React.useState('');
  const fila = FILA.filter(p => !feitos.includes(p.code));
  const concluir = c => setFeitos(f => [...f, c]);
  React.useEffect(() => {
    const h = e => {
      if (/^[0-9]$/.test(e.key)) setCmd(c => (c + e.key).slice(-2));
      if (e.key === 'Enter') {
        setCmd(c => {
          if (c) concluir(c);
          return '';
        });
      }
      if (e.key === 'Backspace') setCmd(c => c.slice(0, -1));
    };
    window.addEventListener('keydown', h);
    return () => window.removeEventListener('keydown', h);
  }, []);
  const atrasados = fila.filter(p => p.s >= 600).length;
  return /*#__PURE__*/React.createElement("div", {
    "data-surface": "kds",
    style: {
      height: '100vh',
      display: 'flex',
      flexDirection: 'column',
      background: 'var(--surface-page)',
      color: 'var(--text-primary)'
    }
  }, /*#__PURE__*/React.createElement("header", {
    style: {
      flex: '0 0 auto',
      height: 64,
      display: 'flex',
      alignItems: 'center',
      gap: 20,
      padding: '0 20px',
      background: 'var(--surface-card)',
      borderBottom: '1px solid var(--border-subtle)'
    }
  }, /*#__PURE__*/React.createElement(BrandMark, {
    inverse: true,
    size: 22,
    subtitle: "KDS \xB7 Pra\xE7a de pizzas"
  }), /*#__PURE__*/React.createElement(SegmentedControl, {
    options: ['Todas', 'Montagem', 'Forno', 'Bebidas'],
    value: praca,
    onChange: setPraca
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginLeft: 'auto',
      display: 'flex',
      alignItems: 'center',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'right'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "Na fila"), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 22px/1 var(--font-mono)'
    }
  }, fila.length)), /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'right'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "Atrasados"), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 22px/1 var(--font-mono)',
      color: atrasados ? 'var(--nx-time-late)' : 'var(--text-primary)'
    }
  }, atrasados)), /*#__PURE__*/React.createElement("div", {
    style: {
      textAlign: 'right'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "M\xE9dia 1h"), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 22px/1 var(--font-mono)'
    }
  }, "11:40")), /*#__PURE__*/React.createElement(SyncStatus, {
    state: "local",
    queued: 62
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      display: 'flex',
      minHeight: 0
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: 20
    }
  }, fila.length ? /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(auto-fill,minmax(316px,1fr))',
      gap: 16,
      alignItems: 'start'
    }
  }, fila.map(p => /*#__PURE__*/React.createElement(OrderTicket, {
    key: p.code,
    code: p.code,
    where: p.where,
    channel: p.ch,
    seconds: p.s,
    items: p.itens,
    fireAt: p.fire,
    footer: /*#__PURE__*/React.createElement(Button, {
      variant: "accent",
      size: "lg",
      iconLeft: "check",
      onClick: () => concluir(p.code)
    }, "Pronto \xB7 ", p.code)
  }))) : /*#__PURE__*/React.createElement(EmptyState, {
    icon: "restaurant",
    title: "Fila vazia",
    action: /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      onClick: () => setFeitos([])
    }, "Recarregar turno")
  }, "Nenhum item aguardando produ\xE7\xE3o nesta pra\xE7a.")), /*#__PURE__*/React.createElement("aside", {
    style: {
      flex: '0 0 300px',
      borderLeft: '1px solid var(--border-subtle)',
      padding: 20,
      display: 'flex',
      flexDirection: 'column',
      gap: 16,
      overflowY: 'auto'
    }
  }, /*#__PURE__*/React.createElement(Forno, null), /*#__PURE__*/React.createElement(AllDay, null), /*#__PURE__*/React.createElement("div", {
    style: {
      background: 'var(--surface-card)',
      border: '1px solid var(--border-subtle)',
      borderRadius: 'var(--brand-radius)',
      padding: 14
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)',
      marginBottom: 10
    }
  }, "Comando"), /*#__PURE__*/React.createElement("div", {
    style: {
      height: 58,
      borderRadius: 'var(--radius-md)',
      background: 'var(--surface-sunken)',
      border: '1px solid var(--border-default)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      font: 'var(--fw-black) 32px/1 var(--font-mono)',
      color: cmd ? 'var(--nx-cyan-400)' : 'var(--text-muted)',
      letterSpacing: '.1em'
    }
  }, cmd || '––'), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      color: 'var(--text-muted)',
      marginTop: 8,
      lineHeight: 1.5
    }
  }, "Digite o n\xFAmero do pedido e ", /*#__PURE__*/React.createElement("strong", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Enter"), " para concluir. Backspace apaga. Sem mouse, sem digita\xE7\xE3o livre."), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      gap: 8,
      marginTop: 12
    }
  }, /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    size: "sm",
    iconLeft: "block"
  }, "Falta insumo"), /*#__PURE__*/React.createElement(Button, {
    variant: "secondary",
    size: "sm",
    iconLeft: "refresh"
  }, "Refazer"))))));
}
window.KdsApp = KdsApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kds/KdsApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/kds/data.jsx
try { (() => {
const FILA = [{
  code: '38',
  where: 'Mesa 03',
  ch: 'DINE_IN',
  s: 742,
  fire: 'agora',
  itens: [{
    qty: 2,
    name: 'Pizza G · Mussarela',
    modifiers: 'bem assada'
  }, {
    qty: 1,
    name: 'Fritas com cheddar',
    done: true
  }]
}, {
  code: '39',
  where: 'Delivery #4821',
  ch: 'DELIVERY',
  s: 611,
  fire: 'agora',
  itens: [{
    qty: 1,
    name: 'Pizza G · Calabresa',
    modifiers: 'sem cebola · borda catupiry'
  }, {
    qty: 1,
    name: 'Refri 2L'
  }]
}, {
  code: '40',
  where: 'Mesa 07',
  ch: 'DINE_IN',
  s: 412,
  fire: 'em 2 min',
  itens: [{
    qty: 1,
    name: 'Pizza G · Frango c/ catupiry'
  }, {
    qty: 1,
    name: 'Pizza G · Portuguesa',
    modifiers: 'sem ovo'
  }]
}, {
  code: '41',
  where: 'Balcão',
  ch: 'COUNTER',
  s: 238,
  fire: 'em 4 min',
  itens: [{
    qty: 1,
    name: 'Pizza M · Mussarela',
    modifiers: 'massa fina'
  }]
}, {
  code: '42',
  where: 'Mesa 11',
  ch: 'DINE_IN',
  s: 96,
  fire: 'em 6 min',
  itens: [{
    qty: 1,
    name: 'Pizza G · Romeu e Julieta'
  }, {
    qty: 2,
    name: 'Suco de laranja'
  }]
}, {
  code: '43',
  where: 'Delivery #4822',
  ch: 'DELIVERY',
  s: 41,
  fire: 'em 8 min',
  itens: [{
    qty: 2,
    name: 'Pizza G · Calabresa',
    modifiers: 'uma sem cebola'
  }]
}];
const ALLDAY = [['Mussarela G', 5], ['Calabresa G', 4], ['Frango c/ catupiry G', 2], ['Portuguesa G', 2], ['Romeu e Julieta', 1], ['Fritas cheddar', 3]];
const FORNO = [{
  c: '38',
  left: '0:40'
}, {
  c: '39',
  left: '1:20'
}, {
  c: '40',
  left: '3:10'
}, null, null];
Object.assign(window, {
  FILA,
  ALLDAY,
  FORNO
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/kds/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/mesa/MesaApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  Card,
  Checkbox,
  QuantityStepper,
  SegmentedControl,
  StatusPill,
  OrderTimer,
  MenuItemCard,
  OrderLine,
  SyncStatus,
  BrandMark,
  AlertBanner,
  ProgressMeter,
  EmptyState
} = window.NexoraDesignSystem_aa692a;
function Chrome({
  children,
  footer,
  title,
  sub,
  onBack,
  right
}) {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      height: '100%',
      background: 'var(--brand-surface)'
    }
  }, /*#__PURE__*/React.createElement("header", {
    style: {
      flex: '0 0 auto',
      background: 'var(--brand-primary)',
      color: 'var(--brand-on-primary)',
      padding: '12px 16px 14px',
      display: 'flex',
      alignItems: 'center',
      gap: 12
    }
  }, onBack ? /*#__PURE__*/React.createElement("button", {
    onClick: onBack,
    "aria-label": "Voltar",
    style: {
      border: 0,
      background: 'rgba(255,255,255,.16)',
      color: '#fff',
      width: 36,
      height: 36,
      borderRadius: 10,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      cursor: 'pointer'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "arrow_back",
    size: 20
  })) : null, /*#__PURE__*/React.createElement("div", {
    style: {
      minWidth: 0,
      flex: '1 1 auto'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--fw-bold) 18px/1.2 var(--font-display)'
    }
  }, title), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      color: 'rgba(255,255,255,.78)',
      marginTop: 2
    }
  }, sub)), right), /*#__PURE__*/React.createElement("main", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: '16px',
      display: 'flex',
      flexDirection: 'column',
      gap: 12
    }
  }, children), footer ? /*#__PURE__*/React.createElement("footer", {
    style: {
      flex: '0 0 auto',
      padding: '12px 16px 18px',
      background: 'var(--surface-card)',
      borderTop: '1px solid var(--border-subtle)',
      boxShadow: '0 -6px 20px rgba(16,28,46,.06)'
    }
  }, footer) : null);
}
function Cardapio({
  cat,
  setCat,
  onOpen,
  cart,
  onCart
}) {
  const itens = PRODUTOS.filter(p => p.cat === cat);
  const total = cart.reduce((s, i) => s + i.preco * i.qty, 0);
  return /*#__PURE__*/React.createElement(Chrome, {
    title: "Dona Betinha",
    sub: "Mesa 07 \xB7 4 pessoas",
    right: /*#__PURE__*/React.createElement(SyncStatus, {
      state: "local",
      queued: 3
    }),
    footer: cart.length ? /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "touch",
      block: true,
      iconLeft: "shopping_cart",
      onClick: onCart
    }, "Ver pedido \xB7 ", brl(total)) : /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      block: true,
      iconLeft: "notifications_active"
    }, "Chamar gar\xE7om"))
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flex: '0 0 auto',
      gap: 8,
      overflowX: 'auto',
      margin: '0 -16px',
      padding: '0 16px 4px'
    }
  }, CATEGORIAS.map(c => /*#__PURE__*/React.createElement("button", {
    key: c,
    onClick: () => setCat(c),
    style: {
      flex: '0 0 auto',
      height: 38,
      padding: '0 16px',
      borderRadius: 999,
      cursor: 'pointer',
      border: '1px solid ' + (c === cat ? 'var(--brand-primary)' : 'var(--border-default)'),
      background: c === cat ? 'var(--brand-primary)' : 'var(--surface-card)',
      color: c === cat ? 'var(--brand-on-primary)' : 'var(--text-secondary)',
      font: 'var(--fw-semibold) 14px/1 var(--font-sans)',
      whiteSpace: 'nowrap'
    }
  }, c))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 8,
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "schedule",
    size: 16
  }), " Fila da cozinha agora: ", /*#__PURE__*/React.createElement("strong", {
    style: {
      color: 'var(--text-primary)'
    }
  }, "~14 min"), " \xB7 prazo calculado pela fila"), itens.map(p => /*#__PURE__*/React.createElement(MenuItemCard, {
    key: p.id,
    name: p.nome,
    description: p.desc,
    price: brl(p.preco),
    prepMinutes: p.prep,
    unavailable: p.esgotado,
    badge: p.tag ? /*#__PURE__*/React.createElement(Badge, {
      tone: "accent",
      size: "sm"
    }, p.tag) : null,
    onClick: () => onOpen(p)
  })));
}
function Produto({
  produto,
  onBack,
  onAdd
}) {
  const [qty, setQty] = React.useState(1);
  const [meio, setMeio] = React.useState(false);
  const [borda, setBorda] = React.useState(true);
  const extra = (borda ? 8 : 0) + (meio ? 4 : 0);
  return /*#__PURE__*/React.createElement(Chrome, {
    title: produto.nome,
    sub: produto.prep + ' min de preparo',
    onBack: onBack,
    footer: /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 12,
        alignItems: 'center'
      }
    }, /*#__PURE__*/React.createElement(QuantityStepper, {
      value: qty,
      onChange: setQty
    }), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "touch",
      block: true,
      iconLeft: "add_shopping_cart",
      onClick: () => onAdd({
        ...produto,
        qty,
        mods: [meio && 'meio a meio · Mussarela', borda && 'borda catupiry'].filter(Boolean).join(' · '),
        preco: produto.preco + extra
      })
    }, "Adicionar \xB7 ", brl((produto.preco + extra) * qty)))
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      height: 168,
      borderRadius: 'var(--brand-radius)',
      background: 'var(--surface-sunken)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      color: 'var(--text-disabled)',
      flexDirection: 'column',
      gap: 6
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "add_photo_alternate",
    size: 34
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--type-caption)'
    }
  }, "foto do produto \u2014 a fornecer pelo estabelecimento")), /*#__PURE__*/React.createElement("p", {
    style: {
      font: 'var(--type-body-lg)',
      color: 'var(--text-secondary)',
      margin: 0
    }
  }, produto.desc), /*#__PURE__*/React.createElement(Card, {
    title: "Meio a meio",
    subtitle: "Pre\xE7o da fra\xE7\xE3o de maior valor (RN-009)",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(Checkbox, {
    label: "Dividir em dois sabores",
    checked: meio,
    onChange: e => setMeio(e.target.checked)
  }), meio ? /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 8,
      paddingTop: 8,
      borderTop: '1px solid var(--border-subtle)'
    }
  }, PRODUTOS.filter(p => p.cat === 'Pizzas salgadas' && p.id !== produto.id && !p.esgotado).map(p => /*#__PURE__*/React.createElement(Checkbox, {
    key: p.id,
    type: "radio",
    name: "metade",
    label: '2ª metade · ' + p.nome,
    price: brl(p.preco),
    defaultChecked: p.id === 'p2'
  }))) : null), MODIFICADORES.map(g => /*#__PURE__*/React.createElement(Card, {
    key: g.grupo,
    title: g.grupo,
    padding: "tight"
  }, g.opcoes.map(o => /*#__PURE__*/React.createElement(Checkbox, {
    key: o.n,
    type: g.tipo === 'radio' ? 'radio' : 'checkbox',
    name: g.grupo,
    label: o.n,
    price: o.p ? '+ ' + brl(o.p) : null,
    checked: g.grupo === 'Borda' && o.n === 'Catupiry' ? borda : undefined,
    defaultChecked: g.tipo === 'radio' && o.n === 'Tradicional' ? true : undefined,
    onChange: g.grupo === 'Borda' && o.n === 'Catupiry' ? e => setBorda(e.target.checked) : undefined
  })))), /*#__PURE__*/React.createElement(Card, {
    title: "Observa\xE7\xE3o",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("textarea", {
    placeholder: "Ex.: massa bem assada, sem cebola",
    rows: 2,
    style: {
      width: '100%',
      border: '1px solid var(--border-default)',
      borderRadius: 'var(--radius-md)',
      padding: '10px 12px',
      font: 'var(--type-body)',
      resize: 'none',
      outline: 'none'
    }
  })));
}
function Pedido({
  cart,
  onBack,
  onSend,
  onQty
}) {
  const total = cart.reduce((s, i) => s + i.preco * i.qty, 0);
  return /*#__PURE__*/React.createElement(Chrome, {
    title: "Seu pedido",
    sub: cart.length + ' itens · Mesa 07',
    onBack: onBack,
    footer: /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "touch",
      block: true,
      iconLeft: "send",
      onClick: onSend
    }, "Enviar para a cozinha \xB7 ", brl(total))
  }, /*#__PURE__*/React.createElement(Card, {
    padding: "tight"
  }, cart.map((i, x) => /*#__PURE__*/React.createElement(OrderLine, {
    key: x,
    qty: i.qty,
    name: i.nome,
    modifiers: i.mods,
    price: brl(i.preco * i.qty),
    actions: /*#__PURE__*/React.createElement(QuantityStepper, {
      size: "sm",
      value: i.qty,
      onChange: v => onQty(x, v)
    })
  }))), /*#__PURE__*/React.createElement(Card, {
    title: "Sugest\xE3o para acompanhar",
    subtitle: "Baseada no que est\xE1 no pedido",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(MenuItemCard, {
    name: "Refrigerante lata 350ml",
    price: brl(7),
    prepMinutes: 1
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-body-lg)',
      padding: '0 4px'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Subtotal"), /*#__PURE__*/React.createElement("strong", {
    className: "nx-tnum"
  }, brl(total))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-caption)',
      color: 'var(--text-muted)',
      padding: '0 4px'
    }
  }, /*#__PURE__*/React.createElement("span", null, "Taxa de servi\xE7o 10% \u2014 opcional, aplicada no fechamento"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(total * .1))));
}
function Acompanhar({
  onConsumo
}) {
  const etapas = [['Recebido', 'check', 'done'], ['Em produção', 'restaurant', 'done'], ['No forno', 'local_fire_department', 'now'], ['Pronto', 'room_service', ''], ['Na mesa', 'table_restaurant', '']];
  return /*#__PURE__*/React.createElement(Chrome, {
    title: "Acompanhar",
    sub: "Mesa 07 \xB7 pedido #42",
    right: /*#__PURE__*/React.createElement(SyncStatus, {
      state: "local",
      queued: 1
    }),
    footer: /*#__PURE__*/React.createElement("div", {
      style: {
        display: 'flex',
        gap: 10
      }
    }, /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "lg",
      block: true,
      iconLeft: "notifications_active"
    }, "Chamar gar\xE7om"), /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "lg",
      block: true,
      iconLeft: "receipt_long",
      onClick: onConsumo
    }, "Ver consumo"))
  }, /*#__PURE__*/React.createElement(Card, {
    padding: "tight",
    style: {
      alignItems: 'center',
      textAlign: 'center',
      gap: 6,
      padding: '20px 16px'
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-overline)',
      letterSpacing: 'var(--ls-caps)',
      textTransform: 'uppercase',
      color: 'var(--text-muted)'
    }
  }, "No forno agora"), /*#__PURE__*/React.createElement(OrderTimer, {
    seconds: 318,
    warnAt: 600,
    lateAt: 900,
    size: "lg",
    showIcon: true
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, "previs\xE3o de sa\xEDda \xE0s 20:54 \xB7 recalculada pela fila"), /*#__PURE__*/React.createElement("div", {
    style: {
      width: '100%',
      marginTop: 10
    }
  }, /*#__PURE__*/React.createElement(ProgressMeter, {
    value: 318,
    max: 720,
    tone: "accent"
  }))), /*#__PURE__*/React.createElement(Card, {
    title: "Etapas",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 2
    }
  }, etapas.map(([l, ic, st]) => /*#__PURE__*/React.createElement("div", {
    key: l,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 12,
      padding: '10px 0',
      borderBottom: '1px solid var(--border-subtle)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      width: 32,
      height: 32,
      borderRadius: 999,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      flex: '0 0 auto',
      background: st === 'done' ? 'var(--nx-success-100)' : st === 'now' ? 'var(--nx-warning-100)' : 'var(--surface-sunken)',
      color: st === 'done' ? 'var(--nx-success-600)' : st === 'now' ? 'var(--nx-warning-600)' : 'var(--text-disabled)'
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: ic,
    size: 18,
    fill: st !== ''
  })), /*#__PURE__*/React.createElement("span", {
    style: {
      font: st === 'now' ? 'var(--fw-semibold) 16px/1.3 var(--font-sans)' : 'var(--type-body-lg)',
      color: st === '' ? 'var(--text-disabled)' : 'var(--text-primary)'
    }
  }, l), st === 'now' ? /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto'
    }
  }, /*#__PURE__*/React.createElement(StatusPill, {
    status: "IN_OVEN",
    live: true
  })) : st === 'done' ? /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto',
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, "20:41") : null)))));
}
function Consumo({
  onBack
}) {
  const sub = CONSUMO.reduce((s, i) => s + i.preco, 0);
  const [taxa, setTaxa] = React.useState(true);
  const [pedindo, setPedindo] = React.useState(false);
  return /*#__PURE__*/React.createElement(Chrome, {
    title: "Consumo da mesa",
    sub: "Mesa 07 \xB7 aberta \xE0s 20:12",
    onBack: onBack,
    footer: pedindo ? /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "touch",
      block: true,
      iconLeft: "hourglass_top",
      disabled: true
    }, "Conta solicitada \u2014 o caixa foi avisado") : /*#__PURE__*/React.createElement(Button, {
      variant: "primary",
      size: "touch",
      block: true,
      iconLeft: "request_quote",
      onClick: () => setPedindo(true)
    }, "Pedir a conta \xB7 ", brl(sub + (taxa ? sub * .1 : 0)))
  }, pedindo ? /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "success",
    title: "Conta solicitada"
  }, "O caixa e o gar\xE7om Jonas foram avisados. Forma de pagamento pode ser escolhida na mesa.") : null, /*#__PURE__*/React.createElement(Card, {
    padding: "tight"
  }, CONSUMO.map((i, x) => /*#__PURE__*/React.createElement(OrderLine, {
    key: x,
    qty: i.qty,
    name: i.nome,
    modifiers: i.mods,
    note: i.obs,
    price: brl(i.preco),
    status: /*#__PURE__*/React.createElement(StatusPill, {
      status: i.status
    })
  }))), /*#__PURE__*/React.createElement(Card, {
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-body)',
      padding: '4px 0'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "Subtotal"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(sub))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      alignItems: 'center',
      padding: '4px 0'
    }
  }, /*#__PURE__*/React.createElement(Checkbox, {
    label: "Taxa de servi\xE7o 10%",
    compact: true,
    checked: taxa,
    onChange: e => setTaxa(e.target.checked)
  }), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum",
    style: {
      font: 'var(--type-numeric)'
    }
  }, brl(sub * .1))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      justifyContent: 'space-between',
      paddingTop: 10,
      marginTop: 6,
      borderTop: '2px solid var(--border-default)',
      font: 'var(--fw-bold) 20px/1.2 var(--font-sans)'
    }
  }, /*#__PURE__*/React.createElement("span", null, "Total"), /*#__PURE__*/React.createElement("span", {
    className: "nx-tnum"
  }, brl(sub + (taxa ? sub * .1 : 0))))), /*#__PURE__*/React.createElement(Card, {
    title: "Dividir a conta",
    subtitle: "Calculado pelo sistema (RF-SAL-10)",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(SegmentedControl, {
    block: true,
    size: "lg",
    options: [{
      value: 'p',
      label: 'Por pessoa'
    }, {
      value: 'i',
      label: 'Por item'
    }, {
      value: 'v',
      label: 'Valor'
    }],
    value: "p",
    onChange: () => {}
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 12,
      display: 'flex',
      justifyContent: 'space-between',
      font: 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-secondary)'
    }
  }, "4 pessoas \xB7 cada uma"), /*#__PURE__*/React.createElement("strong", {
    className: "nx-tnum"
  }, brl((sub + (taxa ? sub * .1 : 0)) / 4)))));
}
function MesaApp() {
  const [tela, setTela] = React.useState('cardapio');
  const [cat, setCat] = React.useState('Pizzas salgadas');
  const [produto, setProduto] = React.useState(null);
  const [cart, setCart] = React.useState([]);
  const add = i => {
    setCart(c => [...c, i]);
    setTela('pedido');
  };
  const qty = (x, v) => setCart(c => v <= 0 ? c.filter((_, j) => j !== x) : c.map((i, j) => j === x ? {
    ...i,
    qty: v
  } : i));
  if (tela === 'produto') return /*#__PURE__*/React.createElement(Produto, {
    produto: produto,
    onBack: () => setTela('cardapio'),
    onAdd: add
  });
  if (tela === 'pedido') return /*#__PURE__*/React.createElement(Pedido, {
    cart: cart,
    onBack: () => setTela('cardapio'),
    onQty: qty,
    onSend: () => {
      setCart([]);
      setTela('acompanhar');
    }
  });
  if (tela === 'acompanhar') return /*#__PURE__*/React.createElement(Acompanhar, {
    onConsumo: () => setTela('consumo')
  });
  if (tela === 'consumo') return /*#__PURE__*/React.createElement(Consumo, {
    onBack: () => setTela('acompanhar')
  });
  return /*#__PURE__*/React.createElement(Cardapio, {
    cat: cat,
    setCat: setCat,
    cart: cart,
    onCart: () => setTela('pedido'),
    onOpen: p => {
      setProduto(p);
      setTela('produto');
    }
  });
}
window.MesaApp = MesaApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/mesa/MesaApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/mesa/data.jsx
try { (() => {
const CATEGORIAS = ['Pizzas salgadas', 'Pizzas doces', 'Porções', 'Bebidas', 'Sobremesas'];
const PRODUTOS = [{
  id: 'p1',
  cat: 'Pizzas salgadas',
  nome: 'Calabresa G',
  desc: 'Molho de tomate, mussarela, calabresa fatiada, cebola',
  preco: 64.9,
  prep: 12,
  tag: 'Mais vendida'
}, {
  id: 'p2',
  cat: 'Pizzas salgadas',
  nome: 'Mussarela G',
  desc: 'Molho de tomate, mussarela, orégano',
  preco: 58.0,
  prep: 11
}, {
  id: 'p3',
  cat: 'Pizzas salgadas',
  nome: 'Frango com catupiry G',
  desc: 'Frango desfiado, catupiry, milho',
  preco: 69.9,
  prep: 13
}, {
  id: 'p4',
  cat: 'Pizzas salgadas',
  nome: 'Portuguesa G',
  desc: 'Presunto, ovo, cebola, azeitona, mussarela',
  preco: 72.0,
  prep: 13,
  esgotado: true
}, {
  id: 'p5',
  cat: 'Porções',
  nome: 'Fritas com cheddar',
  desc: 'Porção 400g, cheddar e bacon',
  preco: 34.0,
  prep: 8
}, {
  id: 'p6',
  cat: 'Bebidas',
  nome: 'Refrigerante lata 350ml',
  desc: 'Cola, guaraná ou laranja',
  preco: 7.0,
  prep: 1
}, {
  id: 'p7',
  cat: 'Bebidas',
  nome: 'Suco de laranja 500ml',
  desc: 'Natural, sem açúcar',
  preco: 12.0,
  prep: 3
}, {
  id: 'p8',
  cat: 'Sobremesas',
  nome: 'Pizza doce Romeu e Julieta',
  desc: 'Goiabada cremosa e queijo minas',
  preco: 48.0,
  prep: 10
}];
const MODIFICADORES = [{
  grupo: 'Ponto da massa',
  tipo: 'radio',
  opcoes: [{
    n: 'Tradicional',
    p: 0
  }, {
    n: 'Fina',
    p: 0
  }, {
    n: 'Bem assada',
    p: 0
  }]
}, {
  grupo: 'Borda',
  tipo: 'check',
  opcoes: [{
    n: 'Catupiry',
    p: 8
  }, {
    n: 'Cheddar',
    p: 8
  }, {
    n: 'Chocolate',
    p: 10
  }]
}, {
  grupo: 'Remover',
  tipo: 'check',
  opcoes: [{
    n: 'Sem cebola',
    p: 0
  }, {
    n: 'Sem azeitona',
    p: 0
  }, {
    n: 'Sem orégano',
    p: 0
  }]
}];
const CONSUMO = [{
  qty: 1,
  nome: 'Pizza G · Calabresa / Mussarela',
  mods: 'meio a meio · borda catupiry',
  obs: 'sem cebola',
  preco: 72.9,
  status: 'IN_OVEN'
}, {
  qty: 2,
  nome: 'Refrigerante lata 350ml',
  preco: 14.0,
  status: 'SERVED'
}, {
  qty: 1,
  nome: 'Fritas com cheddar',
  preco: 34.0,
  status: 'READY'
}];
const brl = v => 'R$ ' + v.toFixed(2).replace('.', ',');
Object.assign(window, {
  CATEGORIAS,
  PRODUTOS,
  MODIFICADORES,
  CONSUMO,
  brl
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/mesa/data.jsx", error: String((e && e.message) || e) }); }

// ui_kits/painel-dono/PainelApp.jsx
try { (() => {
const {
  Button,
  IconButton,
  Badge,
  Icon,
  Card,
  SideNav,
  TopBar,
  SegmentedControl,
  StatTile,
  ProgressMeter,
  DataTable,
  StatusPill,
  OrderTimer,
  AlertBanner,
  SyncStatus,
  BrandMark,
  TableCard,
  Select
} = window.NexoraDesignSystem_aa692a;
function Pulso() {
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      background: 'var(--surface-inverse)',
      borderRadius: 'var(--brand-radius)',
      padding: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 10,
      marginBottom: 16
    }
  }, /*#__PURE__*/React.createElement(Icon, {
    name: "monitor_heart",
    size: 20,
    color: "var(--nx-green-400)"
  }), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-semibold) 15px/1 var(--font-sans)',
      color: '#fff'
    }
  }, "Pulso \u2014 agora"), /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto'
    }
  }, /*#__PURE__*/React.createElement(SyncStatus, {
    state: "online",
    lastSync: "h\xE1 4 s"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(5,1fr)',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    variant: "pulse",
    label: "Faturamento hoje",
    value: "R$ 4.180",
    delta: "+12,4%",
    comparison: "vs. mesma ter\xE7a"
  }), /*#__PURE__*/React.createElement(StatTile, {
    variant: "pulse",
    label: "Pedidos em atraso",
    value: "3",
    icon: "warning"
  }), /*#__PURE__*/React.createElement(StatTile, {
    variant: "pulse",
    label: "Tempo m\xE9dio \xB7 1h",
    value: "11:40",
    target: "\u2264 10 min"
  }), /*#__PURE__*/React.createElement(StatTile, {
    variant: "pulse",
    label: "Mesas ocupadas",
    value: "6/8",
    comparison: "ocupa\xE7\xE3o 75%"
  }), /*#__PURE__*/React.createElement(StatTile, {
    variant: "pulse",
    label: "Alertas abertos",
    value: "2",
    icon: "notifications_active"
  }))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 360px',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Pedidos em produ\xE7\xE3o",
    subtitle: "Cron\xF4metro por pedido \xB7 toda linha abre at\xE9 o evento de origem",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 8
    }
  }, [['38', 'Mesa 03', 742], ['39', 'Delivery #4821', 611], ['40', 'Mesa 07', 412], ['41', 'Balcão', 238], ['42', 'Mesa 11', 96]].map(([c, w, s]) => /*#__PURE__*/React.createElement("div", {
    key: c,
    style: {
      display: 'flex',
      alignItems: 'center',
      gap: 14,
      padding: '10px 12px',
      borderRadius: 'var(--radius-md)',
      background: 'var(--surface-page)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--fw-black) 20px/1 var(--font-mono)',
      color: 'var(--text-secondary)',
      minWidth: 28
    }
  }, c), /*#__PURE__*/React.createElement("span", {
    style: {
      font: 'var(--type-body)',
      flex: '1 1 auto'
    }
  }, w), /*#__PURE__*/React.createElement(StatusPill, {
    status: s > 600 ? 'LATE' : 'IN_OVEN'
  }), /*#__PURE__*/React.createElement(OrderTimer, {
    seconds: s,
    size: "sm"
  }), /*#__PURE__*/React.createElement(IconButton, {
    icon: "chevron_right",
    label: "Abrir pedido",
    size: "sm"
  }))))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "danger",
    title: "3 pedidos acima da meta",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Ver fila")
  }, "Pico das 21h com 1 pizzaiolo na montagem."), /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "warning",
    title: "Forno ocioso com fila h\xE1 4 min",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Ver KDS")
  }, "2 posi\xE7\xF5es livres e 6 pedidos esperando \u2014 perda de capacidade."), /*#__PURE__*/React.createElement(Card, {
    title: "Meta do dia",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(ProgressMeter, {
    value: 4180,
    max: 6000,
    display: "R$ 4.180",
    tone: "brand",
    caption: "de R$ 6.000 \xB7 faltam R$ 1.820",
    size: "lg"
  })), /*#__PURE__*/React.createElement(Card, {
    title: "Mesas",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 10
    }
  }, /*#__PURE__*/React.createElement(TableCard, {
    name: "Mesa 08",
    status: "BILL_REQUESTED",
    elapsed: "1h 04",
    guests: 3,
    total: "R$ 186,40",
    attention: true
  }), /*#__PURE__*/React.createElement(TableCard, {
    name: "Mesa 03",
    status: "READY",
    elapsed: "26 min",
    guests: 4,
    total: "R$ 164,80"
  }))))));
}
function Desempenho() {
  const max = Math.max(...DEMANDA.flat());
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(4,1fr)',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    label: "Tempo total m\xE9dio",
    value: "15:40",
    unit: "min",
    icon: "timer",
    delta: "-1,2 min",
    deltaDirection: "up",
    target: "\u2264 10 min"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Percentil 90",
    value: "23:10",
    unit: "min",
    icon: "show_chart",
    delta: "+2,4 min",
    deltaDirection: "down",
    comparison: "o cliente insatisfeito"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Ader\xEAncia ao prazo",
    value: "82",
    unit: "%",
    icon: "task_alt",
    delta: "+4 p.p.",
    target: "\u2265 85%"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Pizzas por hora (real)",
    value: "31",
    icon: "local_pizza",
    comparison: "teto te\xF3rico 42"
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Tempo por etapa",
    subtitle: "Onde est\xE1 o gargalo \u2014 m\xE9dia \xD7 padr\xE3o da ficha"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, ETAPAS.map(([n, v, alvo, u]) => /*#__PURE__*/React.createElement(ProgressMeter, {
    key: n,
    label: n,
    value: v,
    max: 9,
    display: v.toFixed(1) + ' ' + u,
    target: alvo,
    tone: v > alvo ? 'warning' : 'accent',
    caption: 'padrão ' + alvo + ' ' + u
  })))), /*#__PURE__*/React.createElement(Card, {
    title: "Mapa de calor da demanda",
    subtitle: "Pedidos por dia da semana e faixa hor\xE1ria"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '40px repeat(7,1fr)',
      gap: 3,
      font: 'var(--type-caption)'
    }
  }, /*#__PURE__*/React.createElement("span", null), HORAS.map(h => /*#__PURE__*/React.createElement("span", {
    key: h,
    style: {
      textAlign: 'center',
      color: 'var(--text-muted)'
    }
  }, h)), DIAS.map((d, i) => /*#__PURE__*/React.createElement(React.Fragment, {
    key: d
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: 'var(--text-muted)',
      alignSelf: 'center'
    }
  }, d), DEMANDA[i].map((v, j) => /*#__PURE__*/React.createElement("span", {
    key: j,
    title: v + ' pedidos',
    style: {
      height: 30,
      borderRadius: 4,
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
      background: 'color-mix(in oklab, var(--nx-navy-800) ' + Math.round(v / max * 100) + '%, var(--nx-gray-100))',
      color: v / max > .5 ? '#fff' : 'var(--text-secondary)',
      font: 'var(--fw-medium) 11px/1 var(--font-mono)'
    }
  }, v))))), /*#__PURE__*/React.createElement("div", {
    style: {
      marginTop: 14,
      font: 'var(--type-caption)',
      color: 'var(--text-muted)'
    }
  }, "Pico sustentado: sexta, 21h \u2014 base para escala de pessoal e promessa de prazo."))), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1fr 1fr 1fr',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Venda por canal",
    padding: "tight"
  }, [['Salão', 'R$ 78.240', 61, 'brand'], ['Delivery próprio', 'R$ 32.180', 25, 'accent'], ['iFood', 'R$ 18.000', 14, 'warning']].map(([n, v, p, t]) => /*#__PURE__*/React.createElement("div", {
    key: n,
    style: {
      marginBottom: 14
    }
  }, /*#__PURE__*/React.createElement(ProgressMeter, {
    label: n,
    value: p,
    display: v,
    tone: t,
    caption: p + '% do faturamento'
  })))), /*#__PURE__*/React.createElement(Card, {
    title: "Pessoas",
    padding: "tight"
  }, /*#__PURE__*/React.createElement(DataTable, {
    compact: true,
    columns: [{
      key: 'n',
      header: 'Garçom'
    }, {
      key: 'm',
      header: 'Mesas',
      numeric: true
    }, {
      key: 't',
      header: 'Ticket',
      numeric: true
    }],
    rows: [{
      n: 'Jonas',
      m: '128',
      t: 'R$ 96'
    }, {
      n: 'Rita',
      m: '116',
      t: 'R$ 88'
    }, {
      n: 'Pedro',
      m: '74',
      t: 'R$ 71'
    }]
  })), /*#__PURE__*/React.createElement(Card, {
    title: "Qualidade",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 14
    }
  }, /*#__PURE__*/React.createElement(ProgressMeter, {
    label: "Nota m\xE9dia",
    value: 4.6,
    max: 5,
    display: "4,6",
    tone: "success"
  }), /*#__PURE__*/React.createElement(ProgressMeter, {
    label: "Retrabalho (re-fire)",
    value: 2.1,
    max: 10,
    display: "2,1%",
    tone: "warning"
  }), /*#__PURE__*/React.createElement(ProgressMeter, {
    label: "Ruptura de item",
    value: 1.4,
    max: 10,
    display: "1,4%",
    tone: "accent"
  })))));
}
function Resultado() {
  const cls = {
    'Estrela': 'success',
    'Cavalo de batalha': 'warning',
    'Quebra-cabeça': 'info',
    'Abacaxi': 'danger'
  };
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 20
    }
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: 'repeat(4,1fr)',
      gap: 12
    }
  }, /*#__PURE__*/React.createElement(StatTile, {
    label: "CMV",
    value: "32,8",
    unit: "%",
    icon: "inventory_2",
    delta: "+2,1 p.p.",
    deltaDirection: "down",
    target: "\u2264 30%"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Custo de pessoal",
    value: "24,3",
    unit: "%",
    icon: "badge",
    comparison: "folha + encargos"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Prime cost",
    value: "57,1",
    unit: "%",
    icon: "functions",
    delta: "-1,4 p.p.",
    target: "\u2264 65%"
  }), /*#__PURE__*/React.createElement(StatTile, {
    label: "Ponto de equil\xEDbrio",
    value: "R$ 3.940",
    unit: "/dia",
    icon: "balance",
    comparison: "m\xE9dia realizada R$ 4.280"
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'grid',
      gridTemplateColumns: '1.4fr 1fr',
      gap: 20,
      alignItems: 'start'
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Engenharia de card\xE1pio",
    subtitle: "Volume \xD7 margem de contribui\xE7\xE3o \u2014 gerado da ficha t\xE9cnica",
    padding: "none",
    actions: /*#__PURE__*/React.createElement(Button, {
      variant: "secondary",
      size: "sm",
      iconLeft: "download"
    }, "Exportar")
  }, /*#__PURE__*/React.createElement(DataTable, {
    onRowClick: () => {},
    columns: [{
      key: 'p',
      header: 'Produto'
    }, {
      key: 'v',
      header: 'Vendidos',
      numeric: true
    }, {
      key: 'fat',
      header: 'Faturamento',
      numeric: true
    }, {
      key: 'cst',
      header: 'Custo/un',
      numeric: true
    }, {
      key: 'mg',
      header: 'Margem',
      numeric: true,
      render: r => r.mg.toFixed(1) + '%'
    }, {
      key: 'cl',
      header: 'Classe',
      render: r => /*#__PURE__*/React.createElement(Badge, {
        tone: cls[r.cl],
        size: "sm"
      }, r.cl)
    }],
    rows: CARDAPIO
  })), /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column',
      gap: 16
    }
  }, /*#__PURE__*/React.createElement(Card, {
    title: "Resultado do per\xEDodo",
    subtitle: "Julho \xB7 composi\xE7\xE3o",
    padding: "tight"
  }, /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      flexDirection: 'column'
    }
  }, RESULTADO.map(([n, v, p], i) => /*#__PURE__*/React.createElement("div", {
    key: n,
    style: {
      display: 'flex',
      alignItems: 'baseline',
      gap: 10,
      padding: '9px 0',
      borderTop: i ? '1px solid var(--border-subtle)' : 0,
      font: n.startsWith('(=)') ? 'var(--fw-bold) 15px/1.3 var(--font-sans)' : 'var(--type-body)'
    }
  }, /*#__PURE__*/React.createElement("span", {
    style: {
      color: n.startsWith('(−)') ? 'var(--text-secondary)' : 'var(--text-primary)'
    }
  }, n), /*#__PURE__*/React.createElement("span", {
    style: {
      marginLeft: 'auto',
      fontFamily: 'var(--font-mono)',
      fontVariantNumeric: 'tabular-nums',
      color: n === '(=) Resultado' ? 'var(--nx-success-600)' : 'var(--text-primary)'
    }
  }, v), /*#__PURE__*/React.createElement("span", {
    style: {
      width: 52,
      textAlign: 'right',
      font: 'var(--type-caption)',
      color: 'var(--text-muted)',
      fontFamily: 'var(--font-mono)'
    }
  }, p))))), /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "danger",
    title: "Camar\xE3o G com margem de 22,4%",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Reprecificar")
  }, "6 unidades no m\xEAs. Reformular a ficha ou tirar do card\xE1pio."), /*#__PURE__*/React.createElement(AlertBanner, {
    tone: "warning",
    title: "Diverg\xEAncia CMV te\xF3rico \xD7 real: 6,2%",
    actions: /*#__PURE__*/React.createElement(Button, {
      size: "sm",
      variant: "secondary"
    }, "Abrir contagem")
  }, "Mussarela: te\xF3rico 41,2 kg \xD7 real 36,8 kg. Porcionamento ou perda."))));
}
function PainelApp() {
  const [view, setView] = React.useState('pulso');
  const [per, setPer] = React.useState('Mês');
  return /*#__PURE__*/React.createElement("div", {
    style: {
      display: 'flex',
      height: '100vh',
      background: 'var(--surface-page)'
    }
  }, /*#__PURE__*/React.createElement(SideNav, {
    brand: /*#__PURE__*/React.createElement(BrandMark, {
      inverse: true,
      size: 22,
      subtitle: "Painel do dono"
    }),
    activeId: view,
    onSelect: setView,
    items: [{
      group: 'Tempo real'
    }, {
      id: 'pulso',
      label: 'Pulso',
      icon: 'monitor_heart',
      count: 2
    }, {
      group: 'Gestão'
    }, {
      id: 'desemp',
      label: 'Desempenho',
      icon: 'insights'
    }, {
      id: 'result',
      label: 'Resultado e custo',
      icon: 'account_balance_wallet'
    }, {
      id: 'estoque',
      label: 'Estoque e ficha',
      icon: 'inventory_2'
    }, {
      id: 'fin',
      label: 'Financeiro',
      icon: 'savings'
    }, {
      group: 'Configuração'
    }, {
      id: 'metas',
      label: 'Metas e limiares',
      icon: 'flag'
    }, {
      id: 'aud',
      label: 'Auditoria',
      icon: 'history'
    }],
    footer: /*#__PURE__*/React.createElement(SyncStatus, {
      state: "online",
      lastSync: "h\xE1 4 s"
    })
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      display: 'flex',
      flexDirection: 'column',
      minWidth: 0
    }
  }, /*#__PURE__*/React.createElement(TopBar, {
    title: view === 'desemp' ? 'Desempenho operacional' : view === 'result' ? 'Resultado e custo' : 'Pulso da operação',
    subtitle: "Dona Betinha \xB7 ter\xE7a, 22:48",
    right: /*#__PURE__*/React.createElement(React.Fragment, null, /*#__PURE__*/React.createElement(SegmentedControl, {
      options: ['Hoje', '7 dias', 'Mês'],
      value: per,
      onChange: setPer
    }), /*#__PURE__*/React.createElement(Select, {
      options: ['Todos os canais', 'Salão', 'Delivery próprio', 'iFood']
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "download",
      label: "Exportar"
    }), /*#__PURE__*/React.createElement(IconButton, {
      icon: "notifications",
      label: "Alertas",
      badge: 2
    }))
  }), /*#__PURE__*/React.createElement("div", {
    style: {
      flex: '1 1 auto',
      overflowY: 'auto',
      padding: 24
    }
  }, view === 'desemp' ? /*#__PURE__*/React.createElement(Desempenho, null) : view === 'result' ? /*#__PURE__*/React.createElement(Resultado, null) : /*#__PURE__*/React.createElement(Pulso, null))));
}
window.PainelApp = PainelApp;
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/painel-dono/PainelApp.jsx", error: String((e && e.message) || e) }); }

// ui_kits/painel-dono/data.jsx
try { (() => {
const ETAPAS = [['Fila (T0→T1)', 1.6, 3, 'min'], ['Montagem (T1→T2)', 3.2, 4, 'min'], ['Cocção (T2→T3)', 7.1, 7, 'min'], ['Finalização (T3→T4)', 1.4, 2, 'min'], ['Expedição (T4→T5)', 2.4, 2, 'min']];
const HORAS = ['17h', '18h', '19h', '20h', '21h', '22h', '23h'];
const DEMANDA = [[4, 6, 9, 14, 22, 18, 7], [3, 5, 8, 12, 19, 15, 6], [5, 8, 12, 19, 28, 24, 11], [7, 11, 17, 26, 38, 32, 15], [9, 14, 21, 31, 44, 39, 19], [6, 9, 13, 20, 29, 26, 12], [4, 6, 10, 15, 21, 17, 8]];
const DIAS = ['Seg', 'Ter', 'Qua', 'Qui', 'Sex', 'Sáb', 'Dom'];
const CARDAPIO = [{
  p: 'Calabresa G',
  v: 182,
  fat: 'R$ 11.812',
  cst: 'R$ 18,42',
  mg: 71.6,
  cl: 'Estrela'
}, {
  p: 'Mussarela G',
  v: 164,
  fat: 'R$ 9.512',
  cst: 'R$ 30,04',
  mg: 48.2,
  cl: 'Cavalo de batalha'
}, {
  p: 'Frango c/ catupiry G',
  v: 38,
  fat: 'R$ 2.656',
  cst: 'R$ 21,60',
  mg: 69.1,
  cl: 'Quebra-cabeça'
}, {
  p: 'Portuguesa G',
  v: 96,
  fat: 'R$ 6.912',
  cst: 'R$ 34,10',
  mg: 52.6,
  cl: 'Cavalo de batalha'
}, {
  p: 'Camarão G',
  v: 6,
  fat: 'R$ 588',
  cst: 'R$ 76,10',
  mg: 22.4,
  cl: 'Abacaxi'
}, {
  p: 'Romeu e Julieta',
  v: 24,
  fat: 'R$ 1.152',
  cst: 'R$ 12,80',
  mg: 73.3,
  cl: 'Quebra-cabeça'
}];
const RESULTADO = [['Receita bruta', 'R$ 128.420', ''], ['(−) CMV', 'R$ 42.120', '32,8%'], ['(−) Pessoal', 'R$ 31.180', '24,3%'], ['(=) Prime cost', 'R$ 73.300', '57,1%'], ['(−) Custo fixo', 'R$ 28.400', '22,1%'], ['(−) Taxa de cartão', 'R$ 2.184', '1,7%'], ['(=) Resultado', 'R$ 24.536', '19,1%']];
Object.assign(window, {
  ETAPAS,
  HORAS,
  DEMANDA,
  DIAS,
  CARDAPIO,
  RESULTADO
});
})(); } catch (e) { __ds_ns.__errors.push({ path: "ui_kits/painel-dono/data.jsx", error: String((e && e.message) || e) }); }

__ds_ns.Badge = __ds_scope.Badge;

__ds_ns.BrandMark = __ds_scope.BrandMark;

__ds_ns.NexoraLogo = __ds_scope.NexoraLogo;

__ds_ns.NexoraLoader = __ds_scope.NexoraLoader;

__ds_ns.NexoraSplash = __ds_scope.NexoraSplash;

__ds_ns.Button = __ds_scope.Button;

__ds_ns.Card = __ds_scope.Card;

__ds_ns.Icon = __ds_scope.Icon;

__ds_ns.IconButton = __ds_scope.IconButton;

__ds_ns.DataTable = __ds_scope.DataTable;

__ds_ns.ProgressMeter = __ds_scope.ProgressMeter;

__ds_ns.StatTile = __ds_scope.StatTile;

__ds_ns.AlertBanner = __ds_scope.AlertBanner;

__ds_ns.EmptyState = __ds_scope.EmptyState;

__ds_ns.OrderTimer = __ds_scope.OrderTimer;

__ds_ns.StatusPill = __ds_scope.StatusPill;

__ds_ns.SyncStatus = __ds_scope.SyncStatus;

__ds_ns.Checkbox = __ds_scope.Checkbox;

__ds_ns.Field = __ds_scope.Field;

__ds_ns.Input = __ds_scope.Input;

__ds_ns.NumericKeypad = __ds_scope.NumericKeypad;

__ds_ns.QuantityStepper = __ds_scope.QuantityStepper;

__ds_ns.Select = __ds_scope.Select;

__ds_ns.Switch = __ds_scope.Switch;

__ds_ns.SegmentedControl = __ds_scope.SegmentedControl;

__ds_ns.SideNav = __ds_scope.SideNav;

__ds_ns.TopBar = __ds_scope.TopBar;

__ds_ns.MenuItemCard = __ds_scope.MenuItemCard;

__ds_ns.OrderLine = __ds_scope.OrderLine;

__ds_ns.OrderTicket = __ds_scope.OrderTicket;

__ds_ns.TableCard = __ds_scope.TableCard;

})();
