import {
  BrandMark,
  createNeutralBrandingResponse,
  EmptyState,
  pickBrandLogo,
  RuntimeBrandingProvider,
  useColorScheme,
  useRuntimeBranding,
} from '@nexora/ui';
import './styles.css';

export interface MenuHomeProps {
  readonly tenantName: string;
  readonly welcome: string;
  readonly logo?: string;
}

export function MenuHome({ tenantName, welcome, logo }: Readonly<MenuHomeProps>) {
  return (
    <main className="menu-shell">
      <header className="menu-hero">
        <div className="menu-brand">
          <BrandMark {...(logo ? { logoSrc: logo } : {})} tenantName={tenantName} size={40} />
          <p>Cardápio da casa</p>
        </div>
        <h1>{tenantName}</h1>
        <p className="menu-welcome">{welcome}</p>
      </header>
      <section className="menu-empty" aria-labelledby="menu-title">
        <span className="menu-kicker">Preparado agora</span>
        <h2 id="menu-title">Nosso cardápio</h2>
        <EmptyState icon="restaurant_menu">
          Itens disponíveis aparecem aqui assim que a cozinha abrir.
        </EmptyState>
      </section>
    </main>
  );
}

function BrandedMenu() {
  const { tenant, branding } = useRuntimeBranding();
  const scheme = useColorScheme();
  // US-003, gap "logo dark nunca é consumido no frontend": antes só branding.logo.light era
  // lido, então um cliente com o celular em modo escuro via a logo clara (contraste ruim sobre
  // fundo escuro). pickBrandLogo escolhe pelo esquema de cor do dispositivo, com fallback para a
  // variante que existir (tenant pode ter configurado só uma das duas).
  const logo = pickBrandLogo(branding.logo, scheme);
  return (
    <MenuHome
      tenantName={tenant.name}
      welcome={branding.texts.welcome}
      {...(logo ? { logo } : {})}
    />
  );
}

export function App() {
  return (
    <RuntimeBrandingProvider fallback={createNeutralBrandingResponse()}>
      <BrandedMenu />
    </RuntimeBrandingProvider>
  );
}
