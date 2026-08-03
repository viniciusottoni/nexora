import { useEffect, useId, useMemo, useState } from 'react';
import { Button, Card, Field, Input } from '@nexora/ui';
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
    <main className="roles-shell" aria-labelledby="roles-title">
      <header className="roles-header">
        <div>
          <p className="roles-eyebrow">EQUIPE E ACESSOS</p>
          <h1 id="roles-title">Papéis e permissões</h1>
          <p className="roles-lead">
            Defina o que cada função pode fazer. Acesso não marcado permanece bloqueado.
          </p>
        </div>
        <Button type="button" onClick={() => setCreating(true)}>
          Novo papel
        </Button>
      </header>

      {notice ? (
        <p className="roles-notice" role="status">
          {notice}
        </p>
      ) : null}

      <div className="roles-workbench">
        <nav className="role-list nx-stagger" aria-label="Papéis cadastrados">
          {roles.map((role) => (
            <button
              type="button"
              className={`role-list__item ${role.id === selected?.id ? 'role-list__item--active' : ''}`}
              key={role.id}
              onClick={() => setSelectedId(role.id)}
            >
              <span>
                <strong>{role.name}</strong>
                <small>
                  {role.code} · {role.userCount} {role.userCount === 1 ? 'pessoa' : 'pessoas'}
                </small>
              </span>
              <span className="role-list__count">{role.permissions.length}</span>
            </button>
          ))}
        </nav>

        {selected ? (
          <Card className="role-editor">
            <div className="role-editor__heading">
              <div>
                <p className="roles-eyebrow">
                  {selected.system ? 'MODELO DO SISTEMA' : 'PAPEL PERSONALIZADO'}
                </p>
                <h2>{selected.name}</h2>
              </div>
              {permissions.length === 0 ? (
                <span className="role-empty">Nenhuma ação liberada</span>
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
                      <label className="permission-option" key={permission.code}>
                        <input
                          type="checkbox"
                          checked={permissions.includes(permission.code)}
                          disabled={ownerLock}
                          onChange={() => toggle(permission.code)}
                          aria-label={`Permitir ${permission.description}`}
                        />
                        <span className="permission-option__copy">
                          <strong>{permission.description}</strong>
                          <code>{permission.code}</code>
                        </span>
                        {permission.sensitive ? (
                          <span className="permission-sensitive">Ação sensível</span>
                        ) : null}
                      </label>
                    );
                  })}
                </fieldset>
              ))}
            </div>

            {selected.code === 'OWNER' ? (
              <p className="owner-lock">
                OWNER mantém acesso completo para evitar bloqueio administrativo do estabelecimento.
              </p>
            ) : null}
            <div className="role-editor__footer">
              <p>Alterações geram evento, auditoria e alerta para gestores.</p>
              <Button type="button" busy={busy} onClick={() => void save()}>
                Salvar permissões
              </Button>
            </div>
          </Card>
        ) : (
          <Card className="role-editor role-editor--empty">Nenhum papel cadastrado.</Card>
        )}
      </div>

      {creating ? (
        <div className="roles-dialog-backdrop">
          <section
            className="roles-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="create-role-title"
          >
            <p className="roles-eyebrow">DENY-BY-DEFAULT</p>
            <h2 id="create-role-title">Criar papel</h2>
            <p>Novo papel começa sem acesso. Permissões podem ser concedidas depois.</p>
            <div className="roles-dialog__fields">
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
            </div>
            <div className="roles-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setCreating(false)}>
                Cancelar
              </Button>
              <Button type="button" busy={busy} onClick={() => void createRole()}>
                Criar sem permissões
              </Button>
            </div>
          </section>
        </div>
      ) : null}
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
