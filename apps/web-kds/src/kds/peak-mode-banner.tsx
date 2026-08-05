import { AlertBanner, Button, Icon } from '@nexora/ui';
import './peak-mode-banner.css';

export interface PeakModeBannerProps {
  /** Modo pico ativo agora (automático ou forçado manualmente) — US-047 §4 "indicação clara de que está ativo". */
  readonly active: boolean;
  /** Operador desligou manualmente por último — mostra o convite discreto para reativar mesmo com o modo inativo. */
  readonly manuallyDisabled: boolean;
  readonly onToggle: () => void;
}

/**
 * US-047 §4/§10 — badge/banner no topo da fila. Dois estados visíveis (o terceiro, "nada a
 * mostrar", é `null` — a tela não ganha nenhum elemento novo quando o modo pico nunca foi
 * cogitado):
 *
 * 1. `active` — banner de destaque: "cartões reduzidos ao essencial" + botão para desligar.
 * 2. `!active && manuallyDisabled` — aviso discreto de que o operador suprimiu o automático (senão
 *    a fila continuar grande sem simplificar pareceria um bug) + botão para reativar.
 *
 * Entrada com `nx-anim-toast-in` (não `nx-anim-in`, reservado a cartões/seções de página — este é
 * um banner flutuante no topo, mesma categoria de toast/alerta) — troca de estado sempre
 * desmonta/remonta o `AlertBanner` (chave por `active`), então a transição nunca é um salto seco,
 * conforme US-047 §10 ("transição visual suave, sem reorganização brusca").
 */
export function PeakModeBanner({ active, manuallyDisabled, onToggle }: Readonly<PeakModeBannerProps>) {
  if (!active && !manuallyDisabled) return null;

  return active ? (
    <AlertBanner
      key="active"
      tone="warning"
      icon="speed"
      title="Modo pico ativo"
      className="kds-peak-mode-banner nx-anim-toast-in"
      actions={
        <Button type="button" size="touch" variant="secondary" onClick={onToggle}>
          Desativar modo pico
        </Button>
      }
    >
      Fila grande — os cartões mostram só código, produto, quantidade e tempo. Toque em um cartão
      para ver observações e modificadores.
    </AlertBanner>
  ) : (
    <AlertBanner
      key="manually-disabled"
      tone="neutral"
      icon="speed"
      title="Modo pico desativado manualmente"
      className="kds-peak-mode-banner kds-peak-mode-banner--muted nx-anim-toast-in"
      actions={
        <Button type="button" size="touch" variant="ghost" onClick={onToggle}>
          <Icon name="play_arrow" size={18} />
          Reativar
        </Button>
      }
    >
      Vai continuar desligado até você reativar, mesmo que a fila fique maior.
    </AlertBanner>
  );
}
