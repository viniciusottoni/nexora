import type { PublicMenuResponse, PublicTableInfoDto } from '@nexora/contracts';
import { BrandMark, EmptyState, MenuItemCard } from '@nexora/ui';

export interface TableMenuViewProps {
  readonly table: PublicTableInfoDto;
  readonly menu: PublicMenuResponse;
  readonly logo?: string;
}

/**
 * Cardápio da mesa já resolvida — componente presentacional (props só, sem hook de branding),
 * mesmo espírito de `MenuHome` (app.tsx): mantém a marca/dado vindos de fora, testável sem
 * `RuntimeBrandingProvider`. `BrandedTableAccessPage` (table-access-page.tsx) é quem injeta
 * `logo`/`tenantName` a partir do contexto de branding em runtime.
 */
export function TableMenuView({ table, menu, logo }: Readonly<TableMenuViewProps>) {
  return (
    <main className="menu-access-shell">
      <header className="menu-access-hero">
        <div className="menu-brand">
          <BrandMark {...(logo ? { logoSrc: logo } : {})} tenantName={menu.tenantName} size={40} />
          <p>
            Mesa {table.label} · {table.areaName}
          </p>
        </div>
        <h1>{menu.tenantName}</h1>
      </header>
      {menu.categories.length === 0 ? (
        <EmptyState icon="restaurant_menu">
          O cardápio ainda está sendo preparado — chame o garçom para pedir.
        </EmptyState>
      ) : (
        menu.categories.map((category) => (
          <section key={category.id} className="menu-access-category" aria-labelledby={`category-${category.id}`}>
            <h2 id={`category-${category.id}`}>{category.name}</h2>
            <div className="menu-access-items">
              {category.products.map((product) => (
                <MenuItemCard
                  key={product.id}
                  name={product.name}
                  description={product.description ?? undefined}
                  {...(product.imageUrl ? { imageSrc: product.imageUrl } : {})}
                  price={product.fromPrice ? `A partir de R$ ${product.fromPrice.replace('.', ',')}` : 'Consulte'}
                />
              ))}
            </div>
          </section>
        ))
      )}
    </main>
  );
}
