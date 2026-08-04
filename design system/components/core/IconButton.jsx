import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from './Icon.jsx';
injectCss('iconbtn',`
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
export function IconButton({icon,size='md',variant='ghost',badge,label,...rest}){
  const g=size==='lg'?24:size==='sm'?18:20;
  return React.createElement('button',{type:'button','aria-label':label,title:label,className:['nxIB','nxIB--'+size,'nxIB--'+variant].join(' '),...rest},
    React.createElement(Icon,{name:icon,size:g}),
    badge?React.createElement('span',{className:'nxIB__dot'},badge):null);
}
