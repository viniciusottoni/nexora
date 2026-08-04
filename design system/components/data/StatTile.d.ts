/**
 * Indicador único. Regra de produto: número solto não gera decisão — sempre
 * informe `comparison` (período anterior) ou `target` (meta).
 */
export interface StatTileProps {
  label: React.ReactNode;
  value: React.ReactNode;
  /** Sufixo pequeno: "min", "%", "pedidos". */
  unit?: string;
  /** Variação já formatada, ex. "+12,4%". */
  delta?: string;
  deltaDirection?: 'up' | 'down' | 'flat';
  /** Contra o que se compara, ex. "vs. mesma terça". */
  comparison?: React.ReactNode;
  /** Meta do indicador, ex. "≤ 10 min". */
  target?: React.ReactNode;
  icon?: string;
  size?: 'md' | 'lg';
  /** `pulse` = fundo navy, para a faixa de tempo real do painel do dono. */
  variant?: 'card' | 'flat' | 'pulse';
}
export function StatTile(props: StatTileProps): JSX.Element;
