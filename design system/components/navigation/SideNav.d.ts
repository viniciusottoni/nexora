/**
 * Navegação lateral das telas de gestão (painel do dono, caixa, admin).
 * @startingPoint section="Navegação" subtitle="Lateral, barra superior e controle segmentado" viewport="700x340"
 */
export interface SideNavItem {
  id?: string;
  label?: React.ReactNode;
  icon?: string;
  /** Contador de pendências (alertas, mesas aguardando). */
  count?: number;
  /** Cabeçalho de seção — use em vez de id/label/icon. */
  group?: string;
}
export interface SideNavProps {
  /** Normalmente um `<BrandMark />`. */
  brand?: React.ReactNode;
  items: SideNavItem[];
  activeId?: string;
  onSelect?: (id: string) => void;
  footer?: React.ReactNode;
  /** `dark` (navy) na plataforma; `light` quando o tenant tem marca clara. */
  variant?: 'dark' | 'light';
}
export function SideNav(props: SideNavProps): JSX.Element;
