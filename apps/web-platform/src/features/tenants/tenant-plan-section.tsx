import { useEffect, useId, useMemo, useState } from 'react';
import { AlertBanner, Badge, Button, Card, Field, Icon, Input, Modal, Select } from '@nexora/ui';

import {
  createTenantPlanApi,
  type ApiProblem,
  type PlatformPlanSummary,
  type TenantPlanApi,
  type TenantPlanView,
} from './tenant-plan-api.js';
import './tenant-plan-section.css';

export interface TenantPlanSectionProps {
  readonly tenantId: string;
  readonly api?: TenantPlanApi;
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível carregar o plano comercial.';
}

/** `datetime-local` (sem fuso) → ISO com offset — o valor do `<input>` é sempre hora local do navegador. */
function toIsoWithOffset(dateTimeLocalValue: string): string | undefined {
  if (!dateTimeLocalValue) return undefined;
  const date = new Date(dateTimeLocalValue);
  if (Number.isNaN(date.getTime())) return undefined;
  return date.toISOString();
}

function formatDateTime(iso: string): string {
  const date = new Date(iso);
  if (Number.isNaN(date.getTime())) return iso;
  return date.toLocaleString('pt-BR', { dateStyle: 'short', timeStyle: 'short' });
}

/**
 * US-154 · Gestão de planos e configuração comercial — seção autocontida de plano comercial de um
 * estabelecimento: consulta plano atual/capacidades efetivas/agendamento/consistência, oferece
 * upgrade/downgrade com vigência e motivo obrigatórios (comparação antes/depois), e alerta de
 * divergência com ação de reconciliar. Recebe `tenantId` como prop e faz o PRÓPRIO fetch — não
 * depende de dados de `tenant-detail-page.tsx` (integração central posterior importa e renderiza
 * este componente dentro do Card já existente lá, ver relatório final da tarefa). Mesmo padrão
 * visual/de qualidade de `tenant-detail-page.tsx` (US-153): `Card` com `Modal` de confirmação e
 * motivo obrigatório, classes `db-page`/`db-stack`/`db-hint`/`db-code`, animação `nx-anim-in`/
 * `nx-stagger` (`packages/ui/src/components/motion.css`).
 */
export function TenantPlanSection({ tenantId, api: providedApi }: Readonly<TenantPlanSectionProps>) {
  const api = useMemo(() => providedApi ?? createTenantPlanApi(), [providedApi]);

  const [plan, setPlan] = useState<TenantPlanView>();
  const [catalog, setCatalog] = useState<PlatformPlanSummary[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<unknown>();

  const [reconcileBusy, setReconcileBusy] = useState(false);
  const [reconcileError, setReconcileError] = useState('');

  const [modalOpen, setModalOpen] = useState(false);
  const [targetPlanCode, setTargetPlanCode] = useState('');
  const [effectiveAtLocal, setEffectiveAtLocal] = useState('');
  const [reason, setReason] = useState('');
  const [formBusy, setFormBusy] = useState(false);
  const [formError, setFormError] = useState('');

  const reasonFieldId = useId();
  const effectiveAtFieldId = useId();
  const targetPlanFieldId = useId();

  async function reload() {
    setLoading(true);
    setError(undefined);
    try {
      const [planView, plans] = await Promise.all([api.get(tenantId), api.listPlans()]);
      setPlan(planView);
      setCatalog(plans);
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
    Promise.all([api.get(tenantId), api.listPlans()])
      .then(([planView, plans]) => {
        if (!cancelled) {
          setPlan(planView);
          setCatalog(plans);
        }
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

  function openModal() {
    setTargetPlanCode('');
    setEffectiveAtLocal('');
    setReason('');
    setFormError('');
    setModalOpen(true);
  }

  function closeModal() {
    if (formBusy) return;
    setModalOpen(false);
  }

  async function confirmChange() {
    if (!plan) return;
    const effectiveAt = toIsoWithOffset(effectiveAtLocal);
    setFormBusy(true);
    setFormError('');
    try {
      await api.update(tenantId, plan.version, {
        plan: targetPlanCode,
        reason: reason.trim(),
        ...(effectiveAt ? { effectiveAt } : {}),
      });
      setModalOpen(false);
      await reload();
    } catch (caught) {
      setFormError(toMessage(caught));
    } finally {
      setFormBusy(false);
    }
  }

  async function reconcile() {
    setReconcileBusy(true);
    setReconcileError('');
    try {
      await api.reconcile(tenantId);
      await reload();
    } catch (caught) {
      setReconcileError(toMessage(caught));
    } finally {
      setReconcileBusy(false);
    }
  }

  const activePlanOptions = catalog
    .filter((candidate) => candidate.active)
    .map((candidate) => ({ value: candidate.code, label: candidate.name }));

  const selectedPlanSummary = catalog.find((candidate) => candidate.code === targetPlanCode);
  const currentCapabilities = plan?.effectiveCapabilities ?? [];
  const targetCapabilities = selectedPlanSummary?.capabilities ?? [];
  const gainedCapabilities = targetCapabilities.filter((cap) => !currentCapabilities.includes(cap));
  const lostCapabilities = currentCapabilities.filter((cap) => !targetCapabilities.includes(cap));
  const isDowngrade = lostCapabilities.length > 0;

  const canConfirm = targetPlanCode.length > 0 && effectiveAtLocal.length > 0 && reason.trim().length > 0;

  return (
    <Card
      title="Plano comercial"
      actions={
        plan ? (
          <Button type="button" variant="primary" size="sm" onClick={openModal}>
            <Icon name="workspace_premium" /> Alterar plano
          </Button>
        ) : null
      }
    >
      {loading ? (
        <div className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando plano comercial…
        </div>
      ) : error !== undefined ? (
        <AlertBanner tone="danger" title="Não foi possível carregar o plano comercial">
          {toMessage(error)}
        </AlertBanner>
      ) : plan ? (
        <div className="db-stack nx-stagger">
          <dl className="tenant-plan__kv">
            <div>
              <dt>Plano atual</dt>
              <dd>
                <Badge tone="brand">{plan.current}</Badge>
              </dd>
            </div>
            <div>
              <dt>Versão</dt>
              <dd className="db-code">{plan.version}</dd>
            </div>
            <div>
              <dt>Agendamento</dt>
              <dd>
                {plan.scheduled
                  ? `${plan.scheduled.plan} a partir de ${formatDateTime(plan.scheduled.effectiveAt)}`
                  : 'Nenhuma mudança agendada'}
              </dd>
            </div>
            <div>
              <dt>Consistência</dt>
              <dd>
                <Badge tone={plan.consistent ? 'success' : 'warning'}>
                  {plan.consistent ? 'Consistente' : 'Divergente'}
                </Badge>
              </dd>
            </div>
          </dl>

          <div>
            <p className="db-hint">Capacidades efetivas</p>
            {currentCapabilities.length === 0 ? (
              <p className="db-hint">Nenhuma capacidade reconciliada ainda.</p>
            ) : (
              <ul className="tenant-plan__capabilities">
                {currentCapabilities.map((capability) => (
                  <li key={capability}>{capability}</li>
                ))}
              </ul>
            )}
          </div>

          {!plan.consistent ? (
            <AlertBanner
              tone="warning"
              title="Divergência entre plano e configuração"
              actions={
                <Button type="button" variant="secondary" size="sm" busy={reconcileBusy} onClick={() => void reconcile()}>
                  Reconciliar agora
                </Button>
              }
            >
              As capacidades efetivas deste estabelecimento não correspondem ao catálogo do plano
              corrente ({plan.current}). Reconcilie para aplicar as capacidades do plano vigente —
              nenhuma correção acontece automaticamente.
            </AlertBanner>
          ) : null}

          {reconcileError ? <AlertBanner tone="danger">{reconcileError}</AlertBanner> : null}
        </div>
      ) : null}

      {plan ? (
        <Modal
          open={modalOpen}
          onClose={closeModal}
          eyebrow="Plano comercial"
          title="Alterar plano"
          actions={
            <>
              <Button type="button" variant="ghost" onClick={closeModal} disabled={formBusy}>
                Cancelar
              </Button>
              <Button
                type="button"
                variant="primary"
                busy={formBusy}
                disabled={!canConfirm}
                onClick={() => void confirmChange()}
              >
                Confirmar mudança
              </Button>
            </>
          }
        >
          {formError ? <AlertBanner tone="danger">{formError}</AlertBanner> : null}

          <div className="tenant-plan__form-grid">
            <Field label="Novo plano" htmlFor={targetPlanFieldId} required>
              <Select
                id={targetPlanFieldId}
                required
                value={targetPlanCode}
                onChange={(event) => setTargetPlanCode(event.target.value)}
                options={[{ value: '', label: 'Selecione um plano' }, ...activePlanOptions]}
              />
            </Field>
            <Field
              label="Data de vigência"
              htmlFor={effectiveAtFieldId}
              required
              hint="Vigência no passado ou agora aplica a mudança imediatamente."
            >
              <Input
                id={effectiveAtFieldId}
                type="datetime-local"
                required
                value={effectiveAtLocal}
                onChange={(event) => setEffectiveAtLocal(event.target.value)}
              />
            </Field>
          </div>

          <Field label="Motivo" htmlFor={reasonFieldId} required hint="Obrigatório — registrado no histórico com autor e data.">
            <Input
              id={reasonFieldId}
              required
              value={reason}
              onChange={(event) => setReason(event.target.value)}
              placeholder="Ex.: Aditivo contratual #32"
            />
          </Field>

          {selectedPlanSummary ? (
            <div className="tenant-plan__compare">
              <div className="tenant-plan__compare-column">
                <h4>Antes ({plan.current})</h4>
                <ul>
                  {currentCapabilities.length === 0 ? (
                    <li className="db-hint">Nenhuma</li>
                  ) : (
                    currentCapabilities.map((capability) => (
                      <li
                        key={capability}
                        className={`tenant-plan__compare-item${lostCapabilities.includes(capability) ? ' tenant-plan__compare-item--lost' : ''}`}
                      >
                        {capability}
                      </li>
                    ))
                  )}
                </ul>
              </div>
              <div className="tenant-plan__compare-column">
                <h4>Depois ({selectedPlanSummary.name})</h4>
                <ul>
                  {targetCapabilities.length === 0 ? (
                    <li className="db-hint">Nenhuma</li>
                  ) : (
                    targetCapabilities.map((capability) => (
                      <li
                        key={capability}
                        className={`tenant-plan__compare-item${gainedCapabilities.includes(capability) ? ' tenant-plan__compare-item--gained' : ''}`}
                      >
                        {capability}
                      </li>
                    ))
                  )}
                </ul>
              </div>
            </div>
          ) : null}

          {isDowngrade ? (
            <AlertBanner tone="warning" title="Este é um downgrade">
              As capacidades riscadas acima ({lostCapabilities.join(', ')}) serão perdidas nesta mudança.
            </AlertBanner>
          ) : null}
        </Modal>
      ) : null}
    </Card>
  );
}
