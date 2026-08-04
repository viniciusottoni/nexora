import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('select',`
.nxSel{position:relative;display:flex;align-items:center;background:var(--surface-card);border:var(--border-1) solid var(--border-default);border-radius:var(--radius-md);transition:var(--transition-control)}
.nxSel:focus-within{border-color:var(--border-brand);box-shadow:var(--focus-ring)}
.nxSel--md{height:var(--density-desk-control)}.nxSel--lg{height:var(--density-touch-min)}
.nxSel__el{appearance:none;border:0;background:transparent;outline:none;font:var(--type-body);color:var(--text-primary);padding:0 var(--sp-9) 0 var(--sp-5);width:100%;height:100%;cursor:pointer}
.nxSel__ch{position:absolute;right:var(--sp-4);pointer-events:none;color:var(--text-muted)}
`);
export function Select({size='md',options=[],children,...rest}){
  return React.createElement('div',{className:'nxSel nxSel--'+size},
    React.createElement('select',{className:'nxSel__el',...rest},
      children||options.map(o=>{const v=typeof o==='string'?o:o.value,l=typeof o==='string'?o:o.label;
        return React.createElement('option',{key:v,value:v},l);})),
    React.createElement('span',{className:'nxSel__ch'},React.createElement(Icon,{name:'expand_more',size:20})));
}
