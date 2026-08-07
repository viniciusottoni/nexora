import type { HTMLAttributes, ReactNode } from 'react';

export interface SideNavItem {
  readonly id?: string;
  readonly label?: ReactNode;
  readonly icon?: string;
  /** Contador de pendências (alertas, mesas aguardando). */
  readonly count?: number;
  /** Cabeçalho de seção — use em vez de id/label/icon. */
  readonly group?: string;
}

export interface SideNavProps extends Omit<HTMLAttributes<HTMLElement>, 'className' | 'onSelect'> {
  /** Normalmente um `<BrandMark />`. */
  readonly brand?: ReactNode;
  readonly items: readonly SideNavItem[];
  readonly activeId?: string;
  readonly onSelect?: (id: string) => void;
  readonly footer?: ReactNode;
  /** `dark` (navy) na plataforma; `light` quando o tenant tem marca clara. */
  readonly variant?: 'dark' | 'light';
  readonly className?: string;
}

function ItemIcon({ name, active }: Readonly<{ name: string; active: boolean }>) {
  return (
    <span
      aria-hidden="true"
      className="material-symbols-rounded"
      style={{
        fontSize: '20px',
        lineHeight: 1,
        flex: '0 0 auto',
        userSelect: 'none',
        fontVariationSettings: `'FILL' ${active ? 1 : 0}, 'wght' 400, 'GRAD' 0, 'opsz' 20`,
      }}
    >
      {name}
    </span>
  );
}

/**
 * Navegação lateral fixa das telas de gestão (painel do dono, caixa, admin).
 * Um dos dois únicos elementos de chrome fixo do produto — ver Design System readme.md
 * § "Espaço, densidade e layout".
 */
export function SideNav({
  brand,
  items,
  activeId,
  onSelect,
  footer,
  variant = 'dark',
  className = '',
  ...props
}: Readonly<SideNavProps>) {
  return (
    <nav {...props} className={`db-sidenav ${variant === 'light' ? 'db-sidenav--light' : ''} ${className}`.trim()}>
      {brand ? <div className="db-sidenav__brand">{brand}</div> : null}
      <div className="db-sidenav__scroll">
        {items.map((item, index) =>
          item.group ? (
            <div key={`group-${index}`} className="db-sidenav__group">
              {item.group}
            </div>
          ) : (
            <button
              key={item.id}
              type="button"
              className={`db-sidenav__item ${item.id === activeId ? 'db-sidenav__item--on' : ''}`.trim()}
              aria-current={item.id === activeId ? 'page' : undefined}
              onClick={() => {
                if (item.id) onSelect?.(item.id);
              }}
            >
              {item.icon ? <ItemIcon name={item.icon} active={item.id === activeId} /> : null}
              <span>{item.label}</span>
              {item.count ? <span className="db-sidenav__count">{item.count}</span> : null}
            </button>
          ),
        )}
      </div>
      {footer ? <div className="db-sidenav__foot">{footer}</div> : null}
    </nav>
  );
}
