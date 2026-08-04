import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('seg',`
.nxSg{display:inline-flex;background:var(--surface-sunken);border-radius:var(--radius-md);padding:3px;gap:2px}
.nxSg button{border:0;background:transparent;color:var(--text-secondary);font:var(--type-label);padding:0 var(--sp-5);height:30px;border-radius:var(--radius-sm);cursor:pointer;transition:var(--transition-control);display:inline-flex;align-items:center;gap:var(--sp-3);white-space:nowrap}
.nxSg button:hover{color:var(--text-primary)}
.nxSg button[aria-pressed="true"]{background:var(--surface-card);color:var(--text-primary);font-weight:var(--fw-semibold);box-shadow:var(--shadow-subtle)}
.nxSg--lg button{height:42px;font-size:var(--fs-16);padding:0 var(--sp-7)}
.nxSg--block{display:flex}.nxSg--block button{flex:1 1 0;justify-content:center}
`);
export function SegmentedControl({options=[],value,onChange,size='md',block=false,...rest}){
  return React.createElement('div',{role:'group',className:['nxSg',size==='lg'?'nxSg--lg':'',block?'nxSg--block':''].filter(Boolean).join(' '),...rest},
    options.map(o=>{const v=typeof o==='string'?o:o.value,l=typeof o==='string'?o:o.label;
      return React.createElement('button',{key:v,type:'button','aria-pressed':v===value,onClick:()=>onChange&&onChange(v)},
        (o.icon?React.createElement(Icon,{key:'i',name:o.icon,size:18}):null),l);}));
}
