import { AlertBanner, Button, Card, Icon, StatTile } from '@nexora/ui';
import type { PlatformSummaryResponse } from '@nexora/contracts';

import './platform-overview-page.css';

export interface PlatformOverviewPageProps {
  /** `undefined` no modo offline (US-150 §9) — a estrutura visual e a navegação continuam, os contadores não. */
  readonly summary: PlatformSummaryResponse | undefined;
  readonly onCreateTenant: () => void;
  readonly onOpenTenants: () => void;
  readonly onOpenInstallations: () => void;
  readonly onOpenSupportAccess: () => void;
}

/**
 * US-150 §3.1/§10 — raiz autenticada do painel: panorama pequeno (RN-015, §15 "análises
 * aprofundadas pertencem às páginas específicas") mais os atalhos de navegação exigidos pelo
 * critério de aceite "Entrada pela raiz". "Novo estabelecimento" é sempre uma AÇÃO aqui, nunca o
 * conteúdo único da tela — o formulário em si vive em `/estabelecimentos/novo` (US-002).
 */
export function PlatformOverviewPage({
  summary,
  onCreateTenant,
  onOpenTenants,
  onOpenInstallations,
  onOpenSupportAccess,
}: Readonly<PlatformOverviewPageProps>) {
  return (
    <main className="db-page nx-anim-in" aria-labelledby="overview-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma Replay</p>
          <h1 className="db-page__title" id="overview-title">
            Visão geral
          </h1>
          <p className="db-page__lead">
            Panorama de todos os estabelecimentos e instalações do parque.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" onClick={onCreateTenant}>
            <Icon name="add_business" /> Novo estabelecimento
          </Button>
        </div>
      </header>

      {summary ? (
        <div className="platform-overview__grid nx-stagger">
          <StatTile
            label="Estabelecimentos"
            value={summary.tenants.total}
            icon="storefront"
            comparison={`${summary.tenants.active} ativos`}
          />
          <StatTile
            label="Precisam de atenção"
            value={summary.tenants.attention}
            icon="warning"
          />
          <StatTile label="Instalações saudáveis" value={summary.installations.healthy} icon="dns" />
          <StatTile label="Instalações degradadas" value={summary.installations.degraded} icon="sync_problem" />
          <StatTile label="Instalações fora do ar" value={summary.installations.offline} icon="cloud_off" />
          <StatTile label="Convites pendentes" value={summary.pendingInvites} icon="mail" />
        </div>
      ) : (
        <AlertBanner tone="warning" title="Sem conexão com a nuvem">
          Os dados não puderam ser atualizados. A navegação continua disponível.
        </AlertBanner>
      )}

      <div className="platform-overview__shortcuts nx-stagger">
        <Card
          as="article"
          title="Estabelecimentos"
          subtitle="Localize e abra qualquer estabelecimento autorizado."
          actions={
            <Button type="button" variant="secondary" size="sm" onClick={onOpenTenants}>
              Ver diretório
            </Button>
          }
        />
        <Card
          as="article"
          title="Instalações"
          subtitle="Saúde do parque de servidores locais."
          actions={
            <Button type="button" variant="secondary" size="sm" onClick={onOpenInstallations}>
              Ver instalações
            </Button>
          }
        />
        <Card
          as="article"
          title="Auditoria e suporte"
          subtitle="Atalhos de acesso auditado e histórico de suporte."
          actions={
            <Button type="button" variant="secondary" size="sm" onClick={onOpenSupportAccess}>
              Ver auditoria e suporte
            </Button>
          }
        />
      </div>
    </main>
  );
}
