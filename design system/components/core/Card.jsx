import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('card',`
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
export function Card({title,subtitle,actions,footer,children,elevation='card',interactive=false,padding='default',...rest}){
  return React.createElement('section',{className:['nxCard',elevation==='flat'?'nxCard--flat':'',elevation==='raised'?'nxCard--raised':'',interactive?'nxCard--interactive':''].filter(Boolean).join(' '),...rest},
    (title||actions)?React.createElement('header',{className:'nxCard__head'},
      React.createElement('div',null,
        React.createElement('div',{className:'nxCard__t'},title),
        subtitle?React.createElement('div',{className:'nxCard__s'},subtitle):null),
      actions?React.createElement('div',{style:{display:'flex',gap:'var(--sp-3)',alignItems:'center'}},actions):null):null,
    React.createElement('div',{className:'nxCard__body'+(padding==='tight'?' nxCard__body--tight':padding==='none'?' nxCard__body--none':'')},children),
    footer?React.createElement('footer',{className:'nxCard__foot'},footer):null);
}
