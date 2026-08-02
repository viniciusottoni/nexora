# ADR-012 — Estratégia de geração de treino híbrida

Status: Aceito

## Contexto

O AWAKEN promete personalização real. O app não pode gerar treinos incompatíveis com limitações, equipamentos, nível ou tempo disponível. Usar apenas IA generativa aumenta risco de treino incorreto, inconsistente ou perigoso.

## Decisão

Usar geração híbrida de treino: base curada de exercícios, regras determinísticas, apoio de IA e validação final determinística.

## Implementação

- Criar catálogo próprio de exercícios.
- Classificar exercícios por nível, grupo muscular, equipamento, objetivo, impacto e contraindicações.
- Filtrar exercícios antes de montar o treino.
- Usar IA apenas para ajudar na composição, explicação ou variação.
- Validar resultado final contra regras do perfil do usuário.
- Rejeitar treino gerado se violar limitação física ou equipamento disponível.
- Registrar motivo de rejeição para melhoria.

## Consequências

A personalização fica mais confiável e o app evita a percepção de IA fake. A equipe precisa investir em catálogo de exercícios bem estruturado.

## Critérios de aceite

- Usuário sem equipamento recebe treino sem equipamento.
- Iniciante não recebe treino avançado.
- Limitações físicas são respeitadas.
- Falha de IA não impede uso do app.
