import { useEffect, useId, useState } from 'react';
import { Badge, Button, Card, DataTable, Field, Input, Select, Switch } from '@nexora/ui';
import type { CreateStationRequest, StationDto, UpdateStationRequest } from '@nexora/contracts';
import { STATION_COLOR_OPTIONS, stationColorCssValue } from './stations-api.js';
import './stations.css';

export interface StationManagementPageProps {
  readonly stations: readonly StationDto[];
  readonly onCreate: (input: CreateStationRequest) => Promise<StationDto>;
  readonly onUpdate: (id: string, input: UpdateStationRequest) => Promise<StationDto>;
  readonly onDelete: (id: string) => Promise<void>;
}

/**
 * Cadastro de praças de produção (US-017) — cor consistente com KDS/produto/relatórios, gargalo
 * único (só uma praça marcada por vez) e bloqueio de exclusão quando há produto vinculado.
 */
export function StationManagementPage({
  stations,
  onCreate,
  onUpdate,
  onDelete,
}: Readonly<StationManagementPageProps>) {
  const createCodeFieldId = useId();
  const createNameFieldId = useId();
  const createColorFieldId = useId();
  const createCapacityFieldId = useId();
  const createPositionFieldId = useId();
  const editNameFieldId = useId();
  const editColorFieldId = useId();
  const editCapacityFieldId = useId();
  const editPositionFieldId = useId();

  const [selectedId, setSelectedId] = useState(stations[0]?.id);
  const selected = stations.find((station) => station.id === selectedId) ?? stations[0];

  const [name, setName] = useState(selected?.name ?? '');
  const [color, setColor] = useState(selected?.color ?? STATION_COLOR_OPTIONS[0].value);
  const [capacitySlots, setCapacitySlots] = useState(selected?.capacitySlots?.toString() ?? '');
  const [position, setPosition] = useState(selected?.position?.toString() ?? '0');
  const [isBottleneck, setIsBottleneck] = useState(selected?.isBottleneck ?? false);

  const [creating, setCreating] = useState(false);
  const [createCode, setCreateCode] = useState('');
  const [createName, setCreateName] = useState('');
  const [createColor, setCreateColor] = useState<string>(STATION_COLOR_OPTIONS[0].value);
  const [createCapacity, setCreateCapacity] = useState('');
  const [createPosition, setCreatePosition] = useState(String(stations.length + 1));
  const [createBottleneck, setCreateBottleneck] = useState(false);

  const [confirmingDeleteId, setConfirmingDeleteId] = useState<string>();
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (!selected) return;
    setName(selected.name);
    setColor(selected.color ?? STATION_COLOR_OPTIONS[0].value);
    setCapacitySlots(selected.capacitySlots?.toString() ?? '');
    setPosition(selected.position.toString());
    setIsBottleneck(selected.isBottleneck);
  }, [
    selected?.id,
    selected?.name,
    selected?.color,
    selected?.capacitySlots,
    selected?.position,
    selected?.isBottleneck,
  ]);

  const currentBottleneck = stations.find((station) => station.isBottleneck);

  async function createStation() {
    if (!createCode.trim() || !createName.trim()) return;
    setBusy(true);
    setError(undefined);
    try {
      const created = await onCreate({
        code: createCode.trim().toUpperCase(),
        name: createName.trim(),
        color: createColor,
        capacitySlots: createCapacity.trim() ? Number(createCapacity) : undefined,
        isBottleneck: createBottleneck,
        position: createPosition.trim() ? Number(createPosition) : 0,
      });
      setSelectedId(created.id);
      setCreating(false);
      setCreateCode('');
      setCreateName('');
      setCreateColor(STATION_COLOR_OPTIONS[0].value);
      setCreateCapacity('');
      setCreateBottleneck(false);
      setNotice(
        created.isBottleneck
          ? `Praça criada. ${currentBottleneck ? `${currentBottleneck.name} deixou de ser o gargalo — só uma praça pode ser o gargalo por vez.` : ''}`
          : 'Praça criada.',
      );
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function save() {
    if (!selected) return;
    setBusy(true);
    setError(undefined);
    try {
      const updated = await onUpdate(selected.id, {
        name: name.trim(),
        color,
        capacitySlots: capacitySlots.trim() ? Number(capacitySlots) : undefined,
        isBottleneck,
        position: position.trim() ? Number(position) : 0,
      });
      setNotice(
        updated.isBottleneck && currentBottleneck && currentBottleneck.id !== updated.id
          ? `Alterações salvas. ${currentBottleneck.name} deixou de ser o gargalo — só uma praça pode ser o gargalo por vez.`
          : 'Alterações salvas.',
      );
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function confirmDelete(id: string) {
    setBusy(true);
    setError(undefined);
    try {
      await onDelete(id);
      setConfirmingDeleteId(undefined);
      setNotice('Praça excluída.');
      if (selectedId === id) setSelectedId(undefined);
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="stations-shell" aria-labelledby="stations-title">
      <header className="stations-header">
        <div>
          <p className="stations-eyebrow">COZINHA</p>
          <h1 id="stations-title">Praças de produção</h1>
          <p className="stations-lead">
            Organize forno, montagem, bebidas e demais praças da cozinha. A praça marcada como
            gargalo determina o ritmo real da produção.
          </p>
        </div>
        <Button type="button" onClick={() => setCreating(true)}>
          Nova praça
        </Button>
      </header>

      {notice ? (
        <p className="stations-notice" role="status">
          {notice}
        </p>
      ) : null}
      {error ? (
        <p className="stations-error" role="alert">
          {error}
        </p>
      ) : null}

      <div className="stations-workbench">
        <Card padding="none" className="stations-table-card">
          <DataTable
            rowKey="id"
            rows={stations}
            onRowClick={(row) => setSelectedId(row.id)}
            columns={[
              {
                key: 'color',
                header: '',
                width: '2.5rem',
                render: (row) => (
                  <span
                    className="station-swatch"
                    style={{ background: stationColorCssValue(row.color) }}
                    aria-hidden="true"
                  />
                ),
              },
              { key: 'code', header: 'Código', render: (row) => <code>{row.code}</code> },
              { key: 'name', header: 'Nome' },
              {
                key: 'capacitySlots',
                header: 'Capacidade',
                align: 'right',
                render: (row) => (row.capacitySlots ? `${row.capacitySlots} slots` : '—'),
              },
              {
                key: 'isBottleneck',
                header: 'Gargalo',
                render: (row) => (row.isBottleneck ? <Badge tone="warning">Gargalo</Badge> : null),
              },
              {
                key: 'linkedProductCount',
                header: 'Produtos',
                align: 'right',
                render: (row) => row.linkedProductCount,
              },
            ]}
          />
        </Card>

        {selected ? (
          <Card
            className="station-editor"
            title={selected.name}
            subtitle={`Código ${selected.code}`}
          >
            <Field label="Nome da praça" htmlFor={editNameFieldId}>
              <Input
                id={editNameFieldId}
                value={name}
                onChange={(event) => setName(event.target.value)}
              />
            </Field>

            <Field label="Cor de identificação" htmlFor={editColorFieldId}>
              <Select
                id={editColorFieldId}
                value={color}
                onChange={(event) => setColor(event.target.value)}
                options={STATION_COLOR_OPTIONS.map((option) => ({
                  value: option.value,
                  label: option.label,
                }))}
              />
            </Field>

            <div className="station-editor__row">
              <Field
                label="Capacidade (slots)"
                htmlFor={editCapacityFieldId}
                hint="Posições simultâneas do recurso — em branco significa sem limite"
              >
                <Input
                  id={editCapacityFieldId}
                  type="number"
                  min={1}
                  numeric
                  value={capacitySlots}
                  onChange={(event) => setCapacitySlots(event.target.value)}
                />
              </Field>
              <Field label="Posição no KDS" htmlFor={editPositionFieldId}>
                <Input
                  id={editPositionFieldId}
                  type="number"
                  min={0}
                  numeric
                  value={position}
                  onChange={(event) => setPosition(event.target.value)}
                />
              </Field>
            </div>

            <Switch
              label="Marcar como gargalo"
              aria-label="Marcar como gargalo"
              description="O gargalo é, por definição, um só — marcar esta praça desmarca a atual."
              checked={isBottleneck}
              onChange={(event) => setIsBottleneck(event.target.checked)}
            />
            {isBottleneck && currentBottleneck && currentBottleneck.id !== selected.id ? (
              <p className="station-bottleneck-warning">
                {currentBottleneck.name} deixará de ser o gargalo.
              </p>
            ) : null}

            <div className="station-editor__footer">
              {selected.linkedProductCount > 0 ? (
                <span className="station-linked-warning">
                  {selected.linkedProductCount} produto(s) vinculado(s) — reatribua antes de
                  excluir.
                </span>
              ) : confirmingDeleteId === selected.id ? (
                <span className="station-delete-confirm">
                  Excluir esta praça?
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    onClick={() => setConfirmingDeleteId(undefined)}
                  >
                    Cancelar
                  </Button>
                  <Button
                    type="button"
                    variant="danger"
                    size="sm"
                    busy={busy}
                    onClick={() => void confirmDelete(selected.id)}
                  >
                    Confirmar exclusão
                  </Button>
                </span>
              ) : (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => setConfirmingDeleteId(selected.id)}
                >
                  Excluir praça
                </Button>
              )}
              <Button type="button" busy={busy} onClick={() => void save()}>
                Salvar praça
              </Button>
            </div>
          </Card>
        ) : (
          <Card className="station-editor station-editor--empty">Nenhuma praça cadastrada.</Card>
        )}
      </div>

      {creating ? (
        <div className="stations-dialog-backdrop">
          <section
            className="stations-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-station-title"
          >
            <p className="stations-eyebrow">NOVA PRAÇA</p>
            <h2 id="create-station-title">Criar praça</h2>
            <div className="stations-dialog__fields">
              <Field label="Nome" htmlFor={createNameFieldId}>
                <Input
                  id={createNameFieldId}
                  value={createName}
                  onChange={(event) => setCreateName(event.target.value)}
                />
              </Field>
              <Field
                label="Código"
                htmlFor={createCodeFieldId}
                hint="Letras maiúsculas, números e sublinhado"
              >
                <Input
                  id={createCodeFieldId}
                  value={createCode}
                  onChange={(event) => setCreateCode(event.target.value.toUpperCase())}
                />
              </Field>
              <Field label="Cor de identificação" htmlFor={createColorFieldId}>
                <Select
                  id={createColorFieldId}
                  value={createColor}
                  onChange={(event) => setCreateColor(event.target.value)}
                  options={STATION_COLOR_OPTIONS.map((option) => ({
                    value: option.value,
                    label: option.label,
                  }))}
                />
              </Field>
              <div className="station-editor__row">
                <Field label="Capacidade (slots)" htmlFor={createCapacityFieldId}>
                  <Input
                    id={createCapacityFieldId}
                    type="number"
                    min={1}
                    numeric
                    value={createCapacity}
                    onChange={(event) => setCreateCapacity(event.target.value)}
                  />
                </Field>
                <Field label="Posição no KDS" htmlFor={createPositionFieldId}>
                  <Input
                    id={createPositionFieldId}
                    type="number"
                    min={0}
                    numeric
                    value={createPosition}
                    onChange={(event) => setCreatePosition(event.target.value)}
                  />
                </Field>
              </div>
              <Switch
                label="Marcar como gargalo"
                aria-label="Marcar nova praça como gargalo"
                description="Só uma praça pode ser o gargalo por vez."
                checked={createBottleneck}
                onChange={(event) => setCreateBottleneck(event.target.checked)}
              />
            </div>
            <div className="stations-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createStation()}>
                Criar praça
              </Button>
            </div>
          </section>
        </div>
      ) : null}
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
