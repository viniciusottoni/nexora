import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('qty',`
.nxQty{display:inline-flex;align-items:center;border:var(--border-1) solid var(--border-default);border-radius:var(--radius-pill);background:var(--surface-card);overflow:hidden}
.nxQty button{width:44px;height:44px;border:0;background:transparent;color:var(--brand-primary);display:flex;align-items:center;justify-content:center;cursor:pointer;transition:var(--transition-control)}
.nxQty button:hover{background:var(--surface-sunken)}
.nxQty button[disabled]{color:var(--text-disabled);cursor:not-allowed}
.nxQty__v{min-width:36px;text-align:center;font:var(--fw-semibold) var(--fs-16)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxQty--sm button{width:32px;height:32px}.nxQty--sm .nxQty__v{min-width:26px;font-size:var(--fs-14)}
`);
export function QuantityStepper({value=1,min=0,max=99,onChange,size='md',...rest}){
  const set=v=>onChange&&onChange(Math.min(max,Math.max(min,v)));
  return React.createElement('div',{className:'nxQty'+(size==='sm'?' nxQty--sm':''),...rest},
    React.createElement('button',{type:'button','aria-label':'Diminuir',disabled:value<=min,onClick:()=>set(value-1)},React.createElement(Icon,{name:'remove',size:size==='sm'?16:20})),
    React.createElement('span',{className:'nxQty__v'},value),
    React.createElement('button',{type:'button','aria-label':'Aumentar',disabled:value>=max,onClick:()=>set(value+1)},React.createElement(Icon,{name:'add',size:size==='sm'?16:20})));
}
