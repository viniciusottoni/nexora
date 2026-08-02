// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { SyncStatus } from './sync-status.js';

describe('SyncStatus', () => {
  it('mostra sincronizado por padrão, sem sugerir dado defasado como tempo real', () => {
    render(<SyncStatus lastSync="há 4 s" />);

    const status = screen.getByText('Sincronizado');
    expect(status).toHaveClass('db-sync-status--online');
    expect(status).toHaveAttribute('title', 'Última sincronização há 4 s');
    expect(screen.getByText('· há 4 s')).toBeInTheDocument();
  });

  it('expõe honestamente o modo local e a fila pendente', () => {
    render(<SyncStatus state="local" queued={38} />);

    const status = screen.getByText('Modo local');
    expect(status).toHaveClass('db-sync-status--local');
    expect(screen.getByText('· 38 na fila')).toBeInTheDocument();
  });

  it('sinaliza sync atrasada', () => {
    render(<SyncStatus state="delayed" />);

    expect(screen.getByText('Sync atrasada')).toHaveClass('db-sync-status--delayed');
  });
});
