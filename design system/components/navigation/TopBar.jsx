import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('topbar',`
.nxTop{height:var(--topbar-h);flex:0 0 auto;display:flex;align-items:center;gap:var(--sp-6);padding:0 var(--gutter-page);background:var(--surface-card);border-bottom:var(--border-1) solid var(--border-subtle)}
.nxTop--sunken{background:var(--surface-page)}
.nxTop--brand{background:var(--brand-primary);border-bottom-color:transparent;color:var(--brand-on-primary)}
.nxTop__t{font:var(--type-h2);color:inherit;white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.nxTop__s{font:var(--type-caption);color:var(--text-muted);margin-top:1px}
.nxTop--brand .nxTop__s{color:rgba(255,255,255,.72)}
.nxTop__sp{flex:1 1 auto}
.nxTop__r{display:flex;align-items:center;gap:var(--sp-4);flex:0 0 auto}
`);
export function TopBar({title,subtitle,left,right,variant='card',...rest}){
  return React.createElement('header',{className:'nxTop'+(variant==='sunken'?' nxTop--sunken':variant==='brand'?' nxTop--brand':''),...rest},
    left,
    (title||subtitle)?React.createElement('div',{style:{minWidth:0}},
      React.createElement('div',{className:'nxTop__t'},title),
      subtitle?React.createElement('div',{className:'nxTop__s'},subtitle):null):null,
    React.createElement('div',{className:'nxTop__sp'}),
    right?React.createElement('div',{className:'nxTop__r'},right):null);
}
