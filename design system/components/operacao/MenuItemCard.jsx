import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('menuitem',`
.nxMi{display:flex;gap:var(--sp-5);padding:var(--sp-5);background:var(--surface-card);border:var(--border-1) solid var(--border-subtle);border-radius:var(--brand-radius);text-align:left;cursor:pointer;transition:var(--transition-control),box-shadow var(--dur-fast) var(--ease-standard);width:100%;align-items:flex-start}
.nxMi:hover{box-shadow:var(--shadow-raised);border-color:var(--border-default)}
.nxMi__ph{width:88px;height:88px;flex:0 0 auto;border-radius:var(--radius-md);background:var(--surface-sunken);display:flex;align-items:center;justify-content:center;color:var(--text-disabled);overflow:hidden}
.nxMi__ph img{width:100%;height:100%;object-fit:cover}
.nxMi__b{min-width:0;flex:1 1 auto;display:flex;flex-direction:column;gap:var(--sp-2)}
.nxMi__n{font:var(--fw-semibold) var(--fs-16)/1.25 var(--font-sans);color:var(--text-primary)}
.nxMi__d{font:var(--type-caption);color:var(--text-muted);display:-webkit-box;-webkit-line-clamp:2;-webkit-box-orient:vertical;overflow:hidden;text-wrap:pretty}
.nxMi__f{display:flex;align-items:center;gap:var(--sp-5);margin-top:var(--sp-2)}
.nxMi__p{font:var(--fw-bold) var(--fs-16)/1 var(--font-mono);font-variant-numeric:tabular-nums;color:var(--text-primary)}
.nxMi__t{font:var(--type-caption);color:var(--text-muted);display:inline-flex;align-items:center;gap:3px}
.nxMi--out{opacity:.55;cursor:not-allowed}
.nxMi--out .nxMi__p{text-decoration:line-through}
`);
export function MenuItemCard({name,description,price,prepMinutes,imageSrc,unavailable=false,badge,...rest}){
  return React.createElement('button',{type:'button',disabled:unavailable,className:'nxMi'+(unavailable?' nxMi--out':''),...rest},
    React.createElement('span',{className:'nxMi__ph'},
      imageSrc?React.createElement('img',{src:imageSrc,alt:''}):React.createElement(Icon,{name:'local_pizza',size:28})),
    React.createElement('span',{className:'nxMi__b'},
      React.createElement('span',{className:'nxMi__n'},name),
      description?React.createElement('span',{className:'nxMi__d'},description):null,
      React.createElement('span',{className:'nxMi__f'},
        React.createElement('span',{className:'nxMi__p'},price),
        prepMinutes?React.createElement('span',{className:'nxMi__t'},React.createElement(Icon,{name:'schedule',size:14}),prepMinutes+' min'):null,
        unavailable?React.createElement('span',{className:'nxMi__t',style:{color:'var(--text-danger)'}},React.createElement(Icon,{name:'block',size:14}),'Esgotado'):null,
        badge)));
}
