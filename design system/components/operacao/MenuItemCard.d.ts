/**
 * Linha de produto do cardápio — mesa (PWA), app do garçom e delivery.
 * Item sem insumo aparece esgotado e desabilitado (RF-CAT-07, RF-EST-12).
 */
export interface MenuItemCardProps {
  name: React.ReactNode;
  /** Ingredientes — o cliente precisa saber o que leva. */
  description?: React.ReactNode;
  /** Preço já formatado ("R$ 64,90"). */
  price: React.ReactNode;
  /** Tempo de preparo padrão do produto (RF-CAT-08). */
  prepMinutes?: number;
  imageSrc?: string;
  unavailable?: boolean;
  /** Badge extra à direita do preço ("Mais vendida"). */
  badge?: React.ReactNode;
}
export function MenuItemCard(props: MenuItemCardProps): JSX.Element;
