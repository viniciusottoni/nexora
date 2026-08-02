# Runbook de backup e recuperação do edge

Objetivos: RTO menor que 30 minutos; RPO zero apenas para eventos já sincronizados. Eventos somente no disco destruído podem ser perdidos.

## Rotina automática

- Dump PostgreSQL local a cada hora, formato custom comprimido.
- Retenção local: últimos 24 dumps.
- Upload remoto criptografado a cada 6 horas; diário deve ser agendado pela plataforma com classe `daily`.
- O storage da nuvem elimina cópias vencidas a cada upload: 30 dias para seis-horário e 90 dias para diário.
- Falha de upload envia alerta à plataforma e grava no syslog.
- Teste trimestral obrigatório em equipamento limpo.

Comandos:

```bash
sudo ./backup.sh local
sudo ./backup.sh remote
sudo ./backup.sh daily
sudo ./restore.sh --verify /var/backups/replay-edge/hourly/edge-<data>.dump
```

Backup remoto `.enc` usa PBKDF2 e cifra AES-256. Chave fica local em `/etc/replay-edge/backup-encryption.key`, modo `0600`; custódia/recuperação segura da chave do tenant deve ser definida na plataforma antes do piloto.

Na nuvem, defina `BACKUP_STORAGE_DIR` para um volume persistente com acesso exclusivo da API. O endpoint valida a assinatura Ed25519 da instalação, o SHA-256 do conteúdo e grava auditoria para upload ou alerta de falha.

## Falha de software

1. Rode `sudo ./doctor.sh`.
2. Consulte logs sem expor segredos:

   ```bash
   sudo docker compose --env-file /etc/replay-edge/edge.env -f docker-compose.yml logs --since 15m api-edge sync-worker
   ```

3. Reinicie serviço afetado. Não apague volume.
4. Persistindo por 60 segundos, acione suporte Replay com saída do doctor.

## Verificar backup antes de restaurar

```bash
sudo ./restore.sh --verify <arquivo.dump-ou-dump.enc>
```

O comando cria banco temporário, restaura com `--exit-on-error`, executa `SELECT 1` e remove banco temporário. Arquivo não é considerado backup válido sem esse teste.

## Aplicar restauração

Aviso: este procedimento substitui banco operacional. Confirme arquivo e aprovação do responsável.

1. Preserve equipamento/disco antigo desligado.
2. Instale imagem no equipamento reserva e copie backup e chave de cifra por meio seguro.
3. Valide backup com `--verify`.
4. Aplique:

   ```bash
   sudo ./restore.sh --apply <arquivo.dump-ou-dump.enc> --confirm
   ```

5. Script faz backup preventivo, para API/worker/web, recria banco, restaura e sobe serviços.
6. Rode `sudo ./doctor.sh`.
7. Confirme pedidos abertos, último caixa e cursor de sync com gerente.
8. Reconecte internet. Eventos já consolidados devem ser reconciliados pela nuvem.
9. Registre duração, backup usado, perda observada e responsável.

Não use `docker compose down -v`; `-v` apaga dados locais.

## Ensaio trimestral

- Use backup real anonimizado ou autorizado.
- Restaure em mini-PC limpo/reserva.
- Meça do acionamento ao health verde.
- Confirme integridade funcional, não só `SELECT 1`.
- Meta: menos de 30 minutos.
- Guarde evidência e ação corretiva para qualquer falha.
