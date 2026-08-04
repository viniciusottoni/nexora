import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('sync',`
.nxSy{display:inline-flex;align-items:center;gap:var(--sp-3);height:28px;padding:0 var(--sp-4);border-radius:var(--radius-pill);font:var(--fw-medium) var(--fs-12)/1 var(--font-sans);border:var(--border-1) solid transparent;white-space:nowrap}
.nxSy--online{background:var(--nx-success-100);color:var(--nx-success-600)}
.nxSy--local{background:var(--nx-warning-100);color:var(--nx-warning-600)}
.nxSy--delayed{background:var(--nx-danger-100);color:var(--nx-danger-600)}
.nxSy__q{font-family:var(--font-mono);opacity:.85}
`);
const TXT={online:['cloud_done','Sincronizado'],local:['wifi_off','Modo local'],delayed:['sync_problem','Sync atrasada']};
export function SyncStatus({state='online',lastSync,queued,...rest}){
  const t=TXT[state]||TXT.online;
  return React.createElement('span',{className:'nxSy nxSy--'+state,title:lastSync?'Última sincronização '+lastSync:undefined,...rest},
    React.createElement(Icon,{name:t[0],size:16}),t[1],
    lastSync?React.createElement('span',{className:'nxSy__q'},'· '+lastSync):null,
    queued?React.createElement('span',{className:'nxSy__q'},'· '+queued+' na fila'):null);
}
