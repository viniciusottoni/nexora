// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { Modal } from './modal.js';

describe('Modal', () => {
  it('não renderiza nada quando fechado', () => {
    render(
      <Modal open={false} onClose={vi.fn()} title="Título">
        conteúdo
      </Modal>,
    );
    expect(screen.queryByRole('dialog')).not.toBeInTheDocument();
  });

  it('renderiza eyebrow, título e ações quando aberto', () => {
    render(
      <Modal open onClose={vi.fn()} eyebrow="NOVA PRAÇA" title="Criar praça" actions={<button>Criar</button>}>
        conteúdo
      </Modal>,
    );
    expect(screen.getByText('NOVA PRAÇA')).toBeInTheDocument();
    expect(screen.getByRole('heading', { name: 'Criar praça' })).toBeInTheDocument();
    expect(screen.getByRole('button', { name: 'Criar' })).toBeInTheDocument();
    expect(screen.getByRole('dialog')).toHaveAttribute('aria-modal', 'true');
  });

  it('fecha ao pressionar Escape', () => {
    const onClose = vi.fn();
    render(
      <Modal open onClose={onClose} title="Criar praça">
        conteúdo
      </Modal>,
    );
    fireEvent.keyDown(document, { key: 'Escape' });
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it('aplica a classe de acento quando tone="danger"', () => {
    render(
      <Modal open onClose={vi.fn()} title="Revogar dispositivo?" tone="danger">
        conteúdo
      </Modal>,
    );
    expect(screen.getByRole('dialog').className).toContain('db-modal--danger');
  });

  it('fecha ao clicar no backdrop, mas não ao clicar dentro do painel', () => {
    const onClose = vi.fn();
    render(
      <Modal open onClose={onClose} title="Criar praça">
        conteúdo
      </Modal>,
    );
    fireEvent.click(screen.getByRole('dialog'));
    expect(onClose).not.toHaveBeenCalled();

    fireEvent.click(screen.getByRole('dialog').parentElement!);
    expect(onClose).toHaveBeenCalledTimes(1);
  });
});
