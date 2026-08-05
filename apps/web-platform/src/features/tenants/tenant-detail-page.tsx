import { useEffect, useId, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Card, DataTable, EmptyState, Field, Icon, Input, Modal, ProgressMeter } from '@nexora/ui';
import {
  ONBOARDING_STEP_LABELS,
  type TenantOverviewInstallation,
  type TenantOverviewInstallationStatus,
  type TenantOverviewInviteStatus,
  type TenantOverviewResponse,
  type TenantOverviewStore,
  type TenantHealth,
  type TenantStatus,
} from '@nexora/contracts';

import {
  createTenantOverviewApi,
  isTenantStatusTransitionTarget,
  type ApiProblem,
  type TenantOverviewApi,
  type TenantStatusTransitionTarget,
} from './tenant-detail-api.js';
import { maskOwnerEmail } from './tenants-directory-view-model.js';
import './tenant-detail-page.css';

export interface TenantDetailPageProps {
  readonly tenantId: string;
  readonly api?: TenantOverviewApi;
  readonly onBack: () => void;
  readonly onRequestSupportAccess: (tenantId: string) => void;
  readonly onOpenInstallations: () => void;
  readonly onOpenBusinessTemplates: () => void;
}

const STATUS_LABEL: Record<TenantStatus, string> = {
  PROVISIONED: 'Provisionado',
  INSTALLING: 'Instalando',
  ACTIVE: 'Ativo',
  SUSPENDED: 'Suspenso',
  CANCELLED: 'Cancelado',
};

const STATUS_TONE: Record<TenantStatus, 'success' | 'warning' | 'danger' | 'neutral'> = {
  PROVISIONED: 'neutral',
  INSTALLING: 'neutral',
  ACTIVE: 'success',
  SUSPENDED: 'warning',
  CANCELLED: 'danger',
};

/**
 * US-153 · Ciclo de vida do estabelecimento — rótulo do botão de ação administrativa por status
 * ALVO. A máquina de estados em si nunca é decidida aqui: `overview.tenant.availableTransitions`
 * (computado no servidor) já filtra quais destes botões fazem sentido para o status atual.
 */
const TRANSITION_ACTION_LABEL: Record<TenantStatusTransitionTarget, string> = {
  SUSPENDED: 'Suspender',
  ACTIVE: 'Reativar',
  CANCELLED: 'Cancelar',
};

const TRANSITION_MODAL_TITLE: Record<TenantStatusTransitionTarget, string> = {
  SUSPENDED: 'Suspender estabelecimento?',
  ACTIVE: 'Reativar estabelecimento?',
  CANCELLED: 'Cancelar estabelecimento?',
};

/** §10 "Modal mostra impacto antes da confirmação" — texto substantivo, em PT-BR, por alvo. */
const TRANSITION_IMPACT_COPY: Record<TenantStatusTransitionTarget, string> = {
  SUSPENDED: 'Novas sessões de gestão e operação seguirão a política de suspensão até a reativação.',
  ACTIVE: 'As dependências técnicas serão validadas antes de reativar.',
  CANCELLED:
    'Esta ação é permanente. O estabelecimento e seus dados de histórico são preservados, mas o encerramento não pode ser desfeito.',
};

const TRANSITION_CONFIRM_LABEL: Record<TenantStatusTransitionTarget, string> = {
  SUSPENDED: 'Sim, suspender estabelecimento',
  ACTIVE: 'Sim, reativar estabelecimento',
  CANCELLED: 'Sim, cancelar estabelecimento',
};

const TRANSITION_DISMISS_LABEL: Record<TenantStatusTransitionTarget, string> = {
  SUSPENDED: 'Cancelar',
  ACTIVE: 'Cancelar',
  CANCELLED: 'Manter estabelecimento',
};

const INSTALLATION_STATUS_LABEL: Record<TenantOverviewInstallationStatus, string> = {
  PENDING: 'Pendente',
  ACTIVE: 'Ativa',
  OFFLINE: 'Offline',
};

const INSTALLATION_STATUS_TONE: Record<TenantOverviewInstallationStatus, 'neutral' | 'success' | 'warning'> = {
  PENDING: 'neutral',
  ACTIVE: 'success',
  OFFLINE: 'warning',
};

const HEALTH_LABEL: Record<TenantHealth, string> = {
  OK: 'Saudável',
  DEGRADED: 'Degradada',
  DOWN: 'Fora do ar',
  UNKNOWN: 'Desconhecida',
};

const HEALTH_TONE: Record<TenantHealth, 'success' | 'warning' | 'danger' | 'neutral'> = {
  OK: 'success',
  DEGRADED: 'warning',
  DOWN: 'danger',
  UNKNOWN: 'neutral',
};

const INVITE_STATUS_LABEL: Record<TenantOverviewInviteStatus, string> = {
  PENDING: 'Convite pendente',
  ACCEPTED: 'Convite aceito',
  EXPIRED: 'Convite expirado',
};

const INVITE_STATUS_TONE: Record<TenantOverviewInviteStatus, 'warning' | 'success' | 'danger'> = {
  PENDING: 'warning',
  ACCEPTED: 'success',
  EXPIRED: 'danger',
};

/** `yyyy-mm-ddTHH:mm:ssZ` → `DD/MM/AAAA HH:mm`, mesmo padrão de `tenants-directory-page.tsx`. */
function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar o estabelecimento.';
}

function isNotFound(reason: unknown): boolean {
  return typeof reason === 'object' && reason !== null && (reason as ApiProblem).status === 404;
}

/**
 * US-153 §4 cenário "Concorrência" — `CONCURRENCY_CONFLICT` ganha uma mensagem própria com a
 * instrução de recarregar (o `error.message` do servidor pode não deixar isso explícito); os
 * demais códigos (`REASON_REQUIRED`, `TENANT_STATUS_TRANSITION_INVALID`) já chegam em PT-BR
 * substantivo via ProblemDetails (ADR-021) e são exibidos como vieram.
 */
function toTransitionMessage(reason: unknown): string {
  const problem = reason as ApiProblem;
  if (problem?.code === 'CONCURRENCY_CONFLICT') {
    return 'Outro administrador já alterou este estabelecimento — recarregue a página e tente novamente.';
  }
  if (reason instanceof Error && reason.message) return reason.message;
  return 'Não foi possível concluir a transição de status.';
}

/**
 * US-152 · Visão 360 e acesso aos módulos do estabelecimento — ficha administrativa completa de
 * um tenant (cadastro, proprietário, lojas, instalações, checklist de implantação e links).
 * RN-015 "isolamento total": esta tela agrega só metadados administrativos — nenhum pedido, caixa,
 * estoque ou financeiro do cliente aparece aqui. A única porta para algo próximo de dado de
 * negócio é `onRequestSupportAccess`, que inicia o fluxo auditado da US-145; a tela nunca concede
 * acesso implícito.
 */
export function TenantDetailPage({
  tenantId,
  api: providedApi,
  onBack,
  onRequestSupportAccess,
  onOpenInstallations,
  onOpenBusinessTemplates,
}: Readonly<TenantDetailPageProps>) {
  const api = useMemo(() => providedApi ?? createTenantOverviewApi(), [providedApi]);

  const [overview, setOverview] = useState<TenantOverviewResponse>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>();

  // US-153 · Ciclo de vida do estabelecimento — estado do diálogo de confirmação de transição.
  const [transitionTarget, setTransitionTarget] = useState<TenantStatusTransitionTarget>();
  const [transitionReason, setTransitionReason] = useState('');
  const [slugConfirmation, setSlugConfirmation] = useState('');
  const [transitionBusy, setTransitionBusy] = useState(false);
  const [transitionError, setTransitionError] = useState('');
  const transitionReasonFieldId = useId();
  const slugConfirmationFieldId = useId();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(undefined);
    api
      .get(tenantId)
      .then((result) => {
        if (!cancelled) setOverview(result);
      })
      .catch((reason: unknown) => {
        if (!cancelled) setError(reason);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [api, tenantId]);

  const notFound = error !== undefined && isNotFound(error);

  function openTransition(target: TenantStatusTransitionTarget) {
    setTransitionTarget(target);
    setTransitionReason('');
    setSlugConfirmation('');
    setTransitionError('');
  }

  function closeTransition() {
    if (transitionBusy) return;
    setTransitionTarget(undefined);
  }

  async function confirmTransition() {
    if (!overview || !transitionTarget) return;
    setTransitionBusy(true);
    setTransitionError('');
    try {
      await api.transitionStatus(tenantId, overview.tenant.statusVersion, {
        targetStatus: transitionTarget,
        reason: transitionReason.trim(),
      });
      setTransitionTarget(undefined);
      setTransitionReason('');
      setSlugConfirmation('');
      // Recarrega a visão 360 inteira — mais simples e correto que reconstruir localmente
      // `availableTransitions` (máquina de estados que só o servidor conhece).
      try {
        const refreshed = await api.get(tenantId);
        setOverview(refreshed);
      } catch {
        // Falha ao atualizar após sucesso não é fatal — a transição já foi confirmada pelo
        // servidor; a próxima ação do administrador tenta o `get` de novo.
      }
    } catch (caught) {
      setTransitionError(toTransitionMessage(caught));
    } finally {
      setTransitionBusy(false);
    }
  }

  const transitionReasonTrimmed = transitionReason.trim();
  const canConfirmTransition =
    transitionReasonTrimmed.length > 0 &&
    (transitionTarget !== 'CANCELLED' || slugConfirmation === overview?.tenant.slug);

  return (
    <main className="db-page nx-anim-in" aria-labelledby="tenant-detail-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · estabelecimento</p>
          <h1 className="db-page__title" id="tenant-detail-title">
            {overview?.tenant.name ?? 'Detalhe do estabelecimento'}
          </h1>
          <p className="db-page__lead db-code">{tenantId}</p>
        </div>
        <div className="db-page__actions">
          {overview ? (
            <Button type="button" variant="secondary" onClick={() => onRequestSupportAccess(tenantId)}>
              <Icon name="support_agent" /> Solicitar acesso de suporte
            </Button>
          ) : null}
          <Button type="button" variant="ghost" onClick={onBack}>
            <Icon name="arrow_back" /> Voltar ao diretório
          </Button>
        </div>
      </header>

      {loading ? (
        <Card>
          <div className="db-loading" role="status">
            <span className="nx-spinner" aria-hidden="true" />
            Carregando estabelecimento…
          </div>
        </Card>
      ) : notFound ? (
        <EmptyState icon="search_off" title="Estabelecimento não encontrado">
          <p className="db-hint">Este estabelecimento não existe ou foi removido.</p>
          <Button type="button" variant="secondary" size="sm" onClick={onBack}>
            <Icon name="arrow_back" /> Voltar ao diretório
          </Button>
        </EmptyState>
      ) : error !== undefined ? (
        <AlertBanner tone="danger" title="Não foi possível carregar o estabelecimento">
          {toMessage(error)}
        </AlertBanner>
      ) : overview ? (
        <div className="db-stack nx-stagger">
          <Card
            title="Cabeçalho"
            actions={
              overview.tenant.availableTransitions.filter(isTenantStatusTransitionTarget).length > 0 ? (
                <div className="tenant-detail__status-actions">
                  {overview.tenant.availableTransitions.filter(isTenantStatusTransitionTarget).map((target) => (
                    <Button
                      key={target}
                      type="button"
                      variant={target === 'CANCELLED' ? 'danger' : 'secondary'}
                      size="sm"
                      onClick={() => openTransition(target)}
                    >
                      {TRANSITION_ACTION_LABEL[target]}
                    </Button>
                  ))}
                </div>
              ) : null
            }
          >
            <dl className="tenant-detail__kv">
              <div>
                <dt>Nome</dt>
                <dd>{overview.tenant.name}</dd>
              </div>
              <div>
                <dt>Endereço</dt>
                <dd className="db-code">{overview.tenant.slug}</dd>
              </div>
              <div>
                <dt>Status</dt>
                <dd>
                  <Badge tone={STATUS_TONE[overview.tenant.status]}>{STATUS_LABEL[overview.tenant.status]}</Badge>
                </dd>
              </div>
              <div>
                <dt>Plano</dt>
                <dd>{overview.tenant.plan}</dd>
              </div>
              <div>
                <dt>Modelo de negócio</dt>
                <dd>{overview.tenant.template ?? 'Não configurado'}</dd>
              </div>
              <div>
                <dt>Domínio</dt>
                <dd>{overview.tenant.domain ?? 'Não configurado'}</dd>
              </div>
              <div>
                <dt>Criado em</dt>
                <dd>{formatDateTime(overview.tenant.createdAt)}</dd>
              </div>
              <div>
                <dt>Atualizado em</dt>
                <dd>{formatDateTime(overview.tenant.updatedAt)}</dd>
              </div>
              <div>
                <dt>ID</dt>
                <dd className="db-code">{overview.tenant.id}</dd>
              </div>
            </dl>
          </Card>

          <Card
            title="Resumo de branding e configuração"
            actions={
              <Button type="button" variant="ghost" size="sm" onClick={onOpenBusinessTemplates}>
                Ver modelos de negócio
              </Button>
            }
          >
            <dl className="tenant-detail__kv">
              <div>
                <dt>Plano</dt>
                <dd>{overview.tenant.plan}</dd>
              </div>
              <div>
                <dt>Modelo de negócio</dt>
                <dd>{overview.tenant.template ?? 'Não configurado'}</dd>
              </div>
            </dl>
          </Card>

          <Card title="Proprietário">
            {overview.owner ? (
              <dl className="tenant-detail__kv">
                <div>
                  <dt>Nome</dt>
                  <dd>{overview.owner.name}</dd>
                </div>
                <div>
                  <dt>E-mail</dt>
                  <dd>{maskOwnerEmail(overview.owner.email)}</dd>
                </div>
                <div>
                  <dt>Convite</dt>
                  <dd>
                    <Badge tone={INVITE_STATUS_TONE[overview.owner.inviteStatus]}>
                      {INVITE_STATUS_LABEL[overview.owner.inviteStatus]}
                    </Badge>
                  </dd>
                </div>
              </dl>
            ) : (
              <p className="db-hint">Não configurado</p>
            )}
          </Card>

          <Card title="Lojas" padding={overview.stores.length === 0 ? 'default' : 'none'}>
            {overview.stores.length === 0 ? (
              <EmptyState icon="storefront" title="Nenhuma loja cadastrada">
                Este estabelecimento ainda não tem loja cadastrada.
              </EmptyState>
            ) : (
              <DataTable<TenantOverviewStore>
                rowKey="id"
                rows={overview.stores}
                columns={[
                  { key: 'name', header: 'Nome', render: (row) => row.name },
                  { key: 'timezone', header: 'Fuso horário', render: (row) => row.timezone },
                  { key: 'id', header: 'ID', render: (row) => <span className="db-code">{row.id}</span> },
                ]}
              />
            )}
          </Card>

          <Card title="Instalações" padding={overview.installations.length === 0 ? 'default' : 'none'}>
            {overview.installations.length === 0 ? (
              <EmptyState icon="dns" title="Nenhuma instalação registrada">
                Este estabelecimento ainda não tem servidor local instalado.
              </EmptyState>
            ) : (
              <DataTable<TenantOverviewInstallation>
                rowKey="id"
                rows={overview.installations}
                columns={[
                  { key: 'label', header: 'Instalação', render: (row) => row.label },
                  {
                    key: 'status',
                    header: 'Status',
                    render: (row) => (
                      <Badge tone={INSTALLATION_STATUS_TONE[row.status]}>{INSTALLATION_STATUS_LABEL[row.status]}</Badge>
                    ),
                  },
                  {
                    key: 'health',
                    header: 'Saúde',
                    render: (row) => <Badge tone={HEALTH_TONE[row.health]}>{HEALTH_LABEL[row.health]}</Badge>,
                  },
                  { key: 'id', header: 'ID', render: (row) => <span className="db-code">{row.id}</span> },
                ]}
              />
            )}
          </Card>

          <Card title="Checklist de implantação">
            <ProgressMeter
              label="Passos concluídos"
              value={overview.deployment.completed}
              max={overview.deployment.total}
              display={`${overview.deployment.completed}/${overview.deployment.total}`}
              tone={overview.deployment.completed === overview.deployment.total ? 'success' : 'brand'}
            />
            {overview.deployment.nextAction ? (
              <AlertBanner tone="warning" title="Próxima ação recomendada">
                {ONBOARDING_STEP_LABELS[overview.deployment.nextAction]} — outras pendências do checklist
                podem continuar em aberto além desta.
              </AlertBanner>
            ) : (
              <AlertBanner tone="success" title="Implantação concluída">
                Todos os passos do roteiro de implantação foram concluídos.
              </AlertBanner>
            )}
          </Card>

          <Card title="Links">
            <ul className="tenant-detail__links">
              <li>
                <span className="tenant-detail__links-label">
                  <strong>Cardápio público</strong>
                </span>
                {overview.links.publicMenu ? (
                  <a
                    className="tenant-detail__links-value"
                    href={overview.links.publicMenu}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {overview.links.publicMenu} <Icon name="open_in_new" size={16} />
                  </a>
                ) : (
                  <span className="db-hint">Não configurado</span>
                )}
              </li>
              <li>
                <span className="tenant-detail__links-label">
                  <strong>Painel administrativo</strong>
                </span>
                {overview.links.admin ? (
                  <a
                    className="tenant-detail__links-value"
                    href={overview.links.admin}
                    target="_blank"
                    rel="noopener noreferrer"
                  >
                    {overview.links.admin} <Icon name="open_in_new" size={16} />
                  </a>
                ) : (
                  <span className="db-hint">Não configurado</span>
                )}
              </li>
              <li>
                <span className="tenant-detail__links-label">
                  <strong>Instalações</strong>
                  <span className="db-hint">Saúde do parque local — sempre disponível.</span>
                </span>
                <Button type="button" variant="secondary" size="sm" onClick={onOpenInstallations}>
                  <Icon name="dns" /> Ver instalações
                </Button>
              </li>
            </ul>
          </Card>
        </div>
      ) : null}

      {overview && transitionTarget ? (
        <Modal
          open
          onClose={closeTransition}
          tone={transitionTarget === 'CANCELLED' ? 'danger' : 'default'}
          eyebrow="Ciclo de vida do estabelecimento"
          title={TRANSITION_MODAL_TITLE[transitionTarget]}
          actions={
            <>
              <Button type="button" variant="ghost" onClick={closeTransition} disabled={transitionBusy}>
                {TRANSITION_DISMISS_LABEL[transitionTarget]}
              </Button>
              <Button
                type="button"
                variant={transitionTarget === 'CANCELLED' ? 'danger' : 'primary'}
                busy={transitionBusy}
                disabled={!canConfirmTransition}
                onClick={() => void confirmTransition()}
              >
                {TRANSITION_CONFIRM_LABEL[transitionTarget]}
              </Button>
            </>
          }
        >
          <p>{TRANSITION_IMPACT_COPY[transitionTarget]}</p>
          {transitionError ? <AlertBanner tone="danger">{transitionError}</AlertBanner> : null}
          <Field label="Motivo" htmlFor={transitionReasonFieldId}>
            <Input
              id={transitionReasonFieldId}
              required
              value={transitionReason}
              onChange={(event) => setTransitionReason(event.target.value)}
            />
          </Field>
          {transitionTarget === 'CANCELLED' ? (
            <Field
              label={`Digite "${overview.tenant.slug}" para confirmar`}
              htmlFor={slugConfirmationFieldId}
              hint="Confirmação extra só nesta tela — não é enviada ao servidor."
            >
              <Input
                id={slugConfirmationFieldId}
                value={slugConfirmation}
                onChange={(event) => setSlugConfirmation(event.target.value)}
              />
            </Field>
          ) : null}
        </Modal>
      ) : null}
    </main>
  );
}
