import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('alert',`
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
const IC={info:'info',success:'check_circle',warning:'warning',danger:'error',neutral:'notifications'};
export function AlertBanner({tone='info',title,children,actions,icon,...rest}){
  return React.createElement('div',{role:'status',className:'nxAl nxAl--'+tone,...rest},
    React.createElement(Icon,{name:icon||IC[tone],size:22,fill:true}),
    React.createElement('div',{style:{minWidth:0}},
      title?React.createElement('div',{className:'nxAl__t'},title):null,
      children?React.createElement('div',{className:'nxAl__b'},children):null),
    actions?React.createElement('div',{className:'nxAl__a'},actions):null);
}
