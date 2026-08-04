import React from 'react';
import {injectCss} from '../nx-css.js';
injectCss('table',`
.nxTbWrap{width:100%;overflow-x:auto}
.nxTb{width:100%;border-collapse:collapse;font:var(--type-body)}
.nxTb th{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:var(--text-muted);text-align:left;padding:var(--sp-4) var(--sp-6);border-bottom:var(--border-1) solid var(--border-subtle);white-space:nowrap;background:var(--surface-card);position:sticky;top:0}
.nxTb td{padding:0 var(--sp-6);height:var(--density-desk-row);border-bottom:var(--border-1) solid var(--border-subtle);color:var(--text-primary);vertical-align:middle}
.nxTb tbody tr:last-child td{border-bottom:0}
.nxTb tbody tr:hover td{background:var(--surface-page)}
.nxTb--clickable tbody tr{cursor:pointer}
.nxTb__num{text-align:right;font-family:var(--font-mono);font-variant-numeric:tabular-nums}
.nxTb--compact td{height:32px;font-size:var(--fs-13)}
.nxTb tfoot td{height:var(--density-desk-row);font-weight:var(--fw-semibold);background:var(--surface-sunken);border-top:var(--border-2) solid var(--border-default)}
`);
export function DataTable({columns=[],rows=[],footer,compact=false,onRowClick,rowKey,...rest}){
  return React.createElement('div',{className:'nxTbWrap',...rest},
    React.createElement('table',{className:['nxTb',compact?'nxTb--compact':'',onRowClick?'nxTb--clickable':''].filter(Boolean).join(' ')},
      React.createElement('thead',null,React.createElement('tr',null,
        columns.map(c=>React.createElement('th',{key:c.key,style:{textAlign:c.align==='right'?'right':c.align==='center'?'center':'left',width:c.width}},c.header)))),
      React.createElement('tbody',null,rows.map((r,i)=>React.createElement('tr',{key:rowKey?r[rowKey]:i,onClick:onRowClick?()=>onRowClick(r):undefined},
        columns.map(c=>React.createElement('td',{key:c.key,className:c.numeric?'nxTb__num':undefined,
          style:{textAlign:c.align==='center'?'center':undefined}},c.render?c.render(r):r[c.key]))))),
      footer?React.createElement('tfoot',null,footer):null));
}
