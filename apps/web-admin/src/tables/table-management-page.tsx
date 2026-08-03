import { useId, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  DataTable,
  EmptyState,
  Field,
  Input,
  Modal,
  Select,
  StatusPill,
} from '@nexora/ui';
import type { AreaDto, TableDto } from '@nexora/contracts';
import './tables.css';

export interface TableManagementPageProps {
  readonly areas: readonly AreaDto[];
  readonly tables: readonly TableDto[];
  readonly onCreateArea: (name: string) => Promise<void>;
  readonly onDeactivateArea: (id: string) => Promise<void>;
  readonly onActivateArea: (id: string) => Promise<void>;
  readonly onDeleteArea: (id: string) => Promise<void>;
  readonly onCreateTable: (input: {
    areaId: string;
    label: string;
    seats: number;
  }) => Promise<void>;
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
  const selectedArea = areas.find((area) => area.id === selectedAreaId);
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
    if (!selectedAreaId || !selectedArea) return null;

    if (selectedArea.active) {
      return (
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => void guarded(() => onDeactivateArea(selectedAreaId))}
        >
          Desativar ambiente
        </Button>
      );
    }

    return (
      <>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          onClick={() => void guarded(() => onActivateArea(selectedAreaId))}
        >
          Reativar ambiente
        </Button>
        <Button
          type="button"
          variant="danger"
          size="sm"
          onClick={() => void guarded(() => onDeleteArea(selectedAreaId))}
        >
          Excluir ambiente
        </Button>
      </>
    );
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="tables-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Salão</p>
          <h1 className="db-page__title" id="tables-title">
            Ambientes e mesas
          </h1>
          <p className="db-page__lead">
            Cadastre os ambientes do salão, crie mesas em lote e exporte os QR Codes para impressão.
          </p>
        </div>
        <div className="db-page__actions">
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
        <AlertBanner tone="success" title="Salão atualizado">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      <div className="db-workbench">
        <nav className="db-list nx-stagger" aria-label="Ambientes cadastrados">
          <button
            type="button"
            className={`db-list__item ${selectedAreaId === undefined ? 'db-list__item--on' : ''}`}
            onClick={() => setSelectedAreaId(undefined)}
          >
            <span className="db-list__name">Todos os ambientes</span>
            <span className="db-list__count">{tables.length}</span>
          </button>
          {areas.map((area) => (
            <button
              type="button"
              key={area.id}
              className={`db-list__item ${area.id === selectedAreaId ? 'db-list__item--on' : ''} ${area.active ? '' : 'db-list__item--off'}`}
              onClick={() => setSelectedAreaId(area.id)}
            >
              <span className="db-list__text">
                <span className="db-list__name">{area.name}</span>
                {area.active ? null : <span className="db-list__meta">Desativado</span>}
              </span>
              <span className="db-list__count">{area.tableCount}</span>
            </button>
          ))}
        </nav>

        <Card
          as="section"
          aria-label="Mesas do ambiente selecionado"
          padding="none"
          title={selectedArea ? `Mesas · ${selectedArea.name}` : 'Mesas'}
          subtitle="Cada mesa tem um QR Code próprio; rotacionar o token invalida o impresso anterior."
          actions={
            <>
              <Button
                type="button"
                size="sm"
                onClick={openCreateTable}
                disabled={activeAreas.length === 0}
              >
                Nova mesa
              </Button>
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={openBulkCreate}
                disabled={activeAreas.length === 0}
              >
                Criar mesas em lote
              </Button>
              {renderAreaToolbarActions()}
            </>
          }
        >
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
                  render: (row) => (
                    <Badge tone={row.active ? 'success' : 'neutral'} size="sm">
                      {row.active ? 'Ativa' : 'Inativa'}
                    </Badge>
                  ),
                },
                {
                  key: 'actions',
                  header: 'Ações',
                  align: 'right',
                  render: (row) => (
                    <div className="table-row-actions">
                      <Button
                        type="button"
                        variant="ghost"
                        size="sm"
                        onClick={() => setRotating(row)}
                      >
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
                      <Button
                        type="button"
                        variant="danger"
                        size="sm"
                        onClick={() => setDeletingTable(row)}
                      >
                        Excluir
                      </Button>
                    </div>
                  ),
                },
              ]}
              rows={visibleTables}
            />
          )}
        </Card>
      </div>

      <Modal
        open={creatingArea}
        onClose={() => setCreatingArea(false)}
        title="Novo ambiente"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setCreatingArea(false)}>
              Cancelar
            </Button>
            <Button type="button" busy={busy} onClick={() => void createArea()}>
              Criar ambiente
            </Button>
          </>
        }
      >
        <Field
          label="Nome do ambiente"
          htmlFor={areaNameFieldId}
          hint="Ex.: Salão, Varanda, Mezanino"
        >
          <Input
            id={areaNameFieldId}
            value={newAreaName}
            onChange={(event) => setNewAreaName(event.target.value)}
          />
        </Field>
      </Modal>

      <Modal
        open={creatingTable}
        onClose={() => setCreatingTable(false)}
        title="Nova mesa"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setCreatingTable(false)}>
              Cancelar
            </Button>
            <Button type="button" busy={busy} onClick={() => void createTable()}>
              Criar mesa
            </Button>
          </>
        }
      >
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
      </Modal>

      <Modal
        open={bulkCreating}
        onClose={() => setBulkCreating(false)}
        eyebrow="Criação em lote"
        title="Criar mesas em lote"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setBulkCreating(false)}>
              Cancelar
            </Button>
            <Button type="button" busy={busy} onClick={() => void createBulk()}>
              Criar mesas {bulkFrom} a {bulkTo}
            </Button>
          </>
        }
      >
        <p>Cadastre um intervalo inteiro de mesas de uma vez — ideal para o onboarding do salão.</p>
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
      </Modal>

      <Modal
        open={rotating !== undefined}
        onClose={() => setRotating(undefined)}
        tone="danger"
        eyebrow="Ação imediata"
        title={`Rotacionar token da mesa ${rotating?.label}?`}
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setRotating(undefined)}>
              Cancelar
            </Button>
            <Button type="button" variant="danger" busy={busy} onClick={() => void confirmRotate()}>
              Sim, rotacionar token
            </Button>
          </>
        }
      >
        <p>
          O QR Code impresso hoje deixará de funcionar imediatamente. Você precisará exportar o PDF
          novamente e substituir o código colado na mesa.
        </p>
      </Modal>

      <Modal
        open={deletingTable !== undefined}
        onClose={() => setDeletingTable(undefined)}
        tone="danger"
        eyebrow="Ação imediata"
        title={`Excluir mesa ${deletingTable?.label}?`}
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setDeletingTable(undefined)}>
              Cancelar
            </Button>
            <Button type="button" variant="danger" busy={busy} onClick={() => void confirmDelete()}>
              Sim, excluir mesa
            </Button>
          </>
        }
      >
        <p>
          Se esta mesa já tiver sessões no histórico, a exclusão será recusada — desative-a em vez
          de excluir para manter o histórico.
        </p>
      </Modal>
    </main>
  );
}
