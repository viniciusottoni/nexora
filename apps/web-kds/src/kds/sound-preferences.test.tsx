// @vitest-environment jsdom
import '@testing-library/jest-dom/vitest';
import { cleanup, fireEvent, render, screen } from '@testing-library/react';
import type { KdsDeviceSoundPreferences } from '@nexora/contracts';
import type { KdsQueueItem, KdsThresholdState } from '@nexora/contracts';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';

vi.mock('../notifications/alert-sound.js', () => ({
  configureAlertSound: vi.fn(),
  playLateAlertChime: vi.fn(),
  previewAlertTone: vi.fn(),
}));

import { playLateAlertChime, previewAlertTone } from '../notifications/alert-sound.js';
import {
  DEFAULT_SOUND_PREFERENCES,
  SILENT_ALERT_FLASH_CLASS_NAME,
  SoundSettingsPanel,
  useSoundAlerts,
} from './sound-preferences.js';

function makeItem(overrides: Partial<KdsQueueItem> & { orderItemId: string }): KdsQueueItem {
  return {
    orderId: 'order-1',
    orderCode: 'A01',
    productName: 'Pizza Margherita',
    quantity: 1,
    modifiers: [],
    notes: null,
    status: 'QUEUED',
    placedAt: new Date().toISOString(),
    elapsedSeconds: 0,
    thresholdState: 'NORMAL' as KdsThresholdState,
    warnSeconds: 300,
    criticalSeconds: 600,
    table: null,
    channel: 'DineIn',
    fractions: [],
    ...overrides,
  } as KdsQueueItem;
}

function HookHarness({
  items,
  preferences,
}: Readonly<{ items: readonly KdsQueueItem[]; preferences: KdsDeviceSoundPreferences }>) {
  const { silentFlashItemIds } = useSoundAlerts(items, preferences);
  return <div data-testid="flash-ids">{Array.from(silentFlashItemIds).join(',')}</div>;
}

describe('useSoundAlerts (US-045)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
    vi.useRealTimers();
  });

  it('toca o alerta de atraso quando um item cruza para CRITICAL', () => {
    const critical = makeItem({ orderItemId: 'item-1', thresholdState: 'CRITICAL' });
    const { rerender } = render(
      <HookHarness items={[makeItem({ orderItemId: 'item-1', thresholdState: 'WARNING' })]} preferences={DEFAULT_SOUND_PREFERENCES} />,
    );
    expect(playLateAlertChime).not.toHaveBeenCalled();

    rerender(<HookHarness items={[critical]} preferences={DEFAULT_SOUND_PREFERENCES} />);
    expect(playLateAlertChime).toHaveBeenCalledTimes(1);
  });

  it('não repete o alerta a cada re-render enquanto o item permanece CRITICAL sem passar do intervalo', () => {
    const critical = makeItem({ orderItemId: 'item-1', thresholdState: 'CRITICAL' });
    const { rerender } = render(<HookHarness items={[critical]} preferences={DEFAULT_SOUND_PREFERENCES} />);
    expect(playLateAlertChime).toHaveBeenCalledTimes(1);

    // Mesmo item, nova referência de array (como cada poll de kds-queue-page.tsx produz) — ainda CRITICAL, não é uma NOVA transição.
    rerender(<HookHarness items={[{ ...critical }]} preferences={DEFAULT_SOUND_PREFERENCES} />);
    expect(playLateAlertChime).toHaveBeenCalledTimes(1);
  });

  it('repete o alerta de atraso no intervalo configurado (lateRepeatSeconds) enquanto o item continuar crítico', () => {
    vi.useFakeTimers();
    const critical = makeItem({ orderItemId: 'item-1', thresholdState: 'CRITICAL' });
    const preferences: KdsDeviceSoundPreferences = { ...DEFAULT_SOUND_PREFERENCES, lateRepeatSeconds: 5 };
    render(<HookHarness items={[critical]} preferences={preferences} />);
    expect(playLateAlertChime).toHaveBeenCalledTimes(1);

    vi.advanceTimersByTime(4000);
    expect(playLateAlertChime).toHaveBeenCalledTimes(1); // ainda não passou 5s

    vi.advanceTimersByTime(1500);
    expect(playLateAlertChime).toHaveBeenCalledTimes(2); // passou 5s desde o primeiro disparo

    vi.advanceTimersByTime(5000);
    expect(playLateAlertChime).toHaveBeenCalledTimes(3); // repetiu de novo, nunca contínuo
  });

  it('modo silencioso: item que cruza para CRITICAL não toca som, mas ganha reforço visual', () => {
    const silentPreferences: KdsDeviceSoundPreferences = { ...DEFAULT_SOUND_PREFERENCES, enabled: false };
    const { rerender } = render(
      <HookHarness items={[makeItem({ orderItemId: 'item-1', thresholdState: 'WARNING' })]} preferences={silentPreferences} />,
    );
    rerender(
      <HookHarness items={[makeItem({ orderItemId: 'item-1', thresholdState: 'CRITICAL' })]} preferences={silentPreferences} />,
    );

    expect(playLateAlertChime).not.toHaveBeenCalled();
    expect(screen.getByTestId('flash-ids')).toHaveTextContent('item-1');
  });

  it('modo silencioso: pedido novo também recebe reforço visual (sem duplicar o som, que já é da página)', () => {
    const silentPreferences: KdsDeviceSoundPreferences = { ...DEFAULT_SOUND_PREFERENCES, enabled: false };
    const { rerender } = render(<HookHarness items={[]} preferences={silentPreferences} />);

    rerender(<HookHarness items={[makeItem({ orderItemId: 'item-novo' })]} preferences={silentPreferences} />);

    expect(screen.getByTestId('flash-ids')).toHaveTextContent('item-novo');
  });
});

describe('SoundSettingsPanel (US-045 §3.1/§10)', () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(() => {
    cleanup();
  });

  it('renderiza os controles e propaga mudanças via onChange', () => {
    const onChange = vi.fn();
    render(
      <SoundSettingsPanel open onClose={vi.fn()} preferences={DEFAULT_SOUND_PREFERENCES} onChange={onChange} />,
    );

    fireEvent.click(screen.getByRole('switch', { name: /Som ativado/i }));
    expect(onChange).toHaveBeenCalledWith({ enabled: false });

    fireEvent.change(screen.getByLabelText('Volume do som do KDS'), { target: { value: '0.5' } });
    expect(onChange).toHaveBeenCalledWith({ volume: 0.5 });

    fireEvent.change(screen.getByLabelText('Intervalo de repetição do alerta de atraso, em segundos'), {
      target: { value: '90' },
    });
    expect(onChange).toHaveBeenCalledWith({ lateRepeatSeconds: 90 });
  });

  it('"testar som" toca o timbre configurado imediatamente, sem esperar evento real', () => {
    render(
      <SoundSettingsPanel open onClose={vi.fn()} preferences={DEFAULT_SOUND_PREFERENCES} onChange={vi.fn()} />,
    );

    fireEvent.click(screen.getAllByRole('button', { name: /Testar/i })[0]!);
    expect(previewAlertTone).toHaveBeenCalledWith('CHIME', DEFAULT_SOUND_PREFERENCES.volume);

    fireEvent.click(screen.getAllByRole('button', { name: /Testar/i })[1]!);
    expect(previewAlertTone).toHaveBeenCalledWith('ALERT', DEFAULT_SOUND_PREFERENCES.volume);
  });

  it('não renderiza nada quando open é false', () => {
    render(
      <SoundSettingsPanel open={false} onClose={vi.fn()} preferences={DEFAULT_SOUND_PREFERENCES} onChange={vi.fn()} />,
    );
    expect(screen.queryByText('Som da cozinha')).not.toBeInTheDocument();
  });
});

describe('exports auxiliares', () => {
  it('expõe a classe de reforço visual e os defaults do contrato de API (US-045 §7)', () => {
    expect(SILENT_ALERT_FLASH_CLASS_NAME).toBe('kds-sound-alert-flash');
    expect(DEFAULT_SOUND_PREFERENCES).toEqual({
      enabled: true,
      volume: 0.8,
      newOrderTone: 'CHIME',
      lateTone: 'ALERT',
      lateRepeatSeconds: 60,
    });
  });
});
