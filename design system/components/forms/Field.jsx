import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('field',`
.nxField{display:flex;flex-direction:column;gap:var(--sp-3);min-width:0}
.nxField__lab{font:var(--type-label);color:var(--text-secondary);display:flex;gap:var(--sp-2);align-items:baseline}
.nxField__req{color:var(--nx-danger-500)}
.nxField__hint{font:var(--type-caption);color:var(--text-muted)}
.nxField__err{font:var(--type-caption);color:var(--text-danger);display:flex;gap:var(--sp-2);align-items:center}
`);
export function Field({label,hint,error,required,htmlFor,children,...rest}){
  return React.createElement('div',{className:'nxField',...rest},
    label?React.createElement('label',{className:'nxField__lab',htmlFor},label,required?React.createElement('span',{className:'nxField__req'},'*'):null):null,
    children,
    error?React.createElement('span',{className:'nxField__err'},error):hint?React.createElement('span',{className:'nxField__hint'},hint):null);
}
