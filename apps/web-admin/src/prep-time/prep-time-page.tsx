import { useId, useState } from 'react';
import { Badge, Button, Card, Field, Input, Select } from '@nexora/ui';
import type { PrepTimeAnalysisResponse, StationDto } from '@nexora/contracts';
import { stationColorCssValue } from '../stations/stations-api.js';
import './prep-time.css';

/** Uma linha da lista — uma variação de produto, com o produto que a contém. */
export interface PrepTimeVariantRow {
  readonly variantId: string;
  readonly variantName: string;
  readonly productId: string;
  readonly productName: string;
  readonly prepMinutes: number;
  readonly warnMinutes: number | null;
  readonly criticalMinutes: number | null;
  readonly stationId: string | null;
  readonly stationCode: string | null;
  readonly stationName: string | null;
}

export interface UpdatePrepTimeInput {
  readonly prepMinutes: number;
  readonly warnMinutes: number | null;
  readonly criticalMinutes: number | null;
}

export interface PrepTimePageProps {
  readonly variants: readonly PrepTimeVariantRow[];
  readonly stations: readonly StationDto[];
  readonly onUpdatePrepTime: (variantId: string, input: UpdatePrepTimeInput) => Promise<void>;
  readonly onReassignStation: (productId: string, stationId: string | null) => Promise<void>;
  readonly onLoadAnalysis: (variantId: string) => Promise<PrepTimeAnalysisResponse>;
}

const NO_STATION = '__none__';

export function PrepTimePage({
  variants,
  stations,
  onUpdatePrepTime,
  onReassignStation,
  onLoadAnalysis,
}: Readonly<PrepTimePageProps>) {
  return (
    <main className="prep-time-shell" aria-labelledby="prep-time-title">
      <header className="prep-time-header">
        <div>
          <p className="prep-time-eyebrow">CARDÁPIO · TEMPO E PRAÇA</p>
          <h1 id="prep-time-title">Tempo de preparo e praça por produto</h1>
          <p className="prep-time-lead">
            Defina quanto tempo cada variação leva e em qual praça é feita. O prazo dinâmico e o
            roteamento da fila do KDS usam essa informação.
          </p>
        </div>
      </header>

      {variants.length === 0 ? (
        <Card className="prep-time-empty">Nenhuma variação cadastrada.</Card>
      ) : (
        <ul className="prep-time-list nx-stagger" aria-label="Variações de produto">
          {variants.map((row) => (
            <PrepTimeRow
              key={row.variantId}
              row={row}
              stations={stations}
              onUpdatePrepTime={onUpdatePrepTime}
              onReassignStation={onReassignStation}
              onLoadAnalysis={onLoadAnalysis}
            />
          ))}
        </ul>
      )}
    </main>
  );
}

function PrepTimeRow({
  row,
  stations,
  onUpdatePrepTime,
  onReassignStation,
  onLoadAnalysis,
}: Readonly<{
  row: PrepTimeVariantRow;
  stations: readonly StationDto[];
  onUpdatePrepTime: PrepTimePageProps['onUpdatePrepTime'];
  onReassignStation: PrepTimePageProps['onReassignStation'];
  onLoadAnalysis: PrepTimePageProps['onLoadAnalysis'];
}>) {
  const prepFieldId = useId();
  const warnFieldId = useId();
  const criticalFieldId = useId();
  const stationFieldId = useId();

  const [prepMinutes, setPrepMinutes] = useState(String(row.prepMinutes));
  const [warnMinutes, setWarnMinutes] = useState(
    row.warnMinutes === null ? '' : String(row.warnMinutes),
  );
  const [criticalMinutes, setCriticalMinutes] = useState(
    row.criticalMinutes === null ? '' : String(row.criticalMinutes),
  );
  const [stationId, setStationId] = useState(row.stationId ?? NO_STATION);
  const [savingTime, setSavingTime] = useState(false);
  const [savingStation, setSavingStation] = useState(false);
  const [error, setError] = useState<string>();
  const [analysis, setAnalysis] = useState<PrepTimeAnalysisResponse>();
  const [loadingAnalysis, setLoadingAnalysis] = useState(false);

  const currentStation = stations.find((station) => station.id === stationId);

  async function saveTime() {
    setError(undefined);
    const parsedPrepMinutes = Number(prepMinutes);
    const parsedWarnMinutes = warnMinutes.trim() === '' ? null : Number(warnMinutes);
    const parsedCriticalMinutes = criticalMinutes.trim() === '' ? null : Number(criticalMinutes);

    if (!Number.isInteger(parsedPrepMinutes) || parsedPrepMinutes < 0) {
      setError('O tempo de preparo deve ser um número inteiro não negativo.');
      return;
    }
    if (
      parsedWarnMinutes !== null &&
      (!Number.isInteger(parsedWarnMinutes) || parsedWarnMinutes < parsedPrepMinutes)
    ) {
      setError('O limiar de atenção não pode ser menor que o tempo de preparo.');
      return;
    }
    const criticalFloor = parsedWarnMinutes ?? parsedPrepMinutes;
    if (
      parsedCriticalMinutes !== null &&
      (!Number.isInteger(parsedCriticalMinutes) || parsedCriticalMinutes < criticalFloor)
    ) {
      setError('O limiar crítico não pode ser menor que o limiar de atenção.');
      return;
    }

    setSavingTime(true);
    try {
      await onUpdatePrepTime(row.variantId, {
        prepMinutes: parsedPrepMinutes,
        warnMinutes: parsedWarnMinutes,
        criticalMinutes: parsedCriticalMinutes,
      });
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setSavingTime(false);
    }
  }

  async function saveStation(nextStationId: string) {
    setStationId(nextStationId);
    setError(undefined);
    setSavingStation(true);
    try {
      await onReassignStation(row.productId, nextStationId === NO_STATION ? null : nextStationId);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setSavingStation(false);
    }
  }

  async function loadAnalysis() {
    if (analysis) {
      setAnalysis(undefined);
      return;
    }
    setLoadingAnalysis(true);
    try {
      setAnalysis(await onLoadAnalysis(row.variantId));
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setLoadingAnalysis(false);
    }
  }

  return (
    <li className="prep-time-row">
      <Card className="prep-time-card">
        <div className="prep-time-card__heading">
          <div>
            <strong>{row.productName}</strong>
            <span className="prep-time-variant-name">{row.variantName}</span>
          </div>
          {currentStation ? (
            <span className="prep-time-station-tag">
              <span
                className="prep-time-station-tag__swatch"
                style={{ background: stationColorCssValue(currentStation.color) }}
                aria-hidden="true"
              />
              {currentStation.name}
            </span>
          ) : (
            <Badge tone="neutral" square>
              Sem praça
            </Badge>
          )}
        </div>

        <div className="prep-time-card__fields">
          <Field label="Preparo (min)" htmlFor={prepFieldId}>
            <Input
              id={prepFieldId}
              type="number"
              min={0}
              numeric
              value={prepMinutes}
              onChange={(event) => setPrepMinutes(event.target.value)}
            />
          </Field>
          <Field label="Atenção (min)" htmlFor={warnFieldId} hint="Vazio herda o padrão do tenant">
            <Input
              id={warnFieldId}
              type="number"
              min={0}
              numeric
              placeholder="Herdado"
              value={warnMinutes}
              onChange={(event) => setWarnMinutes(event.target.value)}
            />
          </Field>
          <Field
            label="Crítico (min)"
            htmlFor={criticalFieldId}
            hint="Vazio herda o padrão do tenant"
          >
            <Input
              id={criticalFieldId}
              type="number"
              min={0}
              numeric
              placeholder="Herdado"
              value={criticalMinutes}
              onChange={(event) => setCriticalMinutes(event.target.value)}
            />
          </Field>
          <Field label="Praça de produção" htmlFor={stationFieldId}>
            <Select
              id={stationFieldId}
              value={stationId}
              onChange={(event) => void saveStation(event.target.value)}
              disabled={savingStation}
            >
              <option value={NO_STATION}>Sem praça</option>
              {stations.map((station) => (
                <option key={station.id} value={station.id}>
                  {station.name}
                </option>
              ))}
            </Select>
          </Field>
        </div>

        <div className="prep-time-card__footer">
          <Button
            type="button"
            variant="ghost"
            onClick={() => void loadAnalysis()}
            busy={loadingAnalysis}
          >
            {analysis ? 'Ocultar comparativo' : 'Ver comparativo estimado x real'}
          </Button>
          <Button type="button" busy={savingTime} onClick={() => void saveTime()}>
            Salvar tempo de preparo
          </Button>
        </div>

        {error ? (
          <p className="prep-time-error" role="alert">
            {error}
          </p>
        ) : null}

        {analysis ? <PrepTimeAnalysisPanel analysis={analysis} /> : null}
      </Card>
    </li>
  );
}

function PrepTimeAnalysisPanel({ analysis }: Readonly<{ analysis: PrepTimeAnalysisResponse }>) {
  return (
    <div className="prep-time-analysis" role="status">
      <div className="prep-time-analysis__row">
        <span>Cadastrado</span>
        <strong>{analysis.configuredMinutes} min</strong>
      </div>
      <div className="prep-time-analysis__row">
        <span>Real médio (30 dias)</span>
        <strong>
          {analysis.actualAvgMinutes === null ? '—' : `${analysis.actualAvgMinutes.toFixed(1)} min`}
        </strong>
      </div>
      <div className="prep-time-analysis__row">
        <span>Amostra</span>
        <strong>{analysis.sampleSize} pedido(s)</strong>
      </div>
      <div className="prep-time-analysis__row">
        <span>Limiares efetivos</span>
        <strong>
          {analysis.effectiveWarnMinutes} / {analysis.effectiveCriticalMinutes} min
          {analysis.warnMinutesInherited || analysis.criticalMinutesInherited
            ? ' (herdado do tenant)'
            : ''}
        </strong>
      </div>
      {analysis.suggestion !== null ? (
        <p className="prep-time-analysis__suggestion">
          Divergência relevante — considere ajustar para {analysis.suggestion} min.
        </p>
      ) : analysis.note ? (
        <p className="prep-time-analysis__note">{analysis.note}</p>
      ) : null}
    </div>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
