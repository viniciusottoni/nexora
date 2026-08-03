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
} from '@nexora/ui';
import type {
  CreateRoleRequest,
  PermissionCatalogItem,
  PermissionCode,
  RoleDto,
  UpdateRoleRequest,
} from '@nexora/contracts';
import './roles.css';

export interface RoleManagementPageProps {
  readonly roles: readonly RoleDto[];
  readonly permissionCatalog: readonly PermissionCatalogItem[];
  readonly onCreate: (input: CreateRoleRequest) => Promise<RoleDto>;
  readonly onUpdate: (id: string, input: UpdateRoleRequest) => Promise<RoleDto>;
}

export function RoleManagementPage({
  roles,
  permissionCatalog,
  onCreate,
  onUpdate,
}: Readonly<RoleManagementPageProps>) {
  const nameFieldId = useId();
  const createNameFieldId = useId();
  const createCodeFieldId = useId();
  const [selectedId, setSelectedId] = useState(roles[0]?.id);
  const selected = roles.find((role) => role.id === selectedId) ?? roles[0];
  const [name, setName] = useState(selected?.name ?? '');
  const [permissions, setPermissions] = useState<readonly PermissionCode[]>(
    selected?.permissions ?? [],
  );
  const [creating, setCreating] = useState(false);
  const [createName, setCreateName] = useState('');
  const [createCode, setCreateCode] = useState('');
  const [busy, setBusy] = useState(false);
  const [notice, setNotice] = useState<string>();

  useEffect(() => {
    if (!selected) return;
    setName(selected.name);
    setPermissions(selected.permissions);
  }, [selected?.id, selected?.name, selected?.permissions]);

  const groups = useMemo(() => groupPermissions(permissionCatalog), [permissionCatalog]);

  async function createRole() {
    if (!createName.trim() || !createCode.trim()) return;
    setBusy(true);
    try {
      const created = await onCreate({
        code: createCode.trim().toUpperCase(),
        name: createName.trim(),
        permissions: [],
      });
      setSelectedId(created.id);
      setCreating(false);
      setCreateName('');
      setCreateCode('');
      setNotice(
        'Papel criado sem permissões. Todo acesso continua negado até concessão explícita.',
      );
    } finally {
      setBusy(false);
    }
  }

  async function save() {
    if (!selected) return;
    setBusy(true);
    try {
      await onUpdate(selected.id, { name: name.trim(), permissions: [...permissions] });
      setNotice('Permissões salvas. Gestores foram alertados sobre a alteração.');
    } finally {
      setBusy(false);
    }
  }

  function toggle(permission: PermissionCode) {
    if (selected?.code === 'OWNER' && permission === '*') return;
    setPermissions((current) =>
      current.includes(permission)
        ? current.filter((candidate) => candidate !== permission)
        : [...current, permission],
    );
    setNotice(undefined);
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="roles-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Equipe e acessos</p>
          <h1 className="db-page__title" id="roles-title">
            Papéis e permissões
          </h1>
          <p className="db-page__lead">
            Defina o que cada função pode fazer. Acesso não marcado permanece bloqueado.
          </p>
        </div>
        <div className="db-page__actions">
          <Button type="button" onClick={() => setCreating(true)}>
            Novo papel
          </Button>
        </div>
      </header>

      {notice ? (
        <AlertBanner tone="success" title="Permissões atualizadas">
          {notice}
        </AlertBanner>
      ) : null}

      {roles.length === 0 ? (
        <Card padding="none">
          <EmptyState icon="shield_person" title="Nenhum papel cadastrado">
            Papel é o que define o que cada função da equipe pode fazer no sistema. Comece em "Novo
            papel", acima.
          </EmptyState>
        </Card>
      ) : (
        <div className="db-workbench">
          <nav className="db-list nx-stagger" aria-label="Papéis cadastrados">
            {roles.map((role) => (
              <button
                type="button"
                className={`db-list__item ${role.id === selected?.id ? 'db-list__item--on' : ''}`}
                key={role.id}
                onClick={() => setSelectedId(role.id)}
              >
                <span className="db-list__text">
                  <span className="db-list__name">{role.name}</span>
                  <span className="db-list__meta">
                    <span className="db-code">{role.code}</span> · {role.userCount}{' '}
                    {role.userCount === 1 ? 'pessoa' : 'pessoas'}
                  </span>
                </span>
                <span className="db-list__count">{role.permissions.length}</span>
              </button>
            ))}
          </nav>

          {selected ? (
            <Card>
              <div className="role-editor__heading">
                <div>
                  <p className="db-page__eyebrow">
                    {selected.system ? 'Modelo do sistema' : 'Papel personalizado'}
                  </p>
                  <h2>{selected.name}</h2>
                </div>
                {permissions.length === 0 ? (
                  <Badge tone="warning">Nenhuma ação liberada</Badge>
                ) : null}
              </div>

              <Field label="Nome exibido" htmlFor={nameFieldId}>
                <Input
                  id={nameFieldId}
                  value={name}
                  onChange={(event) => setName(event.target.value)}
                />
              </Field>

              <div className="permission-groups nx-stagger">
                {groups.map(([resource, items]) => (
                  <fieldset className="permission-group" key={resource}>
                    <legend>{resource}</legend>
                    {items.map((permission) => {
                      const ownerLock = selected.code === 'OWNER' && permission.code === '*';
                      return (
                        <Checkbox
                          className="permission-option"
                          key={permission.code}
                          checked={permissions.includes(permission.code)}
                          disabled={ownerLock}
                          onChange={() => toggle(permission.code)}
                          aria-label={`Permitir ${permission.description}`}
                          label={
                            <span className="permission-option__copy">
                              <strong>{permission.description}</strong>
                              <code>{permission.code}</code>
                              {permission.sensitive ? (
                                <span className="permission-sensitive">Ação sensível</span>
                              ) : null}
                            </span>
                          }
                        />
                      );
                    })}
                  </fieldset>
                ))}
              </div>

              {selected.code === 'OWNER' ? (
                <AlertBanner tone="warning" title="Acesso total é fixo neste papel">
                  OWNER mantém acesso completo para evitar bloqueio administrativo do
                  estabelecimento.
                </AlertBanner>
              ) : null}
              <div className="db-editor__footer">
                <p className="db-hint">
                  Alterações geram evento, auditoria e alerta para gestores.
                </p>
                <Button type="button" busy={busy} onClick={() => void save()}>
                  Salvar permissões
                </Button>
              </div>
            </Card>
          ) : null}
        </div>
      )}

      <Modal
        open={creating}
        onClose={() => setCreating(false)}
        eyebrow="Deny-by-default"
        title="Criar papel"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
              Cancelar
            </Button>
            <Button type="button" busy={busy} onClick={() => void createRole()}>
              Criar sem permissões
            </Button>
          </>
        }
      >
        <p>Novo papel começa sem acesso. Permissões podem ser concedidas depois.</p>
        <Field label="Nome do papel" htmlFor={createNameFieldId}>
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
      </Modal>
    </main>
  );
}

function groupPermissions(
  catalog: readonly PermissionCatalogItem[],
): ReadonlyArray<readonly [string, readonly PermissionCatalogItem[]]> {
  const groups = new Map<string, PermissionCatalogItem[]>();
  for (const item of catalog) {
    const existing = groups.get(item.resource) ?? [];
    existing.push(item);
    groups.set(item.resource, existing);
  }
  return [...groups.entries()];
}
