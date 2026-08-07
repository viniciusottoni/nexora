import { useCallback, useEffect, useId, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  EmptyState,
  Field,
  Icon,
  Input,
  Modal,
} from '@nexora/ui';
import type { AttentionItemType, AttentionQueueItem, AttentionSeverity } from '@nexora/contracts';

import { createPlatformAttentionApi, type PlatformAttentionApi } from './platform-attention-api.js';
import './platform-attention-page.css';

export interface PlatformAttentionPageProps {
  readonly api?: PlatformAttentionApi;
  readonly initialSeverity?: readonly AttentionSeverity[];
  readonly navigate: (path: string) => void;
  readonly onRequestSupportAccess: (tenantId: string) => void;
}

const SEVERITY_OPTIONS: readonly AttentionSeverity[] = ['CRITICAL', 'HIGH', 'MEDIUM', 'LOW'];

const SEVERITY_LABEL: Record<AttentionSeverity, string> = {
  CRITICAL: 'Crítica',
  HIGH: 'Alta',
  MEDIUM: 'Média',
  LOW: 'Baixa',
};

const SEVERITY_TONE: Record<AttentionSeverity, 'danger' | 'warning' | 'neutral'> = {
  CRITICAL: 'danger',
  HIGH: 'danger',
  MEDIUM: 'warning',
  LOW: 'neutral',
};

const TYPE_LABEL: Record<AttentionItemType, string> = {
  INSTALLATION_OFFLINE: 'Instalação fora do ar',
  INSTALLATION_DEGRADED: 'Instalação degradada',
  INVITE_EXPIRED: 'Convite expirado',
  PROVISIONING_STALLED: 'Provisionamento parado',
};

const TYPE_ICON: Record<AttentionItemType, string> = {
  INSTALLATION_OFFLINE: 'cloud_off',
  INSTALLATION_DEGRADED: 'sync_problem',
  INVITE_EXPIRED: 'mail',
  PROVISIONING_STALLED: 'hourglass_bottom',
};

const IS_INSTALLATION_ISSUE = (type: AttentionItemType) =>
  type === 'INSTALLATION_OFFLINE' || type === 'INSTALLATION_DEGRADED';

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

function triggerDownload(blob: Blob, fileName: string) {
  const url = URL.createObjectURL(blob);
  const link = document.createElement('a');
  link.href = url;
  link.download = fileName;
  document.body.appendChild(link);
  link.click();
  link.remove();
  URL.revokeObjectURL(url);
}

/**
 * US-157 · Central operacional, auditoria e atalhos de suporte — página raiz da central: fila
 * priorizada de atenção (Gherkin "Priorização explicável"), com filtro por severidade, atalhos
 * contextuais para diagnóstico (US-140) e suporte (US-145), reconhecimento sem apagar o fato
 * original (RN-004), exportação auditável e indicação de horário da última coleta mesmo quando
 * alguma fonte falha (Gherkin "Falha parcial").
 */
export function PlatformAttentionPage({
  api: providedApi,
  initialSeverity,
  navigate,
  onRequestSupportAccess,
}: Readonly<PlatformAttentionPageProps>) {
  const api = useMemo(() => providedApi ?? createPlatformAttentionApi(), [providedApi]);

  const [severity, setSeverity] = useState<readonly AttentionSeverity[]>(initialSeverity ?? []);
  const [items, setItems] = useState<AttentionQueueItem[]>();
  const [nextCursor, setNextCursor] = useState<string | null>(null);
  const [collectedAt, setCollectedAt] = useState<string>();
  const [unavailableSources, setUnavailableSources] = useState<readonly string[]>([]);
  const [loading, setLoading] = useState(true);
  const [loadingMore, setLoadingMore] = useState(false);
  const [error, setError] = useState<unknown>();

  const [exportBusy, setExportBusy] = useState(false);
  const [exportError, setExportError] = useState('');

  const [ackTarget, setAckTarget] = useState<AttentionQueueItem>();
  const [ackReason, setAckReason] = useState('');
  const [ackBusy, setAckBusy] = useState(false);
  const [ackError, setAckError] = useState('');
  const ackReasonFieldId = useId();

  const load = useCallback(
    async (options?: { append?: boolean; cursor?: string }) => {
      const append = options?.append ?? false;
      if (append) setLoadingMore(true);
      else {
        setLoading(true);
        setError(undefined);
      }
      try {
        const cursor = append ? options?.cursor : undefined;
        const result = await api.list({
          ...(severity.length > 0 ? { severity } : {}),
          ...(cursor ? { cursor } : {}),
        });
        setItems((previous) => (append ? [...(previous ?? []), ...result.data] : result.data));
        setNextCursor(result.nextCursor);
        setCollectedAt(result.meta.collectedAt);
        setUnavailableSources(result.meta.unavailableSources);
      } catch (caught) {
        if (append) setError(caught);
        else setError(caught);
      } finally {
        if (append) setLoadingMore(false);
        else setLoading(false);
      }
    },
    [api, severity],
  );

  useEffect(() => {
    void load();
  }, [load]);

  function toggleSeverity(value: AttentionSeverity) {
    setSeverity((current) =>
      current.includes(value) ? current.filter((s) => s !== value) : [...current, value],
    );
  }

  async function exportCsv() {
    setExportBusy(true);
    setExportError('');
    try {
      const blob = await api.exportCsv(severity.length > 0 ? { severity } : {});
      triggerDownload(blob, `central-de-atencao-${new Date().toISOString().slice(0, 10)}.csv`);
    } catch (caught) {
      setExportError(toMessage(caught));
    } finally {
      setExportBusy(false);
    }
  }

  function openAcknowledge(item: AttentionQueueItem) {
    setAckTarget(item);
    setAckReason('');
    setAckError('');
  }

  function closeAcknowledge() {
    if (ackBusy) return;
    setAckTarget(undefined);
  }

  async function confirmAcknowledge() {
    if (!ackTarget) return;
    setAckBusy(true);
    setAckError('');
    try {
      await api.acknowledge(ackTarget.id, { reason: ackReason.trim() });
      setItems((previous) => previous?.filter((entry) => entry.id !== ackTarget.id));
      setAckTarget(undefined);
    } catch (caught) {
      setAckError(toMessage(caught));
    } finally {
      setAckBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="attention-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · central operacional</p>
          <h1 className="db-page__title" id="attention-title">
            Central de atenção
          </h1>
          <p className="db-page__lead">
            Fila priorizada de estabelecimentos que exigem ação — instalação offline, convite
            expirado ou provisionamento parado — ordenada por criticidade, sem esconder itens menos
            graves.
          </p>
        </div>
        <div className="db-page__actions">
          <Button
            type="button"
            variant="secondary"
            busy={exportBusy}
            onClick={() => void exportCsv()}
          >
            <Icon name="download" /> Exportar CSV
          </Button>
        </div>
      </header>

      {exportError ? <AlertBanner tone="danger">{exportError}</AlertBanner> : null}

      <Card padding="default" className="platform-attention__filters-card">
        <div
          className="platform-attention__filters"
          role="group"
          aria-label="Filtrar por severidade"
        >
          {SEVERITY_OPTIONS.map((option) => {
            const active = severity.includes(option);
            return (
              <button
                key={option}
                type="button"
                className={`platform-attention__filter-chip${active ? ' platform-attention__filter-chip--active' : ''}`}
                aria-pressed={active}
                onClick={() => toggleSeverity(option)}
              >
                <Badge tone={SEVERITY_TONE[option]} size="sm">
                  {SEVERITY_LABEL[option]}
                </Badge>
              </button>
            );
          })}
          {severity.length > 0 ? (
            <Button type="button" variant="ghost" size="sm" onClick={() => setSeverity([])}>
              Limpar filtro
            </Button>
          ) : null}
        </div>
        {collectedAt ? (
          <p className="platform-attention__collected-at db-hint">
            Última coleta: {formatDateTime(collectedAt)}
          </p>
        ) : null}
      </Card>

      {unavailableSources.length > 0 ? (
        <AlertBanner
          tone="warning"
          title="Algumas fontes de dado estão temporariamente indisponíveis"
        >
          Os dados administrativos disponíveis continuam visíveis abaixo. Fontes indisponíveis nesta
          coleta: {unavailableSources.join(', ')}.
          {collectedAt ? ` Horário da última coleta: ${formatDateTime(collectedAt)}.` : ''}
        </AlertBanner>
      ) : null}

      {loading ? (
        <Card>
          <div className="db-loading" role="status">
            <span className="nx-spinner" aria-hidden="true" />
            Carregando fila de atenção…
          </div>
        </Card>
      ) : error !== undefined ? (
        <AlertBanner tone="danger" title="Não foi possível carregar a fila de atenção">
          {toMessage(error)}
        </AlertBanner>
      ) : !items || items.length === 0 ? (
        <EmptyState icon="task_alt" title="Nenhuma pendência no momento">
          Nenhum estabelecimento exige atenção com os filtros atuais.
        </EmptyState>
      ) : (
        <>
          <ul className="platform-attention__list nx-stagger">
            {items.map((item) => (
              <li key={item.id} className="platform-attention__item">
                <span className="platform-attention__icon" aria-hidden="true">
                  <Icon name={TYPE_ICON[item.type]} />
                </span>
                <div className="platform-attention__content">
                  <div className="platform-attention__head">
                    <Badge tone={SEVERITY_TONE[item.severity]}>
                      {SEVERITY_LABEL[item.severity]}
                    </Badge>
                    <span className="platform-attention__type">{TYPE_LABEL[item.type]}</span>
                    <span className="platform-attention__tenant">{item.tenantName}</span>
                  </div>
                  <p className="platform-attention__reason">{item.reason}</p>
                  <p className="db-hint">Desde {formatDateTime(item.since)}</p>
                </div>
                <div className="platform-attention__actions">
                  <Button
                    type="button"
                    variant="secondary"
                    size="sm"
                    onClick={() => navigate(item.action.href)}
                  >
                    {item.action.kind === 'OPEN_DIAGNOSTICS'
                      ? 'Ver diagnóstico'
                      : 'Abrir estabelecimento'}
                  </Button>
                  {IS_INSTALLATION_ISSUE(item.type) ? (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      onClick={() => onRequestSupportAccess(item.tenantId)}
                    >
                      <Icon name="support_agent" size={16} /> Solicitar suporte
                    </Button>
                  ) : null}
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => openAcknowledge(item)}
                  >
                    Reconhecer
                  </Button>
                </div>
              </li>
            ))}
          </ul>

          {nextCursor ? (
            <div className="platform-attention__load-more">
              <Button
                type="button"
                variant="secondary"
                busy={loadingMore}
                onClick={() => void load({ append: true, cursor: nextCursor })}
              >
                Carregar mais
              </Button>
            </div>
          ) : null}
        </>
      )}

      {ackTarget ? (
        <Modal
          open
          onClose={closeAcknowledge}
          eyebrow="Reconhecer pendência"
          title={`Reconhecer: ${TYPE_LABEL[ackTarget.type]}`}
          actions={
            <>
              <Button type="button" variant="ghost" onClick={closeAcknowledge} disabled={ackBusy}>
                Cancelar
              </Button>
              <Button
                type="button"
                variant="primary"
                busy={ackBusy}
                disabled={ackReason.trim().length === 0}
                onClick={() => void confirmAcknowledge()}
              >
                Confirmar reconhecimento
              </Button>
            </>
          }
        >
          <p>
            {ackTarget.tenantName} — {ackTarget.reason}. O reconhecimento remove este item da fila
            ativa sem apagar o fato original; se a condição persistir ou se repetir, ela volta a
            aparecer.
          </p>
          {ackError ? <AlertBanner tone="danger">{ackError}</AlertBanner> : null}
          <Field
            label="Motivo"
            htmlFor={ackReasonFieldId}
            required
            hint="Obrigatório — registrado com autor e data."
          >
            <Input
              id={ackReasonFieldId}
              required
              value={ackReason}
              onChange={(event) => setAckReason(event.target.value)}
              placeholder="Ex.: Cliente avisado, aguardando retorno."
            />
          </Field>
        </Modal>
      ) : null}
    </main>
  );
}
