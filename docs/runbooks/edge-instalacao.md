# Runbook de instalação do edge

Escopo: técnico de campo. Meta: servidor operacional em até 30 minutos.

## Antes de ir à loja

- Confirme hardware e nobreak conforme `edge-hardware.md`.
- Obtenha da plataforma o comando com `tenant` e token de uso único.
- Garanta saída HTTPS para a API da nuvem. Nenhuma porta do edge deve ser exposta à internet.
- Reserve IP no DHCP para o mini-PC e aponte `edge.local` para esse IP no DNS da LAN.
- Tenha acesso administrativo local. O script grava chaves privadas em `/etc/replay-edge/keys`, modo `0600`.
- Receba da release as referências `EDGE_API_IMAGE` e `EDGE_WEB_IMAGE`, ambas fixadas por `@sha256`.

## Instalar

1. Ligue mini-PC ao nobreak e à LAN cabeada.
2. Confirme Docker Engine, Docker Compose v2, `curl`, `jq`, `openssl` e `flock`.
3. Copie `infra/edge` para o servidor e entre na pasta.
4. Exporte os valores entregues junto da release. As imagens devem conter digest, não tag mutável:

   ```bash
   export EDGE_CLOUD_URL=https://api.exemplo.com
   export EDGE_API_IMAGE=registry.exemplo.com/replay-api@sha256:<digest>
   export EDGE_WEB_IMAGE=registry.exemplo.com/replay-web@sha256:<digest>
   ```

5. Execute o comando entregue pela plataforma, preservando somente essas variáveis:

   ```bash
   sudo --preserve-env=EDGE_CLOUD_URL,EDGE_API_IMAGE,EDGE_WEB_IMAGE \
     ./install.sh --tenant=<uuid> --token=<token>
   ```

6. Aguarde mensagem `Instalação concluída`.
7. Instale `/etc/replay-edge/tls/local-ca.crt` como CA confiável nos terminais da loja.
8. Abra `https://edge.local/v1/health`. PostgreSQL e Redis devem estar `OK`.
9. Execute `sudo ./doctor.sh` e anexe saída ao checklist da implantação. Saída não contém token nem chave privada.

## O que o script faz

1. Valida host e Docker.
2. Gera identidade Ed25519 local; chave privada nunca sai do servidor.
3. Gera CA e certificado TLS para `edge.local`.
4. Registra instalação na nuvem e consome token antes de criar containers.
5. Baixa cardápio e configuração inicial.
6. Grava configuração local com permissão `0600`.
7. Sobe PostgreSQL 16, Redis, API, web/nginx, worker de sync e Watchtower.
8. Instala backup horário e aguarda health check verde.

## Falha e retomada

- Token recusado: nenhum container é criado. Peça novo token se expirado ou consumido por outro servidor.
- Internet caiu antes do registro: reexecute mesmo comando quando voltar.
- Internet caiu após registro: checkpoint em `/var/lib/replay-edge/install/registration.json` evita consumir token de novo. Reexecute mesmo comando; carga inicial retoma.
- O bootstrap importa tenant, loja, configuração, estações e catálogo no PostgreSQL antes da API.
- Energia caiu: reexecute mesmo comando. Volumes Docker e checkpoints são preservados.
- Reexecução normal é segura: não recria chaves, não apaga volumes e reconcilia containers.

Nunca copie `edge-private.pem`, `backup-encryption.key`, `postgres-password` ou `edge.env` para chamado, chat ou repositório.

## Aceite de campo

- [ ] `doctor.sh` sem falhas
- [ ] `https://edge.local` abre em dois terminais da LAN
- [ ] cardápio/config inicial presentes
- [ ] cabo WAN removido por 10 minutos e operação local continua
- [ ] `backup.sh local` cria dump
- [ ] `test-backup-restore.sh` confirma `Backup restaurável`
- [ ] duração total registrada; alvo menor que 30 minutos

Pendências externas ao script: validação pelo PO, revisão por outro desenvolvedor e ensaio por técnico que não escreveu a implementação.
