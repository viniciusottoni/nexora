import { useId, useState } from 'react';
import { Badge, Button, Card, Field, Input, Modal } from '@nexora/ui';
import type { DeviceDto } from '@nexora/contracts';
import './devices.css';

export interface DeviceManagementPageProps {
  readonly devices: readonly DeviceDto[];
  readonly onCreatePairingCode?: () => Promise<Readonly<{ code: string; expiresAt: string }>>;
  readonly onRename: (id: string, label: string) => Promise<void>;
  readonly onRevoke: (id: string) => Promise<void>;
  readonly onDelete: (id: string) => Promise<void>;
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
  onDelete,
}: Readonly<DeviceManagementPageProps>) {
  const renameFieldId = useId();
  const [pairing, setPairing] = useState<Readonly<{ code: string; expiresAt: string }>>();
  const [busyCode, setBusyCode] = useState(false);
  const [revoking, setRevoking] = useState<DeviceDto>();
  const [deleting, setDeleting] = useState<DeviceDto>();
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

  async function confirmDelete() {
    if (!deleting) return;
    await onDelete(deleting.id);
    setDeleting(undefined);
  }

  async function confirmRename() {
    if (!renaming || !newLabel.trim()) return;
    await onRename(renaming.id, newLabel.trim());
    setRenaming(undefined);
    setNewLabel('');
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="devices-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Acesso à operação</p>
          <h1 className="db-page__title" id="devices-title">
            Dispositivos autorizados
          </h1>
          <p className="db-page__lead">
            Veja quem acessa a loja e retire imediatamente terminais perdidos.
          </p>
        </div>
        <div className="db-page__actions">
          {onCreatePairingCode ? (
            <Button type="button" busy={busyCode} onClick={() => void createCode()}>
              Autorizar novo dispositivo
            </Button>
          ) : (
            <p className="db-hint">Novo pareamento deve ser criado no painel local da loja.</p>
          )}
        </div>
      </header>

      {pairing ? (
        <Card className="pairing-code-panel" role="status" aria-live="polite">
          <div>
            <p className="db-page__eyebrow">Código de uso único</p>
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

      <section className="device-grid nx-stagger" aria-label="Lista de dispositivos">
        {devices.map((device) => (
          <Card
            className={`device-card ${device.active ? '' : 'device-card--inactive'}`}
            key={device.id}
          >
            <div className="device-card__topline">
              <Badge tone={device.active ? 'success' : 'neutral'} size="sm">
                {device.active ? 'Autorizado' : 'Revogado'}
              </Badge>
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
              <p className="db-hint db-hint--warning">Sem acesso há mais de 30 dias</p>
            ) : null}
            <div className="device-card__actions">
              <Button
                type="button"
                variant="secondary"
                size="sm"
                onClick={() => {
                  setRenaming(device);
                  setNewLabel(device.label);
                }}
              >
                Renomear
              </Button>
              {device.active ? (
                <Button
                  type="button"
                  variant="danger"
                  size="sm"
                  aria-label={`Revogar ${device.label}`}
                  onClick={() => setRevoking(device)}
                >
                  Revogar
                </Button>
              ) : (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  aria-label={`Excluir ${device.label}`}
                  onClick={() => setDeleting(device)}
                >
                  Excluir
                </Button>
              )}
            </div>
          </Card>
        ))}
      </section>

      <Modal
        open={renaming !== undefined}
        onClose={() => setRenaming(undefined)}
        title="Renomear dispositivo"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setRenaming(undefined)}>
              Cancelar
            </Button>
            <Button type="button" onClick={() => void confirmRename()}>
              Salvar nome
            </Button>
          </>
        }
      >
        <Field label="Nome do dispositivo" htmlFor={renameFieldId}>
          <Input
            id={renameFieldId}
            value={newLabel}
            onChange={(event) => setNewLabel(event.target.value)}
          />
        </Field>
      </Modal>

      <Modal
        open={revoking !== undefined}
        onClose={() => setRevoking(undefined)}
        tone="danger"
        eyebrow="Ação imediata"
        title="Revogar dispositivo?"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setRevoking(undefined)}>
              Manter acesso
            </Button>
            <Button type="button" variant="danger" onClick={() => void confirmRevoke()}>
              Sim, revogar dispositivo
            </Button>
          </>
        }
      >
        {revoking ? (
          <p>
            <strong>{revoking.label}</strong> perderá acesso. Todas as sessões ativas serão
            encerradas imediatamente.
          </p>
        ) : null}
      </Modal>

      <Modal
        open={deleting !== undefined}
        onClose={() => setDeleting(undefined)}
        tone="danger"
        eyebrow="Limpeza da lista"
        title="Excluir dispositivo?"
        actions={
          <>
            <Button type="button" variant="ghost" onClick={() => setDeleting(undefined)}>
              Cancelar
            </Button>
            <Button type="button" variant="danger" onClick={() => void confirmDelete()}>
              Sim, excluir dispositivo
            </Button>
          </>
        }
      >
        {deleting ? (
          <p>
            <strong>{deleting.label}</strong> sai desta lista. O acesso já foi revogado; isto só
            remove o registro da listagem.
          </p>
        ) : null}
      </Modal>
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
