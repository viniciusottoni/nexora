import { AlertBanner, Button, Card, Field, Input, Select } from '@nexora/ui';
import type { GrantSupportAccessResponse } from '@nexora/contracts';
import { useId, useMemo, useState, type FormEvent } from 'react';

import { createSupportAccessApi, type ApiProblem, type SupportAccessApi } from './support-access-api.js';
import './grant-support-access-page.css';

interface GrantSupportAccessPageProps {
  readonly api?: SupportAccessApi;
}

const DURATION_OPTIONS = [
  { value: '15', label: '15 minutos' },
  { value: '30', label: '30 minutos' },
  { value: '60', label: '1 hora' },
  { value: '120', label: '2 horas' },
  { value: '240', label: '4 horas' },
  { value: '480', label: '8 horas (máximo)' },
];

/**
 * US-145 "Acesso de suporte auditado" — solicitação de acesso da Replay aos dados de um tenant.
 * Único ponto do produto autorizado a burlar o isolamento multi-tenant (RN-015), por isso motivo
 * e duração são sempre exigidos (§10 "motivo obrigatório e substantivo, com referência ao
 * chamado") e o token revelado é mostrado uma única vez — mesmo padrão de mascaramento/revelação
 * de `provision-tenant-page.tsx`.
 */
export function GrantSupportAccessPage({ api: providedApi }: GrantSupportAccessPageProps) {
  const tenantIdFieldId = useId();
  const reasonFieldId = useId();
  const durationFieldId = useId();
  const api = useMemo(() => providedApi ?? createSupportAccessApi(), [providedApi]);

  const [tenantId, setTenantId] = useState('');
  const [reason, setReason] = useState('');
  const [durationMinutes, setDurationMinutes] = useState('60');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<GrantSupportAccessResponse>();
  const [tokenRevealed, setTokenRevealed] = useState(false);
  const [copyStatus, setCopyStatus] = useState('');
  const [copyCount, setCopyCount] = useState(0);

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const response = await api.grant(tenantId.trim(), {
        reason: reason.trim(),
        durationMinutes: Number(durationMinutes),
      });
      setResult(response);
    } catch (caught) {
      const problem = caught as ApiProblem;
      setError(problem.message);
    } finally {
      setBusy(false);
    }
  };

  const copyToken = async () => {
    if (!result) return;
    await navigator.clipboard.writeText(result.token);
    setCopyStatus('Token copiado. Guarde-o em local seguro.');
    setCopyCount((count) => count + 1);
  };

  const requestAnother = () => {
    setResult(undefined);
    setTokenRevealed(false);
    setCopyStatus('');
    setReason('');
  };

  if (result) {
    return (
      <main className="db-page nx-anim-in" aria-labelledby="support-access-result-title">
        <header className="db-page__header">
          <div className="db-page__heading">
            <p className="db-page__eyebrow">Suporte auditado</p>
            <h1 className="db-page__title" id="support-access-result-title">
              Acesso concedido
            </h1>
            <p className="db-page__lead">
              O cliente foi notificado no mesmo instante e pode revogar este acesso a qualquer
              momento pelo histórico dele.
            </p>
          </div>
        </header>

        <Card
          className="db-form-card"
          title="Token de escopo especial"
          subtitle="O token só existe nesta tela — não é possível recuperá-lo depois."
        >
          <AlertBanner tone="warning" title="Token de uso único">
            Não compartilhe este token em canais públicos — expira automaticamente no prazo
            concedido e pode ser revogado pelo cliente a qualquer momento.
          </AlertBanner>

          <p className="support-access-meta">
            <span>Expira em: {new Date(result.expiresAt).toLocaleString('pt-BR')}</span>
            <span>
              Cliente notificado:{' '}
              {result.notifiedCustomer ? 'sim' : 'não foi possível localizar um contato'}
            </span>
          </p>

          <code className="support-access-token">
            {tokenRevealed ? result.token : maskToken(result.token)}
          </code>

          <div className="support-access-actions">
            <Button
              type="button"
              variant="ghost"
              size="sm"
              onClick={() => setTokenRevealed((visible) => !visible)}
            >
              {tokenRevealed ? 'Ocultar token' : 'Revelar token'}
            </Button>
            <Button type="button" size="sm" onClick={() => void copyToken()}>
              Copiar token
            </Button>
          </div>
          <p key={copyCount} className="support-access-copy-status" role="status" aria-live="polite">
            {copyStatus}
          </p>

          <div className="db-editor__footer">
            <p className="db-hint">Precisa de outro acesso? Registre um novo motivo.</p>
            <Button type="button" variant="ghost" onClick={requestAnother}>
              Solicitar outro acesso
            </Button>
          </div>
        </Card>
      </main>
    );
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="support-access-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · suporte</p>
          <h1 className="db-page__title" id="support-access-title">
            Solicitar acesso de suporte
          </h1>
          <p className="db-page__lead">
            Única exceção autorizada ao isolamento entre estabelecimentos — sempre com motivo,
            duração limitada e visível ao cliente.
          </p>
        </div>
      </header>

      <form onSubmit={(event) => void submit(event)}>
        <Card className="db-form-card" padding="none">
          <div className="db-form-row support-access-form-body">
            <Field
              label="Estabelecimento (id)"
              htmlFor={tenantIdFieldId}
              hint="Id do tenant a ser acessado."
            >
              <Input
                id={tenantIdFieldId}
                required
                value={tenantId}
                onChange={(event) => setTenantId(event.target.value)}
              />
            </Field>
            <Field
              label="Motivo"
              htmlFor={reasonFieldId}
              hint="Substantivo, com referência ao chamado (ex.: 'Investigação de divergência de caixa — chamado #482')."
            >
              <Input
                id={reasonFieldId}
                required
                value={reason}
                onChange={(event) => setReason(event.target.value)}
              />
            </Field>
            <Field label="Duração" htmlFor={durationFieldId}>
              <Select
                id={durationFieldId}
                value={durationMinutes}
                options={DURATION_OPTIONS}
                onChange={(event) => setDurationMinutes(event.target.value)}
              />
            </Field>
          </div>

          <div className="support-access-footer">
            {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
            <div className="db-editor__footer">
              <p className="db-hint">
                Um token de escopo especial é gerado e o cliente é notificado imediatamente.
              </p>
              <Button type="submit" busy={busy}>
                {busy ? 'Solicitando…' : 'Solicitar acesso'}
              </Button>
            </div>
          </div>
        </Card>
      </form>
    </main>
  );
}

function maskToken(token: string): string {
  if (token.length <= 8) return '•'.repeat(token.length);
  return `${token.slice(0, 4)}${'•'.repeat(Math.max(token.length - 8, 4))}${token.slice(-4)}`;
}
