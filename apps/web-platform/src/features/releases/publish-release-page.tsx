import { AlertBanner, Button, Card, Field, Input, ProgressMeter, StatTile } from '@nexora/ui';
import type { PublishReleaseResponse, ReleaseRolloutResponse } from '@nexora/contracts';
import { useId, useMemo, useState, type FormEvent } from 'react';

import { createReleasesApi, type ApiProblem, type ReleasesApi } from './releases-api.js';
import './publish-release-page.css';

interface PublishReleasePageProps {
  readonly api?: ReleasesApi;
}

interface PublishForm {
  version: string;
  rolloutPercent: number;
  notes: string;
}

const INITIAL_FORM: PublishForm = { version: '', rolloutPercent: 10, notes: '' };

/**
 * US-146 "Atualização controlada do parque" — publica uma versão nova (ou amplia a liberação
 * gradual de uma já publicada, US-146 §3.1) e mostra o progresso do rollout no parque
 * (total/atualizadas/falhas/pendentes, US-146 §10 "Progresso da liberação visível no painel de
 * plataforma"). ADR-019: a atualização é PUXADA por cada edge, dentro da própria janela
 * configurada — esta tela só declara o que está disponível, nunca força nada em uma instalação.
 */
export function PublishReleasePage({ api: providedApi }: Readonly<PublishReleasePageProps>) {
  const versionFieldId = useId();
  const rolloutFieldId = useId();
  const notesFieldId = useId();
  const lookupFieldId = useId();

  const api = useMemo(() => providedApi ?? createReleasesApi(), [providedApi]);

  const [form, setForm] = useState(INITIAL_FORM);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const [published, setPublished] = useState<PublishReleaseResponse>();

  const [lookupVersion, setLookupVersion] = useState('');
  const [rollout, setRollout] = useState<ReleaseRolloutResponse>();
  const [rolloutBusy, setRolloutBusy] = useState(false);
  const [rolloutError, setRolloutError] = useState('');

  const submit = async (event: FormEvent) => {
    event.preventDefault();
    setBusy(true);
    setError('');
    try {
      const result = await api.publish({
        version: form.version,
        rolloutPercent: form.rolloutPercent,
        notes: form.notes.trim().length > 0 ? form.notes.trim() : null,
      });
      setPublished(result);
      setLookupVersion(result.release.version);
      await loadRollout(result.release.version);
    } catch (caught) {
      const problem = caught as ApiProblem;
      setError(
        problem.code === 'RELEASE_ROLLOUT_CANNOT_DECREASE'
          ? 'Esta versão já está liberada para um percentual maior — a liberação gradual nunca reduz.'
          : problem.message,
      );
    } finally {
      setBusy(false);
    }
  };

  const loadRollout = async (version: string) => {
    if (!version.trim()) return;
    setRolloutBusy(true);
    setRolloutError('');
    try {
      setRollout(await api.rollout(version.trim()));
    } catch (caught) {
      setRollout(undefined);
      setRolloutError((caught as ApiProblem).message);
    } finally {
      setRolloutBusy(false);
    }
  };

  return (
    <main className="db-page nx-anim-in" aria-labelledby="releases-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Plataforma · parque</p>
          <h1 className="db-page__title" id="releases-title">
            Versões e liberação gradual
          </h1>
          <p className="db-page__lead">
            Publique uma versão nova do software de edge e acompanhe a liberação — cada instalação
            atualiza sozinha, dentro da própria janela configurada, com rollback automático se o
            health check falhar.
          </p>
        </div>
      </header>

      <div className="db-workbench db-workbench--rail">
        <form onSubmit={(event) => void submit(event)}>
          <Card
            className="db-form-card"
            title="Publicar release"
            subtitle="Republicar a mesma versão com um percentual maior amplia o rollout em curso."
          >
            <div className="db-form-row">
              <Field label="Versão" htmlFor={versionFieldId} hint="Ex.: 1.5.0">
                <Input
                  id={versionFieldId}
                  required
                  maxLength={20}
                  placeholder="1.5.0"
                  value={form.version}
                  onChange={(event) => setForm((current) => ({ ...current, version: event.target.value }))}
                />
              </Field>
              <Field
                label="Percentual de liberação"
                htmlFor={rolloutFieldId}
                hint="Subconjunto do parque elegível para esta versão."
              >
                <Input
                  id={rolloutFieldId}
                  required
                  type="number"
                  min={0}
                  max={100}
                  numeric
                  suffix="%"
                  value={form.rolloutPercent}
                  onChange={(event) =>
                    setForm((current) => ({ ...current, rolloutPercent: Number(event.target.value) }))
                  }
                />
              </Field>
            </div>
            <Field label="Notas" htmlFor={notesFieldId} hint="Opcional — visível só para a equipe de plataforma.">
              <textarea
                id={notesFieldId}
                className="releases-notes-input"
                rows={3}
                maxLength={2000}
                value={form.notes}
                onChange={(event) => setForm((current) => ({ ...current, notes: event.target.value }))}
              />
            </Field>

            {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

            <div className="db-editor__footer">
              <p className="db-hint">Backup, download, migration e health check acontecem no próprio edge.</p>
              <Button type="submit" busy={busy}>
                {busy ? 'Publicando…' : 'Publicar release'}
              </Button>
            </div>
          </Card>
        </form>

        <Card
          title="Progresso da liberação"
          subtitle="Consulte o rollout de qualquer versão já publicada."
        >
          <div className="db-form-row">
            <Field label="Versão para consultar" htmlFor={lookupFieldId}>
              <Input
                id={lookupFieldId}
                placeholder="1.5.0"
                value={lookupVersion}
                onChange={(event) => setLookupVersion(event.target.value)}
              />
            </Field>
            <Button
              type="button"
              variant="ghost"
              size="sm"
              busy={rolloutBusy}
              onClick={() => void loadRollout(lookupVersion)}
            >
              Consultar
            </Button>
          </div>

          {rolloutError ? <AlertBanner tone="danger">{rolloutError}</AlertBanner> : null}

          {published && !rollout && !rolloutBusy && !rolloutError ? (
            <p className="db-hint">Consulte a versão publicada acima para ver o progresso.</p>
          ) : null}

          {rollout ? (
            <div aria-label="Progresso da liberação">
              <ProgressMeter
                label="Instaladas"
                value={rollout.updated}
                max={Math.max(rollout.total, 1)}
                display={`${rollout.updated} de ${rollout.total}`}
                tone={rollout.failed > 0 ? 'warning' : 'success'}
                caption={
                  rollout.total === 0
                    ? 'Nenhuma instalação elegível ainda para esta versão.'
                    : undefined
                }
              />

              <div className="db-form-row nx-stagger">
                <StatTile label="Total elegível" value={rollout.total} icon="dns" />
                <StatTile label="Atualizadas" value={rollout.updated} icon="check_circle" />
                <StatTile
                  label="Falhas"
                  value={rollout.failed}
                  icon="error"
                  deltaDirection={rollout.failed > 0 ? 'down' : 'flat'}
                />
                <StatTile label="Pendentes" value={rollout.pending} icon="hourglass_empty" />
              </div>

              {rollout.failed > 0 ? (
                <AlertBanner tone="warning" title="Rollbacks nesta versão">
                  {rollout.failed} instalação{rollout.failed === 1 ? '' : 'ões'} reverteu para a
                  versão anterior automaticamente após falha de health check.
                </AlertBanner>
              ) : null}
            </div>
          ) : null}
        </Card>
      </div>
    </main>
  );
}
