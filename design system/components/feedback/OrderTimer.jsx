import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('timer',`
.nxTm{display:inline-flex;align-items:center;gap:var(--sp-3);font-family:var(--font-mono);font-variant-numeric:tabular-nums;font-weight:var(--fw-bold);border-radius:var(--radius-md);line-height:1}
.nxTm--sm{font-size:var(--fs-14);padding:var(--sp-2) var(--sp-3)}
.nxTm--md{font-size:var(--fs-20);padding:var(--sp-3) var(--sp-4)}
.nxTm--lg{font-size:var(--fs-42);padding:var(--sp-3) var(--sp-5)}
.nxTm--late{animation:nx-pulse-alert 1.2s var(--ease-in-out) infinite}
`);
function fmt(s){const m=Math.floor(Math.abs(s)/60),r=Math.abs(s)%60;return (s<0?'-':'')+m+':'+String(r).padStart(2,'0');}
export function OrderTimer({seconds=0,warnAt=300,lateAt=600,size='md',showIcon=false,onDark=false,...rest}){
  const state=seconds>=lateAt?'late':seconds>=warnAt?'warn':'ok';
  const fg={ok:'var(--nx-time-ok)',warn:'var(--nx-time-warn)',late:'var(--nx-time-late)'}[state];
  const bg=onDark?{ok:'var(--nx-time-ok-bg)',warn:'var(--nx-time-warn-bg)',late:'var(--nx-time-late-bg)'}[state]
                 :{ok:'var(--nx-success-100)',warn:'var(--nx-warning-100)',late:'var(--nx-danger-100)'}[state];
  return React.createElement('span',{className:['nxTm','nxTm--'+size,state==='late'?'nxTm--late':''].filter(Boolean).join(' '),
    style:{color:fg,background:bg},...rest},
    showIcon?React.createElement(Icon,{name:'timer',size:size==='lg'?32:size==='sm'?14:18}):null,fmt(seconds));
}
