import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('sidenav',`
.nxNav{width:var(--sidebar-w);flex:0 0 auto;background:var(--surface-inverse);color:#fff;display:flex;flex-direction:column;height:100%;min-height:0}
.nxNav--light{background:var(--surface-card);color:var(--text-primary);border-right:var(--border-1) solid var(--border-subtle)}
.nxNav__brand{padding:var(--sp-7) var(--sp-6);display:flex;align-items:center;gap:var(--sp-4)}
.nxNav__scroll{flex:1 1 auto;overflow-y:auto;padding:var(--sp-3) var(--sp-4) var(--sp-8)}
.nxNav__grp{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:rgba(255,255,255,.42);padding:var(--sp-6) var(--sp-4) var(--sp-3)}
.nxNav--light .nxNav__grp{color:var(--text-muted)}
.nxNav__i{display:flex;align-items:center;gap:var(--sp-5);height:42px;padding:0 var(--sp-4);border-radius:var(--radius-md);color:rgba(255,255,255,.76);font:var(--type-label);cursor:pointer;transition:var(--transition-control);border:0;background:transparent;width:100%;text-align:left}
.nxNav--light .nxNav__i{color:var(--text-secondary)}
.nxNav__i:hover{background:rgba(255,255,255,.08);color:#fff}
.nxNav--light .nxNav__i:hover{background:var(--surface-sunken);color:var(--text-primary)}
.nxNav__i--on{background:rgba(255,255,255,.14);color:#fff;font-weight:var(--fw-semibold)}
.nxNav--light .nxNav__i--on{background:var(--surface-brand-subtle);color:var(--nx-navy-800)}
.nxNav__c{margin-left:auto;font:var(--type-overline);background:var(--nx-danger-500);color:#fff;min-width:18px;height:18px;border-radius:var(--radius-pill);display:flex;align-items:center;justify-content:center;padding:0 5px}
.nxNav__foot{padding:var(--sp-5) var(--sp-6);border-top:var(--border-1) solid rgba(255,255,255,.1)}
.nxNav--light .nxNav__foot{border-color:var(--border-subtle)}
`);
export function SideNav({brand,items=[],activeId,onSelect,footer,variant='dark',...rest}){
  return React.createElement('nav',{className:'nxNav'+(variant==='light'?' nxNav--light':''),...rest},
    brand?React.createElement('div',{className:'nxNav__brand'},brand):null,
    React.createElement('div',{className:'nxNav__scroll'},items.map((it,i)=>
      it.group?React.createElement('div',{key:'g'+i,className:'nxNav__grp'},it.group)
        :React.createElement('button',{key:it.id,type:'button',
            className:'nxNav__i'+(it.id===activeId?' nxNav__i--on':''),
            onClick:()=>onSelect&&onSelect(it.id)},
            React.createElement(Icon,{name:it.icon,size:20,fill:it.id===activeId}),
            React.createElement('span',null,it.label),
            it.count?React.createElement('span',{className:'nxNav__c'},it.count):null))),
    footer?React.createElement('div',{className:'nxNav__foot'},footer):null);
}
