# ADR-031 · Gestão de segredos e credenciais

| | |
|---|---|
| **Status** | Aceito |
| **Data** | 31/07/2026 |
| **Decisores** | Tech Lead, DevOps |
| **Relacionados** | ADR-024, ADR-025, ADR-007 |
| **Requisitos afetados** | RNF-SEG-10, RNF-SEG-12, RNF-SEG-16 |

---

## Contexto

O sistema lida com segredos de três naturezas bem diferentes:

1. **Da plataforma** — chaves de assinatura de JWT, credenciais de banco, storage
2. **Do tenant** — credenciais de pagamento, certificado fiscal, chaves de webhook
3. **Da instalação** — par de chaves de cada edge para autenticar o sync

O agravante é que o servidor local fica **fisicamente na loja**, em ambiente que não controlamos, potencialmente acessível a terceiros. Um segredo em texto claro nesse disco é um segredo comprometido.

## Decisão

**Segredos nunca no repositório, nunca no cliente e nunca em texto claro no edge.**

- Plataforma: variáveis de ambiente injetadas pelo gerenciador de segredos
- Tenant: criptografados no banco com chave da plataforma
- Instalação: par de chaves gerado localmente; a privada nunca sai da loja

## Detalhamento

### Segredos da plataforma

| Segredo | Onde | Rotação |
|---|---|---|
| Chave de assinatura de JWT | Gerenciador de segredos → env | 180 dias |
| Credencial do banco | Idem | 90 dias |
| Credencial de storage | Idem | 180 dias |
| Chave VAPID (push) | Idem | Anual |
| Chave mestra de criptografia | Gerenciador, com versionamento | Anual, com re-encriptação |

Nenhum `.env` versionado. O repositório contém apenas `.env.example`, com nomes e sem valores.

### Segredos do tenant

Credenciais de pagamento e certificado fiscal são criptografados em repouso com envelope encryption:

```sql
CREATE TABLE tenant_secret (
  tenant_id     UUID NOT NULL,
  key           TEXT NOT NULL,        -- 'mercadopago.accessToken'
  ciphertext    BYTEA NOT NULL,       -- AES-256-GCM
  key_version   INT  NOT NULL,        -- permite rotação sem reprocessar tudo
  created_at    TIMESTAMPTZ NOT NULL,
  PRIMARY KEY (tenant_id, key)
);
```

| Regra | Motivo |
|---|---|
| Descriptografado apenas em memória, no momento do uso | Reduz janela de exposição |
| **Nunca** trafega para o edge | Pagamento online é operação de nuvem |
| **Nunca** retorna pela API, nem ao próprio gestor | Interface mostra apenas `••••1234` |
| Alteração gera evento de auditoria | Rastreabilidade |
| RLS aplicado (ADR-004) | Isolamento entre tenants |

### Segredos da instalação

```
Instalação do edge
   ├─ gera par de chaves Ed25519 localmente
   ├─ envia a chave pública à nuvem, autenticado pelo token de instalação (uso único)
   └─ guarda a privada em /etc/dona-betinha/keys (permissão 0600, dono root)
```

A chave privada **nunca sai da loja**. Toda requisição de sync é assinada; a nuvem verifica com a chave pública (RNF-SEG-12).

Comprometimento de uma loja não compromete nenhuma outra — a chave é individual e revogável.

### Disco criptografado no edge

O volume de dados do edge usa criptografia em repouso (LUKS), com chave derivada de segredo obtido da nuvem no boot. Um disco roubado não entrega os dados.

> Compromisso aceito: se a loja ficar sem internet **durante um reboot**, o edge não consegue obter a chave. Mitigação: chave em cache local protegida por TPM quando disponível; caso contrário, procedimento manual documentado no runbook.

### Rede do edge

| Regra | Motivo |
|---|---|
| Nenhuma porta exposta à internet | RNF-SEG-16 |
| Sync sempre por conexão **de saída** | Elimina a superfície de ataque de entrada |
| Acesso administrativo apenas pela LAN ou por túnel autenticado | — |
| Firewall configurado no `install.sh` | Padrão seguro por instalação |

### Prevenção de vazamento no código

| Camada | Ferramenta |
|---|---|
| Pre-commit | `gitleaks` |
| CI | Varredura de segredos (bloqueante) |
| Logs | Redação automática de campos sensíveis (ADR-022) |
| Revisão | Checklist de PR |

## Alternativas consideradas

| Alternativa | Prós | Contras | Por que foi descartada |
|---|---|---|---|
| Segredos em `.env` versionado | Simples | Vazamento garantido no histórico do Git | Inaceitável |
| Segredos do tenant em texto claro no banco | Simples | Dump de banco expõe credenciais de todos os clientes | Risco desproporcional |
| Cofre dedicado (Vault) | Recurso completo | Mais um serviço para operar; desproporcional ao porte | Gerenciador da plataforma + envelope encryption atende |
| Credenciais de pagamento no edge | Pagamento local funcionaria offline | Segredo em máquina fisicamente acessível | Pagamento online já depende de internet — não há ganho |
| Uma chave só para todo o parque | Simples | Comprometer uma loja compromete todas | Inaceitável |

## Consequências

**Positivas**

- Comprometimento de uma loja é contido àquela loja
- Dump de banco não expõe credenciais utilizáveis
- Disco roubado não entrega dados
- Rotação possível sem downtime, graças ao versionamento de chave

**Negativas**

- Complexidade operacional de rotação
- Disco criptografado depende de conectividade no boot (ou TPM)
- Envelope encryption adiciona uma camada a manter

**Mitigações**

- Rotação automatizada por job, com re-encriptação em lote
- Procedimento manual documentado para boot sem conectividade
- Chave em cache protegida por TPM onde o hardware permitir

## Como validar

- Varredura de segredos no CI, bloqueante
- Teste: credencial de tenant nunca retorna pela API, nem para `OWNER`
- Teste: requisição de sync com assinatura inválida é rejeitada e registrada
- Auditoria trimestral de permissões e de segredos ativos
- Ensaio de revogação de chave de instalação

## Revisitar quando

- O parque crescer a ponto de justificar um cofre dedicado
- Um requisito de certificação exigir gestão de chaves específica
