import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('empty',`
.nxEm{display:flex;flex-direction:column;align-items:center;justify-content:center;gap:var(--sp-4);padding:var(--sp-11) var(--sp-8);text-align:center;color:var(--text-muted)}
.nxEm__ic{width:56px;height:56px;border-radius:var(--radius-pill);background:var(--surface-sunken);display:flex;align-items:center;justify-content:center;color:var(--text-muted)}
.nxEm__t{font:var(--type-h3);color:var(--text-primary)}
.nxEm__b{font:var(--type-body);color:var(--text-muted);max-width:46ch;text-wrap:pretty}
`);
export function EmptyState({icon='inbox',title,children,action,...rest}){
  return React.createElement('div',{className:'nxEm',...rest},
    React.createElement('span',{className:'nxEm__ic'},React.createElement(Icon,{name:icon,size:28})),
    title?React.createElement('div',{className:'nxEm__t'},title):null,
    children?React.createElement('div',{className:'nxEm__b'},children):null,
    action?React.createElement('div',{style:{marginTop:'var(--sp-3)'}},action):null);
}
