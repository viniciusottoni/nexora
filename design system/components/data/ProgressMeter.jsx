import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('meter',`
.nxMt{display:flex;flex-direction:column;gap:var(--sp-3);min-width:0}
.nxMt__top{display:flex;justify-content:space-between;align-items:baseline;gap:var(--sp-4)}
.nxMt__lab{font:var(--type-label);color:var(--text-secondary)}
.nxMt__val{font:var(--fw-semibold) var(--fs-14)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxMt__track{height:8px;border-radius:var(--radius-pill);background:var(--surface-sunken);overflow:hidden;position:relative}
.nxMt--lg .nxMt__track{height:14px}
.nxMt__fill{height:100%;border-radius:var(--radius-pill);transition:width var(--dur-slow) var(--ease-standard)}
.nxMt__fill--brand{background:var(--brand-primary)}
.nxMt__fill--success{background:var(--nx-success-500)}
.nxMt__fill--warning{background:var(--nx-warning-500)}
.nxMt__fill--danger{background:var(--nx-danger-500)}
.nxMt__fill--accent{background:var(--nx-teal-500)}
.nxMt__mark{position:absolute;top:-3px;bottom:-3px;width:2px;background:var(--text-primary);opacity:.55}
.nxMt__cap{font:var(--type-caption);color:var(--text-muted)}
`);
export function ProgressMeter({label,value=0,max=100,display,tone='brand',target,caption,size='md',...rest}){
  const pct=Math.max(0,Math.min(100,(value/max)*100));
  return React.createElement('div',{className:'nxMt'+(size==='lg'?' nxMt--lg':''),...rest},
    (label||display)?React.createElement('div',{className:'nxMt__top'},
      React.createElement('span',{className:'nxMt__lab'},label),
      display?React.createElement('span',{className:'nxMt__val'},display):null):null,
    React.createElement('div',{className:'nxMt__track',role:'meter','aria-valuenow':value,'aria-valuemax':max},
      React.createElement('div',{className:'nxMt__fill nxMt__fill--'+tone,style:{width:pct+'%'}}),
      target!=null?React.createElement('span',{className:'nxMt__mark',style:{left:Math.max(0,Math.min(100,(target/max)*100))+'%'}}):null),
    caption?React.createElement('span',{className:'nxMt__cap'},caption):null);
}
