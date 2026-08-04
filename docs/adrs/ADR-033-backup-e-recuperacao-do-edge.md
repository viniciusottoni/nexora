# ADR-033 · Backup e recuperação do servidor local

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps, PO |
| **Relacionados** | ADR-001, ADR-007, ADR-019, ADR-027 |
| **Requisitos afetados** | RNF-DIS-04, RNF-DIS-05, RNF-IMP-06, RNF-IMP-07 |

---

## Contexto

O ADR-001 colocou um servidor físico dentro da loja. Isso resolveu a dependência de internet e criou um novo modo de falha, identificado como **risco T1 (impacto crítico)** na arquitetura: o equipamento pode queimar, o disco pode falhar, a energia pode oscilar — numa sexta-feira à noite, com o salão cheio e ninguém tecnicamente capacitado no local.

Esta é a pergunta que o cliente vai fazer, e ela precisa de resposta antes do piloto: **"e se esse computador quebrar?"**

## Decisão

**Três camadas de proteção**, com objetivos declarados de RTO menor que 30 minutos e RPO igual a zero para evento já sincronizado:

1. **Prevenção** — nobreak, disco monitorado, atualização com backup prévio
2. **Backup** — local a cada hora, remoto a cada 6 horas
3. **Recuperação** — equipamento reserva pré-configurado e runbook ensaiado

## Detalhamento

### Camada 1 — Prevenção

| Medida | Detalhe |
|---|---|
| Nobreak | Obrigatório; mínimo 600 VA, recomendado 1200 VA |
| Desligamento gracioso | Ao detectar bateria baixa via USB |
| Monitoramento de disco | SMART e uso reportados no heartbeat (ADR-022) |
| Alerta de disco | Acima de 80% de uso |
| Backup antes de atualizar | Obrigatório (ADR-019) |

### Camada 2 — Backup

```
A cada hora (local)
  pg_dump comprimido → /var/backups/dona-betinha/hourly/
  Retenção: 24 arquivos

A cada 6 horas (remoto)
  dump comprimido e criptografado → object storage
  Retenção: 30 dias

Diário (remoto)
  dump completo → object storage, retenção 90 dias
```

Backup remoto é criptografado com a chave do tenant (ADR-031) antes de sair da loja.

### Camada 3 — Recuperação

**Cenário A — falha de software (mais comum)**

```
1. Watchdog detecta serviço fora        (< 30 s)
2. Reinicia o container                 (< 60 s)
3. Não resolveu? → alerta à Replay
4. Suporte remoto atua                  (< 15 min)
```

**Cenário B — falha de hardware**

```
1. Instalação some do monitoramento     → alerta imediato
2. Operação entra em contingência (ADR-027) e o gerente aciona o procedimento manual
3. Equipamento reserva é ligado
   ├─ imagem já instalada e pareada
   ├─ restaura o último backup local (do disco antigo, se acessível)
   │  ou o último backup remoto
   └─ sobe em menos de 15 min
4. Eventos não sincronizados que existiam apenas no disco antigo:
   recuperados se o disco estiver íntegro; caso contrário, perdidos
```

> Ponto de honestidade: **RPO zero vale para o que já sincronizou.** Evento gerado offline e ainda não enviado, em disco fisicamente destruído, é perda real. É por isso que o backup local é horário e o remoto de 6 em 6 horas — para reduzir essa janela ao mínimo praticável.

**Cenário C — corrupção de dados**

```
1. Verificação de integridade diária detecta (doc. 10, §6.1)
2. Suporte avalia a extensão
3. Restauração pontual ou completa, conforme o caso
4. Reconciliação com a nuvem: eventos já sincronizados são reaplicados
```

### Equipamento reserva

| Item | Definição |
|---|---|
| Quem mantém | **[PENDÊNCIA]** — Replay ou cliente; definir em contrato |
| Onde fica | Na loja, desligado |
| Estado | Imagem instalada, pareado, atualizado mensalmente |
| Custo | Aproximadamente o de um mini-PC |
| Alternativa | Estoque na Replay, com entrega em até 4 h para lojas próximas |

**Recomendação:** para o piloto, equipamento reserva na própria loja. É a única forma de cumprir RTO de 30 minutos.

### Runbook

Documento impresso e plastificado, deixado na loja, com o procedimento passo a passo. **Deve ser ensaiado antes da etapa P3 do piloto** (doc. 09, §7) — o gerente precisa ter feito o procedimento pelo menos uma vez, com acompanhamento, antes de precisar dele de verdade.

### Restauração testada

Trimestralmente, em ambiente de caos: restaurar backup real, subir, verificar integridade e medir o tempo. Backup não testado não é backup.

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Sem reserva, comprar quando falhar | Custo zero antecipado | RTO de dias | Loja parada é inaceitável |
| Dois edges em alta disponibilidade | RTO próximo de zero | Dobra custo e complexidade em cada loja | Desproporcional ao porte |
| Só backup remoto | Simples | Restauração depende de internet — que pode ser justamente o problema | Frágil no pior cenário |
| Réplica em tempo real para a nuvem | Sempre atual | Depende de internet contínua | Contraria a premissa do produto |
| Voltar ao papel na falha | Custo zero | Perde tudo o que o sistema criou; retrabalho enorme | Aceitável apenas como último recurso |

## Consequências

**Positivas**

- Resposta concreta à pergunta que o cliente certamente fará
- RTO de 30 min com reserva local
- Janela de perda limitada a 1 hora no pior caso
- Restauração testada trimestralmente

**Negativas**

- Custo do equipamento reserva
- Backup horário consome disco e I/O
- Runbook exige treinamento e reciclagem
- **[PENDÊNCIA]** de quem mantém a reserva precisa entrar no contrato

**Mitigações**

- Backup incremental e compressão para reduzir I/O
- Reserva também serve como ambiente de teste de atualização
- Ensaio de contingência incluído no treinamento do gerente

## Como validar

- Ensaio trimestral de restauração, com tempo medido
- Simulação de falha de hardware durante o piloto (fora do horário de pico)
- Verificação diária de integridade do backup (arquivo existe, tamanho plausível, restaurável)
- Alerta de instalação ausente dispara em até 10 min

## Revisitar quando

- A frequência real de falha justificar alta disponibilidade
- O custo do hardware cair a ponto de tornar o par de servidores trivial
