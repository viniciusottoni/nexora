import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
import {StatusPill} from '../feedback/StatusPill.jsx';
injectCss('tablecard',`
.nxTc{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);padding:var(--sp-5);display:flex;flex-direction:column;gap:var(--sp-4);cursor:pointer;transition:var(--transition-control),box-shadow var(--dur-fast) var(--ease-standard);text-align:left;min-height:132px;box-shadow:var(--shadow-subtle)}
.nxTc:hover{box-shadow:var(--shadow-raised);border-color:var(--border-default)}
.nxTc__top{display:flex;align-items:center;justify-content:space-between;gap:var(--sp-4)}
.nxTc__n{font:var(--fw-bold) var(--fs-20)/1 var(--font-display);color:var(--text-primary)}
.nxTc__meta{display:flex;align-items:center;gap:var(--sp-5);font:var(--type-caption);color:var(--text-muted)}
.nxTc__meta span{display:inline-flex;align-items:center;gap:3px}
.nxTc__v{margin-top:auto;font:var(--fw-bold) var(--fs-18)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxTc--attention{border-color:var(--nx-danger-500);box-shadow:0 0 0 1px var(--nx-danger-500)}
.nxTc--free{background:var(--surface-page);border-style:dashed;box-shadow:none}
.nxTc--free .nxTc__n{color:var(--text-muted)}
`);
export function TableCard({name,status='FREE',elapsed,guests,total,waiter,attention=false,...rest}){
  return React.createElement('button',{type:'button',className:['nxTc',status==='FREE'?'nxTc--free':'',attention?'nxTc--attention':''].filter(Boolean).join(' '),...rest},
    React.createElement('div',{className:'nxTc__top'},
      React.createElement('span',{className:'nxTc__n'},name),
      React.createElement(StatusPill,{status,live:attention})),
    React.createElement('div',{className:'nxTc__meta'},
      guests?React.createElement('span',null,React.createElement(Icon,{name:'group',size:14}),guests):null,
      elapsed?React.createElement('span',null,React.createElement(Icon,{name:'schedule',size:14}),elapsed):null,
      waiter?React.createElement('span',null,React.createElement(Icon,{name:'room_service',size:14}),waiter):null),
    total?React.createElement('div',{className:'nxTc__v'},total):null);
}
