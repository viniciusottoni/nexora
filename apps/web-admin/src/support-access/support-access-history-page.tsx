import { useState } from 'react';
import { AlertBanner, Badge, Button, Card, DataTable, EmptyState } from '@nexora/ui';
import type { SupportAccessSummary } from '@nexora/contracts';
import './support-access.css';

export interface SupportAccessHistoryPageProps {
  readonly grants: readonly SupportAccessSummary[];
  readonly onRevoke: (id: string) => Promise<void>;
}

/**
 * US-145 §10 "Histórico de acessos sempre disponível ao cliente, sem precisar pedir" +
 * "Revogação em um clique". Lista somente-leitura (quem/quando/duração/motivo/situação) com um
 * botão de revogar por linha ativa — segue `category-management-page.tsx` como template de tela
 * de lista com `Card`/`DataTable`.
 */
export function SupportAccessHistoryPage({
  grants,
  onRevoke,
}: Readonly<SupportAccessHistoryPageProps>) {
  const [busyId, setBusyId] = useState<string>();
  const [error, setError] = useState<string>();
  const [notice, setNotice] = useState<string>();

  const ordered = [...grants].sort(
    (a, b) => new Date(b.grantedAt).getTime() - new Date(a.grantedAt).getTime(),
  );

  async function revoke(grant: SupportAccessSummary) {
    setBusyId(grant.id);
    setError(undefined);
    try {
      await onRevoke(grant.id);
      setNotice('Acesso de suporte revogado. Ele deixa de valer imediatamente.');
    } catch (reason) {
      setError(toMessage(reason));
    } finally {
      setBusyId(undefined);
    }
  }

  return (
    <main className="db-page nx-anim-in" aria-labelledby="support-access-history-title">
      <header className="db-page__header">
        <div className="db-page__heading">
          <p className="db-page__eyebrow">Segurança</p>
          <h1 className="db-page__title" id="support-access-history-title">
            Acessos de suporte
          </h1>
          <p className="db-page__lead">
            Todo acesso da equipe da Nexora aos dados do seu estabelecimento fica registrado aqui
            — quem acessou, quando, por quanto tempo e por quê. Você pode revogar a qualquer
            momento.
          </p>
        </div>
      </header>

      {notice ? <AlertBanner tone="success">{notice}</AlertBanner> : null}
      {error ? <AlertBanner tone="danger">{error}</AlertBanner> : null}

      {ordered.length === 0 ? (
        <EmptyState icon="verified_user" title="Nenhum acesso de suporte registrado">
          Quando a equipe de suporte precisar acessar seus dados, o pedido aparecerá aqui.
        </EmptyState>
      ) : (
        <Card
          padding="none"
          title="Histórico de acessos"
          subtitle="Mais recente primeiro."
        >
          <DataTable
            rowKey="id"
            rows={ordered}
            columns={[
              {
                key: 'reason',
                header: 'Motivo',
                render: (row) => <span className="support-access-reason">{row.reason}</span>,
              },
              {
                key: 'grantedAt',
                header: 'Concedido em',
                render: (row) => new Date(row.grantedAt).toLocaleString('pt-BR'),
              },
              {
                key: 'durationMinutes',
                header: 'Duração',
                align: 'right',
                render: (row) => `${row.durationMinutes} min`,
              },
              {
                key: 'status',
                header: 'Situação',
                render: (row) =>
                  row.revokedAt ? (
                    <Badge tone="neutral">Revogado</Badge>
                  ) : row.isActive ? (
                    <Badge tone="warning">Ativo</Badge>
                  ) : (
                    <Badge tone="neutral">Expirado</Badge>
                  ),
              },
              {
                key: 'actions',
                header: '',
                render: (row) =>
                  row.isActive ? (
                    <Button
                      type="button"
                      variant="ghost"
                      size="sm"
                      busy={busyId === row.id}
                      onClick={(event) => {
                        event.stopPropagation();
                        void revoke(row);
                      }}
                    >
                      Revogar
                    </Button>
                  ) : null,
              },
            ]}
          />
        </Card>
      )}
    </main>
  );
}

function toMessage(reason: unknown): string {
  return reason instanceof Error ? reason.message : 'Não foi possível concluir a operação.';
}
