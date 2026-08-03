import { Button, Card, Field, Input } from '@nexora/ui';
import type { CreateTenantRequest, CreateTenantResponse } from '@nexora/contracts';
import { useEffect, useId, useMemo, useRef, useState, type FormEvent } from 'react';

import { createTenantsApi, type ApiProblem, type TenantsApi } from './tenants-api.js';
import { deriveSlugSuggestion, maskInstallationCommand } from './provisioning-view-model.js';
import './provision-tenant-page.css';

interface ProvisionTenantPageProps {
  readonly api?: TenantsApi;
}

const INITIAL_FORM: CreateTenantRequest = {
  name: '',
  slug: '',
  plan: 'COMPLETO',
  template: 'PIZZERIA',
  owner: { name: '', email: '' },
  store: { name: 'Matriz', timezone: 'America/Sao_Paulo' },
};

export function ProvisionTenantPage({ api: providedApi }: ProvisionTenantPageProps) {
  const nameFieldId = useId();
  const slugFieldId = useId();
  const ownerNameFieldId = useId();
  const ownerEmailFieldId = useId();
  const storeNameFieldId = useId();
  const api = useMemo(() => providedApi ?? createTenantsApi(), [providedApi]);
  const [form, setForm] = useState(INITIAL_FORM);
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
    return (
      <main className="provision-shell">
        <header className="provision-heading">
          <span className="provision-eyebrow">Estabelecimento provisionado</span>
          <h1>{form.name} está pronto para implantação</h1>
          <p>A base foi criada. Continue pela instalação segura do servidor local.</p>
        </header>

        <section className="provision-result-grid" aria-label="Próximos passos">
          <Card className="install-card">
            <div className="install-card__number" aria-hidden="true">
              01
            </div>
            <div>
              <h2>Instale o servidor local</h2>
              <p className="secure-note">
                Token de uso único. Não compartilhe este comando em canais públicos.
              </p>
              <code className="install-command">
                {commandRevealed
                  ? result.installCommand
                  : maskInstallationCommand(result.installCommand)}
              </code>
              <div className="install-actions">
                <Button
                  type="button"
                  onClick={() => setCommandRevealed((visible) => !visible)}
                  variant="ghost"
                >
                  {commandRevealed ? 'Ocultar token' : 'Revelar token'}
                </Button>
                <Button type="button" onClick={() => void copyCommand()}>
                  Copiar comando
                </Button>
              </div>
              {/* key força remontagem a cada confirmação — a frase de sucesso é sempre igual, então
                  sem isso uma segunda cópia não reacionaria a entrada suave (ver .copy-status). */}
              <p key={copyCount} className="copy-status" role="status" aria-live="polite">
                {copyStatus}
              </p>
            </div>
          </Card>

          <Card className="checklist-card">
            <div className="checklist-card__header">
              <div>
                <span className="provision-eyebrow">Implantação</span>
                <h2>Checklist de lançamento</h2>
              </div>
              <strong>
                {result.checklist.filter(({ status }) => status === 'COMPLETED').length}/9
              </strong>
            </div>
            {/* nx-stagger: os 9 passos do checklist entram em cascata, no mesmo espírito das
                seções numeradas do formulário — reforça a leitura sequencial do lançamento. */}
            <ol className="deployment-list nx-stagger">
              {result.checklist.map((item, index) => (
                <li key={item.code} data-status={item.status}>
                  <span aria-hidden="true">
                    {item.status === 'COMPLETED' ? '✓' : String(index + 1).padStart(2, '0')}
                  </span>
                  <div>
                    <strong>{item.label}</strong>
                    <small>{item.status === 'COMPLETED' ? 'Concluído' : 'Pendente'}</small>
                  </div>
                </li>
              ))}
            </ol>
          </Card>
        </section>
      </main>
    );
  }

  return (
    <main className="provision-shell">
      <header className="provision-heading">
        <span className="provision-eyebrow">Plataforma · Novo cliente</span>
        <h1>Provisionar estabelecimento</h1>
        <p>Uma base segura, pronta para receber marca, cardápio e operação.</p>
      </header>

      <form onSubmit={(event) => void submit(event)}>
        {/* nx-stagger: cada seção numerada (01 Negócio, 02 Proprietário, 03 Loja) entra em
            cascata na montagem do formulário, reforçando a ordem do wizard sem exigir passos
            separados de fato. */}
        <Card className="provision-form-card nx-stagger">
          <section className="form-section" aria-labelledby="business-heading">
            <div className="form-section__heading">
              <span>01</span>
              <div>
                <h2 id="business-heading">Negócio</h2>
                <p>Identificação e configuração comercial.</p>
              </div>
            </div>
            <div className="form-grid">
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
              <label className="native-field">
                <span>Plano</span>
                <select
                  value={form.plan}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, plan: event.target.value }))
                  }
                >
                  <option value="COMPLETO">Completo</option>
                  <option value="OPERACAO">Operação</option>
                  <option value="GESTAO">Gestão</option>
                </select>
              </label>
              <label className="native-field">
                <span>Modelo de negócio</span>
                <select value={form.template} disabled>
                  <option value="PIZZERIA">Pizzaria</option>
                </select>
                <small>Novos modelos entram como configuração do produto.</small>
              </label>
            </div>
          </section>

          <section className="form-section" aria-labelledby="owner-heading">
            <div className="form-section__heading">
              <span>02</span>
              <div>
                <h2 id="owner-heading">Proprietário</h2>
                <p>Receberá convite válido por 72 horas.</p>
              </div>
            </div>
            <div className="form-grid">
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
              <span>03</span>
              <div>
                <h2 id="store-heading">Primeira loja</h2>
                <p>Unidade matriz e fuso da operação.</p>
              </div>
            </div>
            <div className="form-grid">
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
              <label className="native-field">
                <span>Fuso horário</span>
                <select
                  value={form.store.timezone}
                  onChange={(event) =>
                    setForm((current) => ({
                      ...current,
                      store: { ...current.store, timezone: event.target.value },
                    }))
                  }
                >
                  <option value="America/Sao_Paulo">Brasília (GMT−3)</option>
                  <option value="America/Manaus">Manaus (GMT−4)</option>
                  <option value="America/Rio_Branco">Rio Branco (GMT−5)</option>
                </select>
              </label>
            </div>
          </section>

          <footer className="form-footer">
            <p>Tenant, loja, papéis, praças e convite serão criados em uma única transação.</p>
            <Button
              type="submit"
              busy={busy}
              disabled={slugStatus === 'TAKEN' || slugStatus === 'CHECKING'}
            >
              {busy ? 'Provisionando…' : 'Criar estabelecimento'}
            </Button>
          </footer>
          {error ? (
            <div className="form-error" role="alert">
              {error}
            </div>
          ) : null}
        </Card>
      </form>
    </main>
  );
}
