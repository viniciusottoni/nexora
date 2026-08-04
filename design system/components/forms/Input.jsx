import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('input',`
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
export function Input({size='md',icon,suffix,prefix,invalid=false,numeric=false,disabled,...rest}){
  return React.createElement('div',{className:['nxIn','nxIn--'+size,invalid?'nxIn--invalid':'',disabled?'nxIn--disabled':'',numeric?'nxIn--numeric':''].filter(Boolean).join(' ')},
    icon?React.createElement(Icon,{name:icon,size:18,color:'var(--text-muted)'}):null,
    prefix?React.createElement('span',{className:'nxIn__af'},prefix):null,
    React.createElement('input',{className:'nxIn__el',disabled,...rest}),
    suffix?React.createElement('span',{className:'nxIn__af'},suffix):null);
}
