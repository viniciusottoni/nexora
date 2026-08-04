/**
 * Carregamento padrão da plataforma: o símbolo da Nexora numa moeda que quica e gira
 * 360° no eixo Y, vista de frente, com o rótulo do que está acontecendo embaixo.
 */
export interface NexoraLoaderProps {
  /** O que está acontecendo, em uma linha: "Carregando", "Preparando o acesso". */
  label?: string;
  /** Diâmetro da moeda em px. */
  size?: number;
  /** Quicadas antes de `onSettled`. Omitido = quica até ser desmontado. */
  bounces?: number;
  /** Sobre navy/azul da marca. */
  inverse?: boolean;
  onSettled?: () => void;
  className?: string;
}
export function NexoraLoader(props: NexoraLoaderProps): JSX.Element;

/**
 * Abertura padrão de login e primeiro acesso: a marca quica `bounces` vezes e some
 * enquanto o cartão abre do centro para as pontas (do meio para a esquerda e para a
 * direita).
 */
export interface NexoraSplashProps {
  label?: string;
  bounces?: number;
  /** Chamado uma vez, quando o cartão termina de abrir — ponto certo para revelar
   *  algo abaixo dele (ex.: crédito do fornecedor) sem competir com a abertura. */
  onOpened?: () => void;
  children?: unknown;
}
export function NexoraSplash(props: NexoraSplashProps): JSX.Element;
