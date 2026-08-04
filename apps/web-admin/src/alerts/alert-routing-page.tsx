import { useEffect, useId, useMemo, useState } from 'react';
import { AlertBanner, Button, Card, Checkbox, Field, Input, Select } from '@nexora/ui';
import {
  alertEngineTypes,
  type AlertRoutingConfig,
  type AlertRoutingRule,
  type AlertRoutingRulePatch,
  type AlertRoutingScope,
  type RoleDto,
  type UpdateAlertRoutingRequest,
} from '@nexora/contracts';
import { AlertRoutingApi } from './alert-routing-api.js';
import './alerts.css';

/**
 * US-082 (Direcionamento por perfil e por ação) §10 — "quem deve ser avisado quando um pedido
 * atrasa?", com pré-visualização de quem receberia cada tipo de alerta. Auto-suficiente (busca os
 * próprios dados via `AlertRoutingApi`), mas recebe `roles` como prop porque a lista de papéis já
 * é carregada no boot do `CloudAdmin` para outras telas (Papéis e permissões) — não faz sentido
 * buscá-la de novo aqui.
 *
 * Bancada padrão `db-workbench db-workbench--rail`: os 7 tipos de alerta do catálogo fixo do motor
 * (`alertEngineTypes`, não configurável — US-080 §2) à esquerda, editor da regra selecionada à
 * direita. Uma única regra é editada e salva por vez (PATCH é `Record<AlertType, Patch>`, mas o
 * contrato pede "um tipo por vez" — CLAUDE.md da tarefa).
 */

const ALERT_TYPE_LABEL: Record<string, string> = {
  ORDER_LATE: 'Pedido atrasado',
  AVG_TIME_ABOVE_TARGET: 'Tempo médio acima da meta',
  PRODUCT_UNAVAILABLE: 'Produto indisponível',
  CASH_DIVERGENCE: 'Divergência de caixa',
  SYNC_DELAY: 'Atraso de sincronização',
  CANCELLATION_ABOVE_THRESHOLD: 'Cancelamento acima do padrão',
  DISCOUNT_ABOVE_THRESHOLD: 'Desconto acima do padrão',
};

const SCOPE_OPTIONS: ReadonlyArray<{ readonly value: AlertRoutingScope; readonly label: string }> = [
  { value: 'TENANT', label: 'Todos do papel' },
  { value: 'RESPONSIBLE', label: 'Quem responde pela entidade' },
  { value: 'TABLE_OWNER', label: 'Garçom dono da mesa' },
  { value: 'STATION', label: 'Quem está na praça/estação' },
];

const SCOPE_DESCRIPTION: Record<AlertRoutingScope, string> = {
  TENANT: 'todos que têm este papel no estabelecimento',
  RESPONSIBLE: 'apenas quem responde por esta entidade (ex.: o garçom responsável pela mesa)',
  TABLE_OWNER: 'o garçom dono da mesa envolvida',
  STATION: 'quem está na praça/estação envolvida',
};

const DEFAULT_RULE: AlertRoutingRule = {
  roles: [],
  scope: 'TENANT',
  escalateAfterSeconds: null,
  groupWindowSeconds: null,
};

interface EditableRule {
  readonly roles: readonly string[];
  readonly scope: AlertRoutingScope;
  readonly escalateAfterSeconds: string;
  readonly groupWindowSeconds: string;
}

function toEditable(rule: AlertRoutingRule): EditableRule {
  return {
    roles: rule.roles,
    scope: rule.scope,
    escalateAfterSeconds: rule.escalateAfterSeconds?.toString() ?? '',
    groupWindowSeconds: rule.groupWindowSeconds?.toString() ?? '',
  };
}

export interface AlertRoutingPageProps {
  readonly roles: readonly RoleDto[];
  /** Injetável para teste — padrão `new AlertRoutingApi()`. */
  readonly alertRoutingApi?: AlertRoutingApi;
}

export function AlertRoutingPage({
  roles,
  alertRoutingApi = new AlertRoutingApi(),
}: Readonly<AlertRoutingPageProps>) {
  const scopeFieldId = useId();
  const escalateFieldId = useId();
  const groupWindowFieldId = useId();

  const [config, setConfig] = useState<AlertRoutingConfig>();
  const [selectedType, setSelectedType] = useState<string>(alertEngineTypes[0]);
  const [edited, setEdited] = useState<EditableRule>();
  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();

  useEffect(() => {
    let active = true;
    setLoading(true);
    setError(undefined);
    alertRoutingApi
      .get()
      .then((result) => {
        if (active) setConfig(result);
      })
      .catch((reason: unknown) => {
        if (active) setError(toMessage(reason));
      })
      .finally(() => {
        if (active) setLoading(false);
      });
    return () => {
      active = false;
    };
  }, [alertRoutingApi]);

  const baselineRule = config?.[selectedType] ?? DEFAULT_RULE;

  useEffect(() => {
    setEdited(toEditable(baselineRule));
    // Reage à troca de tipo selecionado ou a uma configuração recém-carregada/salva — `baselineRule`
    // é derivado desses dois, incluí-lo também na dependência re-executaria o efeito a cada render.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [selectedType, config]);

  function toggleRole(code: string) {
    setEdited((current) => {
      if (!current) return current;
      const nextRoles = current.roles.includes(code)
        ? current.roles.filter((candidate) => candidate !== code)
        : [...current.roles, code];
      return { ...current, roles: nextRoles };
    });
    setNotice(undefined);
  }

  const dirty = useMemo(() => {
    if (!edited) return false;
    return (
      !sameRoles(edited.roles, baselineRule.roles) ||
      edited.scope !== baselineRule.scope ||
      normalizeSeconds(edited.escalateAfterSeconds) !== baselineRule.escalateAfterSeconds ||
      normalizeSeconds(edited.groupWindowSeconds) !== baselineRule.groupWindowSeconds
    );
  }, [edited, baselineRule]);

  const preview = edited ? buildPreview(edited, roles) : '';

  async function save() {
    if (!edited || !dirty) return;
    setBusy(true);
    setError(undefined);
    try {
      const patch: AlertRoutingRulePatch = {};
      if (!sameRoles(edited.roles, baselineRule.roles)) patch.roles = [...edited.roles];
      if (edited.scope !== baselineRule.scope) patch.scope = edited.scope;
      const escalate = normalizeSeconds(edited.escalateAfterSeconds);
      if (escalate !== baselineRule.escalateAfterSeconds) patch.escalateAfterSeconds = escalate;
      const groupWindow = normalizeSeconds(edited.groupWindowSeconds);
      if (groupWindow !== baselineRule.groupWindowSeconds) patch.groupWindowSeconds = groupWindow;

      const request: UpdateAlertRoutingRequest = { [selectedType]: patch };
      const updated = await alertRoutingApi.update(request);
      setConfig(updated);
      setNotice('Direcionamento atualizado.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="alert-routing-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Alertas</p>
          <h1 className="db-page__title" id="alert-routing-title">
            Direcionamento de alertas
          </h1>
          <p className="db-page__lead">
            Quem deve ser avisado quando um pedido atrasa, um item fica pronto ou o caixa fecha
            divergente? Cada tipo de alerta tem seus próprios papéis, escopo e escalonamento
            (US-082) — só quem age recebe, ninguém aprende a ignorar o sistema.
          </p>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Direcionamento salvo">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? (
        <AlertBanner tone="danger" title="Falha ao carregar direcionamento">
          {error}
        </AlertBanner>
      ) : null}

      {loading ? (
        <p className="db-loading" role="status">
          <span className="nx-spinner" aria-hidden="true" />
          Carregando direcionamento…
        </p>
      ) : config ? (
        <div className="db-workbench db-workbench--rail">
          <nav className="db-list nx-stagger" aria-label="Tipos de alerta">
            {alertEngineTypes.map((type) => {
              const rule = config[type] ?? DEFAULT_RULE;
              return (
                <button
                  type="button"
                  key={type}
                  className={`db-list__item ${type === selectedType ? 'db-list__item--on' : ''}`}
                  onClick={() => setSelectedType(type)}
                >
                  <span className="db-list__text">
                    <span className="db-list__name">{ALERT_TYPE_LABEL[type] ?? type}</span>
                    <span className="db-list__meta">
                      {rule.roles.length > 0
                        ? `${rule.roles.length} papel(éis)`
                        : 'Sem destinatário'}
                    </span>
                  </span>
                </button>
              );
            })}
          </nav>

          {edited ? (
            <Card
              className="db-form-card"
              title={ALERT_TYPE_LABEL[selectedType] ?? selectedType}
              subtitle="Papéis, escopo e escalonamento para este tipo de alerta."
            >
              <fieldset className="alert-routing-roles">
                <legend>Quem recebe</legend>
                {roles.length === 0 ? (
                  <p className="db-hint">Nenhum papel cadastrado ainda.</p>
                ) : (
                  roles.map((role) => (
                    <Checkbox
                      key={role.id}
                      checked={edited.roles.includes(role.code)}
                      onChange={() => toggleRole(role.code)}
                      label={role.name}
                    />
                  ))
                )}
              </fieldset>

              <Field
                label="Escopo"
                htmlFor={scopeFieldId}
                hint="Recorte de quem, dentro do papel, recebe o alerta."
              >
                <Select
                  id={scopeFieldId}
                  value={edited.scope}
                  onChange={(event) =>
                    setEdited((current) =>
                      current
                        ? { ...current, scope: event.target.value as AlertRoutingScope }
                        : current,
                    )
                  }
                  options={SCOPE_OPTIONS}
                />
              </Field>

              <div className="db-form-row">
                <Field
                  label="Escalonar após (segundos)"
                  htmlFor={escalateFieldId}
                  hint="Sem reconhecimento nesse prazo, o alerta escala. Em branco = sem escalonamento."
                >
                  <Input
                    id={escalateFieldId}
                    type="number"
                    min={1}
                    numeric
                    value={edited.escalateAfterSeconds}
                    onChange={(event) =>
                      setEdited((current) =>
                        current
                          ? { ...current, escalateAfterSeconds: event.target.value }
                          : current,
                      )
                    }
                  />
                </Field>
                <Field
                  label="Agrupar em janelas de (segundos)"
                  htmlFor={groupWindowFieldId}
                  hint="Ocorrências repetidas nesse intervalo viram um único item na central (US-083). Em branco = sem agrupamento."
                >
                  <Input
                    id={groupWindowFieldId}
                    type="number"
                    min={1}
                    numeric
                    value={edited.groupWindowSeconds}
                    onChange={(event) =>
                      setEdited((current) =>
                        current ? { ...current, groupWindowSeconds: event.target.value } : current,
                      )
                    }
                  />
                </Field>
              </div>

              <AlertBanner tone="info" icon="visibility" title="Pré-visualização">
                {preview}
              </AlertBanner>

              <div className="db-editor__footer">
                <p className="db-hint">
                  {dirty ? 'Alterações pendentes' : 'Nenhuma alteração pendente'}
                </p>
                <Button type="button" busy={busy} disabled={!dirty} onClick={() => void save()}>
                  Salvar direcionamento
                </Button>
              </div>
            </Card>
          ) : null}
        </div>
      ) : null}
    </main>
  );
}

function sameRoles(a: readonly string[], b: readonly string[]): boolean {
  if (a.length !== b.length) return false;
  const setB = new Set(b);
  return a.every((role) => setB.has(role));
}

function normalizeSeconds(value: string): number | null {
  const trimmed = value.trim();
  if (!trimmed) return null;
  const parsed = Number(trimmed);
  return Number.isFinite(parsed) && parsed > 0 ? parsed : null;
}

function formatSeconds(seconds: number): string {
  if (seconds % 60 === 0) return `${seconds / 60} min`;
  return `${seconds}s`;
}

function buildPreview(rule: EditableRule, roles: readonly RoleDto[]): string {
  if (rule.roles.length === 0) {
    return 'Nenhum papel selecionado — este alerta não seria entregue a ninguém.';
  }
  const roleNames = rule.roles
    .map((code) => roles.find((role) => role.code === code)?.name ?? code)
    .join(', ');
  let text = `Vai para: ${roleNames} — ${SCOPE_DESCRIPTION[rule.scope]}.`;
  const escalate = normalizeSeconds(rule.escalateAfterSeconds);
  if (escalate) text += ` Sem reconhecimento, escala após ${formatSeconds(escalate)}.`;
  const groupWindow = normalizeSeconds(rule.groupWindowSeconds);
  if (groupWindow) {
    text += ` Ocorrências repetidas se agrupam em janelas de ${formatSeconds(groupWindow)}.`;
  }
  return text;
}

function toMessage(reason: unknown): string {
  return reason instanceof Error
    ? reason.message
    : 'Não foi possível carregar o direcionamento de alertas.';
}
