// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { PeakModeBanner } from './peak-mode-banner.js';

describe('PeakModeBanner (US-047 §4/§10)', () => {
  afterEach(() => {
    cleanup();
  });

  it('não renderiza nada quando o modo pico nunca foi ativado nem desativado manualmente', () => {
    const { container } = render(<PeakModeBanner active={false} manuallyDisabled={false} onToggle={vi.fn()} />);
    expect(container).toBeEmptyDOMElement();
  });

  it('mostra indicação clara de que o modo pico está ativo, com botão para desativar', () => {
    const onToggle = vi.fn();
    render(<PeakModeBanner active manuallyDisabled={false} onToggle={onToggle} />);

    expect(screen.getByText('Modo pico ativo')).toBeInTheDocument();
    expect(screen.getByText(/mostram só código, produto, quantidade e tempo/)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Desativar modo pico/ }));
    expect(onToggle).toHaveBeenCalledOnce();
  });

  it('mostra aviso discreto de desativação manual, com botão para reativar', () => {
    const onToggle = vi.fn();
    render(<PeakModeBanner active={false} manuallyDisabled onToggle={onToggle} />);

    expect(screen.getByText('Modo pico desativado manualmente')).toBeInTheDocument();
    expect(screen.getByText(/vai continuar desligado até você reativar/i)).toBeInTheDocument();

    fireEvent.click(screen.getByRole('button', { name: /Reativar/ }));
    expect(onToggle).toHaveBeenCalledOnce();
  });

  it('prioriza o estado ativo quando as duas flags chegam simultaneamente (não deveria acontecer, mas não deve travar)', () => {
    render(<PeakModeBanner active manuallyDisabled onToggle={vi.fn()} />);
    expect(screen.getByText('Modo pico ativo')).toBeInTheDocument();
    expect(screen.queryByText('Modo pico desativado manualmente')).not.toBeInTheDocument();
  });
});
