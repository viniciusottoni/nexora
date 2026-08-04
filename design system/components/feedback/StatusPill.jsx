import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('status',`
.nxSt{display:inline-flex;align-items:center;gap:var(--sp-3);height:26px;padding:0 var(--sp-4) 0 var(--sp-3);border-radius:var(--radius-pill);font:var(--fw-semibold) var(--fs-12)/1 var(--font-sans);white-space:nowrap}
.nxSt--lg{height:34px;font-size:var(--fs-14);padding:0 var(--sp-5) 0 var(--sp-4)}
.nxSt__d{width:8px;height:8px;border-radius:50%;background:currentColor;flex:0 0 auto}
.nxSt--live .nxSt__d{animation:nx-pulse-alert 1.6s var(--ease-in-out) infinite}
`);
const MAP={
  FREE:['Livre','var(--text-secondary)','var(--surface-sunken)'],
  OPEN:['Ocupada','var(--nx-blue-600)','var(--nx-blue-100)'],
  QUEUED:['Na fila','var(--text-secondary)','var(--surface-sunken)'],
  FIRED:['Em produção','var(--nx-warning-600)','var(--nx-warning-100)'],
  IN_OVEN:['No forno','var(--nx-warning-600)','var(--nx-warning-100)'],
  OUT_OF_OVEN:['Fora do forno','var(--nx-cyan-600)','var(--nx-cyan-100)'],
  READY:['Pronto','var(--nx-success-600)','var(--nx-success-100)'],
  SERVED:['Entregue','var(--nx-teal-600)','var(--nx-teal-100)'],
  BILL_REQUESTED:['Conta pedida','var(--nx-navy-700)','var(--surface-brand-subtle)'],
  PAID:['Pago','var(--nx-success-600)','var(--nx-success-100)'],
  CLOSED:['Fechada','var(--text-secondary)','var(--surface-sunken)'],
  DISPATCHED:['Em rota','var(--nx-cyan-600)','var(--nx-cyan-100)'],
  DELIVERED:['Entregue','var(--nx-success-600)','var(--nx-success-100)'],
  CANCELLED:['Cancelado','var(--nx-danger-600)','var(--nx-danger-100)'],
  LATE:['Atrasado','var(--nx-danger-600)','var(--nx-danger-100)'],
  UNAVAILABLE:['Em falta','var(--nx-danger-600)','var(--nx-danger-100)']
};
export function StatusPill({status,label,size='md',live=false,...rest}){
  const m=MAP[status]||['—','var(--text-secondary)','var(--surface-sunken)'];
  return React.createElement('span',{className:['nxSt',size==='lg'?'nxSt--lg':'',live?'nxSt--live':''].filter(Boolean).join(' '),
    style:{color:m[1],background:m[2]},...rest},
    React.createElement('span',{className:'nxSt__d'}),label||m[0]);
}
