import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from './Icon.jsx';
injectCss('btn',`
.nxBtn{display:inline-flex;align-items:center;justify-content:center;gap:var(--sp-3);font-family:var(--font-sans);font-weight:var(--fw-semibold);border:var(--border-1) solid transparent;border-radius:var(--brand-radius);cursor:pointer;transition:var(--transition-control);text-decoration:none;white-space:nowrap}
.nxBtn:focus-visible{outline:none;box-shadow:var(--focus-ring)}
.nxBtn[disabled]{cursor:not-allowed;opacity:.45}
.nxBtn--sm{height:var(--density-desk-control);padding:0 var(--sp-5);font-size:var(--fs-13)}
.nxBtn--md{height:40px;padding:0 var(--sp-6);font-size:var(--fs-14)}
.nxBtn--lg{height:var(--density-touch-min);padding:0 var(--sp-7);font-size:var(--fs-16)}
.nxBtn--touch{height:var(--density-touch-lg);padding:0 var(--sp-8);font-size:var(--fs-18)}
.nxBtn--block{width:100%}
.nxBtn--primary{background:var(--brand-primary);color:var(--brand-on-primary)}
.nxBtn--primary:hover:not([disabled]){background:var(--brand-primary-hover,var(--action-primary-hover))}
.nxBtn--primary:active:not([disabled]){background:var(--brand-primary-active,var(--action-primary-active))}
.nxBtn--accent{background:var(--action-accent);color:var(--action-accent-text)}
.nxBtn--accent:hover:not([disabled]){background:var(--action-accent-hover)}
.nxBtn--secondary{background:var(--surface-card);color:var(--text-primary);border-color:var(--border-default);box-shadow:var(--shadow-subtle)}
.nxBtn--secondary:hover:not([disabled]){background:var(--surface-sunken);border-color:var(--border-strong)}
.nxBtn--ghost{background:transparent;color:var(--text-secondary)}
.nxBtn--ghost:hover:not([disabled]){background:var(--surface-sunken);color:var(--text-primary)}
.nxBtn--danger{background:var(--nx-danger-600);color:#fff}
.nxBtn--danger:hover:not([disabled]){background:var(--nx-danger-700)}
.nxBtn--danger:focus-visible{box-shadow:var(--focus-ring-danger)}
.nxBtn:active:not([disabled]){transform:translateY(1px)}
`);
export function Button({children,variant='primary',size='md',iconLeft,iconRight,block=false,as='button',...rest}){
  const cls=['nxBtn','nxBtn--'+variant,'nxBtn--'+size,block?'nxBtn--block':''].filter(Boolean).join(' ');
  const g=size==='touch'?24:size==='lg'?22:18;
  return React.createElement(as,{className:cls,...rest},
    iconLeft?React.createElement(Icon,{name:iconLeft,size:g}):null,
    children,
    iconRight?React.createElement(Icon,{name:iconRight,size:g}):null);
}
