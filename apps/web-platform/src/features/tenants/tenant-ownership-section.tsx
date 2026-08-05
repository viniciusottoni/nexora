import { useEffect, useId, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Card, DataTable, EmptyState, Field, Icon, Input, Modal, Switch } from '@nexora/ui';

import {
  createTenantOwnershipApi,
  type ApiProblem,
  type TenantOwnershipApi,
  type TenantOwnershipDeliveryStatus,
  type TenantOwnershipInvite,
  type TenantOwnershipInviteStatus,
  type TenantOwnershipOwnerStatus,
  type TenantOwnershipTransferHistory,
  type TenantOwnershipView,
} from './tenant-ownership-api.js';
import { maskOwnerEmail } from './tenants-directory-view-model.js';
import './tenant-ownership-section.css';

export interface TenantOwnershipSectionProps {
  readonly tenantId: string;
  readonly api?: TenantOwnershipApi;
}

const OWNER_STATUS_LABEL: Record<TenantOwnershipOwnerStatus, string> = {
  NONE: 'Sem proprietário',
  INVITED: 'Convidado',
  ACTIVE: 'Ativo',
  INACTIVE: 'Inativo',
  BLOCKED: 'Bloqueado',
};

const OWNER_STATUS_TONE: Record<TenantOwnershipOwnerStatus, 'neutral' | 'warning' | 'success' | 'danger'> = {
  NONE: 'neutral',
  INVITED: 'warning',
  ACTIVE: 'success',
  INACTIVE: 'neutral',
  BLOCKED: 'danger',
};

const INVITE_STATUS_LABEL: Record<TenantOwnershipInviteStatus, string> = {
  PENDING: 'Pendente',
  ACCEPTED: 'Aceito',
  EXPIRED: 'Expirado',
  REVOKED: 'Revogado',
};

const INVITE_STATUS_TONE: Record<TenantOwnershipInviteStatus, 'warning' | 'success' | 'danger' | 'neutral'> = {
  PENDING: 'warning',
  ACCEPTED: 'success',
  EXPIRED: 'danger',
  REVOKED: 'neutral',
};

const DELIVERY_STATUS_LABEL: Record<TenantOwnershipDeliveryStatus, string> = {
  PENDING: 'Entrega pendente',
  SENT: 'Entregue',
  FAILED: 'Falha na entrega',
  UNKNOWN: 'Sem informação',
};

const DELIVERY_STATUS_TONE: Record<TenantOwnershipDeliveryStatus, 'warning' | 'success' | 'danger' | 'neutral'> = {
  PENDING: 'warning',
  SENT: 'success',
  FAILED: 'danger',
  UNKNOWN: 'neutral',
};

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar a titularidade do estabelecimento.';
}

/**
 * US-155 · Proprietários, usuários iniciais e convites — seção administrativa AUTOCONTIDA (recebe
 * `tenantId` como prop e faz o próprio fetch, não depende de `tenant-detail-page.tsx`; integração
 * central posterior importa e renderiza este componente dentro de um `Card` já existente lá, mesmo
 * padrão de `tenant-plan-section.tsx`, US-154). Cobre: estado do proprietário (convidado/ativo/
 * bloqueado/sem proprietário), histórico de convites (nunca segredo — só metadados), reenvio/
 * correção, revogação, transferência de titularidade e desbloqueio administrativo (nunca senha).
 */
export function TenantOwnershipSection({ tenantId, api: providedApi }: Readonly<TenantOwnershipSectionProps>) {
  const api = useMemo(() => providedApi ?? createTenantOwnershipApi(), [providedApi]);

  const [view, setView] = useState<TenantOwnershipView>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>();

  const [reissueOpen, setReissueOpen] = useState(false);
  const [reissueName, setReissueName] = useState('');
  const [reissueEmail, setReissueEmail] = useState('');
  const [reissueReason, setReissueReason] = useState('');
  const [reissueBusy, setReissueBusy] = useState(false);
  const [reissueError, setReissueError] = useState('');

  const [revokeTarget, setRevokeTarget] = useState<TenantOwnershipInvite>();
  const [revokeReason, setRevokeReason] = useState('');
  const [revokeBusy, setRevokeBusy] = useState(false);
  const [revokeError, setRevokeError] = useState('');

  const [transferOpen, setTransferOpen] = useState(false);
  const [transferNewOwnerId, setTransferNewOwnerId] = useState('');
  const [transferReason, setTransferReason] = useState('');
  const [transferKeepAsAdmin, setTransferKeepAsAdmin] = useState(false);
  const [transferBusy, setTransferBusy] = useState(false);
  const [transferError, setTransferError] = useState('');

  const [unlockOpen, setUnlockOpen] = useState(false);
  const [unlockReason, setUnlockReason] = useState('');
  const [unlockBusy, setUnlockBusy] = useState(false);
  const [unlockError, setUnlockError] = useState('');

  const reissueNameFieldId = useId();
  const reissueEmailFieldId = useId();
  const reissueReasonFieldId = useId();
  const revokeReasonFieldId = useId();
  const transferNewOwnerFieldId = useId();
  const transferReasonFieldId = useId();
  const unlockReasonFieldId = useId();

  async function reload() {
    setLoading(true);
    setError(undefined);
    try {
      setView(await api.get(tenantId));
    } catch (caught) {
      setError(caught);
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(undefined);
    api
      .get(tenantId)
      .then((result) => {
        if (!cancelled) setView(result);
      })
      .catch((caught: unknown) => {
        if (!cancelled) setError(caught);
      })
      .finally(() => {
        if (!cancelled) setLoading(false);
      });
    return () => {
      cancelled = true;
    };
  }, [api, tenantId]);

  function openReissue() {
    setReissueName(view?.owner.name ?? '');
    setReissueEmail(view?.owner.email ?? '');
    setReissueReason('');
    setReissueError('');
    setReissueOpen(true);
  }

  async function confirmReissue() {
    setReissueBusy(true);
    setReissueError('');
    try {
      await api.createInvite(tenantId, {
        name: reissueName.trim(),
        email: reissueEmail.trim(),
        reason: reissueReason.trim(),
      });
      setReissueOpen(false);
      await reload();
    } catch (caught) {
      setReissueError(toMessage(caught));
    } finally {
      setReissueBusy(false);
    }
  }

  function openRevoke(invite: TenantOwnershipInvite) {
    setRevokeTarget(invite);
    setRevokeReason('');
    setRevokeError('');
  }

  async function confirmRevoke() {
    if (!revokeTarget) return;
    setRevokeBusy(true);
    setRevokeError('');
    try {
      await api.revokeInvite(tenantId, revokeTarget.id, revokeReason.trim());
      setRevokeTarget(undefined);
      await reload();
    } catch (caught) {
      setRevokeError(toMessage(caught));
    } finally {
      setRevokeBusy(false);
    }
  }

  function openTransfer() {
    setTransferNewOwnerId('');
    setTransferReason('');
    setTransferKeepAsAdmin(false);
    setTransferError('');
    setTransferOpen(true);
  }

  async function confirmTransfer() {
    setTransferBusy(true);
    setTransferError('');
    try {
      await api.transferOwnership(tenantId, {
        newOwnerUserId: transferNewOwnerId.trim(),
        reason: transferReason.trim(),
        keepPreviousAsAdmin: transferKeepAsAdmin,
      });
      setTransferOpen(false);
      await reload();
    } catch (caught) {
      setTransferError(toMessage(caught));
    } finally {
      setTransferBusy(false);
    }
  }

  function openUnlock() {
    setUnlockReason('');
    setUnlockError('');
    setUnlockOpen(true);
  }

  async function confirmUnlock() {
    setUnlockBusy(true);
    setUnlockError('');
    try {
      await api.unlock(tenantId, unlockReason.trim());
      setUnlockOpen(false);
      await reload();
    } catch (caught) {
      setUnlockError(toMessage(caught));
    } finally {
      setUnlockBusy(false);
    }
  }

  const owner = view?.owner;
  const canReissue = owner?.status === 'INVITED';
  const canTransfer = owner?.id != null && owner.status !== 'NONE';
  const canUnlock = owner?.status === 'BLOCKED';

  return (
    <Card
      title="Titularidade e acesso inicial"
      actions={
        view ? (
          <div className="tenant-ownership__header-actions">
            {canReissue ? (
              <Button type="button" variant="secondary" size="sm" onClick={openReissue}>
                <Icon name="mail" /> Reenviar/corrigir convite
              </Button>
            ) : null}
            {canUnlock ? (
              <Button type="button" variant="secondary" size="sm" onClick={openUnlock}>
                <Icon name="lock_open" /> Desbloquear acesso
              </Button>
            ) : null}
            {canTransfer ? (
              <Button type="button" variant="primary" size="sm" onClick={openTransfer}>
                <Icon name="sync_alt" /> Transferir titularidade
              </Button>
            ) : null}
          </div>
        ) : null
      }
    >
      {loading ? (
        <div className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando titularidade…
        </div>
      ) : error !== undefined ? (
        <AlertBanner tone="danger" title="Não foi possível carregar a titularidade">
          {toMessage(error)}
        </AlertBanner>
      ) : view ? (
        <div className="db-stack nx-stagger">
          <dl className="tenant-ownership__kv">
            <div>
              <dt>Proprietário</dt>
              <dd>{owner?.name ?? 'Não informado'}</dd>
            </div>
            <div>
              <dt>E-mail</dt>
              <dd>{maskOwnerEmail(owner?.email)}</dd>
            </div>
            <div>
              <dt>Estado do acesso</dt>
              <dd>
                <Badge tone={OWNER_STATUS_TONE[owner?.status ?? 'NONE']}>
                  {OWNER_STATUS_LABEL[owner?.status ?? 'NONE']}
                </Badge>
              </dd>
            </div>
          </dl>

          <div>
            <p className="db-hint">Histórico de convites</p>
            {view.invites.length === 0 ? (
              <EmptyState icon="mail" title="Nenhum convite registrado">
                Este estabelecimento ainda não teve nenhum convite de proprietário emitido.
              </EmptyState>
            ) : (
              <DataTable<TenantOwnershipInvite>
                rowKey="id"
                rows={view.invites}
                columns={[
                  { key: 'sentTo', header: 'Enviado para', render: (row) => maskOwnerEmail(row.sentTo) },
                  {
                    key: 'status',
                    header: 'Status',
                    render: (row) => <Badge tone={INVITE_STATUS_TONE[row.status]}>{INVITE_STATUS_LABEL[row.status]}</Badge>,
                  },
                  {
                    key: 'deliveryStatus',
                    header: 'Entrega',
                    render: (row) => (
                      <Badge tone={DELIVERY_STATUS_TONE[row.deliveryStatus]}>{DELIVERY_STATUS_LABEL[row.deliveryStatus]}</Badge>
                    ),
                  },
                  { key: 'expiresAt', header: 'Expira em', render: (row) => formatDateTime(row.expiresAt) },
                  {
                    key: 'reason',
                    header: 'Motivo',
                    render: (row) => row.revokedReason ?? row.reason ?? <span className="db-hint">—</span>,
                  },
                  {
                    key: 'actions',
                    header: '',
                    render: (row) =>
                      row.status === 'PENDING' ? (
                        <Button type="button" variant="ghost" size="sm" onClick={() => openRevoke(row)}>
                          Revogar
                        </Button>
                      ) : null,
                  },
                ]}
              />
            )}
          </div>

          <div>
            <p className="db-hint">Histórico de transferências</p>
            {view.transfers.length === 0 ? (
              <p className="db-hint">Nenhuma transferência de titularidade registrada.</p>
            ) : (
              <DataTable<TenantOwnershipTransferHistory>
                rowKey="id"
                rows={view.transfers}
                columns={[
                  { key: 'transferredAt', header: 'Data', render: (row) => formatDateTime(row.transferredAt) },
                  { key: 'previousOwnerUserId', header: 'Anterior', render: (row) => <span className="db-code">{row.previousOwnerUserId}</span> },
                  { key: 'newOwnerUserId', header: 'Novo', render: (row) => <span className="db-code">{row.newOwnerUserId}</span> },
                  { key: 'reason', header: 'Motivo', render: (row) => row.reason },
                  {
                    key: 'previousKeptAsAdmin',
                    header: 'Anterior manteve acesso?',
                    render: (row) => (row.previousKeptAsAdmin ? 'Sim' : 'Não'),
                  },
                ]}
              />
            )}
          </div>
        </div>
      ) : null}

      {view ? (
        <Modal
          open={reissueOpen}
          onClose={() => !reissueBusy && setReissueOpen(false)}
          eyebrow="Acesso inicial do proprietário"
          title="Reenviar ou corrigir convite"
          actions={
            <>
              <Button type="button" variant="ghost" onClick={() => setReissueOpen(false)} disabled={reissueBusy}>
                Cancelar
              </Button>
              <Button
                type="button"
                variant="primary"
                busy={reissueBusy}
                disabled={!reissueName.trim() || !reissueEmail.trim() || !reissueReason.trim()}
                onClick={() => void confirmReissue()}
              >
                Confirmar envio
              </Button>
            </>
          }
        >
          {reissueError ? <AlertBanner tone="danger">{reissueError}</AlertBanner> : null}
          <AlertBanner tone="warning" title="O link anterior deixa de funcionar">
            Ao confirmar, um novo convite é gerado com validade de 72 horas e QUALQUER convite
            anterior deste proprietário é imediatamente invalidado — quem já recebeu o link antigo
            não conseguirá mais usá-lo.
          </AlertBanner>
          <Field label="Nome" htmlFor={reissueNameFieldId} required>
            <Input id={reissueNameFieldId} required value={reissueName} onChange={(event) => setReissueName(event.target.value)} />
          </Field>
          <Field label="E-mail" htmlFor={reissueEmailFieldId} required hint="Se for diferente do atual, isto corrige o convite antes da aceitação.">
            <Input
              id={reissueEmailFieldId}
              type="email"
              required
              value={reissueEmail}
              onChange={(event) => setReissueEmail(event.target.value)}
            />
          </Field>
          <Field label="Motivo" htmlFor={reissueReasonFieldId} required hint="Obrigatório — registrado no histórico com autor e data.">
            <Input
              id={reissueReasonFieldId}
              required
              value={reissueReason}
              onChange={(event) => setReissueReason(event.target.value)}
              placeholder="Ex.: Correção solicitada no chamado #91"
            />
          </Field>
        </Modal>
      ) : null}

      {revokeTarget ? (
        <Modal
          open
          onClose={() => !revokeBusy && setRevokeTarget(undefined)}
          eyebrow="Acesso inicial do proprietário"
          title="Revogar convite pendente?"
          actions={
            <>
              <Button type="button" variant="ghost" onClick={() => setRevokeTarget(undefined)} disabled={revokeBusy}>
                Cancelar
              </Button>
              <Button
                type="button"
                variant="danger"
                busy={revokeBusy}
                disabled={!revokeReason.trim()}
                onClick={() => void confirmRevoke()}
              >
                Sim, revogar convite
              </Button>
            </>
          }
        >
          {revokeError ? <AlertBanner tone="danger">{revokeError}</AlertBanner> : null}
          <p>
            O convite enviado para <strong>{maskOwnerEmail(revokeTarget.sentTo)}</strong> deixará de
            poder ser aceito.
          </p>
          <Field label="Motivo" htmlFor={revokeReasonFieldId} required>
            <Input id={revokeReasonFieldId} required value={revokeReason} onChange={(event) => setRevokeReason(event.target.value)} />
          </Field>
        </Modal>
      ) : null}

      {view ? (
        <Modal
          open={transferOpen}
          onClose={() => !transferBusy && setTransferOpen(false)}
          eyebrow="Acesso inicial do proprietário"
          title="Transferir titularidade"
          actions={
            <>
              <Button type="button" variant="ghost" onClick={() => setTransferOpen(false)} disabled={transferBusy}>
                Cancelar
              </Button>
              <Button
                type="button"
                variant="primary"
                busy={transferBusy}
                disabled={!transferNewOwnerId.trim() || !transferReason.trim()}
                onClick={() => void confirmTransfer()}
              >
                Confirmar transferência
              </Button>
            </>
          }
        >
          {transferError ? <AlertBanner tone="danger">{transferError}</AlertBanner> : null}
          <Field
            label="ID do novo proprietário"
            htmlFor={transferNewOwnerFieldId}
            required
            hint="Usuário precisa já existir neste estabelecimento."
          >
            <Input
              id={transferNewOwnerFieldId}
              required
              value={transferNewOwnerId}
              onChange={(event) => setTransferNewOwnerId(event.target.value)}
              placeholder="00000000-0000-0000-0000-000000000000"
            />
          </Field>
          <Field label="Motivo" htmlFor={transferReasonFieldId} required hint="Obrigatório — registrado no histórico com autor e data.">
            <Input
              id={transferReasonFieldId}
              required
              value={transferReason}
              onChange={(event) => setTransferReason(event.target.value)}
              placeholder="Ex.: Alteração societária"
            />
          </Field>
          <Switch
            checked={transferKeepAsAdmin}
            onChange={(event) => setTransferKeepAsAdmin(event.target.checked)}
            label="Manter proprietário anterior com acesso administrativo"
            description="Quando ativo, o anterior mantém um papel administrativo equivalente (se existir um no catálogo do estabelecimento) — nunca o papel de proprietário. Quando desativado, o anterior perde o papel de proprietário sem manter nenhum acesso adicional por acidente."
          />
        </Modal>
      ) : null}

      <Modal
        open={unlockOpen}
        onClose={() => !unlockBusy && setUnlockOpen(false)}
        eyebrow="Acesso inicial do proprietário"
        title="Desbloquear acesso do proprietário?"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setUnlockOpen(false)} disabled={unlockBusy}>
              Cancelar
            </Button>
            <Button type="button" variant="primary" busy={unlockBusy} disabled={!unlockReason.trim()} onClick={() => void confirmUnlock()}>
              Sim, desbloquear
            </Button>
          </>
        }
      >
        {unlockError ? <AlertBanner tone="danger">{unlockError}</AlertBanner> : null}
        <p>
          Isto reverte o bloqueio da conta. A senha do proprietário NUNCA é definida ou exibida por
          este fluxo — continua sendo a mesma que ele já tinha.
        </p>
        <Field label="Motivo" htmlFor={unlockReasonFieldId} required>
          <Input id={unlockReasonFieldId} required value={unlockReason} onChange={(event) => setUnlockReason(event.target.value)} />
        </Field>
      </Modal>
    </Card>
  );
}
