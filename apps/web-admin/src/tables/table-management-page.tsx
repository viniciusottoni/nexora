import { useId, useMemo, useState } from 'react';
import { Button, DataTable, EmptyState, Field, Input, Select, StatusPill } from '@nexora/ui';
import type { AreaDto, TableDto } from '@nexora/contracts';
import './tables.css';

export interface TableManagementPageProps {
  readonly areas: readonly AreaDto[];
  readonly tables: readonly TableDto[];
  readonly onCreateArea: (name: string) => Promise<void>;
  readonly onDeactivateArea: (id: string) => Promise<void>;
  readonly onActivateArea: (id: string) => Promise<void>;
  readonly onDeleteArea: (id: string) => Promise<void>;
  readonly onCreateTable: (input: { areaId: string; label: string; seats: number }) => Promise<void>;
  readonly onCreateTablesBulk: (input: {
    areaId: string;
    from: number;
    to: number;
    seats: number;
  }) => Promise<void>;
  readonly onRotateToken: (tableId: string) => Promise<void>;
  readonly onDeactivateTable: (tableId: string) => Promise<void>;
  readonly onActivateTable: (tableId: string) => Promise<void>;
  readonly onDeleteTable: (tableId: string) => Promise<void>;
  readonly onExportQrCodesPdf: (areaId?: string) => Promise<void>;
}

/**
 * Tela de gestão de ambientes e mesas do salão (US-020). Fluxo principal: cadastrar ambiente ->
 * criar mesas em lote -> exportar QR Codes em PDF para impressão. Rotação de token exige
 * confirmação explícita porque invalida imediatamente o QR Code já colado na mesa (US-020 §10).
 */
export function TableManagementPage({
  areas,
  tables,
  onCreateArea,
  onDeactivateArea,
  onActivateArea,
  onDeleteArea,
  onCreateTable,
  onCreateTablesBulk,
  onRotateToken,
  onDeactivateTable,
  onActivateTable,
  onDeleteTable,
  onExportQrCodesPdf,
}: Readonly<TableManagementPageProps>) {
  const areaNameFieldId = useId();
  const tableAreaFieldId = useId();
  const tableLabelFieldId = useId();
  const tableSeatsFieldId = useId();
  const bulkAreaFieldId = useId();
  const bulkFromFieldId = useId();
  const bulkToFieldId = useId();
  const bulkSeatsFieldId = useId();

  const [selectedAreaId, setSelectedAreaId] = useState<string>();
  const [creatingArea, setCreatingArea] = useState(false);
  const [newAreaName, setNewAreaName] = useState('');
  const [creatingTable, setCreatingTable] = useState(false);
  const [targetAreaId, setTargetAreaId] = useState<string>();
  const [newTableLabel, setNewTableLabel] = useState('');
  const [newTableSeats, setNewTableSeats] = useState(4);
  const [bulkCreating, setBulkCreating] = useState(false);
  const [bulkAreaId, setBulkAreaId] = useState<string>();
  const [bulkFrom, setBulkFrom] = useState(1);
  const [bulkTo, setBulkTo] = useState(20);
  const [bulkSeats, setBulkSeats] = useState(4);
  const [rotating, setRotating] = useState<TableDto>();
  const [deletingTable, setDeletingTable] = useState<TableDto>();
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();

  const activeAreas = useMemo(() => areas.filter((area) => area.active), [areas]);
  const visibleTables = useMemo(
    () => (selectedAreaId ? tables.filter((table) => table.areaId === selectedAreaId) : tables),
    [tables, selectedAreaId],
  );

  async function guarded(action: () => Promise<void>) {
    setBusy(true);
    setError(undefined);
    try {
      await action();
    } catch (reason) {
      setError(reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.');
    } finally {
      setBusy(false);
    }
  }

  async function createArea() {
    if (!newAreaName.trim()) return;
    await guarded(async () => {
      await onCreateArea(newAreaName.trim());
      setCreatingArea(false);
      setNewAreaName('');
    });
  }

  function openCreateTable() {
    setTargetAreaId(selectedAreaId ?? activeAreas[0]?.id);
    setCreatingTable(true);
  }

  function openBulkCreate() {
    setBulkAreaId(selectedAreaId ?? activeAreas[0]?.id);
    setBulkCreating(true);
  }

  async function createTable() {
    const areaId = targetAreaId;
    if (!areaId || !newTableLabel.trim()) return;
    await guarded(async () => {
      await onCreateTable({ areaId, label: newTableLabel.trim(), seats: newTableSeats });
      setCreatingTable(false);
      setNewTableLabel('');
    });
  }

  async function createBulk() {
    const areaId = bulkAreaId;
    if (!areaId) return;
    await guarded(async () => {
      await onCreateTablesBulk({ areaId, from: bulkFrom, to: bulkTo, seats: bulkSeats });
      setBulkCreating(false);
      setNotice(`Mesas ${bulkFrom} a ${bulkTo} criadas com sucesso.`);
    });
  }

  async function confirmRotate() {
    if (!rotating) return;
    await guarded(async () => {
      await onRotateToken(rotating.id);
      setNotice(
        `O QR Code anterior da mesa ${rotating.label} parou de funcionar. Exporte o PDF novamente e substitua o código impresso.`,
      );
      setRotating(undefined);
    });
  }

  async function confirmDelete() {
    if (!deletingTable) return;
    await guarded(async () => {
      await onDeleteTable(deletingTable.id);
      setDeletingTable(undefined);
    });
  }

  function renderAreaToolbarActions() {
    if (!selectedAreaId) return null;
    const selectedArea = areas.find((area) => area.id === selectedAreaId);
    if (!selectedArea) return null;

    if (selectedArea.active) {
      return (
        <Button type="button" variant="ghost" onClick={() => void guarded(() => onDeactivateArea(selectedAreaId))}>
          Desativar ambiente
        </Button>
      );
    }

    return (
      <>
        <Button type="button" variant="ghost" onClick={() => void guarded(() => onActivateArea(selectedAreaId))}>
          Reativar ambiente
        </Button>
        <Button type="button" variant="danger" onClick={() => void guarded(() => onDeleteArea(selectedAreaId))}>
          Excluir ambiente
        </Button>
      </>
    );
  }

  return (
    <main className="tables-shell" aria-labelledby="tables-title">
      <header className="tables-header">
        <div>
          <p className="tables-eyebrow">SALÃO</p>
          <h1 id="tables-title">Ambientes e mesas</h1>
          <p className="tables-lead">
            Cadastre os ambientes do salão, crie mesas em lote e exporte os QR Codes para impressão.
          </p>
        </div>
        <div className="tables-header__actions">
          <Button type="button" variant="ghost" onClick={() => setCreatingArea(true)}>
            Novo ambiente
          </Button>
          <Button
            type="button"
            variant="secondary"
            busy={busy}
            disabled={tables.length === 0}
            onClick={() => void guarded(() => onExportQrCodesPdf(selectedAreaId))}
          >
            Exportar QR Codes
          </Button>
        </div>
      </header>

      {notice ? (
        <p className="tables-notice" role="status">
          {notice}
        </p>
      ) : null}
      {error ? (
        <p className="tables-error" role="alert">
          {error}
        </p>
      ) : null}

      <div className="tables-workbench">
        <nav className="area-list" aria-label="Ambientes cadastrados">
          <button
            type="button"
            className={`area-list__item ${selectedAreaId === undefined ? 'area-list__item--active' : ''}`}
            onClick={() => setSelectedAreaId(undefined)}
          >
            <strong>Todos os ambientes</strong>
            <span className="area-list__count">{tables.length}</span>
          </button>
          {areas.map((area) => (
            <button
              type="button"
              key={area.id}
              className={`area-list__item ${area.id === selectedAreaId ? 'area-list__item--active' : ''} ${area.active ? '' : 'area-list__item--inactive'}`}
              onClick={() => setSelectedAreaId(area.id)}
            >
              <span>
                <strong>{area.name}</strong>
                {area.active ? null : <small>Desativado</small>}
              </span>
              <span className="area-list__count">{area.tableCount}</span>
            </button>
          ))}
        </nav>

        <section aria-label="Mesas do ambiente selecionado">
          <div className="tables-toolbar">
            <Button type="button" onClick={openCreateTable} disabled={activeAreas.length === 0}>
              Nova mesa
            </Button>
            <Button type="button" variant="accent" onClick={openBulkCreate} disabled={activeAreas.length === 0}>
              Criar mesas em lote
            </Button>
            {renderAreaToolbarActions()}
          </div>

          {visibleTables.length === 0 ? (
            <EmptyState icon="table_restaurant" title="Nenhuma mesa cadastrada">
              Cadastre mesas uma a uma ou crie um lote inteiro (ex.: mesas 1 a 20) de uma vez.
            </EmptyState>
          ) : (
            <DataTable<TableDto>
              rowKey="id"
              columns={[
                { key: 'label', header: 'Mesa', render: (row) => <strong>{row.label}</strong> },
                { key: 'areaName', header: 'Ambiente' },
                { key: 'seats', header: 'Assentos', numeric: true },
                {
                  key: 'status',
                  header: 'Status',
                  render: (row) => <StatusPill status={row.status} />,
                },
                {
                  key: 'active',
                  header: 'Ativa',
                  render: (row) => (row.active ? 'Sim' : 'Não'),
                },
                {
                  key: 'actions',
                  header: 'Ações',
                  render: (row) => (
                    <div className="table-row-actions">
                      <Button type="button" variant="ghost" size="sm" onClick={() => setRotating(row)}>
                        Rotacionar token
                      </Button>
                      {row.active ? (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => void guarded(() => onDeactivateTable(row.id))}
                        >
                          Desativar
                        </Button>
                      ) : (
                        <Button
                          type="button"
                          variant="ghost"
                          size="sm"
                          onClick={() => void guarded(() => onActivateTable(row.id))}
                        >
                          Ativar
                        </Button>
                      )}
                      <Button type="button" variant="danger" size="sm" onClick={() => setDeletingTable(row)}>
                        Excluir
                      </Button>
                    </div>
                  ),
                },
              ]}
              rows={visibleTables}
            />
          )}
        </section>
      </div>

      {creatingArea ? (
        <div className="tables-dialog-backdrop">
          <section className="tables-dialog" role="dialog" aria-modal="true" aria-labelledby="create-area-title">
            <h2 id="create-area-title">Novo ambiente</h2>
            <Field label="Nome do ambiente" htmlFor={areaNameFieldId} hint="Ex.: Salão, Varanda, Mezanino">
              <Input id={areaNameFieldId} value={newAreaName} onChange={(event) => setNewAreaName(event.target.value)} />
            </Field>
            <div className="tables-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setCreatingArea(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createArea()}>
                Criar ambiente
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {creatingTable ? (
        <div className="tables-dialog-backdrop">
          <section className="tables-dialog" role="dialog" aria-modal="true" aria-labelledby="create-table-title">
            <h2 id="create-table-title">Nova mesa</h2>
            <Field label="Ambiente" htmlFor={tableAreaFieldId}>
              <Select
                id={tableAreaFieldId}
                value={targetAreaId ?? ''}
                onChange={(event) => setTargetAreaId(event.target.value)}
                options={activeAreas.map((area) => ({ value: area.id, label: area.name }))}
              />
            </Field>
            <Field label="Rótulo da mesa" htmlFor={tableLabelFieldId} hint='Ex.: "12" ou "V3"'>
              <Input
                id={tableLabelFieldId}
                value={newTableLabel}
                onChange={(event) => setNewTableLabel(event.target.value)}
              />
            </Field>
            <Field label="Assentos" htmlFor={tableSeatsFieldId}>
              <Input
                id={tableSeatsFieldId}
                type="number"
                min={1}
                value={newTableSeats}
                onChange={(event) => setNewTableSeats(Number(event.target.value))}
              />
            </Field>
            <div className="tables-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setCreatingTable(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createTable()}>
                Criar mesa
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {bulkCreating ? (
        <div className="tables-dialog-backdrop">
          <section className="tables-dialog" role="dialog" aria-modal="true" aria-labelledby="bulk-create-title">
            <p className="tables-eyebrow">CRIAÇÃO EM LOTE</p>
            <h2 id="bulk-create-title">Criar mesas em lote</h2>
            <p>Cadastre um intervalo inteiro de mesas de uma vez — ideal para o onboarding do salão.</p>
            <div className="tables-dialog__fields">
              <Field label="Ambiente" htmlFor={bulkAreaFieldId}>
                <Select
                  id={bulkAreaFieldId}
                  value={bulkAreaId ?? ''}
                  onChange={(event) => setBulkAreaId(event.target.value)}
                  options={activeAreas.map((area) => ({ value: area.id, label: area.name }))}
                />
              </Field>
              <Field label="De" htmlFor={bulkFromFieldId}>
                <Input
                  id={bulkFromFieldId}
                  type="number"
                  min={1}
                  value={bulkFrom}
                  onChange={(event) => setBulkFrom(Number(event.target.value))}
                />
              </Field>
              <Field label="Até" htmlFor={bulkToFieldId}>
                <Input
                  id={bulkToFieldId}
                  type="number"
                  min={bulkFrom}
                  value={bulkTo}
                  onChange={(event) => setBulkTo(Number(event.target.value))}
                />
              </Field>
              <Field label="Assentos por mesa" htmlFor={bulkSeatsFieldId}>
                <Input
                  id={bulkSeatsFieldId}
                  type="number"
                  min={1}
                  value={bulkSeats}
                  onChange={(event) => setBulkSeats(Number(event.target.value))}
                />
              </Field>
            </div>
            <div className="tables-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setBulkCreating(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createBulk()}>
                Criar mesas {bulkFrom} a {bulkTo}
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {rotating ? (
        <div className="tables-dialog-backdrop">
          <section className="tables-dialog tables-dialog--danger" role="dialog" aria-modal="true" aria-labelledby="rotate-title">
            <p className="tables-eyebrow">AÇÃO IMEDIATA</p>
            <h2 id="rotate-title">Rotacionar token da mesa {rotating.label}?</h2>
            <p>
              O QR Code impresso hoje deixará de funcionar imediatamente. Você precisará exportar o PDF novamente e
              substituir o código colado na mesa.
            </p>
            <div className="tables-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setRotating(undefined)}>
                Cancelar
              </Button>
              <Button type="button" variant="danger" busy={busy} onClick={() => void confirmRotate()}>
                Sim, rotacionar token
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {deletingTable ? (
        <div className="tables-dialog-backdrop">
          <section className="tables-dialog tables-dialog--danger" role="dialog" aria-modal="true" aria-labelledby="delete-title">
            <p className="tables-eyebrow">AÇÃO IMEDIATA</p>
            <h2 id="delete-title">Excluir mesa {deletingTable.label}?</h2>
            <p>
              Se esta mesa já tiver sessões no histórico, a exclusão será recusada — desative-a em vez de excluir
              para manter o histórico.
            </p>
            <div className="tables-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setDeletingTable(undefined)}>
                Cancelar
              </Button>
              <Button type="button" variant="danger" busy={busy} onClick={() => void confirmDelete()}>
                Sim, excluir mesa
              </Button>
            </div>
          </section>
        </div>
      ) : null}
    </main>
  );
}
