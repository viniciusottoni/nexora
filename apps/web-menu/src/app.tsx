import {
  BrandMark,
  createNeutralBrandingResponse,
  CreatedByFooter,
  EmptyState,
  pickBrandLogo,
  RuntimeBrandingProvider,
  useColorScheme,
  useRuntimeBranding,
} from '@nexora/ui';
import { extractQrTokenFromLocation } from './table-access/qr-token.js';
import { TableAccessPage } from './table-access/table-access-page.js';
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
  // US-021 §4: presença de qrToken na URL (leitura da câmera) decide o fluxo — com token, é a
  // mesa/cardápio (TableAccessPage); sem token (acesso direto ao domínio, sem ler QR nenhum), cai
  // no cardápio de vitrine estático que já existia (MenuHome).
  const qrToken = typeof window === 'undefined' ? null : extractQrTokenFromLocation(window.location);
  return (
    // US-021 §9: servido pelo Nginx do edge na LAN da loja — mesmo "gap US-003" já corrigido em
    // web-pos/web-kds (endpoint padrão `/v1/public/branding?host=` não funciona sem domínio
    // público). `/v1/local/branding` (Api.Edge, tenant fixo da instalação) é o caminho correto
    // aqui, e é justamente o que faz a marca exibida ser a do ESTABELECIMENTO, nunca a da Replay
    // (ADR-010) — `data-tenant`/`--brand-*` são injetados em runtime por RuntimeBrandingProvider,
    // nunca hardcoded por tenant (ADR-013).
    <RuntimeBrandingProvider fallback={createNeutralBrandingResponse()} endpoint="/v1/local/branding">
      {qrToken ? <TableAccessPage qrToken={qrToken} /> : <BrandedMenu />}
      <CreatedByFooter />
    </RuntimeBrandingProvider>
  );
}
