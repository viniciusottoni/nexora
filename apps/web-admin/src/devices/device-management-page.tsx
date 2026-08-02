import { useId, useState } from 'react';
import { Button, Card, Field, Input } from '@nexora/ui';
import type { DeviceDto } from '@nexora/contracts';
import './devices.css';

export interface DeviceManagementPageProps {
  readonly devices: readonly DeviceDto[];
  readonly onCreatePairingCode?: () => Promise<Readonly<{ code: string; expiresAt: string }>>;
  readonly onRename: (id: string, label: string) => Promise<void>;
  readonly onRevoke: (id: string) => Promise<void>;
}

const kindLabels: Readonly<Record<DeviceDto['kind'], string>> = {
  CASHIER: 'Terminal de caixa',
  KDS: 'Tela da cozinha',
  WAITER: 'Celular de garçom',
  SUPPORT_TABLET: 'Tablet de apoio',
};

export function DeviceManagementPage({
  devices,
  onCreatePairingCode,
  onRename,
  onRevoke,
}: Readonly<DeviceManagementPageProps>) {
  const renameFieldId = useId();
  const [pairing, setPairing] = useState<Readonly<{ code: string; expiresAt: string }>>();
  const [busyCode, setBusyCode] = useState(false);
  const [revoking, setRevoking] = useState<DeviceDto>();
  const [renaming, setRenaming] = useState<DeviceDto>();
  const [newLabel, setNewLabel] = useState('');

  async function createCode() {
    if (!onCreatePairingCode) return;
    setBusyCode(true);
    try {
      setPairing(await onCreatePairingCode());
    } finally {
      setBusyCode(false);
    }
  }

  async function confirmRevoke() {
    if (!revoking) return;
    await onRevoke(revoking.id);
    setRevoking(undefined);
  }

  async function confirmRename() {
    if (!renaming || !newLabel.trim()) return;
    await onRename(renaming.id, newLabel.trim());
    setRenaming(undefined);
    setNewLabel('');
  }

  return (
    <main className="devices-shell" aria-labelledby="devices-title">
      <header className="devices-header">
        <div>
          <p className="devices-eyebrow">ACESSO À OPERAÇÃO</p>
          <h1 id="devices-title">Dispositivos autorizados</h1>
          <p className="devices-lead">
            Veja quem acessa a loja e retire imediatamente terminais perdidos.
          </p>
        </div>
        {onCreatePairingCode ? (
          <Button type="button" busy={busyCode} onClick={() => void createCode()}>
            Autorizar novo dispositivo
          </Button>
        ) : (
          <p className="devices-local-note">
            Novo pareamento deve ser criado no painel local da loja.
          </p>
        )}
      </header>

      {pairing ? (
        <Card className="pairing-code-panel" role="status" aria-live="polite">
          <div>
            <p className="devices-eyebrow">CÓDIGO DE USO ÚNICO</p>
            <strong
              className="pairing-code-panel__code"
              aria-label={`Código ${pairing.code.split('').join(' ')}`}
            >
              {pairing.code}
            </strong>
          </div>
          <p>
            Expira às <time dateTime={pairing.expiresAt}>{formatTime(pairing.expiresAt)}</time>.
            Digite no novo terminal.
          </p>
        </Card>
      ) : null}

      <section className="device-grid" aria-label="Lista de dispositivos">
        {devices.map((device) => (
          <Card
            className={`device-card ${device.active ? '' : 'device-card--inactive'}`}
            key={device.id}
          >
            <div className="device-card__topline">
              <span
                className={`device-status device-status--${device.active ? 'active' : 'revoked'}`}
              >
                {device.active ? 'Autorizado' : 'Revogado'}
              </span>
              <span className="device-kind">{kindLabels[device.kind]}</span>
            </div>
            <h2>{device.label}</h2>
            <p className="device-last-seen">
              <span>Último acesso:</span>{' '}
              {device.lastSeenAt ? (
                <time dateTime={device.lastSeenAt}>{formatDateTime(device.lastSeenAt)}</time>
              ) : (
                'Ainda não acessou'
              )}
            </p>
            {device.needsReview ? (
              <p className="device-review">Sem acesso há mais de 30 dias</p>
            ) : null}
            <div className="device-card__actions">
              <Button
                type="button"
                variant="ghost"
                onClick={() => {
                  setRenaming(device);
                  setNewLabel(device.label);
                }}
              >
                Renomear
              </Button>
              {device.active ? (
                <Button type="button" variant="danger" onClick={() => setRevoking(device)}>
                  Revogar {device.label}
                </Button>
              ) : null}
            </div>
          </Card>
        ))}
      </section>

      {renaming ? (
        <div className="devices-dialog-backdrop">
          <section
            className="devices-dialog"
            role="dialog"
            aria-modal="true"
            aria-labelledby="rename-title"
          >
            <h2 id="rename-title">Renomear dispositivo</h2>
            <Field label="Nome do dispositivo" htmlFor={renameFieldId}>
              <Input
                id={renameFieldId}
                value={newLabel}
                onChange={(event) => setNewLabel(event.target.value)}
              />
            </Field>
            <div className="devices-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setRenaming(undefined)}>
                Cancelar
              </Button>
              <Button type="button" onClick={() => void confirmRename()}>
                Salvar nome
              </Button>
            </div>
          </section>
        </div>
      ) : null}

      {revoking ? (
        <div className="devices-dialog-backdrop">
          <section
            className="devices-dialog devices-dialog--danger"
            role="dialog"
            aria-modal="true"
            aria-labelledby="revoke-title"
          >
            <p className="devices-eyebrow">AÇÃO IMEDIATA</p>
            <h2 id="revoke-title">Revogar dispositivo?</h2>
            <p>
              <strong>{revoking.label}</strong> perderá acesso. Todas as sessões ativas serão
              encerradas imediatamente.
            </p>
            <div className="devices-dialog__actions">
              <Button type="button" variant="ghost" onClick={() => setRevoking(undefined)}>
                Manter acesso
              </Button>
              <Button type="button" variant="danger" onClick={() => void confirmRevoke()}>
                Sim, revogar dispositivo
              </Button>
            </div>
          </section>
        </div>
      ) : null}
    </main>
  );
}

function formatTime(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { hour: '2-digit', minute: '2-digit' }).format(
    new Date(value),
  );
}

function formatDateTime(value: string): string {
  return new Intl.DateTimeFormat('pt-BR', { dateStyle: 'short', timeStyle: 'short' }).format(
    new Date(value),
  );
}
