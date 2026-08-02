// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { AuthorizationModal } from './authorization-modal.js';

describe('AuthorizationModal', () => {
  it('pede PIN superior sobre o contexto e devolve autorização', () => {
    const authorize = vi.fn();
    render(
      <>
        <p>Pedido #A47 permanece aqui</p>
        <AuthorizationModal
          actionLabel="Cancelar item iniciado"
          onAuthorize={authorize}
          onCancel={vi.fn()}
        />
      </>,
    );
    expect(screen.getByText('Pedido #A47 permanece aqui')).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toHaveAccessibleName('Autorização necessária');
    for (const digit of ['9', '9', '1', '1'])
      fireEvent.click(screen.getByRole('button', { name: digit }));
    fireEvent.click(screen.getByRole('button', { name: 'Autorizar' }));
    expect(authorize).toHaveBeenCalledWith('9911');
  });
});
