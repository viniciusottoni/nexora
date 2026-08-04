import React from 'react';
import {injectCss} from '../nx-css.js';
import {Icon} from '../core/Icon.jsx';
injectCss('keypad',`
.nxKp{display:grid;grid-template-columns:repeat(3,1fr);gap:var(--sp-4);width:100%}
.nxKp button{height:var(--density-touch-lg);border:var(--border-1) solid var(--border-default);border-radius:var(--brand-radius);background:var(--surface-card);color:var(--text-primary);font:var(--fw-semibold) var(--fs-24)/1 var(--font-mono);cursor:pointer;transition:var(--transition-control);display:flex;align-items:center;justify-content:center}
.nxKp button:hover{background:var(--surface-sunken)}
.nxKp button:active{transform:translateY(1px);background:var(--surface-brand-subtle)}
.nxKp button.nxKp--ok{background:var(--nx-success-500);border-color:var(--nx-success-500);color:#fff}
.nxKp button.nxKp--ok:hover{background:var(--nx-success-600)}
.nxKp--dark button{background:var(--surface-raised);border-color:var(--border-default);color:var(--text-primary)}
.nxKp__dots{display:flex;gap:var(--sp-4);justify-content:center;margin-bottom:var(--sp-7)}
.nxKp__dot{width:14px;height:14px;border-radius:50%;background:var(--nx-gray-300);transition:background var(--dur-fast) var(--ease-standard)}
.nxKp__dot--on{background:var(--brand-primary)}
.nxKp__dots--dark .nxKp__dot{background:rgba(255,255,255,.24)}
.nxKp__dots--dark .nxKp__dot--on{background:var(--nx-green-400)}
`);
export function NumericKeypad({value='',onChange,onSubmit,length,showDots=false,dark=false,...rest}){
  const push=k=>{if(length&&value.length>=length)return;onChange&&onChange(value+k);};
  const keys=['1','2','3','4','5','6','7','8','9'];
  return React.createElement('div',{...rest},
    showDots?React.createElement('div',{className:'nxKp__dots'+(dark?' nxKp__dots--dark':'')},
      Array.from({length:length||4}).map((_,i)=>React.createElement('span',{key:i,className:'nxKp__dot'+(i<value.length?' nxKp__dot--on':'')}))):null,
    React.createElement('div',{className:'nxKp'+(dark?' nxKp--dark':'')},
      keys.map(k=>React.createElement('button',{key:k,type:'button',onClick:()=>push(k)},k)),
      React.createElement('button',{type:'button','aria-label':'Apagar',onClick:()=>onChange&&onChange(value.slice(0,-1))},React.createElement(Icon,{name:'backspace',size:24})),
      React.createElement('button',{type:'button',onClick:()=>push('0')},'0'),
      React.createElement('button',{type:'button',className:'nxKp--ok','aria-label':'Confirmar',onClick:()=>onSubmit&&onSubmit(value)},React.createElement(Icon,{name:'check',size:28}))));
}
