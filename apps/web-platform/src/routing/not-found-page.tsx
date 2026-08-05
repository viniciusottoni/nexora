import { Button, EmptyState } from '@nexora/ui';

export interface NotFoundPageProps {
  readonly onGoToOverview: () => void;
}

/** US-150 §4/§10 — estado global "recurso inexistente": mantém a casca (é uma rota válida da própria plataforma, só não reconhecida), sem inventar navegação nova. */
export function NotFoundPage({ onGoToOverview }: Readonly<NotFoundPageProps>) {
  return (
    <main className="db-page nx-anim-in">
      <EmptyState icon="search_off" title={<h1>Página não encontrada</h1>}>
        <p>Este endereço não existe no painel da plataforma.</p>
        <Button type="button" onClick={onGoToOverview}>
          Voltar à visão geral
        </Button>
      </EmptyState>
    </main>
  );
}
