/**
 * Marca Nexora em SVG inline, vetorizada de `Assets/logo.jpeg`. Os mesmos caminhos
 * estão em `assets/logo-nexora-*.svg`. Regra de fundo: `color` sobre claro,
 * `white` sobre navy/azul da marca.
 */
export interface NexoraLogoProps {
  /** `full` inclui a assinatura; `lockup` é símbolo + wordmark; `symbol` só o símbolo. */
  variant?: 'lockup' | 'symbol';
  tone?: 'color' | 'white';
  /** Altura em px — a largura sai da proporção do desenho. */
  height?: number;
  /** Um brilho da esquerda para a direita, tocado uma vez por um `.is-open` ancestral
   *  (ver `NexoraSplash`). Sem esse ancestral o brilho fica parado, invisível. */
  shine?: boolean;
  className?: string;
}
export function NexoraLogo(props: NexoraLogoProps): JSX.Element;
