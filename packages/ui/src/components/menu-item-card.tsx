import { forwardRef, type ButtonHTMLAttributes, type ReactNode } from 'react';
import { Icon } from './icon.js';

/**
 * Linha de produto do cardápio — mesa (PWA), app do garçom e delivery.
 * Item sem insumo aparece esgotado e desabilitado (RF-CAT-07, RF-EST-12).
 */
export interface MenuItemCardProps extends Omit<ButtonHTMLAttributes<HTMLButtonElement>, 'name' | 'disabled'> {
  readonly name: ReactNode;
  /** Ingredientes — o cliente precisa saber o que leva. */
  readonly description?: ReactNode;
  /** Preço já formatado ("R$ 64,90"). */
  readonly price: ReactNode;
  /** Tempo de preparo padrão do produto (RF-CAT-08). */
  readonly prepMinutes?: number;
  readonly imageSrc?: string;
  readonly unavailable?: boolean;
  /** Badge extra à direita do preço ("Mais vendida"). */
  readonly badge?: ReactNode;
}

export const MenuItemCard = forwardRef<HTMLButtonElement, Readonly<MenuItemCardProps>>(function MenuItemCard(
  { name, description, price, prepMinutes, imageSrc, unavailable = false, badge, className = '', type = 'button', ...rest },
  ref,
) {
  return (
    <button
      {...rest}
      ref={ref}
      type={type}
      disabled={unavailable}
      aria-disabled={unavailable}
      className={`db-menu-item-card ${unavailable ? 'db-menu-item-card--unavailable' : ''} ${className}`.trim()}
    >
      <span className="db-menu-item-card__photo">
        {imageSrc ? (
          <img src={imageSrc} alt="" />
        ) : (
          // Não existe foto real nas fontes do Design System: placeholder textual
          // explícito, nunca uma imagem genérica inventada.
          <span className="db-menu-item-card__photo-empty">
            <Icon name="local_pizza" size={28} />
            <span className="db-menu-item-card__photo-text">
              foto do produto — a fornecer pelo estabelecimento
            </span>
          </span>
        )}
      </span>
      <span className="db-menu-item-card__body">
        <span className="db-menu-item-card__name">{name}</span>
        {description ? <span className="db-menu-item-card__description">{description}</span> : null}
        <span className="db-menu-item-card__footer">
          <span className="db-menu-item-card__price">{price}</span>
          {prepMinutes ? (
            <span className="db-menu-item-card__tag">
              <Icon name="schedule" size={14} />
              {prepMinutes} min
            </span>
          ) : null}
          {unavailable ? (
            <span className="db-menu-item-card__tag db-menu-item-card__tag--danger">
              <Icon name="block" size={14} />
              Esgotado
            </span>
          ) : null}
          {badge}
        </span>
      </span>
    </button>
  );
});
