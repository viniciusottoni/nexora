import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('stat',`
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
export function StatTile({label,value,unit,delta,deltaDirection,comparison,target,icon,size='md',variant='card',...rest}){
  const dir=deltaDirection||(delta==null?'flat':String(delta).trim().startsWith('-')?'down':'up');
  return React.createElement('div',{className:['nxStat','nxStat--'+size,variant==='pulse'?'nxStat--pulse':variant==='flat'?'nxStat--flat':''].filter(Boolean).join(' '),...rest},
    React.createElement('div',{className:'nxStat__lab'},icon?React.createElement(Icon,{name:icon,size:14}):null,label),
    React.createElement('div',{className:'nxStat__v'},value,unit?React.createElement('span',{className:'nxStat__u'},unit):null),
    (delta!=null||comparison||target)?React.createElement('div',{className:'nxStat__foot'},
      delta!=null?React.createElement('span',{className:'nxStat__d nxStat__d--'+dir},
        React.createElement(Icon,{name:dir==='up'?'arrow_upward':dir==='down'?'arrow_downward':'remove',size:14}),delta):null,
      comparison?React.createElement('span',{className:'nxStat__cmp'},comparison):null,
      target?React.createElement('span',{className:'nxStat__tgt'},'meta ',target):null):null);
}
