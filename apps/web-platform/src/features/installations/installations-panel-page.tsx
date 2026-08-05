import { useEffect, useMemo, useState } from 'react';
import { AlertBanner, Badge, Card, DataTable, EmptyState, Modal } from '@nexora/ui';
import type {
  InstallationDiagnosticsResponse,
  InstallationHealth,
  InstallationIncident,
  PlatformInstallationSummary,
} from '@nexora/contracts';

import { createInstallationsApi, type InstallationsApi } from './installations-api.js';
import './installations-panel-page.css';

export interface InstallationsPanelPageProps {
  readonly api?: InstallationsApi;
}

const HEALTH_LABEL: Record<InstallationHealth, string> = {
  OK: 'Saudável',
  DEGRADED: 'Degradada',
  DOWN: 'Fora do ar',
};

const HEALTH_TONE: Record<InstallationHealth, 'success' | 'warning' | 'danger'> = {
  OK: 'success',
  DEGRADED: 'warning',
  DOWN: 'danger',
};

/** US-140 §10 "ordenação por criticidade como padrão" — DOWN > DEGRADED > OK, mesma ordem que o backend já aplica; reforçada aqui para não depender só do contrato de ordenação da API. */
const HEALTH_RANK: Record<InstallationHealth, number> = { DOWN: 2, DEGRADED: 1, OK: 0 };

/**
 * US-140 "Painel de instalações com saúde" — lista consolidada do parque (versão, último
 * contato, atraso de sincronização, eventos pendentes, saúde), com diagnóstico remoto e
 * histórico de incidentes acessíveis num modal, sem sair da tela (US-140 §10). RN-015: nenhum
 * campo aqui carrega dado de negócio do tenant — só identificação e saúde técnica.
 */
export function InstallationsPanelPage({ api: providedApi }: Readonly<InstallationsPanelPageProps>) {
  const api = useMemo(() => providedApi ?? createInstallationsApi(), [providedApi]);

  const [installations, setInstallations] = useState<readonly PlatformInstallationSummary[]>();
  const [loadError, setLoadError] = useState<string>();

  const [selected, setSelected] = useState<PlatformInstallationSummary>();
  const [diagnostics, setDiagnostics] = useState<InstallationDiagnosticsResponse>();
  const [incidents, setIncidents] = useState<readonly InstallationIncident[]>();
  const [detailBusy, setDetailBusy] = useState(false);
  const [detailError, setDetailError] = useState<string>();

  useEffect(() => {
    let cancelled = false;
    api
      .list()
      .then((data) => {
        if (!cancelled) setInstallations(data);
      })
      .catch((reason: unknown) => {
        if (!cancelled) setLoadError(toMessage(reason));
      });
    return () => {
      cancelled = true;
    };
  }, [api]);

  const ordered = useMemo(
    () => (installations ? [...installations].sort((a, b) => HEALTH_RANK[b.health] - HEALTH_RANK[a.health]) : []),
    [installations],
  );

  async function openDiagnostics(installation: PlatformInstallationSummary) {
    setSelected(installation);
    setDiagnostics(undefined);
    setIncidents(undefined);
    setDetailError(undefined);
    setDetailBusy(true);
    try {
      const [diagnosticsResult, incidentsResult] = await Promise.all([
        api.diagnostics(installation.installationId),
        api.incidents(installation.installationId),
      ]);
      setDiagnostics(diagnosticsResult);
      setIncidents(incidentsResult);
    } catch (reason) {
      setDetailError(toMessage(reason));
    } finally {
      setDetailBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="installations-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · operação</p>
          <h1 className="db-page__title" id="installations-title">
            Instalações
          </h1>
          <p className="db-page__lead">
            Saúde de cada servidor local do parque — versão, último contato, atraso de
            sincronização e eventos pendentes. Descubra o problema antes de o cliente ligar.
          </p>
        </div>
      </header>

      {loadError ? <AlertBanner tone="danger">{loadError}</AlertBanner> : null}

      {!installations ? (
        <Card>
          <div className="db-loading" role="status">
            Carregando instalações…
          </div>
        </Card>
      ) : ordered.length === 0 ? (
        <EmptyState icon="dns" title="Nenhuma instalação ativa">
          Nenhum estabelecimento tem um servidor local instalado ainda.
        </EmptyState>
      ) : (
        <Card
          padding="none"
          title="Parque de instalações"
          subtitle={`${ordered.length} instalação${ordered.length === 1 ? '' : 'ões'} · ordenadas por criticidade`}
        >
          <DataTable
            rowKey="installationId"
            rows={ordered}
            onRowClick={(row) => void openDiagnostics(row)}
            columns={[
              { key: 'tenantName', header: 'Estabelecimento', render: (row) => row.tenantName },
              { key: 'storeName', header: 'Loja', render: (row) => row.storeName },
              {
                key: 'version',
                header: 'Versão',
                render: (row) => (
                  <span className="installations-version">
                    {row.version ?? '—'}
                    {row.version && row.version !== row.expectedVersion ? (
                      <Badge tone="warning" size="sm" title={`Esperada: ${row.expectedVersion}`}>
                        desatualizada
                      </Badge>
                    ) : null}
                  </span>
                ),
              },
              {
                key: 'lastSeenAt',
                header: 'Último contato',
                render: (row) => (row.lastSeenAt ? formatRelative(row.lastSeenAt) : 'Nunca'),
              },
              {
                key: 'syncLagSeconds',
                header: 'Atraso de sincronização',
                align: 'right',
                render: (row) => (row.syncLagSeconds === null ? '—' : formatDuration(row.syncLagSeconds)),
              },
              {
                key: 'pendingEvents',
                header: 'Eventos pendentes',
                numeric: true,
                render: (row) => row.pendingEvents,
              },
              {
                key: 'openAlerts',
                header: 'Alertas abertos',
                numeric: true,
                render: (row) => row.openAlerts,
              },
              {
                key: 'health',
                header: 'Saúde',
                render: (row) => (
                  <Badge tone={HEALTH_TONE[row.health]}>{HEALTH_LABEL[row.health]}</Badge>
                ),
              },
            ]}
          />
        </Card>
      )}

      <Modal
        open={!!selected}
        onClose={() => setSelected(undefined)}
        eyebrow={selected?.tenantName}
        title={selected ? `Diagnóstico — ${selected.storeName}` : 'Diagnóstico'}
      >
        {detailBusy ? (
          <div className="db-loading" role="status">
            Carregando diagnóstico…
          </div>
        ) : detailError ? (
          <AlertBanner tone="danger">{detailError}</AlertBanner>
        ) : (
          <div className="installations-detail">
            {selected ? (
              <Badge tone={HEALTH_TONE[selected.health]}>{HEALTH_LABEL[selected.health]}</Badge>
            ) : null}

            {diagnostics ? (
              <section aria-label="Health check remoto">
                <h3 className="installations-detail__heading">Health check</h3>
                <dl className="installations-healthcheck">
                  <div>
                    <dt>Postgres</dt>
                    <dd>{diagnostics.healthCheck.postgres ?? '—'}</dd>
                  </div>
                  <div>
                    <dt>Redis</dt>
                    <dd>{diagnostics.healthCheck.redis ?? '—'}</dd>
                  </div>
                  <div>
                    <dt>Sincronização</dt>
                    <dd>{diagnostics.healthCheck.sync ?? '—'}</dd>
                  </div>
                  <div>
                    <dt>Uso de disco</dt>
                    <dd>
                      {diagnostics.diskUsagePercent === null ? '—' : `${diagnostics.diskUsagePercent}%`}
                    </dd>
                  </div>
                  <div>
                    <dt>Último backup</dt>
                    <dd>{diagnostics.lastBackupAt ? formatRelative(diagnostics.lastBackupAt) : '—'}</dd>
                  </div>
                </dl>

                <h3 className="installations-detail__heading">Logs recentes</h3>
                {diagnostics.recentLogs.length === 0 ? (
                  <p className="db-hint">Nenhum evento técnico recente registrado.</p>
                ) : (
                  <ul className="installations-log-list nx-stagger">
                    {diagnostics.recentLogs.map((log) => (
                      <li key={`${log.occurredAt}-${log.message}`} data-level={log.level}>
                        <span className="installations-log-list__time">{formatRelative(log.occurredAt)}</span>
                        <span>{log.message}</span>
                      </li>
                    ))}
                  </ul>
                )}
              </section>
            ) : null}

            <section aria-label="Histórico de incidentes">
              <h3 className="installations-detail__heading">Histórico de incidentes</h3>
              {!incidents || incidents.length === 0 ? (
                <p className="db-hint">Nenhum incidente registrado para esta instalação.</p>
              ) : (
                <ul className="installations-incident-list nx-stagger">
                  {incidents.map((incident) => (
                    <li key={incident.id}>
                      <Badge tone={incident.type === 'OFFLINE' ? 'danger' : 'warning'} size="sm">
                        {incident.type === 'OFFLINE' ? 'Fora do ar' : 'Degradada'}
                      </Badge>
                      <span className="installations-incident-list__cause">
                        {incident.cause ?? 'Causa não registrada.'}
                      </span>
                      <span className="installations-incident-list__meta">
                        {incident.resolvedAt ? 'Resolvido' : 'Em aberto'} ·{' '}
                        {incident.durationSeconds === null ? '—' : formatDuration(incident.durationSeconds)}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </div>
        )}
      </Modal>
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

/** Duração curta em pt-BR (ex.: "4 min", "1 h 12 min") — atraso de sincronização e duração de incidente. */
function formatDuration(totalSeconds: number): string {
  if (totalSeconds < 60) return `${totalSeconds} s`;
  const totalMinutes = Math.round(totalSeconds / 60);
  if (totalMinutes < 60) return `${totalMinutes} min`;
  const hours = Math.floor(totalMinutes / 60);
  const minutes = totalMinutes % 60;
  return minutes === 0 ? `${hours} h` : `${hours} h ${minutes} min`;
}

/** "há N min/h/dias" a partir de um instante ISO — sem depender de biblioteca externa de data. */
function formatRelative(isoInstant: string): string {
  const elapsedMs = Date.now() - new Date(isoInstant).getTime();
  if (elapsedMs < 0) return 'agora';
  const elapsedSeconds = Math.floor(elapsedMs / 1000);
  if (elapsedSeconds < 60) return 'agora mesmo';
  if (elapsedSeconds < 3600) return `há ${Math.floor(elapsedSeconds / 60)} min`;
  if (elapsedSeconds < 86400) return `há ${Math.floor(elapsedSeconds / 3600)} h`;
  return `há ${Math.floor(elapsedSeconds / 86400)} dias`;
}
