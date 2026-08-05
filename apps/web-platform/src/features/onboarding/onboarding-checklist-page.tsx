import { useCallback, useEffect, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Card, Icon } from '@nexora/ui';
import {
  ONBOARDING_STEP_LABELS,
  type OnboardingStatusResponse,
  type OnboardingStepKey,
} from '@nexora/contracts';

import { createOnboardingApi, type OnboardingApi, type OnboardingApiProblem } from './onboarding-api.js';
import './onboarding-checklist-page.css';

export interface OnboardingChecklistPageProps {
  /** Tenant cujo roteiro de implantação está sendo acompanhado (US-141 §4 "Checklist de implantação"). */
  readonly tenantId: string;
  readonly tenantName?: string;
  /** Injetável para teste — padrão `createOnboardingApi()`. */
  readonly api?: OnboardingApi;
}

const STATUS_BADGE_TONE: Record<string, 'success' | 'info' | 'neutral'> = {
  DONE: 'success',
  IN_PROGRESS: 'info',
  PENDING: 'neutral',
};

const STATUS_LABEL: Record<string, string> = {
  DONE: 'Concluído',
  IN_PROGRESS: 'Em andamento',
  PENDING: 'Pendente',
};

/**
 * US-141 §4 "Checklist de implantação" e "Validação antes da ativação" — visão da Replay (P9) sobre
 * os nove passos de um tenant específico, com bloqueio de ativação enquanto houver pendência.
 * `tenantId` é obrigatório e vem de quem compõe esta tela (ex.: uma lista/detalhe de estabelecimento)
 * — `apps/web-platform/src/app.tsx` ainda não tem essa navegação nesta tarefa (fora do escopo
 * permitido de edição, ver relatório: o maintainer decide onde plugar esta tela).
 */
export function OnboardingChecklistPage({
  tenantId,
  tenantName,
  api: providedApi,
}: Readonly<OnboardingChecklistPageProps>) {
  const api = useMemo(() => providedApi ?? createOnboardingApi(), [providedApi]);
  const [status, setStatus] = useState<OnboardingStatusResponse>();
  const [error, setError] = useState<string>();
  const [pendingSteps, setPendingSteps] = useState<readonly string[]>();
  const [activating, setActivating] = useState(false);

  const load = useCallback(async () => {
    try {
      setStatus(await api.getStatus(tenantId));
      setError(undefined);
    } catch (reason) {
      setError(toMessage(reason));
    }
  }, [api, tenantId]);

  useEffect(() => {
    void load();
  }, [load]);

  async function activate() {
    setActivating(true);
    setError(undefined);
    setPendingSteps(undefined);
    try {
      await api.activate(tenantId);
      await load();
    } catch (reason) {
      const problem = reason as OnboardingApiProblem;
      if (problem.code === 'ONBOARDING_INCOMPLETE') {
        setPendingSteps(problem.pendingSteps ?? []);
        setError('Ainda há passos pendentes no roteiro de implantação.');
      } else {
        setError(toMessage(reason));
      }
    } finally {
      setActivating(false);
    }
  }

  if (!status) {
    return (
      <main className="db-page nx-anim-in" aria-labelledby="onboarding-title">
        <header className="db-page__header">
          <div className="db-page__heading">
            <p className="db-page__eyebrow">Plataforma · implantação</p>
            <h1 className="db-page__title" id="onboarding-title">
              Checklist de implantação
            </h1>
          </div>
        </header>
        {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
        <output className="db-loading">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando roteiro de implantação…
        </output>
      </main>
    );
  }

  const doneCount = status.steps.filter((step) => step.status === 'DONE').length;
  const activationStep = status.steps.find((step) => step.key === 'ACTIVATION');
  const alreadyActive = activationStep?.status === 'DONE';
  const pendingLabels = (pendingSteps ?? []).map(
    (key) => ONBOARDING_STEP_LABELS[key as OnboardingStepKey] ?? key,
  );

  return (
    <main className="db-page nx-anim-in" aria-labelledby="onboarding-title">
      <header className="db-page__header onboarding-header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · implantação</p>
          <h1 className="db-page__title" id="onboarding-title">
            {tenantName ? `Implantação — ${tenantName}` : 'Checklist de implantação'}
          </h1>
          <p className="db-page__lead">
            Os nove passos da implantação, visíveis para a Replay e para o cliente ao mesmo tempo —
            meta de até 5 dias úteis.
          </p>
        </div>
        <div className="onboarding-progress">
          <strong>{doneCount}/9</strong>
          <span>
            passos concluídos
            {status.elapsedBusinessDays !== null ? (
              <>
                {' · '}
                <span className="onboarding-elapsed">
                  {status.elapsedBusinessDays} {status.elapsedBusinessDays === 1 ? 'dia útil' : 'dias úteis'} decorridos
                </span>
              </>
            ) : null}
          </span>
        </div>
      </header>

      {error ? (
        <AlertBanner tone="danger" title={pendingLabels.length > 0 ? 'Ativação bloqueada' : undefined}>
          {error}
          {pendingLabels.length > 0 ? (
            <ul>
              {pendingLabels.map((label) => (
                <li key={label}>{label}</li>
              ))}
            </ul>
          ) : null}
        </AlertBanner>
      ) : null}

      <Card
        title="Roteiro de implantação"
        subtitle="Progresso quantificado onde possível — o que falta fica claro dos dois lados."
        padding="tight"
      >
        <ol className="onboarding-list nx-stagger">
          {status.steps.map((step) => (
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
                <strong>{ONBOARDING_STEP_LABELS[step.key]}</strong>
                {step.progress?.products !== null && step.progress?.products !== undefined ? (
                  <small>
                    {step.progress.expected !== null && step.progress.expected !== undefined
                      ? `${step.progress.products} de ${step.progress.expected} produtos`
                      : `${step.progress.products} produtos cadastrados`}
                  </small>
                ) : null}
              </div>
              <Badge tone={STATUS_BADGE_TONE[step.status] ?? 'neutral'}>
                {STATUS_LABEL[step.status] ?? step.status}
              </Badge>
            </li>
          ))}
        </ol>
      </Card>

      <div className="db-editor__footer">
        <p className="db-hint">
          {alreadyActive
            ? 'Este estabelecimento já foi ativado.'
            : 'A ativação fecha a medição de tempo de implantação e libera a operação.'}
        </p>
        <Button
          type="button"
          busy={activating}
          disabled={alreadyActive}
          onClick={() => void activate()}
        >
          {activating ? 'Ativando…' : 'Ativar estabelecimento'}
        </Button>
      </div>
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar a implantação.';
}
