import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('orderline',`
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
export function OrderLine({qty,name,modifiers,note,price,status,actions,cancelled=false,...rest}){
  return React.createElement('div',{className:'nxOl'+(cancelled?' nxOl--void':''),...rest},
    React.createElement('span',{className:'nxOl__q'},qty+'×'),
    React.createElement('span',{className:'nxOl__b'},
      React.createElement('span',{className:'nxOl__n'},name),
      modifiers?React.createElement('span',{className:'nxOl__m',style:{display:'block'}},modifiers):null,
      note?React.createElement('span',{className:'nxOl__m',style:{display:'flex',alignItems:'center',gap:'3px'}},React.createElement(Icon,{name:'edit_note',size:14}),note):null,
      status?React.createElement('span',{className:'nxOl__st'},status):null),
    price?React.createElement('span',{className:'nxOl__p'},price):null,
    actions?React.createElement('span',{className:'nxOl__a'},actions):null);
}
