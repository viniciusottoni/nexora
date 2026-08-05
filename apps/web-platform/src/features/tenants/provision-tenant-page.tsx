import { AlertBanner, Button, Card, Field, Icon, Input, Select } from '@nexora/ui';
import type { BusinessTemplateSummary, CreateTenantRequest, CreateTenantResponse } from '@nexora/contracts';
import { useEffect, useId, useMemo, useRef, useState, type FormEvent } from 'react';

import { createTenantsApi, type ApiProblem, type TenantsApi } from './tenants-api.js';
import { deriveSlugSuggestion, maskInstallationCommand } from './provisioning-view-model.js';
import './provision-tenant-page.css';

interface ProvisionTenantPageProps {
  readonly api?: TenantsApi;
  /** US-150 §12 (E2E "novo estabelecimento → retorno à lista") — chamado pelo botão "Voltar aos estabelecimentos" após o provisionamento. */
  readonly onDone?: () => void;
}

const INITIAL_FORM: CreateTenantRequest = {
  name: '',
  slug: '',
  plan: 'COMPLETO',
  template: 'PIZZERIA',
  owner: { name: '', email: '' },
  store: { name: 'Matriz', timezone: 'America/Sao_Paulo' },
};

const PLAN_OPTIONS = [
  { value: 'COMPLETO', label: 'Completo' },
  { value: 'OPERACAO', label: 'Operação' },
  { value: 'GESTAO', label: 'Gestão' },
];

const TIMEZONE_OPTIONS = [
  { value: 'America/Sao_Paulo', label: 'Brasília (GMT−3)' },
  { value: 'America/Manaus', label: 'Manaus (GMT−4)' },
  { value: 'America/Rio_Branco', label: 'Rio Branco (GMT−5)' },
];

export function ProvisionTenantPage({ api: providedApi, onDone }: ProvisionTenantPageProps) {
  const nameFieldId = useId();
  const slugFieldId = useId();
  const planFieldId = useId();
  const templateFieldId = useId();
  const ownerNameFieldId = useId();
  const ownerEmailFieldId = useId();
  const storeNameFieldId = useId();
  const timezoneFieldId = useId();
  const api = useMemo(() => providedApi ?? createTenantsApi(), [providedApi]);
  const [form, setForm] = useState(INITIAL_FORM);
  // US-142: modelos deixaram de ser um seletor travado em "Pizzaria" — vêm do catálogo mantido
  // pela Replay (GET /v1/platform/templates). Enquanto carrega/se falhar, a pizzaria continua
  // disponível como opção (era o único valor possível antes desta história).
  const [templates, setTemplates] = useState<readonly BusinessTemplateSummary[]>([
    { code: 'PIZZERIA', name: 'Pizzaria', version: 1 },
  ]);

  useEffect(() => {
    let cancelled = false;
    void api
      .listTemplates()
      .then((list) => {
        if (!cancelled && list.length > 0) setTemplates(list);
      })
      .catch(() => {
        // Mantém o fallback (pizzaria) — o formulário continua utilizável mesmo se o catálogo
        // de modelos estiver indisponível.
      });
    return () => {
      cancelled = true;
    };
  }, [api]);
  const [slugWasEdited, setSlugWasEdited] = useState(false);
  const [slugStatus, setSlugStatus] = useState<'IDLE' | 'CHECKING' | 'AVAILABLE' | 'TAKEN'>('IDLE');
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [result, setResult] = useState<CreateTenantResponse>();
  const [commandRevealed, setCommandRevealed] = useState(false);
  const [copyStatus, setCopyStatus] = useState('');
  // A frase de confirmação é sempre igual — sem um contador na key, cliques seguidos em "Copiar
  // comando" não remontariam .copy-status (mesmo texto = mesma key), e a entrada suave (ver
  // .copy-status no CSS) só tocaria na primeira vez.
  const [copyCount, setCopyCount] = useState(0);
  const slugCheck = useRef(0);

  useEffect(() => {
    if (!/^[a-z0-9]+(?:-[a-z0-9]+)*$/.test(form.slug)) {
      setSlugStatus('IDLE');
      return;
    }
    const request = ++slugCheck.current;
    setSlugStatus('CHECKING');
    const timeout = window.setTimeout(() => {
      void api
        .checkSlug(form.slug)
        .then((available) => {
          if (request === slugCheck.current) setSlugStatus(available ? 'AVAILABLE' : 'TAKEN');
        })
        .catch(() => {
          if (request === slugCheck.current) setSlugStatus('IDLE');
        });
    }, 350);
    return () => window.clearTimeout(timeout);
  }, [api, form.slug]);

  const updateName = (name: string) => {
    setForm((current) => ({
      ...current,
      name,
      slug: deriveSlugSuggestion(name, current.slug, slugWasEdited),
    }));
  };

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    if (slugStatus === 'TAKEN') {
      setError('Escolha outro endereço para o estabelecimento.');
      return;
    }
    setBusy(true);
    setError('');
    try {
      setResult(await api.provision(form));
    } catch (caught) {
      const problem = caught as ApiProblem;
      setError(
        problem.code === 'SLUG_ALREADY_TAKEN'
          ? 'Este endereço já está em uso. Escolha outro para continuar.'
          : problem.message,
      );
    } finally {
      setBusy(false);
    }
  };

  const copyCommand = async () => {
    if (!result) return;
    await navigator.clipboard.writeText(result.installCommand);
    setCopyStatus('Comando copiado. Guarde-o em local seguro.');
    setCopyCount((count) => count + 1);
  };

  if (result) {
    const completed = result.checklist.filter(({ status }) => status === 'COMPLETED').length;

    return (
      <main className="db-page nx-anim-in" aria-labelledby="provision-result-title">
        <header className="db-page__header">
          <div className="db-page__heading">
            <p className="db-page__eyebrow">Estabelecimento provisionado</p>
            <h1 className="db-page__title" id="provision-result-title">
              {form.name} está pronto para implantação
            </h1>
            <p className="db-page__lead">
              A base foi criada. Continue pela instalação segura do servidor local.
            </p>
          </div>
        </header>

        <div className="db-workbench db-workbench--rail" aria-label="Próximos passos">
          <Card
            className="db-form-card"
            title="Instale o servidor local"
            subtitle="O comando abaixo cria o edge server da loja e consome o token de instalação."
          >
            <AlertBanner tone="warning" title="Token de uso único">
              Não compartilhe este comando em canais públicos — quem tiver o token instala a loja.
            </AlertBanner>

            <code className="install-command">
              {commandRevealed
                ? result.installCommand
                : maskInstallationCommand(result.installCommand)}
            </code>

            <div className="install-actions">
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => setCommandRevealed((visible) => !visible)}
              >
                {commandRevealed ? 'Ocultar token' : 'Revelar token'}
              </Button>
              <Button type="button" size="sm" onClick={() => void copyCommand()}>
                Copiar comando
              </Button>
            </div>
            {/* key força remontagem a cada confirmação — a frase de sucesso é sempre igual, então
                sem isso uma segunda cópia não reacionaria a entrada suave (ver .copy-status). */}
            <p key={copyCount} className="copy-status" role="status" aria-live="polite">
              {copyStatus}
            </p>
            {onDone ? (
              <Button type="button" variant="ghost" size="sm" onClick={onDone}>
                Voltar aos estabelecimentos
              </Button>
            ) : null}
          </Card>

          <Card
            title="Checklist de lançamento"
            subtitle="Implantação em até 5 dias úteis é a meta do produto."
            actions={<strong className="checklist-count">{completed}/9</strong>}
            padding="tight"
          >
            {/* nx-stagger: os 9 passos entram em cascata, no mesmo espírito das seções numeradas
                do formulário — reforça a leitura sequencial do lançamento. */}
            <ol className="deployment-list nx-stagger">
              {result.checklist.map((item) => {
                const done = item.status === 'COMPLETED';
                return (
                  <li key={item.code} data-status={item.status}>
                    <Icon
                      name={done ? 'check_circle' : 'radio_button_unchecked'}
                      size={20}
                      fill={done}
                      color={done ? 'var(--nx-success-500)' : 'var(--text-disabled)'}
                    />
                    <div>
                      <strong>{item.label}</strong>
                      <small>{done ? 'Concluído' : 'Pendente'}</small>
                    </div>
                  </li>
                );
              })}
            </ol>
          </Card>
        </div>
      </main>
    );
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="provision-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · novo cliente</p>
          <h1 className="db-page__title" id="provision-title">
            Provisionar estabelecimento
          </h1>
          <p className="db-page__lead">
            Uma base segura, pronta para receber marca, cardápio e operação. Tudo é configuração —
            nenhuma linha de código por cliente.
          </p>
        </div>
      </header>

      <form onSubmit={(event) => void submit(event)}>
        <Card padding="none">
          <section className="form-section" aria-labelledby="business-heading">
            <div className="form-section__heading">
              <span className="form-section__number" aria-hidden="true">
                01
              </span>
              <div>
                <h2 id="business-heading">Negócio</h2>
                <p className="db-hint">Identificação e configuração comercial.</p>
              </div>
            </div>
            <div className="db-form-row">
              <Field label="Nome do estabelecimento" htmlFor={nameFieldId}>
                <Input
                  id={nameFieldId}
                  required
                  autoComplete="organization"
                  value={form.name}
                  onChange={(event) => updateName(event.target.value)}
                />
              </Field>
              <Field
                label="Endereço na plataforma"
                htmlFor={slugFieldId}
                {...(slugStatus === 'TAKEN' ? { error: 'Este endereço já está em uso.' } : {})}
                hint={
                  slugStatus === 'CHECKING'
                    ? 'Verificando disponibilidade…'
                    : slugStatus === 'AVAILABLE'
                      ? 'Endereço disponível.'
                      : 'Use letras minúsculas, números e hífens.'
                }
              >
                <Input
                  id={slugFieldId}
                  required
                  invalid={slugStatus === 'TAKEN'}
                  value={form.slug}
                  onChange={(event) => {
                    setSlugWasEdited(true);
                    setForm((current) => ({ ...current, slug: event.target.value.toLowerCase() }));
                  }}
                />
              </Field>
              <Field label="Plano" htmlFor={planFieldId}>
                <Select
                  id={planFieldId}
                  value={form.plan}
                  options={PLAN_OPTIONS}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, plan: event.target.value }))
                  }
                />
              </Field>
              <Field
                label="Modelo de negócio"
                htmlFor={templateFieldId}
                hint="Praças, categorias e limiares padrão vêm do modelo escolhido."
              >
                <Select
                  id={templateFieldId}
                  value={form.template}
                  options={templates.map((template) => ({ value: template.code, label: template.name }))}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, template: event.target.value }))
                  }
                />
              </Field>
            </div>
          </section>

          <section className="form-section" aria-labelledby="owner-heading">
            <div className="form-section__heading">
              <span className="form-section__number" aria-hidden="true">
                02
              </span>
              <div>
                <h2 id="owner-heading">Proprietário</h2>
                <p className="db-hint">Receberá convite válido por 72 horas.</p>
              </div>
            </div>
            <div className="db-form-row">
              <Field label="Nome do proprietário" htmlFor={ownerNameFieldId}>
                <Input
                  id={ownerNameFieldId}
                  required
                  autoComplete="name"
                  value={form.owner.name}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      owner: { ...current.owner, name: event.target.value },
                    }))
                  }
                />
              </Field>
              <Field label="E-mail do proprietário" htmlFor={ownerEmailFieldId}>
                <Input
                  id={ownerEmailFieldId}
                  required
                  type="email"
                  autoComplete="email"
                  value={form.owner.email}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      owner: { ...current.owner, email: event.target.value },
                    }))
                  }
                />
              </Field>
            </div>
          </section>

          <section className="form-section" aria-labelledby="store-heading">
            <div className="form-section__heading">
              <span className="form-section__number" aria-hidden="true">
                03
              </span>
              <div>
                <h2 id="store-heading">Primeira loja</h2>
                <p className="db-hint">Unidade matriz e fuso da operação.</p>
              </div>
            </div>
            <div className="db-form-row">
              <Field label="Nome da loja" htmlFor={storeNameFieldId}>
                <Input
                  id={storeNameFieldId}
                  required
                  value={form.store.name}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      store: { ...current.store, name: event.target.value },
                    }))
                  }
                />
              </Field>
              <Field label="Fuso horário" htmlFor={timezoneFieldId}>
                <Select
                  id={timezoneFieldId}
                  value={form.store.timezone}
                  options={TIMEZONE_OPTIONS}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      store: { ...current.store, timezone: event.target.value },
                    }))
                  }
                />
              </Field>
            </div>
          </section>

          <div className="provision-footer">
            {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}
            <div className="db-editor__footer">
              <p className="db-hint">
                Tenant, loja, papéis, praças e convite são criados em uma única transação.
              </p>
              <Button
                type="submit"
                busy={busy}
                disabled={slugStatus === 'TAKEN' || slugStatus === 'CHECKING'}
              >
                {busy ? 'Provisionando…' : 'Criar estabelecimento'}
              </Button>
            </div>
          </div>
        </Card>
      </form>
    </main>
  );
}
