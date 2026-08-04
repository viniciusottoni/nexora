import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from './Icon.jsx';
injectCss('badge',`
.nxBadge{display:inline-flex;align-items:center;gap:var(--sp-2);border-radius:var(--radius-pill);font-family:var(--font-sans);font-weight:var(--fw-semibold);white-space:nowrap;border:var(--border-1) solid transparent}
.nxBadge--sm{height:20px;padding:0 var(--sp-3);font-size:var(--fs-11)}
.nxBadge--md{height:26px;padding:0 var(--sp-4);font-size:var(--fs-12)}
.nxBadge--lg{height:32px;padding:0 var(--sp-5);font-size:var(--fs-14)}
.nxBadge--neutral{background:var(--surface-sunken);color:var(--text-secondary);border-color:var(--border-subtle)}
.nxBadge--brand{background:var(--surface-brand-subtle);color:var(--nx-navy-700)}
.nxBadge--info{background:var(--nx-blue-100);color:var(--nx-blue-600)}
.nxBadge--success{background:var(--nx-success-100);color:var(--nx-success-600)}
.nxBadge--warning{background:var(--nx-warning-100);color:var(--nx-warning-600)}
.nxBadge--danger{background:var(--nx-danger-100);color:var(--nx-danger-600)}
.nxBadge--accent{background:var(--nx-teal-100);color:var(--nx-teal-600)}
.nxBadge--solid{background:var(--nx-navy-800);color:#fff}
.nxBadge--square{border-radius:var(--radius-sm)}
`);
export function Badge({children,tone='neutral',size='md',icon,square=false,...rest}){
  return React.createElement('span',{className:['nxBadge','nxBadge--'+tone,'nxBadge--'+size,square?'nxBadge--square':''].filter(Boolean).join(' '),...rest},
    icon?React.createElement(Icon,{name:icon,size:size==='sm'?12:14}):null,children);
}
