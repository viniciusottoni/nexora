import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('switch',`
.nxSw{display:inline-flex;align-items:center;gap:var(--sp-5);cursor:pointer;font:var(--type-body);color:var(--text-primary);user-select:none}
.nxSw input{appearance:none;margin:0;width:44px;height:26px;flex:0 0 auto;border-radius:var(--radius-pill);background:var(--nx-gray-300);position:relative;cursor:pointer;transition:background var(--dur-fast) var(--ease-standard)}
.nxSw input::after{content:"";position:absolute;top:3px;left:3px;width:20px;height:20px;border-radius:50%;background:#fff;box-shadow:var(--shadow-subtle);transition:transform var(--dur-fast) var(--ease-standard)}
.nxSw input:checked{background:var(--nx-success-500)}
.nxSw input:checked::after{transform:translateX(18px)}
.nxSw input:focus-visible{box-shadow:var(--focus-ring)}
.nxSw__d{font:var(--type-caption);color:var(--text-muted);display:block;margin-top:2px}
`);
export function Switch({label,description,...rest}){
  return React.createElement('label',{className:'nxSw'},
    React.createElement('input',{type:'checkbox',role:'switch',...rest}),
    label?React.createElement('span',null,label,description?React.createElement('span',{className:'nxSw__d'},description):null):null);
}
