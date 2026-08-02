export interface OperatorBarProps {
  readonly userName: string;
  readonly onSwitchOperator: () => void;
}

export function OperatorBar({ userName, onSwitchOperator }: Readonly<OperatorBarProps>) {
  return (
    <aside className="db-operator-bar" aria-label="Operador atual">
      <span className="db-operator-avatar" aria-hidden="true">
        {userName.charAt(0).toUpperCase()}
      </span>
      <div>
        <small>Operador</small>
        <strong>{userName}</strong>
      </div>
      <button type="button" onClick={onSwitchOperator}>
        Trocar operador
      </button>
    </aside>
  );
}
