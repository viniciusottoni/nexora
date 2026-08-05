import { Button, CreatedByFooter, EmptyState } from '@nexora/ui';

export interface AccessDeniedPageProps {
  readonly onSignOut: () => void;
}

/**
 * US-150 §4, cenário "Acesso direto a uma rota protegida" — usuário autenticado sem a policy
 * `PlatformAdmin`. Renderiza SOZINHA, sem a casca (`db-console`/`SideNav`/`TopBar`): RN-015 exige
 * "sem qualquer dado de tenant" e a forma mais segura de garantir isso é nunca montar o shell da
 * plataforma (que já dispara chamadas de dado) até a autorização ser confirmada.
 */
export function AccessDeniedPage({ onSignOut }: Readonly<AccessDeniedPageProps>) {
  return (
    <main className="db-cloud-login">
      <EmptyState icon="block" title={<h1>Acesso negado</h1>}>
        <p role="alert">
          Sua conta não tem permissão de administrador da plataforma Replay. Se você acredita que
          isso é um engano, fale com quem administra sua equipe.
        </p>
        <Button type="button" variant="secondary" onClick={onSignOut}>
          Sair
        </Button>
      </EmptyState>
      <CreatedByFooter />
    </main>
  );
}
