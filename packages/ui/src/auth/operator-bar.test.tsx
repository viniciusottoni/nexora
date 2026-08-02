// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { fireEvent, render, screen } from '@testing-library/react';
import { describe, expect, it, vi } from 'vitest';
import { OperatorBar } from './operator-bar.js';

describe('OperatorBar', () => {
  it('mantém operador visível e abre troca em um toque', () => {
    const switchOperator = vi.fn();
    render(<OperatorBar userName="Ana" onSwitchOperator={switchOperator} />);
    expect(screen.getByLabelText('Operador atual')).toHaveTextContent('Ana');
    fireEvent.click(screen.getByRole('button', { name: 'Trocar operador' }));
    expect(switchOperator).toHaveBeenCalledOnce();
  });
});
