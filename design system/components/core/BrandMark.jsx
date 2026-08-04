import React from 'react';
import {injectCss} from '../nx-css.js';
import {NexoraLogo} from './NexoraLogo.jsx';
injectCss('brand',`
.nxBrand{display:inline-flex;align-items:center;gap:var(--sp-4);min-width:0}
.nxBrand__img{display:block;height:100%;width:auto}
.nxBrand__word{font-family:var(--font-display);font-weight:var(--fw-black);letter-spacing:var(--ls-tight);color:var(--text-brand);line-height:1}
.nxBrand__sub{font:var(--type-overline);letter-spacing:var(--ls-caps);text-transform:uppercase;color:var(--text-muted);margin-top:3px}
.nxBrand--inverse .nxBrand__word{color:#fff}
.nxBrand--inverse .nxBrand__sub{color:rgba(255,255,255,.7)}
.nxBrand__tenant{width:var(--nxBrandSize);height:var(--nxBrandSize);border-radius:var(--radius-md);background:var(--brand-primary);color:var(--brand-on-primary);display:flex;align-items:center;justify-content:center;font-family:var(--font-display);font-weight:var(--fw-black);flex:0 0 auto}
.nxBrand--center{flex-direction:column;justify-content:center;text-align:center;gap:var(--sp-3)}
`);

/* Assinatura de marca. Sem logoSrc nem tenantName desenha a marca Nexora
   (NexoraLogo): colorida sobre fundo claro, branca com inverse. center empilha e
   centraliza — e o arranjo de cartao de login e de primeiro acesso. */

export function BrandMark({logoSrc,tenantName,subtitle,size=28,inverse=false,center=false,...rest}){
  const inner=logoSrc
    ?React.createElement('img',{src:logoSrc,alt:tenantName||'Nexora',className:'nxBrand__img',style:{height:size+'px'}})
    :tenantName
      ?[React.createElement('span',{key:'i',className:'nxBrand__tenant',style:{'--nxBrandSize':size+'px',fontSize:size*.46+'px'}},tenantName.trim().charAt(0).toUpperCase()),
        React.createElement('span',{key:'w'},
          React.createElement('span',{className:'nxBrand__word',style:{fontSize:size*.62+'px'}},tenantName),
          subtitle?React.createElement('span',{className:'nxBrand__sub',style:{display:'block'}},subtitle):null)]
      :React.createElement('span',null,
          React.createElement(NexoraLogo,{variant:'lockup',tone:inverse?'white':'color',height:size}),
          subtitle?React.createElement('span',{className:'nxBrand__sub',style:{display:'block'}},subtitle):null);
  return React.createElement('span',{className:'nxBrand'+(inverse?' nxBrand--inverse':'')+(center?' nxBrand--center':''),...rest},inner);
}
