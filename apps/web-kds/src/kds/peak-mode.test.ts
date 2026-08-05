import { describe, expect, it } from 'vitest';
import { DEFAULT_PEAK_MODE_THRESHOLDS, resolvePeakMode } from './peak-mode.js';

const THRESHOLD = 20;
const HYSTERESIS = 5;

describe('resolvePeakMode (US-047 §4/§10 — histerese)', () => {
  describe('ativação automática (Cenário "Ativação automática", §4)', () => {
    it('permanece inativo abaixo do limiar', () => {
      expect(resolvePeakMode(false, THRESHOLD - 1, THRESHOLD, HYSTERESIS)).toBe(false);
    });

    it('ativa exatamente ao atingir o limiar', () => {
      expect(resolvePeakMode(false, THRESHOLD, THRESHOLD, HYSTERESIS)).toBe(true);
    });

    it('ativa acima do limiar', () => {
      expect(resolvePeakMode(false, 30, THRESHOLD, HYSTERESIS)).toBe(true);
    });
  });

  describe('desativação automática com histerese (Cenário "Desativação automática", §4)', () => {
    it('permanece ativo dentro da faixa de histerese (19 a 15, para limiar 20/histerese 5)', () => {
      for (let orderCount = THRESHOLD - 1; orderCount >= THRESHOLD - HYSTERESIS; orderCount--) {
        expect(resolvePeakMode(true, orderCount, THRESHOLD, HYSTERESIS)).toBe(true);
      }
    });

    it('desativa só ao cair abaixo do limiar menos a histerese (14, para limiar 20/histerese 5)', () => {
      expect(resolvePeakMode(true, THRESHOLD - HYSTERESIS - 1, THRESHOLD, HYSTERESIS)).toBe(false);
    });

    it('desativa quando a fila cai bem abaixo do limiar (cenário da história: 20 → 12)', () => {
      expect(resolvePeakMode(true, 12, THRESHOLD, HYSTERESIS)).toBe(false);
    });

    it('permanece desativado depois de já ter desativado, mesmo que a fila oscile dentro da faixa baixa', () => {
      expect(resolvePeakMode(false, 16, THRESHOLD, HYSTERESIS)).toBe(false);
    });
  });

  describe('não oscila entre os dois modos (o critério mais citado na história, §10/§12)', () => {
    it('uma sequência de contagens crescendo e decrescendo dentro da faixa de histerese não troca de modo a cada item', () => {
      // Simula fila crescendo item a item de 0 até o limiar, oscilando exatamente na borda de
      // baixo (15..19) várias vezes, e só então recuando de vez — o modo pico deve ligar UMA vez
      // (ao cruzar 20) e desligar UMA vez (ao cruzar para 14), nunca mais que isso.
      const sequence = [10, 15, 18, 19, 20, 19, 18, 17, 16, 15, 16, 17, 18, 19, 17, 15, 14, 15, 12];
      let active = false;
      const transitions: boolean[] = [];
      for (const count of sequence) {
        const next = resolvePeakMode(active, count, THRESHOLD, HYSTERESIS);
        if (next !== active) transitions.push(next);
        active = next;
      }

      // Uma ativação (ao alcançar 20) e uma desativação (ao cair para 14) — nenhuma outra troca,
      // apesar da fila ter subido e descido repetidamente dentro da faixa 15–19.
      expect(transitions).toEqual([true, false]);
    });

    it('chamadas repetidas com a MESMA contagem nunca alternam o resultado (estabilidade sob re-render)', () => {
      // `resolvePeakMode` é chamado a cada mudança de fila com o próprio resultado anterior como
      // `currentlyActive` — se a fila ficar parada, o resultado tem que ficar parado também.
      let active = resolvePeakMode(false, 20, THRESHOLD, HYSTERESIS);
      expect(active).toBe(true);
      for (let i = 0; i < 5; i++) {
        active = resolvePeakMode(active, 20, THRESHOLD, HYSTERESIS);
        expect(active).toBe(true);
      }

      let inactive = resolvePeakMode(true, 10, THRESHOLD, HYSTERESIS);
      expect(inactive).toBe(false);
      for (let i = 0; i < 5; i++) {
        inactive = resolvePeakMode(inactive, 10, THRESHOLD, HYSTERESIS);
        expect(inactive).toBe(false);
      }
    });

    it('histerese zero (degenerada) não oscila em chamadas repetidas na borda exata do limiar', () => {
      // Caso limite: sem histerese, o limiar de ativar e o de desativar colapsam no mesmo valor.
      // Mesmo assim, chamar de novo com o resultado anterior no MESMO orderCount não pode alternar
      // — é exatamente o bug que o limite estrito (`<`) em `resolvePeakMode` evita.
      let active = false;
      for (let i = 0; i < 5; i++) {
        active = resolvePeakMode(active, THRESHOLD, THRESHOLD, 0);
        expect(active).toBe(true);
      }

      let alsoActive = true;
      for (let i = 0; i < 5; i++) {
        alsoActive = resolvePeakMode(alsoActive, THRESHOLD, THRESHOLD, 0);
        expect(alsoActive).toBe(true);
      }
    });

    it('histerese zero ainda reage a mudanças reais de contagem (não é um modo "sempre ligado")', () => {
      expect(resolvePeakMode(true, THRESHOLD - 1, THRESHOLD, 0)).toBe(false);
      expect(resolvePeakMode(false, THRESHOLD, THRESHOLD, 0)).toBe(true);
    });
  });

  describe('casos extremos', () => {
    it('fila vazia nunca ativa o modo pico', () => {
      expect(resolvePeakMode(false, 0, THRESHOLD, HYSTERESIS)).toBe(false);
    });

    it('histerese maior que o limiar apenas alarga a faixa (nunca fica negativa por baixo de zero pedidos)', () => {
      expect(resolvePeakMode(true, 0, THRESHOLD, THRESHOLD + 10)).toBe(true);
      expect(resolvePeakMode(true, -1 + 1, THRESHOLD, THRESHOLD + 10)).toBe(true);
    });
  });

  it('DEFAULT_PEAK_MODE_THRESHOLDS reflete o contrato de API da história (§7): limiar 20, histerese 5', () => {
    expect(DEFAULT_PEAK_MODE_THRESHOLDS).toEqual({ thresholdItems: 20, hysteresisItems: 5 });
  });
});
