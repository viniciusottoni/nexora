import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('check',`
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
export function Checkbox({label,type='checkbox',price,compact=false,...rest}){
  return React.createElement('label',{className:['nxCk',type==='radio'?'nxCk--radio':'',compact?'nxCk--compact':''].filter(Boolean).join(' ')},
    React.createElement('input',{type,...rest}),
    React.createElement('span',null,label),
    price?React.createElement('span',{className:'nxCk__price'},price):null);
}
