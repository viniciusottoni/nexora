import { useEffect, useId, useMemo, useState } from 'react';
import {
  AlertBanner,
  Badge,
  Button,
  Card,
  Checkbox,
  EmptyState,
  Field,
  Input,
  Modal,
  Switch,
} from '@nexora/ui';
import {
  validateModifierSelection,
  type CreateModifierGroupRequest,
  type CreateModifierRequest,
  type Modifier,
  type ModifierGroup,
} from '@nexora/contracts';
import './modifiers.css';

export interface ModifierGroupManagementPageProps {
  readonly groups: readonly ModifierGroup[];
  readonly onCreateGroup: (input: CreateModifierGroupRequest) => Promise<ModifierGroup>;
  readonly onUpdateGroup: (
    groupId: string,
    minSelect: number,
    maxSelect: number,
  ) => Promise<ModifierGroup>;
  readonly onDeleteGroup: (groupId: string) => Promise<void>;
  readonly onCreateModifier: (groupId: string, input: CreateModifierRequest) => Promise<Modifier>;
  readonly onUpdateModifierPrice: (
    groupId: string,
    modifierId: string,
    priceDelta: string,
  ) => Promise<Modifier>;
  readonly onSetModifierAvailability: (
    groupId: string,
    modifierId: string,
    isAvailable: boolean,
  ) => Promise<Modifier>;
  readonly onLinkToProduct: (productId: string, groupId: string) => Promise<void>;
  readonly onUnlinkFromProduct: (productId: string, groupId: string) => Promise<void>;
}

export function normalizeMoneyInput(value: string): string | undefined {
  const match = /^(-?)(\d+)(?:[.,](\d{0,2}))?$/.exec(value.trim());
  if (!match) return undefined;
  const fraction = (match[3] ?? '').padEnd(2, '0');
  return `${match[1]}${match[2]}.${fraction}`;
}

/** Converte `money_amount` em centavos inteiros; nenhum cálculo monetário usa ponto flutuante. */
export function moneyToCents(value: string): number | null {
  const normalized = normalizeMoneyInput(value);
  if (!normalized) return null;
  const match = /^(-?)(\d+)\.(\d{2})$/.exec(normalized)!;
  const absolute = Number(match[2]) * 100 + Number(match[3]);
  return match[1] === '-' ? -absolute : absolute;
}

/** Formata `money` (string decimal, ADR-017) como BRL — só para exibição. */
function formatMoney(value: string): string {
  const cents = moneyToCents(value);
  if (cents === null) return value;
  return (cents / 100).toLocaleString('pt-BR', { style: 'currency', currency: 'BRL' });
}

export function ModifierGroupManagementPage({
  groups,
  onCreateGroup,
  onUpdateGroup,
  onDeleteGroup,
  onCreateModifier,
  onUpdateModifierPrice,
  onSetModifierAvailability,
  onLinkToProduct,
  onUnlinkFromProduct,
}: Readonly<ModifierGroupManagementPageProps>) {
  const [selectedId, setSelectedId] = useState(groups[0]?.id);
  const selected = groups.find((group) => group.id === selectedId) ?? groups[0];
  const [creating, setCreating] = useState(false);
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();

  useEffect(() => {
    if (!selectedId && groups[0]) setSelectedId(groups[0].id);
  }, [groups, selectedId]);

  return (
    <main className="db-page nx-anim-in" aria-labelledby="modifiers-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Cardápio</p>
          <h1 className="db-page__title" id="modifiers-title">
            Grupos de modificadores
          </h1>
          <p className="db-page__lead">
            Defina adicionais, remoções e opções obrigatórias reutilizáveis entre produtos.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" onClick={() => setCreating(true)}>
            Novo grupo
          </Button>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Grupos atualizados">
          {notice}
        </AlertBanner>
      ) : null}

      {groups.length === 0 ? (
        <Card padding="none">
          <EmptyState icon="tune" title="Nenhum grupo cadastrado">
            Grupo de modificadores é o que carrega borda, adicional e remoção — um grupo serve
            vários produtos. Comece em "Novo grupo", acima.
          </EmptyState>
        </Card>
      ) : (
        <div className="db-workbench">
          <nav className="db-list nx-stagger" aria-label="Grupos de modificadores cadastrados">
            {groups.map((group) => (
              <button
                type="button"
                key={group.id}
                className={`db-list__item ${group.id === selected?.id ? 'db-list__item--on' : ''}`}
                onClick={() => setSelectedId(group.id)}
              >
                <span className="db-list__text">
                  <span className="db-list__name">{group.name}</span>
                  <span className="db-list__meta">
                    {group.minSelect}–{group.maxSelect} seleções · {group.productIds.length}{' '}
                    {group.productIds.length === 1 ? 'produto' : 'produtos'}
                  </span>
                </span>
                {group.isRequired ? (
                  <Badge tone="warning" size="sm">
                    Obrigatório
                  </Badge>
                ) : (
                  <Badge tone="neutral" size="sm">
                    Opcional
                  </Badge>
                )}
              </button>
            ))}
          </nav>

          {selected ? (
            <GroupEditor
              key={selected.id}
              group={selected}
              busy={busy}
              setBusy={setBusy}
              setNotice={setNotice}
              onUpdateGroup={onUpdateGroup}
              onDeleteGroup={onDeleteGroup}
              onCreateModifier={onCreateModifier}
              onUpdateModifierPrice={onUpdateModifierPrice}
              onSetModifierAvailability={onSetModifierAvailability}
              onLinkToProduct={onLinkToProduct}
              onUnlinkFromProduct={onUnlinkFromProduct}
            />
          ) : null}
        </div>
      )}

      <CreateGroupDialog
        open={creating}
        onCancel={() => setCreating(false)}
        onCreate={async (input) => {
          setBusy(true);
          try {
            const created = await onCreateGroup(input);
            setSelectedId(created.id);
            setCreating(false);
            setNotice('Grupo criado. Vincule-o a um ou mais produtos para começar a usar.');
          } finally {
            setBusy(false);
          }
        }}
      />
    </main>
  );
}

function CreateGroupDialog({
  open,
  onCancel,
  onCreate,
}: Readonly<{
  open: boolean;
  onCancel: () => void;
  onCreate: (input: CreateModifierGroupRequest) => Promise<void>;
}>) {
  const nameId = useId();
  const minId = useId();
  const maxId = useId();
  const [name, setName] = useState('');
  const [minSelect, setMinSelect] = useState(0);
  const [maxSelect, setMaxSelect] = useState(1);
  const [isRequired, setIsRequired] = useState(false);
  const [busy, setBusy] = useState(false);
  const invalid = maxSelect < minSelect;

  useEffect(() => {
    if (!open) return;
    setName('');
    setMinSelect(0);
    setMaxSelect(1);
    setIsRequired(false);
  }, [open]);

  return (
    <Modal
      open={open}
      onClose={onCancel}
      eyebrow="Novo grupo"
      title="Criar grupo de modificadores"
      actions={
        <>
          <Button type="button" variant="ghost" onClick={onCancel}>
            Cancelar
          </Button>
          <Button
            type="button"
            busy={busy}
            disabled={!name.trim() || invalid}
            onClick={() => {
              void (async () => {
                setBusy(true);
                try {
                  await onCreate({
                    name: name.trim(),
                    minSelect,
                    maxSelect,
                    isRequired,
                    sortOrder: 0,
                  });
                } finally {
                  setBusy(false);
                }
              })();
            }}
          >
            Criar grupo
          </Button>
        </>
      }
    >
      <Field label="Nome" htmlFor={nameId} hint='Ex.: "Tamanho", "Adicionais", "Ponto da massa"'>
        <Input id={nameId} value={name} onChange={(event) => setName(event.target.value)} />
      </Field>
      <div className="modifiers-rule-row">
        <Field label="Mínimo de seleções" htmlFor={minId}>
          <Input
            id={minId}
            type="number"
            min={0}
            value={minSelect}
            onChange={(event) => setMinSelect(Number(event.target.value))}
          />
        </Field>
        <Field
          label="Máximo de seleções"
          htmlFor={maxId}
          error={invalid ? 'Máximo não pode ser menor que o mínimo' : undefined}
        >
          <Input
            id={maxId}
            type="number"
            min={0}
            value={maxSelect}
            onChange={(event) => setMaxSelect(Number(event.target.value))}
          />
        </Field>
      </div>
      <Switch
        checked={isRequired}
        onChange={(event) => {
          const required = event.target.checked;
          setIsRequired(required);
          if (required) {
            setMinSelect((current) => Math.max(1, current));
            setMaxSelect((current) => Math.max(1, current));
          }
        }}
        label="Obrigatório"
        description="O cliente precisa escolher antes de adicionar o item ao pedido"
      />
    </Modal>
  );
}

function GroupEditor({
  group,
  busy,
  setBusy,
  setNotice,
  onUpdateGroup,
  onDeleteGroup,
  onCreateModifier,
  onUpdateModifierPrice,
  onSetModifierAvailability,
  onLinkToProduct,
  onUnlinkFromProduct,
}: Readonly<
  {
    group: ModifierGroup;
    busy: boolean;
    setBusy: (value: boolean) => void;
    setNotice: (value: string | undefined) => void;
  } & Pick<
    ModifierGroupManagementPageProps,
    | 'onUpdateGroup'
    | 'onDeleteGroup'
    | 'onCreateModifier'
    | 'onUpdateModifierPrice'
    | 'onSetModifierAvailability'
    | 'onLinkToProduct'
    | 'onUnlinkFromProduct'
  >
>) {
  const minId = useId();
  const maxId = useId();
  const [minSelect, setMinSelect] = useState(group.minSelect);
  const [maxSelect, setMaxSelect] = useState(group.maxSelect);
  const [productId, setProductId] = useState('');
  const [modifierName, setModifierName] = useState('');
  const [modifierPrice, setModifierPrice] = useState('0.00');

  useEffect(() => {
    setMinSelect(group.minSelect);
    setMaxSelect(group.maxSelect);
  }, [group.id, group.minSelect, group.maxSelect]);

  const rangeChanged = minSelect !== group.minSelect || maxSelect !== group.maxSelect;
  const rangeInvalid = maxSelect < minSelect;

  return (
    <div className="db-stack">
      <Card
        className="db-form-card"
        title={group.name}
        subtitle={group.isRequired ? 'Grupo obrigatório' : 'Grupo opcional'}
        actions={
          <Button
            type="button"
            variant="danger"
            size="sm"
            busy={busy}
            onClick={() => {
              void (async () => {
                setBusy(true);
                try {
                  await onDeleteGroup(group.id);
                  setNotice(
                    'Grupo removido. Modificadores e vínculos com produtos foram desfeitos.',
                  );
                } finally {
                  setBusy(false);
                }
              })();
            }}
          >
            Remover grupo
          </Button>
        }
      >
        <p className="db-hint">
          Renomear ou alternar obrigatoriedade exige recriar o grupo hoje — ver relatório da tarefa
          (limitação de <span className="db-code">Nexora.Domain.Catalog.ModifierGroup</span>, fora
          do escopo desta US).
        </p>

        <div className="modifiers-rule-row">
          <Field label="Mínimo de seleções" htmlFor={minId}>
            <Input
              id={minId}
              type="number"
              min={0}
              value={minSelect}
              onChange={(event) => setMinSelect(Number(event.target.value))}
            />
          </Field>
          <Field
            label="Máximo de seleções"
            htmlFor={maxId}
            error={rangeInvalid ? 'Máximo não pode ser menor que o mínimo' : undefined}
          >
            <Input
              id={maxId}
              type="number"
              min={0}
              value={maxSelect}
              onChange={(event) => setMaxSelect(Number(event.target.value))}
            />
          </Field>
          <Button
            type="button"
            busy={busy}
            disabled={!rangeChanged || rangeInvalid}
            onClick={() => {
              void (async () => {
                setBusy(true);
                try {
                  await onUpdateGroup(group.id, minSelect, maxSelect);
                  setNotice(
                    'Regra de seleção atualizada — vale para todos os produtos que reusam este grupo.',
                  );
                } finally {
                  setBusy(false);
                }
              })();
            }}
          >
            Salvar regra
          </Button>
        </div>
      </Card>

      <Card
        as="section"
        aria-label="Modificadores do grupo"
        className="modifier-list"
        title="Modificadores"
        subtitle="Adicional entra com preço; remoção entra com zero e aparece em destaque no KDS."
      >
        {group.modifiers.length === 0 ? (
          <p className="db-hint">Nenhum modificador cadastrado.</p>
        ) : null}
        <ul className="nx-stagger">
          {group.modifiers.map((modifier) => (
            <ModifierRow
              key={modifier.id}
              groupId={group.id}
              modifier={modifier}
              onUpdateModifierPrice={onUpdateModifierPrice}
              onSetModifierAvailability={onSetModifierAvailability}
            />
          ))}
        </ul>

        <div className="modifiers-rule-row modifier-add-form">
          <Field label="Nome do modificador">
            <Input
              value={modifierName}
              onChange={(event) => setModifierName(event.target.value)}
              placeholder='Ex.: "Borda Catupiry", "Sem cebola"'
            />
          </Field>
          <Field
            label="Price delta (R$)"
            hint="0,00 para remoção sem custo, negativo para desconto"
          >
            <Input
              value={modifierPrice}
              onChange={(event) => setModifierPrice(event.target.value)}
              inputMode="decimal"
            />
          </Field>
          <Button
            type="button"
            disabled={!modifierName.trim() || !normalizeMoneyInput(modifierPrice)}
            onClick={() => {
              void (async () => {
                await onCreateModifier(group.id, {
                  name: modifierName.trim(),
                  priceDelta: normalizeMoneyInput(modifierPrice)!,
                  ingredientId: null,
                  quantity: null,
                  sortOrder: group.modifiers.length,
                });
                setModifierName('');
                setModifierPrice('0.00');
              })();
            }}
          >
            Adicionar modificador
          </Button>
        </div>
      </Card>

      <Card
        as="section"
        aria-label="Produtos vinculados"
        className="modifier-link-section"
        title={`Produtos vinculados (${group.productIds.length})`}
        subtitle="Reuso: alterar a regra acima já vale para todos os produtos listados aqui, sem precisar editar cada um."
      >
        <ul className="modifier-product-list nx-stagger">
          {group.productIds.map((id) => (
            <li key={id}>
              <code>{id}</code>
              <Button
                type="button"
                variant="ghost"
                size="sm"
                onClick={() => {
                  void onUnlinkFromProduct(id, group.id);
                }}
              >
                Desvincular
              </Button>
            </li>
          ))}
        </ul>
        <div className="modifiers-rule-row">
          <Field
            label="ID do produto"
            hint="Tela de produto ainda não existe neste worktree (US-010/011) — vínculo manual por ID"
          >
            <Input
              value={productId}
              onChange={(event) => setProductId(event.target.value)}
              placeholder="uuid"
            />
          </Field>
          <Button
            type="button"
            disabled={!productId.trim()}
            onClick={() => {
              void (async () => {
                await onLinkToProduct(productId.trim(), group.id);
                setProductId('');
              })();
            }}
          >
            Vincular produto
          </Button>
        </div>
      </Card>

      <ModifierSelectionPreview group={group} />
    </div>
  );
}

function ModifierRow({
  groupId,
  modifier,
  onUpdateModifierPrice,
  onSetModifierAvailability,
}: Readonly<
  {
    groupId: string;
    modifier: Modifier;
  } & Pick<ModifierGroupManagementPageProps, 'onUpdateModifierPrice' | 'onSetModifierAvailability'>
>) {
  const priceId = useId();
  const [priceDelta, setPriceDelta] = useState(modifier.priceDelta);
  const [savingPrice, setSavingPrice] = useState(false);

  useEffect(() => setPriceDelta(modifier.priceDelta), [modifier.priceDelta]);

  return (
    <li className="modifier-list__item">
      <span className="modifier-list__name">{modifier.name}</span>
      <div className="modifier-list__price-editor">
        <Field label={`Preço de ${modifier.name}`} htmlFor={priceId}>
          <Input
            id={priceId}
            value={priceDelta}
            inputMode="decimal"
            onChange={(event) => setPriceDelta(event.target.value)}
          />
        </Field>
        <Button
          type="button"
          variant="ghost"
          size="sm"
          busy={savingPrice}
          disabled={
            !normalizeMoneyInput(priceDelta) ||
            normalizeMoneyInput(priceDelta) === modifier.priceDelta
          }
          aria-label={`Salvar preço de ${modifier.name}`}
          onClick={() => {
            void (async () => {
              setSavingPrice(true);
              try {
                await onUpdateModifierPrice(groupId, modifier.id, normalizeMoneyInput(priceDelta)!);
              } finally {
                setSavingPrice(false);
              }
            })();
          }}
        >
          Salvar
        </Button>
      </div>
      <Switch
        checked={modifier.isAvailable}
        onChange={(event) => {
          void onSetModifierAvailability(groupId, modifier.id, event.target.checked);
        }}
        label={modifier.isAvailable ? 'Disponível' : 'Indisponível'}
      />
    </li>
  );
}

/**
 * Preview de seleção do cliente — cobre os critérios de aceite de UI da US-012 (§10: grupo
 * obrigatório destacado ANTES de tentar avançar, contador "escolha até N", 4ª seleção acima do
 * limite bloqueada, remoção em destaque) sem depender do carrinho de pedido real (módulo de outra
 * epic, ainda não existe). A validação de limite (<c>validateModifierSelection</c>) é a mesma
 * função pura testada em `modifier-group-management-page.test.tsx`.
 */
function ModifierSelectionPreview({ group }: Readonly<{ group: ModifierGroup }>) {
  const basePriceId = useId();
  const [selected, setSelected] = useState<readonly string[]>([]);
  const [basePrice, setBasePrice] = useState('45.00');
  const [blockedNotice, setBlockedNotice] = useState<string>();

  const availableModifiers = group.modifiers.filter((modifier) => modifier.isAvailable);
  const validation = useMemo(
    () => validateModifierSelection(group, selected.length),
    [group.minSelect, group.maxSelect, selected.length],
  );

  const totalCents = availableModifiers
    .filter((modifier) => selected.includes(modifier.id))
    .reduce(
      (sum, modifier) => sum + (moneyToCents(modifier.priceDelta) ?? 0),
      moneyToCents(basePrice) ?? 0,
    );

  function toggle(modifierId: string) {
    const isSelected = selected.includes(modifierId);
    if (!isSelected && group.maxSelect === 1) {
      setBlockedNotice(undefined);
      setSelected([modifierId]);
      return;
    }
    if (!isSelected && !validation.canSelectMore) {
      setBlockedNotice(
        `Limite de ${group.maxSelect} opções atingido — desmarque uma para escolher outra.`,
      );
      return;
    }
    setBlockedNotice(undefined);
    setSelected((current) =>
      isSelected ? current.filter((id) => id !== modifierId) : [...current, modifierId],
    );
  }

  return (
    <Card
      as="section"
      aria-label="Preview do item (cardápio/KDS)"
      className="modifier-preview db-form-card"
      title="Preview do item"
      subtitle="Como o cliente vê a escolha na mesa e como ela chega ao KDS."
    >
      {group.isRequired && !validation.meetsMinimum ? (
        <AlertBanner tone="warning">
          Escolha pendente: este grupo é obrigatório — selecione ao menos {group.minSelect}.
        </AlertBanner>
      ) : null}
      <p className="modifier-preview__counter">
        Escolha até {group.maxSelect} · {selected.length} selecionado
        {selected.length === 1 ? '' : 's'} · {validation.remaining} restante
        {validation.remaining === 1 ? '' : 's'}
      </p>
      {blockedNotice ? <AlertBanner tone="warning">{blockedNotice}</AlertBanner> : null}

      <Field label="Preço base do item (R$)" htmlFor={basePriceId}>
        <Input
          id={basePriceId}
          value={basePrice}
          onChange={(event) => setBasePrice(event.target.value)}
          inputMode="decimal"
        />
      </Field>

      <div className="modifier-preview__options nx-stagger">
        {availableModifiers.map((modifier) => (
          <Checkbox
            key={modifier.id}
            label={modifier.name}
            price={
              moneyToCents(modifier.priceDelta) === 0
                ? 'sem custo'
                : formatMoney(modifier.priceDelta)
            }
            checked={selected.includes(modifier.id)}
            onChange={() => toggle(modifier.id)}
          />
        ))}
      </div>

      <div className="modifier-preview__ticket" aria-label="Cartão do KDS (simulado)">
        <strong>
          Total do item:{' '}
          {(totalCents / 100).toLocaleString('pt-BR', {
            style: 'currency',
            currency: 'BRL',
          })}
        </strong>
        <ul>
          {availableModifiers
            .filter((modifier) => selected.includes(modifier.id))
            .map((modifier) => {
              const isRemoval = (moneyToCents(modifier.priceDelta) ?? 0) <= 0;
              return (
                <li
                  key={modifier.id}
                  className={isRemoval ? 'modifier-preview__ticket-line--removal' : ''}
                >
                  {isRemoval
                    ? modifier.name.toUpperCase()
                    : `${modifier.name} (${formatMoney(modifier.priceDelta)})`}
                </li>
              );
            })}
        </ul>
      </div>
    </Card>
  );
}
