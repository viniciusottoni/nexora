import { useId, useMemo, useRef, useState } from 'react';
import { AlertBanner, Button, Card, DataTable, EmptyState, Icon } from '@nexora/ui';
import type {
  CatalogImportCommitResponse,
  CatalogImportRowError,
  CatalogImportValidateResponse,
} from '@nexora/contracts';
import { CATALOG_IMPORT_TEMPLATE_FILENAME, CatalogImportApi } from './catalog-import-api.js';
import './catalog-import-page.css';

export interface CatalogImportPageProps {
  readonly api?: CatalogImportApi;
}

type Step = 'PICK' | 'PREVIEW' | 'RESULT';

/**
 * US-144 (Importação de cardápio por planilha) — upload -> validar/pré-visualizar -> confirmar,
 * mesmo espírito de fluxo em etapas de `ProvisionTenantPage` (US-002). Página self-contida (cria a
 * própria `CatalogImportApi`, sem depender de props de um container em `app.tsx` — ver relatório
 * da tarefa para a seção de navegação sugerida).
 */
export function CatalogImportPage({ api: providedApi }: Readonly<CatalogImportPageProps>) {
  const fileInputId = useId();
  const api = useMemo(() => providedApi ?? new CatalogImportApi(), [providedApi]);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const [step, setStep] = useState<Step>('PICK');
  const [file, setFile] = useState<File>();
  const [validation, setValidation] = useState<CatalogImportValidateResponse>();
  const [commitResult, setCommitResult] = useState<CatalogImportCommitResponse>();
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');

  function reset() {
    setStep('PICK');
    setFile(undefined);
    setValidation(undefined);
    setCommitResult(undefined);
    setError('');
    if (fileInputRef.current) fileInputRef.current.value = '';
  }

  async function downloadTemplate() {
    setError('');
    try {
      const blob = await api.downloadTemplate();
      const url = URL.createObjectURL(blob);
      const anchor = document.createElement('a');
      anchor.href = url;
      anchor.download = CATALOG_IMPORT_TEMPLATE_FILENAME;
      anchor.click();
      URL.revokeObjectURL(url);
    } catch (reason) {
      setError(toMessage(reason));
    }
  }

  async function validate() {
    if (!file) return;
    setBusy(true);
    setError('');
    try {
      const response = await api.validate(file);
      setValidation(response);
      setStep('PREVIEW');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function confirmImport() {
    if (!file) return;
    setBusy(true);
    setError('');
    try {
      const response = await api.commit(file);
      setCommitResult(response);
      setStep('RESULT');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="catalog-import-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Cardápio</p>
          <h1 className="db-page__title" id="catalog-import-title">
            Importar cardápio por planilha
          </h1>
          <p className="db-page__lead">
            Cadastre categorias, produtos, variações e preços de uma vez, a partir de uma planilha
            .xlsx — a carga inicial deixa de ser o passo mais lento da implantação.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" variant="ghost" onClick={() => void downloadTemplate()}>
            <Icon name="download" size={18} /> Baixar modelo
          </Button>
        </div>
      </header>

      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {step === 'PICK' ? (
        <Card
          className="db-form-card"
          title="1. Escolha a planilha"
          subtitle="Use o modelo acima como ponto de partida — colunas: categoria, produto, descrição, variação e preço."
        >
          <div className="catalog-import-picker">
            <label htmlFor={fileInputId} className="catalog-import-picker__label">
              <Icon name="upload_file" size={28} />
              <span>{file ? file.name : 'Selecionar arquivo .xlsx'}</span>
            </label>
            <input
              id={fileInputId}
              ref={fileInputRef}
              type="file"
              accept=".xlsx"
              onChange={(event) => setFile(event.target.files?.[0])}
            />
          </div>

          <div className="db-editor__footer">
            <Button type="button" busy={busy} disabled={!file} onClick={() => void validate()}>
              Validar planilha
            </Button>
          </div>
        </Card>
      ) : null}

      {step === 'PREVIEW' && validation ? (
        <PreviewStep
          validation={validation}
          busy={busy}
          onBack={reset}
          onConfirm={() => void confirmImport()}
        />
      ) : null}

      {step === 'RESULT' && commitResult ? (
        <ResultStep result={commitResult} onImportAnother={reset} />
      ) : null}
    </main>
  );
}

function PreviewStep({
  validation,
  busy,
  onBack,
  onConfirm,
}: Readonly<{
  validation: CatalogImportValidateResponse;
  busy: boolean;
  onBack: () => void;
  onConfirm: () => void;
}>) {
  if (!validation.valid) {
    return (
      <Card
        title="Erros na planilha"
        subtitle="Corrija as linhas indicadas e envie o arquivo novamente — nenhum item foi gravado."
      >
        <AlertBanner tone="danger" title={`${validation.errors.length} linha(s) com problema`}>
          Nenhuma linha será importada até que todos os erros sejam corrigidos.
        </AlertBanner>
        <RowErrorsTable errors={validation.errors} />
        <div className="db-editor__footer">
          <Button type="button" variant="ghost" onClick={onBack}>
            Escolher outra planilha
          </Button>
        </div>
      </Card>
    );
  }

  const { toCreate, toUpdate } = validation.preview;

  return (
    <Card
      title="2. Confira antes de importar"
      subtitle="Nada foi gravado ainda — confirme para aplicar as mudanças."
    >
      <div className="catalog-import-preview-grid nx-stagger">
        <PreviewCountCard label="Categorias novas" value={toCreate.categories} />
        <PreviewCountCard label="Produtos novos" value={toCreate.products} />
        <PreviewCountCard label="Variações novas" value={toCreate.variants} />
        <PreviewCountCard label="Categorias atualizadas" value={toUpdate.categories} tone="update" />
        <PreviewCountCard label="Produtos atualizados" value={toUpdate.products} tone="update" />
        <PreviewCountCard label="Variações atualizadas" value={toUpdate.variants} tone="update" />
      </div>

      <div className="db-editor__footer">
        <Button type="button" variant="ghost" onClick={onBack}>
          Escolher outra planilha
        </Button>
        <Button type="button" busy={busy} onClick={onConfirm}>
          Confirmar importação
        </Button>
      </div>
    </Card>
  );
}

function ResultStep({
  result,
  onImportAnother,
}: Readonly<{ result: CatalogImportCommitResponse; onImportAnother: () => void }>) {
  if (!result.valid) {
    return (
      <Card
        title="Erros na planilha"
        subtitle="A planilha mudou desde a validação — corrija as linhas indicadas e tente novamente."
      >
        <AlertBanner tone="danger" title={`${result.errors.length} linha(s) com problema`}>
          Nenhuma linha foi importada.
        </AlertBanner>
        <RowErrorsTable errors={result.errors} />
        <div className="db-editor__footer">
          <Button type="button" onClick={onImportAnother}>
            Escolher outra planilha
          </Button>
        </div>
      </Card>
    );
  }

  return (
    <Card
      title="Importação concluída"
      subtitle="O cardápio foi atualizado — resumo do que foi criado e atualizado."
    >
      <AlertBanner tone="success" title="Cardápio importado com sucesso">
        {result.created.categories} categoria(s), {result.created.products} produto(s) e{' '}
        {result.created.variants} variação(ões) criados; {result.updated.categories} categoria(s),{' '}
        {result.updated.products} produto(s) e {result.updated.variants} variação(ões) atualizados.
      </AlertBanner>

      <div className="catalog-import-preview-grid nx-stagger">
        <PreviewCountCard label="Categorias criadas" value={result.created.categories} />
        <PreviewCountCard label="Produtos criados" value={result.created.products} />
        <PreviewCountCard label="Variações criadas" value={result.created.variants} />
        <PreviewCountCard label="Categorias atualizadas" value={result.updated.categories} tone="update" />
        <PreviewCountCard label="Produtos atualizados" value={result.updated.products} tone="update" />
        <PreviewCountCard label="Variações atualizadas" value={result.updated.variants} tone="update" />
      </div>

      <div className="db-editor__footer">
        <Button type="button" onClick={onImportAnother}>
          Importar outra planilha
        </Button>
      </div>
    </Card>
  );
}

function PreviewCountCard({
  label,
  value,
  tone = 'create',
}: Readonly<{ label: string; value: number; tone?: 'create' | 'update' }>) {
  return (
    <div className={`catalog-import-count catalog-import-count--${tone}`}>
      <strong>{value}</strong>
      <span>{label}</span>
    </div>
  );
}

function RowErrorsTable({ errors }: Readonly<{ errors: readonly CatalogImportRowError[] }>) {
  if (errors.length === 0) {
    return <EmptyState icon="check_circle" title="Nenhum erro encontrado" />;
  }

  return (
    <DataTable
      rowKey="key"
      rows={errors.map((error, index) => ({ ...error, key: `${error.row}-${error.column}-${index}` }))}
      columns={[
        { key: 'row', header: 'Linha', width: '5rem', render: (row) => row.row },
        { key: 'column', header: 'Coluna', width: '10rem', render: (row) => row.column },
        { key: 'message', header: 'Erro', render: (row) => row.message },
      ]}
    />
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
