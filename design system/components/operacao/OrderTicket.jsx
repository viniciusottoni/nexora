import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
import {OrderTimer} from '../feedback/OrderTimer.jsx';
injectCss('ticket',`
.nxTk{background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);display:flex;flex-direction:column;overflow:hidden;min-width:0}
.nxTk__h{display:flex;align-items:center;justify-content:space-between;gap:var(--sp-4);padding:var(--sp-5) var(--sp-5);border-bottom:var(--border-1) solid var(--border-subtle)}
.nxTk__id{display:flex;align-items:baseline;gap:var(--sp-4);min-width:0}
.nxTk__code{font:var(--fw-black) var(--fs-28)/1 var(--font-mono);color:var(--text-primary)}
.nxTk__where{font:var(--fw-semibold) var(--fs-14)/1 var(--font-sans);color:var(--text-secondary);white-space:nowrap;overflow:hidden;text-overflow:ellipsis}
.nxTk__items{list-style:none;margin:0;padding:var(--sp-4) 0;flex:1 1 auto}
.nxTk__it{display:flex;gap:var(--sp-4);padding:var(--sp-3) var(--sp-5);align-items:flex-start}
.nxTk__q{font:var(--fw-black) var(--fs-24)/1.1 var(--font-mono);color:var(--nx-navy-700);flex:0 0 auto;min-width:32px}
[data-surface="kds"] .nxTk__q{color:var(--nx-cyan-400)}
.nxTk__nm{font:var(--type-kds-item);color:var(--text-primary)}
.nxTk__mod{font:var(--fw-medium) var(--fs-14)/1.35 var(--font-sans);color:var(--nx-warning-500);margin-top:2px}
.nxTk__it--done .nxTk__nm{text-decoration:line-through;opacity:.45}
.nxTk__it--done .nxTk__q{opacity:.45}
.nxTk__f{display:flex;align-items:center;gap:var(--sp-4);padding:var(--sp-4) var(--sp-5);border-top:var(--border-1) solid var(--border-subtle);background:var(--surface-sunken)}
.nxTk__fire{margin-left:auto;font:var(--type-caption);color:var(--text-muted);display:inline-flex;align-items:center;gap:3px}
.nxTk--late{border-color:var(--nx-time-late);box-shadow:0 0 0 2px var(--nx-time-late) inset}
`);
export function OrderTicket({code,where,channel,seconds=0,warnAt=300,lateAt=600,items=[],fireAt,footer,onDark=true,...rest}){
  return React.createElement('article',{className:'nxTk'+(seconds>=lateAt?' nxTk--late':''),...rest},
    React.createElement('div',{className:'nxTk__h'},
      React.createElement('div',{className:'nxTk__id'},
        React.createElement('span',{className:'nxTk__code'},code),
        React.createElement('span',{className:'nxTk__where'},where)),
      React.createElement(OrderTimer,{seconds,warnAt,lateAt,size:'md',onDark})),
    React.createElement('ul',{className:'nxTk__items'},items.map((it,i)=>
      React.createElement('li',{key:i,className:'nxTk__it'+(it.done?' nxTk__it--done':'')},
        React.createElement('span',{className:'nxTk__q'},it.qty+'×'),
        React.createElement('span',null,
          React.createElement('div',{className:'nxTk__nm'},it.name),
          it.modifiers?React.createElement('div',{className:'nxTk__mod'},it.modifiers):null)))),
    (footer||channel||fireAt)?React.createElement('div',{className:'nxTk__f'},
      channel?React.createElement('span',{style:{display:'inline-flex',alignItems:'center',gap:'4px',font:'var(--type-caption)',color:'var(--text-secondary)'}},
        React.createElement(Icon,{name:channel==='DELIVERY'?'delivery_dining':channel==='COUNTER'?'takeout_dining':'table_restaurant',size:16}),
        channel==='DELIVERY'?'Delivery':channel==='COUNTER'?'Balcão':'Salão'):null,
      footer,
      fireAt?React.createElement('span',{className:'nxTk__fire'},React.createElement(Icon,{name:'local_fire_department',size:16}),'montar '+fireAt):null):null);
}
