import {
  useEffect,
  useState,
  type AnimationEvent,
  type CSSProperties,
  type HTMLAttributes,
  type ReactNode,
} from 'react';
import { NexoraLogo } from './nexora-logo.js';

/** Se o navegador nunca disparar `animationend` (aba em segundo plano, animação
 *  desligada por política), o splash ainda entrega o conteúdo neste prazo. */
const FALLBACK_MS = 4_000;

export interface NexoraLoaderProps extends HTMLAttributes<HTMLDivElement> {
  /** O que está acontecendo, em uma linha: "Carregando", "Preparando o acesso". */
  readonly label?: string;
  /** Diâmetro da moeda em px. */
  readonly size?: number;
  /** Quicadas antes de `onSettled`. Omitido = quica até ser desmontado. */
  readonly bounces?: number;
  /** Sobre navy/azul da marca. */
  readonly inverse?: boolean;
  readonly onSettled?: () => void;
}

/**
 * Carregamento padrão da plataforma: o símbolo da Nexora dentro de uma moeda que
 * quica e gira 360° no eixo Y, com o rótulo do que está acontecendo embaixo.
 */
export function NexoraLoader({
  label = 'Carregando',
  size = 88,
  bounces,
  inverse = false,
  onSettled,
  className = '',
  style,
  ...props
}: Readonly<NexoraLoaderProps>) {
  const vars = {
    '--db-loader-size': `${size}px`,
    ...(bounces === undefined ? {} : { '--db-loader-bounces': String(bounces) }),
    ...style,
  } as CSSProperties;

  return (
    <div
      {...props}
      className={`db-nexora-loader ${inverse ? 'db-nexora-loader--inverse' : ''} ${className}`.trim()}
      style={vars}
    >
      <div className="db-nexora-loader__stage">
        <span className="db-nexora-loader__shadow" aria-hidden="true" />
        <span
          className="db-nexora-loader__coin"
          onAnimationEnd={(event: AnimationEvent<HTMLSpanElement>) => {
            if (event.target === event.currentTarget) onSettled?.();
          }}
        >
          <span className="db-nexora-loader__flip">
            <NexoraLogo variant="symbol" height={Math.round(size * 0.5)} />
          </span>
        </span>
      </div>
      <p className="db-nexora-loader__label" role="status">
        {label}
      </p>
    </div>
  );
}

export interface NexoraSplashProps {
  readonly label?: string;
  /** Quantas quicadas antes de entregar o conteúdo. */
  readonly bounces?: number;
  /** Chamado uma vez, quando o cartão termina de abrir — depois do `NexoraLogo shine`
   *  interno, é o ponto certo para revelar algo abaixo do cartão (ex.: crédito do
   *  fornecedor) sem competir visualmente com a abertura. */
  readonly onOpened?: () => void;
  readonly children: ReactNode;
}

/**
 * Abertura padrão de login e primeiro acesso: a marca quica `bounces` vezes e some
 * enquanto o cartão abre do centro para as pontas (do meio para a esquerda e para a
 * direita). Depois que o cartão termina de abrir, um `<NexoraLogo shine>` dentro dele
 * brilha uma vez — o gatilho é a classe `.is-open` aplicada aqui.
 */
export function NexoraSplash({
  label = 'Carregando',
  bounces = 2,
  onOpened,
  children,
}: Readonly<NexoraSplashProps>) {
  const [phase, setPhase] = useState<'bouncing' | 'leaving' | 'done'>('bouncing');
  const [opened, setOpened] = useState(false);

  useEffect(() => {
    if (phase === 'done') return undefined;
    const timer = setTimeout(() => setPhase('done'), FALLBACK_MS);
    return () => clearTimeout(timer);
  }, [phase]);

  const contentClassName = [
    'db-nexora-splash__content',
    phase === 'bouncing' ? 'is-waiting' : 'nx-anim-open-x',
    opened ? 'is-open' : '',
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <div className="db-nexora-splash">
      {phase === 'done' ? null : (
        <div
          className={`db-nexora-splash__intro ${phase === 'leaving' ? 'is-leaving' : ''}`.trim()}
          onAnimationEnd={(event: AnimationEvent<HTMLDivElement>) => {
            if (event.target === event.currentTarget) setPhase('done');
          }}
        >
          <NexoraLoader label={label} bounces={bounces} onSettled={() => setPhase('leaving')} />
        </div>
      )}
      <div
        className={contentClassName}
        onAnimationEnd={(event: AnimationEvent<HTMLDivElement>) => {
          if (event.target === event.currentTarget && phase !== 'bouncing') {
            setOpened(true);
            onOpened?.();
          }
        }}
      >
        {children}
      </div>
    </div>
  );
}
