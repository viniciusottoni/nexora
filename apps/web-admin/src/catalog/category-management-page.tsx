import { useEffect, useId, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  DataTable,
  EmptyState,
  Field,
  IconButton,
  Input,
  Modal,
} from '@nexora/ui';
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '@nexora/contracts';
import './catalog.css';

export interface CategoryManagementPageProps {
  readonly categories: readonly CategoryDto[];
  readonly onCreate: (input: CreateCategoryRequest) => Promise<CategoryDto>;
  readonly onUpdate: (id: string, input: UpdateCategoryRequest) => Promise<CategoryDto>;
  readonly onReorder: (order: readonly string[]) => Promise<void>;
  readonly onDeactivate: (id: string) => Promise<void>;
}

/**
 * Cadastro de categorias do cardápio (US-010) — ordenação por posição (setas "mover para
 * cima/baixo" em vez de arrastar com o mouse: mesmo efeito sobre `SortOrder` exigido pelo cenário
 * Gherkin "Ordenação do cardápio", com controle acessível por teclado — decisão documentada no
 * relatório da tarefa) e desativação que preserva o cadastro (nunca exclusão física).
 */
export function CategoryManagementPage({
  categories,
  onCreate,
  onUpdate,
  onReorder,
  onDeactivate,
}: Readonly<CategoryManagementPageProps>) {
  const createNameFieldId = useId();
  const createDescriptionFieldId = useId();
  const editNameFieldId = useId();
  const editDescriptionFieldId = useId();

  const [selectedId, setSelectedId] = useState(categories[0]?.id);
  const selected = categories.find((category) => category.id === selectedId) ?? categories[0];

  const [name, setName] = useState(selected?.name ?? '');
  const [description, setDescription] = useState(selected?.description ?? '');

  const [creating, setCreating] = useState(false);
  const [createName, setCreateName] = useState('');
  const [createDescription, setCreateDescription] = useState('');

  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();
  const [error, setError] = useState<string>();

  useEffect(() => {
    if (!selected) return;
    setName(selected.name);
    setDescription(selected.description ?? '');
  }, [selected?.id, selected?.name, selected?.description]);

  const ordered = [...categories].sort((a, b) => a.position - b.position);

  async function createCategory() {
    if (!createName.trim()) return;
    setBusy(true);
    setError(undefined);
    try {
      const created = await onCreate({
        name: createName.trim(),
        description: createDescription.trim() || undefined,
        position: categories.length,
      });
      setSelectedId(created.id);
      setCreating(false);
      setCreateName('');
      setCreateDescription('');
      setNotice('Categoria criada.');
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
      await onUpdate(selected.id, {
        name: name.trim(),
        description: description.trim() || undefined,
      });
      setNotice('Alterações salvas.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function move(categoryId: string, direction: -1 | 1) {
    const index = ordered.findIndex((category) => category.id === categoryId);
    const target = index + direction;
    if (index < 0 || target < 0 || target >= ordered.length) return;

    const next = [...ordered];
    [next[index], next[target]] = [next[target]!, next[index]!];

    setBusy(true);
    setError(undefined);
    try {
      await onReorder(next.map((category) => category.id));
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  async function toggleActive(category: CategoryDto) {
    setBusy(true);
    setError(undefined);
    try {
      if (category.isActive) {
        await onDeactivate(category.id);
        setNotice('Categoria desativada.');
      } else {
        await onUpdate(category.id, { isActive: true });
        setNotice('Categoria reativada.');
      }
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusy(false);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="categories-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Cardápio</p>
          <h1 className="db-page__title" id="categories-title">
            Categorias
          </h1>
          <p className="db-page__lead">
            Organize o cardápio em grupos (Pizzas, Bebidas, Sobremesas) e defina a ordem de exibição
            em todos os canais.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" onClick={() => setCreating(true)}>
            Nova categoria
          </Button>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Cardápio atualizado">
          {notice}
        </AlertBanner>
      ) : null}
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {ordered.length === 0 ? (
        <EmptyState icon="category" title="Nenhuma categoria cadastrada">
          Crie a primeira categoria para começar a montar o cardápio.
        </EmptyState>
      ) : (
        <div className="db-workbench db-workbench--rail">
          <Card
            padding="none"
            title="Categorias do cardápio"
            subtitle="A ordem aqui é a ordem que o cliente vê em todos os canais."
          >
            <DataTable
              rowKey="id"
              rows={ordered}
              onRowClick={(row) => setSelectedId(row.id)}
              columns={[
                {
                  key: 'reorder',
                  header: '',
                  width: '4.5rem',
                  render: (row) => (
                    <span className="catalog-reorder-controls">
                      <IconButton
                        icon="arrow_upward"
                        label={`Mover ${row.name} para cima`}
                        size="sm"
                        onClick={(event) => {
                          event.stopPropagation();
                          void move(row.id, -1);
                        }}
                      />
                      <IconButton
                        icon="arrow_downward"
                        label={`Mover ${row.name} para baixo`}
                        size="sm"
                        onClick={(event) => {
                          event.stopPropagation();
                          void move(row.id, 1);
                        }}
                      />
                    </span>
                  ),
                },
                { key: 'name', header: 'Nome' },
                {
                  key: 'productCount',
                  header: 'Produtos',
                  align: 'right',
                  render: (row) => row.productCount,
                },
                {
                  key: 'isActive',
                  header: 'Situação',
                  render: (row) =>
                    row.isActive ? (
                      <Badge tone="success">Ativa</Badge>
                    ) : (
                      <Badge tone="neutral">Inativa</Badge>
                    ),
                },
              ]}
            />
          </Card>

          {selected ? (
            <Card
              className="db-form-card"
              title={selected.name}
              subtitle={`Posição ${selected.position + 1}`}
            >
              <Field label="Nome da categoria" htmlFor={editNameFieldId}>
                <Input
                  id={editNameFieldId}
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                />
              </Field>
              <Field
                label="Descrição"
                htmlFor={editDescriptionFieldId}
                hint="Opcional — aparece abaixo do nome no cardápio"
              >
                <Input
                  id={editDescriptionFieldId}
                  value={description}
                  onChange={(event) => setDescription(event.target.value)}
                />
              </Field>

              <div className="db-editor__footer">
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => void toggleActive(selected)}
                  busy={busy}
                >
                  {selected.isActive ? 'Desativar categoria' : 'Reativar categoria'}
                </Button>
                <Button type="button" busy={busy} onClick={() => void save()}>
                  Salvar categoria
                </Button>
              </div>
            </Card>
          ) : null}
        </div>
      )}

      <Modal
        open={creating}
        onClose={() => setCreating(false)}
        eyebrow="Nova categoria"
        title="Criar categoria"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
              Cancelar
            </Button>
            <Button type="button" busy={busy} onClick={() => void createCategory()}>
              Criar categoria
            </Button>
          </>
        }
      >
        <Field label="Nome" htmlFor={createNameFieldId}>
          <Input
            id={createNameFieldId}
            value={createName}
            onChange={(event) => setCreateName(event.target.value)}
          />
        </Field>
        <Field label="Descrição" htmlFor={createDescriptionFieldId} hint="Opcional">
          <Input
            id={createDescriptionFieldId}
            value={createDescription}
            onChange={(event) => setCreateDescription(event.target.value)}
          />
        </Field>
      </Modal>
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
