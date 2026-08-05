import { useCallback, useEffect, useState } from 'react';
import { AlertBanner, Badge, Button, Card, Icon } from '@nexora/ui';
import type { OnboardingStatusResponse, OnboardingStepKey } from '@nexora/contracts';

import { OnboardingApi } from './onboarding-api.js';
import './onboarding-setup-page.css';

export interface OnboardingSetupPageProps {
  /** Tenant do gestor autenticado (US-141 §3.1 "autoatendimento pelo cliente"). */
  readonly tenantId: string;
  /** Injetável para teste — padrão `new OnboardingApi()`. */
  readonly api?: OnboardingApi;
}

/** Linguagem de negócio de cada passo (US-141 §10) — evita jargão técnico ("EDGE_INSTALL") no painel do cliente. */
const STEP_COPY: Record<OnboardingStepKey, { title: string; hint: string }> = {
  TENANT_CREATED: { title: 'Cadastro criado', hint: 'Seu estabelecimento já existe na plataforma.' },
  BRANDING: { title: 'Identidade visual', hint: 'Cores e logo aplicados ao seu cardápio e telas.' },
  MENU: { title: 'Cardápio', hint: 'Produtos, preços e fichas técnicas cadastrados.' },
  TABLES: { title: 'Mesas', hint: 'Ambientes e mesas do salão configurados.' },
  EDGE_INSTALL: { title: 'Servidor local', hint: 'O computador da loja está instalado e conectado.' },
  PAYMENT_CONFIG: { title: 'Meios de pagamento', hint: 'PIX, cartão e demais formas de pagamento configurados.' },
  TRAINING: { title: 'Treinamento da equipe', hint: 'Sua equipe já foi treinada para operar o sistema.' },
  PILOT: { title: 'Piloto acompanhado', hint: 'Um dia de operação real, acompanhado pela Replay.' },
  ACTIVATION: { title: 'Ativação', hint: 'A Replay libera a operação assim que tudo estiver pronto.' },
};

const STATUS_LABEL: Record<string, string> = {
  DONE: 'Concluído',
  IN_PROGRESS: 'Em andamento',
  PENDING: 'Pendente',
};

const STATUS_TONE: Record<string, 'success' | 'info' | 'neutral'> = {
  DONE: 'success',
  IN_PROGRESS: 'info',
  PENDING: 'neutral',
};

/** Passos sem sinal automático — só o próprio gestor sabe dizer que aconteceram (US-141 §3.1). */
const MANUALLY_COMPLETABLE_STEPS: readonly OnboardingStepKey[] = ['TRAINING', 'PILOT'];

/**
 * Assistente de configuração inicial (US-141 §3.1 "Assistente de configuração inicial no painel do
 * cliente") — auto-suficiente (busca os próprios dados via `OnboardingApi`), mesmo padrão de
 * `UnavailableListPage`/`BrandingContainer`. `apps/web-admin/src/app.tsx` ainda não foi integrado a
 * esta tela nesta tarefa (fora do escopo permitido de edição — ver relatório para o id/rótulo/ícone
 * de navegação sugeridos).
 */
export function OnboardingSetupPage({ tenantId, api = new OnboardingApi() }: Readonly<OnboardingSetupPageProps>) {
  const [status, setStatus] = useState<OnboardingStatusResponse>();
  const [error, setError] = useState<string>();
  const [completingKey, setCompletingKey] = useState<OnboardingStepKey>();

  const load = useCallback(async () => {
    try {
      setStatus(await api.getStatus(tenantId));
    } catch (reason) {
      setError(toMessage(reason));
    }
  }, [api, tenantId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function markComplete(key: OnboardingStepKey) {
    setCompletingKey(key);
    setError(undefined);
    try {
      await api.completeStep(tenantId, key);
      await load();
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setCompletingKey(undefined);
    }
  }

  if (!status) {
    return (
      <main className="db-page nx-anim-in" aria-labelledby="onboarding-setup-title">
        <header className="db-page__header">
          <div className="db-page__heading">
            <p className="db-page__eyebrow">Configuração inicial</p>
            <h1 className="db-page__title" id="onboarding-setup-title">
              Assistente de implantação
            </h1>
          </div>
        </header>
        {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
        <output className="db-loading">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando seu roteiro de implantação…
        </output>
      </main>
    );
  }

  const doneCount = status.steps.filter((step) => step.status === 'DONE').length;

  return (
    <main className="db-page nx-anim-in" aria-labelledby="onboarding-setup-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Configuração inicial</p>
          <h1 className="db-page__title" id="onboarding-setup-title">
            Assistente de implantação
          </h1>
          <p className="db-page__lead">
            Acompanhe o que já está pronto e o que falta para colocar sua loja no ar.
          </p>
        </div>
        <div className="onboarding-setup-progress">
          <strong>{doneCount}/9</strong>
          <span>passos concluídos</span>
        </div>
      </header>

      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      <Card
        title="Seu roteiro de implantação"
        subtitle="Marque como concluído o que já aconteceu fora do sistema."
        padding="tight"
      >
        <ol className="onboarding-setup-list nx-stagger">
          {status.steps.map((step) => {
            const copy = STEP_COPY[step.key];
            const canMarkComplete =
              step.status !== 'DONE' && MANUALLY_COMPLETABLE_STEPS.includes(step.key);

            return (
              <li key={step.key} data-status={step.status}>
                <Icon
                  name={
                    step.status === 'DONE'
                      ? 'check_circle'
                      : step.status === 'IN_PROGRESS'
                        ? 'schedule'
                        : 'radio_button_unchecked'
                  }
                  size={20}
                  fill={step.status === 'DONE'}
                  color={
                    step.status === 'DONE'
                      ? 'var(--nx-success-500)'
                      : step.status === 'IN_PROGRESS'
                        ? 'var(--nx-info-500)'
                        : 'var(--text-disabled)'
                  }
                />
                <div>
                  <strong>{copy.title}</strong>
                  <small>
                    {step.progress?.products !== null && step.progress?.products !== undefined
                      ? `${step.progress.products} produtos cadastrados — `
                      : ''}
                    {copy.hint}
                  </small>
                </div>
                {canMarkComplete ? (
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    busy={completingKey === step.key}
                    onClick={() => void markComplete(step.key)}
                  >
                    Marcar concluído
                  </Button>
                ) : (
                  <Badge tone={STATUS_TONE[step.status] ?? 'neutral'}>
                    {STATUS_LABEL[step.status] ?? step.status}
                  </Badge>
                )}
              </li>
            );
          })}
        </ol>
      </Card>
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar a implantação.';
}
