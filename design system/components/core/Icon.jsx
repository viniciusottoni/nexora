import React from 'react';
/* Iconografia Nexora = Material Symbols Rounded (CDN). Ver readme.md › ICONOGRAPHY. */
export function Icon({name,size=20,fill=false,weight=400,color,style,label,...rest}){
  return React.createElement('span',{
    className:'material-symbols-rounded',
    'aria-hidden':label?undefined:'true','aria-label':label,role:label?'img':undefined,
    style:{fontSize:size+'px',lineHeight:1,color:color||'inherit',flex:'0 0 auto',
      fontVariationSettings:`'FILL' ${fill?1:0},'wght' ${weight},'GRAD' 0,'opsz' ${size}`,
      userSelect:'none',...style},...rest},name);
}
