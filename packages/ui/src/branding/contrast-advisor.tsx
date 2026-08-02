import { validateBrandingContrast } from '@nexora/contracts';

export interface ContrastAdvisorProps {
  readonly primary: string;
  readonly surface: string;
  readonly onPrimary: string;
}

export function ContrastAdvisor(props: Readonly<ContrastAdvisorProps>) {
  const result = validateBrandingContrast(props);
  if (result.valid) {
    return <p className="db-contrast db-contrast--valid">Contraste WCAG AA atendido.</p>;
  }
  return (
    <div className="db-contrast db-contrast--warning" role="alert">
      <strong>Cor com contraste WCAG AA insuficiente.</strong>
      {result.issues.map((issue) => (
        <p key={issue.pair}>
          Sugestão para {issue.pair}: <code>{issue.suggested}</code>
        </p>
      ))}
    </div>
  );
}
