// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen, waitFor, within } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import type { PermissionCatalogItem, RoleDto } from '@nexora/contracts';
import { RoleManagementPage } from './role-management-page.js';

const catalog: readonly PermissionCatalogItem[] = [
  {
    code: 'table:read',
    resource: 'Mesas',
    description: 'Consultar',
    sensitive: false,
  },
  {
    code: 'order:cancel_started',
    resource: 'Pedidos',
    description: 'Cancelar item que já entrou em produção',
    sensitive: true,
  },
  {
    code: '*',
    resource: 'Produto inteiro',
    description: 'Acesso completo ao estabelecimento',
    sensitive: true,
  },
];

const roles: readonly RoleDto[] = [
  {
    id: '0198aabb-1111-7000-8000-000000000001',
    code: 'ATENDENTE',
    name: 'Atendente',
    permissions: [],
    system: false,
    userCount: 0,
  },
  {
    id: '0198aabb-1111-7000-8000-000000000002',
    code: 'OWNER',
    name: 'Proprietário',
    permissions: ['*'],
    system: true,
    userCount: 1,
  },
];

const noOp = async () => roles[0]!;

describe('RoleManagementPage', () => {
  it('explica deny-by-default e efeito pratico das permissoes', () => {
    render(
      <RoleManagementPage
        roles={roles}
        permissionCatalog={catalog}
        onCreate={noOp}
        onUpdate={noOp}
      />,
    );

    expect(screen.getByText('Nenhuma ação liberada')).toBeInTheDocument();
    expect(screen.getByText('Cancelar item que já entrou em produção')).toBeInTheDocument();
    expect(screen.getAllByText('Ação sensível')).toHaveLength(2);
  });

  it('cria papel sem permissoes por padrao', async () => {
    const onCreate = vi.fn(noOp);
    render(
      <RoleManagementPage
        roles={roles}
        permissionCatalog={catalog}
        onCreate={onCreate}
        onUpdate={noOp}
      />,
    );

    fireEvent.click(screen.getByRole('button', { name: 'Novo papel' }));
    const dialog = screen.getByRole('dialog', { name: 'Criar papel' });
    fireEvent.change(within(dialog).getByLabelText('Nome do papel'), {
      target: { value: 'Recepcao' },
    });
    fireEvent.change(within(dialog).getByRole('textbox', { name: /C.digo/ }), {
      target: { value: 'RECEPCAO' },
    });
    fireEvent.click(within(dialog).getByRole('button', { name: 'Criar sem permissões' }));

    await waitFor(() =>
      expect(onCreate).toHaveBeenCalledWith({
        code: 'RECEPCAO',
        name: 'Recepcao',
        permissions: [],
      }),
    );
  });

  it('salva selecao explicita e protege acesso total do OWNER', async () => {
    const onUpdate = vi.fn(noOp);
    render(
      <RoleManagementPage
        roles={roles}
        permissionCatalog={catalog}
        onCreate={noOp}
        onUpdate={onUpdate}
      />,
    );

    fireEvent.click(screen.getByLabelText('Permitir Consultar'));
    fireEvent.click(screen.getByRole('button', { name: 'Salvar permissões' }));
    await waitFor(() =>
      expect(onUpdate).toHaveBeenCalledWith(roles[0]?.id, {
        name: 'Atendente',
        permissions: ['table:read'],
      }),
    );

    fireEvent.click(screen.getByRole('button', { name: /Proprietário/ }));
    expect(screen.getByLabelText('Permitir Acesso completo ao estabelecimento')).toBeDisabled();
  });
});
