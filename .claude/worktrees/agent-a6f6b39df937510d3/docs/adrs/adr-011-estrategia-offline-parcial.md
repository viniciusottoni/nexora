# ADR-011 — Estratégia offline parcial

Status: Aceito

## Contexto

Usuários podem iniciar ou executar treino com conexão instável. O AWAKEN não precisa ser 100% offline no MVP, mas não deve quebrar a experiência durante uma quest já carregada.

## Decisão

Adotar offline parcial: leitura de dados recentes em cache e fila local para sincronizar operações simples.

## Implementação

- Usar Drift como banco local.
- Cachear perfil, quest do dia, execução em andamento, histórico recente e card do hunter.
- Criar fila local `sync_queue` para operações pendentes.
- Usar `idempotencyKey` nas operações enviadas depois.
- Mostrar status “sincronizando” quando houver pendência.
- Bloquear geração de nova quest sem conexão.
- Não conceder XP oficial antes da confirmação do backend.

## Consequências

O app fica mais resistente e agradável. A equipe deve controlar conflitos e evitar que dados locais sejam tratados como verdade final.

## Critérios de aceite

- Usuário consegue visualizar quest já carregada sem internet.
- Usuário consegue registrar execução localmente.
- App sincroniza conclusão quando a conexão volta.
- Falha de sincronização mostra mensagem clara.
