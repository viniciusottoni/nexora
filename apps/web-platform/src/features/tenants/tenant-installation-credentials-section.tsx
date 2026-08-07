import { useCallback, useEffect, useId, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  Field,
  Icon,
  Input,
  Modal,
  ProgressMeter,
  Select,
} from '@nexora/ui';

import {
  createInstallationCredentialsApi,
  type InstallationCredentialsApi,
  type ReissueInstallationTokenResult,
  type TenantDeploymentInstallationStatus,
  type TenantDeploymentStatus,
} from './installation-credentials-api.js';
import './tenant-installation-credentials-section.css';

export interface TenantInstallationCredentialsSectionProps {
  readonly tenantId: string;
  readonly api?: InstallationCredentialsApi;
}

const STATUS_LABEL: Record<TenantDeploymentInstallationStatus, string> = {
  PENDING: 'Pendente',
  ACTIVE: 'Ativa',
  OFFLINE: 'Offline',
};

const STATUS_TONE: Record<TenantDeploymentInstallationStatus, 'neutral' | 'success' | 'warning'> = {
  PENDING: 'neutral',
  ACTIVE: 'success',
  OFFLINE: 'warning',
};

const EXPIRES_IN_HOURS_OPTIONS = [
  { value: '1', label: '1 hora' },
  { value: '4', label: '4 horas' },
  { value: '24', label: '24 horas (padrão)' },
  { value: '48', label: '48 horas' },
  { value: '72', label: '72 horas (máximo)' },
];

const DEFAULT_EXPIRES_IN_HOURS = 24;

function maskToken(token: string): string {
  if (token.length <= 8) return '•'.repeat(token.length);
  return `${token.slice(0, 4)}${'•'.repeat(Math.max(8, token.length - 8))}${token.slice(-4)}`;
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}

/**
 * US-156 · Recuperação do provisionamento e token de instalação — seção autocontida (não plugada
 * em `tenant-detail-page.tsx`, ver disciplina de isolamento de arquivos do relatório da tarefa;
 * integração final é um passo separado) para o administrador de plataforma recuperar um
 * provisionamento incompleto: ver o checklist reconstruído a partir de fatos persistidos, reemitir
 * o token de instalação sem duplicar tenant/loja/instalação, e revogar manualmente uma credencial
 * comprometida.
 *
 * O segredo bruto SÓ vive em `useState` local deste componente (nunca em store global,
 * localStorage ou sessionStorage) — ao sair da tela (unmount) ou navegar, o estado é destruído
 * pelo próprio React e o valor não pode ser reaberto, exatamente como a US pede.
 */
export function TenantInstallationCredentialsSection({
  tenantId,
  api: providedApi,
}: Readonly<TenantInstallationCredentialsSectionProps>) {
  const api = useMemo(() => providedApi ?? createInstallationCredentialsApi(), [providedApi]);

  const [status, setStatus] = useState<TenantDeploymentStatus>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>();

  const [reissueModalOpen, setReissueModalOpen] = useState(false);
  const [reason, setReason] = useState('');
  const [expiresInHours, setExpiresInHours] = useState(String(DEFAULT_EXPIRES_IN_HOURS));
  const [reissueBusy, setReissueBusy] = useState(false);
  const [reissueError, setReissueError] = useState('');

  // Exibição única: só existe enquanto este componente estiver montado. Nunca gravado em
  // localStorage/sessionStorage/store global — sair da tela apaga o segredo para sempre.
  const [issued, setIssued] = useState<ReissueInstallationTokenResult>();
  const [tokenRevealed, setTokenRevealed] = useState(false);
  const [copyStatus, setCopyStatus] = useState('');
  const [copyCount, setCopyCount] = useState(0);

  const [revokeModalOpen, setRevokeModalOpen] = useState(false);
  const [revokeReason, setRevokeReason] = useState('');
  const [revokeBusy, setRevokeBusy] = useState(false);
  const [revokeError, setRevokeError] = useState('');
  const [revoked, setRevoked] = useState(false);

  const reasonFieldId = useId();
  const expiresFieldId = useId();
  const revokeReasonFieldId = useId();

  const loadStatus = useCallback(() => {
    let cancelled = false;
    setLoading(true);
    setError(undefined);
    api
      .getDeploymentStatus(tenantId)
      .then((result) => {
        if (!cancelled) setStatus(result);
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

  useEffect(() => loadStatus(), [loadStatus]);

  function openReissueModal() {
    setReason('');
    setExpiresInHours(String(DEFAULT_EXPIRES_IN_HOURS));
    setReissueError('');
    setReissueModalOpen(true);
  }

  function closeReissueModal() {
    if (reissueBusy) return;
    setReissueModalOpen(false);
  }

  async function confirmReissue() {
    if (!status?.installation) return;
    setReissueBusy(true);
    setReissueError('');
    try {
      const result = await api.reissueToken(status.installation.id, {
        reason: reason.trim(),
        expiresInHours: Number(expiresInHours),
      });
      setIssued(result);
      setTokenRevealed(false);
      setRevoked(false);
      setReissueModalOpen(false);
      loadStatus();
    } catch (caught) {
      setReissueError(toMessage(caught));
    } finally {
      setReissueBusy(false);
    }
  }

  async function copyToken() {
    if (!issued?.installToken) return;
    await navigator.clipboard.writeText(issued.installToken);
    setCopyStatus('Token copiado. Guarde-o em local seguro — ele não será mostrado novamente.');
    setCopyCount((count) => count + 1);
  }

  function downloadCommand() {
    if (!issued?.installCommand) return;
    const blob = new Blob([issued.installCommand], { type: 'text/plain' });
    const url = URL.createObjectURL(blob);
    const link = document.createElement('a');
    link.href = url;
    link.download = `install-${tenantId}.sh`;
    link.click();
    URL.revokeObjectURL(url);
    setCopyStatus('Comando baixado. Guarde-o em local seguro — ele não será mostrado novamente.');
    setCopyCount((count) => count + 1);
  }

  function openRevokeModal() {
    setRevokeReason('');
    setRevokeError('');
    setRevokeModalOpen(true);
  }

  function closeRevokeModal() {
    if (revokeBusy) return;
    setRevokeModalOpen(false);
  }

  async function confirmRevoke() {
    if (!status?.installation || !issued) return;
    setRevokeBusy(true);
    setRevokeError('');
    try {
      await api.revokeCredential(status.installation.id, issued.credentialId, revokeReason.trim());
      // Mantém `issued` (só a linha de metadado — credentialId/expiresAt) para a seção continuar
      // mostrando QUE uma credencial foi emitida e revogada nesta sessão; `revoked` é o que decide
      // a UI não tentar mais exibir/mascarar o token (o valor bruto nunca era lido de novo aqui de
      // qualquer forma — só existia em memória desde a emissão).
      setRevoked(true);
      setRevokeModalOpen(false);
      loadStatus();
    } catch (caught) {
      setRevokeError(toMessage(caught));
    } finally {
      setRevokeBusy(false);
    }
  }

  const canConfirmReissue = reason.trim().length > 0 && !reissueBusy;
  const canConfirmRevoke = revokeReason.trim().length > 0 && !revokeBusy;

  return (
    <section
      className="tenant-installation-credentials nx-anim-in"
      aria-labelledby="installation-credentials-title"
    >
      <Card
        title="Recuperação de provisionamento"
        subtitle="Checklist reconstruído a partir do que já está persistido — não depende de você ter visto a tela original."
      >
        {loading ? (
          <div className="db-loading" role="status">
            <span className="nx-spinner" aria-hidden="true" />
            Carregando checklist…
          </div>
        ) : error !== undefined ? (
          <AlertBanner tone="danger" title="Não foi possível carregar o checklist">
            {toMessage(error)}
          </AlertBanner>
        ) : status ? (
          <div className="tenant-installation-credentials__body nx-stagger">
            <ProgressMeter
              label="Passos concluídos"
              value={status.completed}
              max={status.total}
              display={`${status.completed}/${status.total}`}
              tone={status.completed === status.total ? 'success' : 'brand'}
            />

            {status.installation ? (
              <div className="tenant-installation-credentials__badges">
                {/* Dois conceitos DISTINTOS, dois badges — nunca o mesmo rótulo para os dois. */}
                <div className="tenant-installation-credentials__badge-group">
                  <span className="db-hint">Instalação</span>
                  <Badge tone={STATUS_TONE[status.installation.status]}>
                    {status.installation.status === 'PENDING'
                      ? 'Ainda não registrada'
                      : STATUS_LABEL[status.installation.status]}
                  </Badge>
                </div>
                <div className="tenant-installation-credentials__badge-group">
                  <span className="db-hint">Token de instalação</span>
                  <Badge
                    tone={status.installation.canReissueToken ? 'warning' : 'neutral'}
                    icon="vpn_key"
                  >
                    {status.installation.canReissueToken
                      ? 'Disponível para reemissão'
                      : 'Não se aplica (já pareada)'}
                  </Badge>
                </div>
              </div>
            ) : (
              <AlertBanner tone="warning" title="Nenhuma instalação encontrada">
                Este estabelecimento ainda não tem instalação edge criada.
              </AlertBanner>
            )}

            {status.installation?.status === 'PENDING' ? (
              <AlertBanner tone="warning" title="Provisionamento incompleto">
                O tenant, a loja e a instalação já existem, mas a instalação ainda não concluiu o
                pareamento — se o comando original não foi exibido ou copiado, reemita um novo token
                abaixo. Nenhum tenant, loja ou instalação novos serão criados.
              </AlertBanner>
            ) : null}

            {status.installation?.canReissueToken ? (
              <Button type="button" onClick={openReissueModal}>
                <Icon name="autorenew" /> Reemitir token de instalação
              </Button>
            ) : null}
          </div>
        ) : null}
      </Card>

      {issued ? (
        <Card
          className="nx-anim-in"
          title="Token gerado"
          subtitle="Exibição única — copie ou baixe agora."
        >
          {revoked ? (
            <AlertBanner tone="success" title="Credencial revogada">
              Esta credencial foi revogada e não pode mais ser usada para pareamento.
            </AlertBanner>
          ) : issued.installToken === null ? (
            <AlertBanner tone="warning" title="Segredo já exibido">
              Esta é uma repetição da mesma intenção (mesma Idempotency-Key) — o token bruto já foi
              mostrado uma vez e, por definição de exibição única, não volta a aparecer. A rotação
              foi confirmada; se precisar de outro token, reemita novamente.
            </AlertBanner>
          ) : (
            <>
              <AlertBanner tone="warning" title="Copie agora">
                Este token não será mostrado novamente depois que você sair desta tela.
              </AlertBanner>

              <code className="tenant-installation-credentials__token">
                {tokenRevealed ? issued.installToken : maskToken(issued.installToken)}
              </code>

              <div className="tenant-installation-credentials__actions">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setTokenRevealed((visible) => !visible)}
                >
                  {tokenRevealed ? 'Ocultar token' : 'Revelar token'}
                </Button>
                <Button type="button" size="sm" onClick={() => void copyToken()}>
                  <Icon name="content_copy" /> Copiar token
                </Button>
                <Button type="button" variant="secondary" size="sm" onClick={downloadCommand}>
                  <Icon name="download" /> Baixar comando
                </Button>
                <Button type="button" variant="danger" size="sm" onClick={openRevokeModal}>
                  <Icon name="block" /> Revogar agora
                </Button>
              </div>

              <p
                key={copyCount}
                className="tenant-installation-credentials__copy-status"
                role="status"
                aria-live="polite"
              >
                {copyStatus}
              </p>

              <p className="db-hint">
                Validade:{' '}
                {new Date(issued.expiresAt).toLocaleString('pt-BR', {
                  dateStyle: 'short',
                  timeStyle: 'short',
                })}
              </p>
            </>
          )}
        </Card>
      ) : null}

      {status?.installation ? (
        <Modal
          open={reissueModalOpen}
          onClose={closeReissueModal}
          eyebrow="Recuperação de provisionamento"
          title="Reemitir token de instalação?"
          actions={
            <>
              <Button
                type="button"
                variant="ghost"
                onClick={closeReissueModal}
                disabled={reissueBusy}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                busy={reissueBusy}
                disabled={!canConfirmReissue}
                onClick={() => void confirmReissue()}
              >
                Sim, reemitir token
              </Button>
            </>
          }
        >
          <p>
            O token anterior (se houver um pendente) será invalidado imediatamente — quem tiver
            copiado o comando antigo não conseguirá mais parear com ele.
          </p>
          {reissueError ? <AlertBanner tone="danger">{reissueError}</AlertBanner> : null}
          <Field
            label="Motivo"
            htmlFor={reasonFieldId}
            hint="Obrigatório — fica registrado na auditoria."
          >
            <Input
              id={reasonFieldId}
              name="installation-token-reissue-reason"
              autoComplete="off"
              required
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ex.: Comando original não foi exibido"
            />
          </Field>
          <Field label="Validade" htmlFor={expiresFieldId}>
            <Select
              id={expiresFieldId}
              name="installation-token-expiration"
              value={expiresInHours}
              options={EXPIRES_IN_HOURS_OPTIONS}
              onChange={(event) => setExpiresInHours(event.target.value)}
            />
          </Field>
        </Modal>
      ) : null}

      {issued ? (
        <Modal
          open={revokeModalOpen}
          onClose={closeRevokeModal}
          tone="danger"
          eyebrow="Recuperação de provisionamento"
          title="Revogar esta credencial?"
          actions={
            <>
              <Button
                type="button"
                variant="ghost"
                onClick={closeRevokeModal}
                disabled={revokeBusy}
              >
                Cancelar
              </Button>
              <Button
                type="button"
                variant="danger"
                busy={revokeBusy}
                disabled={!canConfirmRevoke}
                onClick={() => void confirmRevoke()}
              >
                Sim, revogar credencial
              </Button>
            </>
          }
        >
          <p>A credencial deixa de funcionar imediatamente. Esta ação não pode ser desfeita.</p>
          {revokeError ? <AlertBanner tone="danger">{revokeError}</AlertBanner> : null}
          <Field
            label="Motivo"
            htmlFor={revokeReasonFieldId}
            hint="Obrigatório — fica registrado na auditoria."
          >
            <Input
              id={revokeReasonFieldId}
              name="installation-token-revoke-reason"
              autoComplete="off"
              required
              value={revokeReason}
              onChange={(event) => setRevokeReason(event.target.value)}
              placeholder="Ex.: Credencial possivelmente exposta"
            />
          </Field>
        </Modal>
      ) : null}
    </section>
  );
}
