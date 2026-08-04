import React from 'react';
import {injectCss} from '../nx-css.js';
import {NexoraLogo} from './NexoraLogo.jsx';
injectCss('nxLoader',`
.nxLoader{--nxLoaderSize:88px;--nxLoaderBounces:infinite;--nxLoaderCycle:calc(var(--dur-slower) + var(--dur-slow));display:grid;justify-items:center;gap:var(--sp-5)}
.nxLoader__stage{position:relative;display:grid;place-items:end center;width:var(--nxLoaderSize);height:calc(var(--nxLoaderSize) * 1.4)}
.nxLoader__shadow{position:absolute;bottom:0;left:14%;width:72%;height:6px;border-radius:50%;background:color-mix(in srgb, var(--nx-navy-900) 24%, transparent);filter:blur(2px);animation:nx-brand-bounce-shadow var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__coin{position:relative;display:grid;place-items:center;width:var(--nxLoaderSize);height:var(--nxLoaderSize);margin-bottom:9px;border-radius:50%;background:var(--surface-card);box-shadow:0 0 0 1px color-mix(in srgb, var(--brand-primary) 18%, transparent),0 12px 26px -14px color-mix(in srgb, var(--nx-navy-900) 65%, transparent);transform-origin:50% 100%;perspective:32rem;animation:nx-brand-bounce var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__flip{display:grid;place-items:center;transform-style:preserve-3d;animation:nx-brand-flip var(--nxLoaderCycle) linear var(--nxLoaderBounces) both}
.nxLoader__label{margin:0;font:var(--type-caption);color:var(--text-muted);text-align:center}
.nxLoader--inverse .nxLoader__label{color:rgba(255,255,255,.72)}
.nxSplash{display:grid;width:100%;justify-items:center}
.nxSplash>*{grid-area:1 / 1}
.nxSplash__intro{display:grid;place-items:center;align-self:center}
.nxSplash__intro.is-leaving{pointer-events:none;animation:nx-brand-exit var(--dur-slow) var(--ease-in-out) both}
.nxSplash__content{align-self:center;width:100%;display:grid;justify-items:center}
.nxSplash__content.is-waiting{visibility:hidden}
.nx-anim-open-x{transform-origin:center center;animation:nx-open-x var(--dur-slower) var(--ease-out) both}
.nxSplash__content.is-open .nxLogo__shine{animation:nx-logo-shine calc(var(--dur-slower) + var(--dur-base)) var(--ease-in-out) 1 both}
`);

/* Carregamento padrao da plataforma: o simbolo da Nexora numa moeda que quica e gira
   360 graus no eixo Y, vista de frente, com o rotulo do que esta acontecendo embaixo.
   NexoraSplash e o uso padrao antes de cartao de login e de primeiro acesso: quica duas
   vezes e some enquanto o cartao abre do centro para os lados (esquerda/direita). Com
   prefers-reduced-motion os --dur-* zeram, o animationend dispara na hora e a tela cai
   direto no conteudo. Depois que o cartao termina de abrir, um NexoraLogo com `shine`
   dentro dele brilha uma vez da esquerda para a direita (gatilho: classe .is-open). */

const NX_SPLASH_FALLBACK_MS = 4000;

export function NexoraLoader({label='Carregando',size=88,bounces,inverse=false,onSettled,className='',style,...rest}){
  return React.createElement('div',{
    className:('nxLoader'+(inverse?' nxLoader--inverse':'')+' '+className).trim(),
    style:{'--nxLoaderSize':size+'px',...(bounces===undefined?{}:{'--nxLoaderBounces':String(bounces)}),...style},
    ...rest},
    React.createElement('div',{className:'nxLoader__stage'},
      React.createElement('span',{className:'nxLoader__shadow','aria-hidden':'true'}),
      React.createElement('span',{className:'nxLoader__coin',
        onAnimationEnd:e=>{if(e.target===e.currentTarget&&onSettled)onSettled();}},
        React.createElement('span',{className:'nxLoader__flip'},
          React.createElement(NexoraLogo,{variant:'symbol',height:Math.round(size*.5)})))),
    React.createElement('p',{className:'nxLoader__label',role:'status'},label));
}

export function NexoraSplash({label='Carregando',bounces=2,onOpened,children}){
  const [phase,setPhase]=React.useState('bouncing');
  const [opened,setOpened]=React.useState(false);
  React.useEffect(()=>{
    if(phase==='done')return undefined;
    const t=setTimeout(()=>setPhase('done'),NX_SPLASH_FALLBACK_MS);
    return ()=>clearTimeout(t);
  },[phase]);
  const contentClassName=['nxSplash__content',phase==='bouncing'?'is-waiting':'nx-anim-open-x',
    opened?'is-open':''].filter(Boolean).join(' ');
  return React.createElement('div',{className:'nxSplash'},
    phase==='done'?null:React.createElement('div',{
      className:'nxSplash__intro'+(phase==='leaving'?' is-leaving':''),
      onAnimationEnd:e=>{if(e.target===e.currentTarget)setPhase('done');}},
      React.createElement(NexoraLoader,{label:label,bounces:bounces,onSettled:()=>setPhase('leaving')})),
    React.createElement('div',{className:contentClassName,
      onAnimationEnd:e=>{if(e.target===e.currentTarget&&phase!=='bouncing'){setOpened(true);if(onOpened)onOpened();}}},children));
}
