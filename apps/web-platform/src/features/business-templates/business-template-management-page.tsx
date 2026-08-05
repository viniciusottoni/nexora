import { useEffect, useId, useState } from 'react';
import { AlertBanner, Badge, Button, Card, DataTable, Field, Input } from '@nexora/ui';
import type { BusinessTemplateDetailResponse, BusinessTemplateSummary } from '@nexora/contracts';
import type { BusinessTemplatesApi } from './business-templates-api.js';
import './business-template-management-page.css';

export interface BusinessTemplateManagementPageProps {
  readonly api: BusinessTemplatesApi;
}

/**
 * Manutenção do catálogo de modelos de negócio pela Replay (US-142 §3.1 "Manutenção dos modelos
 * pela Replay", §10 "Escolha do modelo... com pré-visualização"). Lista os 4 modelos e edita
 * config/seeds como JSON estruturado — fidelidade deliberadamente simples (textarea validado, não
 * um formulário por campo de config): o conteúdo de cada modelo já é grande (8 seções de config +
 * 4 listas de seed) e um editor por campo ficaria fora do orçamento desta tarefa; o JSON bruto é a
 * mesma forma que `business_template.config`/`.seeds` persiste, então não há perda de fidelidade,
 * só de conforto de edição.
 */
export function BusinessTemplateManagementPage({ api }: Readonly<BusinessTemplateManagementPageProps>) {
  const nameFieldId = useId();
  const configFieldId = useId();
  const seedsFieldId = useId();

  const [templates, setTemplates] = useState<readonly BusinessTemplateSummary[]>([]);
  const [selectedCode, setSelectedCode] = useState<string>();
  const [detail, setDetail] = useState<BusinessTemplateDetailResponse>();
  const [name, setName] = useState('');
  const [configText, setConfigText] = useState('');
  const [seedsText, setSeedsText] = useState('');

  const [loading, setLoading] = useState(true);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    void api
      .list()
      .then((list) => {
        if (cancelled) return;
        setTemplates(list);
        setSelectedCode((current) => current ?? list[0]?.code);
      })
      .catch((reason) => !cancelled && setError(toMessage(reason)))
      .finally(() => !cancelled && setLoading(false));
    return () => {
      cancelled = true;
    };
  }, [api]);

  useEffect(() => {
    if (!selectedCode) return;
    let cancelled = false;
    void api
      .get(selectedCode)
      .then((loaded) => {
        if (cancelled) return;
        setDetail(loaded);
        setName(loaded.name);
        setConfigText(prettyPrint(loaded.configJson));
        setSeedsText(prettyPrint(loaded.seedsJson));
      })
      .catch((reason) => !cancelled && setError(toMessage(reason)));
    return () => {
      cancelled = true;
    };
  }, [api, selectedCode]);

  const configJsonError = validateJson(configText);
  const seedsJsonError = validateJson(seedsText);

  async function save() {
    if (!selectedCode || configJsonError || seedsJsonError) return;
    setBusy(true);
    setError(undefined);
    try {
      const updated = await api.update(selectedCode, {
        name: name.trim(),
        configJson: minify(configText),
        seedsJson: minify(seedsText),
      });
      setDetail(updated);
      setTemplates((current) =>
        current.map((template) =>
          template.code === updated.code
            ? { code: updated.code, name: updated.name, version: updated.version }
            : template,
        ),
      );
      setNotice(`Modelo salvo — agora na versão ${updated.version}. Tenants já provisionados não são afetados.`);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="business-templates-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · catálogo de produto</p>
          <h1 className="db-page__title" id="business-templates-title">
            Modelos de negócio
          </h1>
          <p className="db-page__lead">
            Pizzaria, hamburgueria, restaurante e lanchonete — cada um com suas próprias praças,
            categorias e limiares. Editar aqui nunca altera um estabelecimento já provisionado.
          </p>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Catálogo atualizado">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {loading ? (
        <div className="db-loading">Carregando modelos…</div>
      ) : (
        <div className="db-workbench db-workbench--rail">
          <Card
            padding="none"
            title="Modelos cadastrados"
            subtitle="Aplicados na criação de um novo estabelecimento."
          >
            <DataTable
              rowKey="code"
              rows={templates}
              onRowClick={(row) => setSelectedCode(row.code)}
              columns={[
                { key: 'name', header: 'Nome' },
                { key: 'code', header: 'Código' },
                {
                  key: 'version',
                  header: 'Versão',
                  align: 'right',
                  render: (row) => <Badge tone={row.code === selectedCode ? 'brand' : 'neutral'}>v{row.version}</Badge>,
                },
              ]}
            />
          </Card>

          {detail ? (
            <Card
              className="db-form-card"
              title={detail.name}
              subtitle={`Código ${detail.code} · versão atual ${detail.version}`}
            >
              <Field label="Nome do modelo" htmlFor={nameFieldId}>
                <Input id={nameFieldId} value={name} onChange={(event) => setName(event.target.value)} />
              </Field>

              <Field
                label="Configuração (JSON)"
                htmlFor={configFieldId}
                hint="branding, operation, thresholds, modules, fiscal, printers, payments, maintenance."
                {...(configJsonError ? { error: configJsonError } : {})}
              >
                <textarea
                  id={configFieldId}
                  className="business-template-json-editor"
                  spellCheck={false}
                  value={configText}
                  onChange={(event) => setConfigText(event.target.value)}
                />
              </Field>

              <Field
                label="Seeds (JSON)"
                htmlFor={seedsFieldId}
                hint="roles, stations, expenseCategories, financialAccounts."
                {...(seedsJsonError ? { error: seedsJsonError } : {})}
              >
                <textarea
                  id={seedsFieldId}
                  className="business-template-json-editor"
                  spellCheck={false}
                  value={seedsText}
                  onChange={(event) => setSeedsText(event.target.value)}
                />
              </Field>

              <div className="db-editor__footer">
                <p className="db-hint">Salvar incrementa a versão — novos tenants recebem a versão nova; os já criados continuam como estavam.</p>
                <Button
                  type="button"
                  busy={busy}
                  disabled={Boolean(configJsonError) || Boolean(seedsJsonError)}
                  onClick={() => void save()}
                >
                  Salvar modelo
                </Button>
              </div>
            </Card>
          ) : null}
        </div>
      )}
    </main>
  );
}

function prettyPrint(json: string): string {
  try {
    return JSON.stringify(JSON.parse(json), null, 2);
  } catch {
    return json;
  }
}

function minify(json: string): string {
  try {
    return JSON.stringify(JSON.parse(json));
  } catch {
    return json;
  }
}

function validateJson(value: string): string | undefined {
  try {
    JSON.parse(value);
    return undefined;
  } catch {
    return 'JSON inválido.';
  }
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
