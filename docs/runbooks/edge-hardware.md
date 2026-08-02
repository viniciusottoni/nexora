# Hardware de referência do edge

Especificação provisória. Validar com cliente antes da compra.

## Mínimo

- Mini-PC x86-64, 4 núcleos
- 8 GB RAM
- SSD 256 GB com SMART
- Ethernet Gigabit cabeada
- TPM 2.0 quando disponível
- Nobreak 600 VA com USB e desligamento gracioso
- Ubuntu Server LTS 64-bit ou distribuição Linux homologada
- Docker Engine e Compose v2

## Recomendado

- 8 núcleos
- 16 GB RAM
- SSD NVMe 512 GB
- TPM 2.0
- Nobreak 1200 VA com USB
- Segundo mini-PC idêntico, imagem instalada e pareado, desligado na loja

## Rede e segurança

- IP reservado no DHCP e DNS local `edge.local`
- Sem redirecionamento de portas no roteador
- Acesso administrativo apenas pela LAN ou túnel autenticado
- Sync usa somente conexão HTTPS de saída
- VLAN e provisionamento de rede são responsabilidade da infraestrutura do cliente
- Disco criptografado por LUKS; chave em TPM quando houver. Boot sem TPM/internet exige procedimento operacional ainda pendente antes do piloto.

## Capacidade e prevenção

- Alerta de disco a partir de 80%
- PostgreSQL e Redis em volumes separados logicamente
- Temperatura e SMART reportados no heartbeat quando monitoramento da Fase 1 estiver disponível
- Nobreak testado mensalmente; bateria conforme fabricante
- Backup antes de qualquer atualização

## Pendências contratuais

- Quem compra e mantém equipamento principal e reserva
- Prazo para troca física
- Custódia da chave de backup do tenant
- Modelo homologado de mini-PC, SSD e nobreak
- Procedimento de boot do volume LUKS sem internet
