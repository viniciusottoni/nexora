# ADR-013 — Treino de reserva

Status: Aceito

## Contexto

O AWAKEN precisa entregar uma quest diária mesmo quando a geração principal de treino não estiver disponível.

## Decisão

Manter modelos de treino de reserva para o MVP.

## Implementação

- Criar modelos por objetivo, nível, local e equipamento.
- Validar o perfil antes de entregar o treino.
- Permitir edição antes de iniciar a quest.
- Registrar quando o modelo de reserva for usado.

## Consequências

O app fica mais estável e evita tela de erro na jornada principal.

## Critérios de aceite

- Existe modelo para iniciante em casa.
- Existe modelo para academia.
- O treino respeita limitações e equipamentos.
- O evento de uso é registrado.
