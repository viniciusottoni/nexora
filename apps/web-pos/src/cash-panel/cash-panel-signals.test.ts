import { describe, expect, it } from 'vitest';
import { formatPendingItems, formatSessionsSubtitle, formatWaitingSince } from './cash-panel-signals.js';

describe('formatWaitingSince', () => {
  it('formata segundos abaixo de um minuto em "s"', () => {
    expect(formatWaitingSince(42)).toBe('há 42 s');
  });

  it('formata a partir de um minuto em "min" arredondado', () => {
    expect(formatWaitingSince(180)).toBe('há 3 min');
  });

  it('nunca fica negativo mesmo com relógio levemente adiantado no servidor', () => {
    expect(formatWaitingSince(-5)).toBe('há 0 s');
  });
});

describe('formatPendingItems', () => {
  it('sinaliza quando não há pendência', () => {
    expect(formatPendingItems(0)).toBe('Nenhum pendente');
  });

  it('usa singular para 1 item', () => {
    expect(formatPendingItems(1)).toBe('1 item pendente');
  });

  it('usa plural para mais de 1 item', () => {
    expect(formatPendingItems(3)).toBe('3 itens pendentes');
  });
});

describe('formatSessionsSubtitle', () => {
  it('concorda no singular sem busca ativa', () => {
    expect(formatSessionsSubtitle(1, false)).toBe('1 sessão aberta');
  });

  it('concorda no plural sem busca ativa', () => {
    expect(formatSessionsSubtitle(14, false)).toBe('14 sessões abertas');
  });

  it('concorda no singular com busca ativa', () => {
    expect(formatSessionsSubtitle(1, true)).toBe('1 sessão encontrada');
  });

  it('concorda no plural (inclusive zero) com busca ativa', () => {
    expect(formatSessionsSubtitle(0, true)).toBe('0 sessões encontradas');
  });
});
