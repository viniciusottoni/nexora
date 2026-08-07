#!/usr/bin/env node

import net from 'node:net';
import { spawn, spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = fileURLToPath(new URL('../..', import.meta.url));
const devComposePath = join(repoRoot, 'infra/dev/docker-compose.yml');
const processStatePath = join(repoRoot, '.turbo', 'dev-processes.json');
const children = [];
let shuttingDown = false;

process.chdir(repoRoot);

function commandName(command) {
  return command;
}

function isWindowsShellCommand(command) {
  return process.platform === 'win32' && command === 'pnpm';
}

function shellCommand(command, args) {
  return [command, ...args].join(' ');
}

function readDevPostgresPassword() {
  const compose = readFileSync(devComposePath, 'utf8');
  const match = compose.match(/^\s*POSTGRES_PASSWORD:\s*(.+)\s*$/m);
  if (!match) {
    throw new Error('POSTGRES_PASSWORD nao encontrado em infra/dev/docker-compose.yml');
  }

  return match[1].trim().replace(/^['"]|['"]$/g, '');
}

function buildConnection(database) {
  const host = process.env.NEXORA_DEV_DB_HOST ?? 'localhost';
  const port = process.env.NEXORA_DEV_DB_PORT ?? '5432';
  const user = process.env.NEXORA_DEV_DB_USER ?? 'donabetinha';
  const password = process.env.NEXORA_DEV_DB_PASSWORD ?? readDevPostgresPassword();

  return [
    `Host=${host}`,
    `Port=${port}`,
    `Database=${database}`,
    `Username=${user}`,
    `Password=${password}`,
  ].join(';');
}

function probe(command, args) {
  if (isWindowsShellCommand(command)) {
    return spawnSync(shellCommand(command, args), {
      cwd: repoRoot,
      encoding: 'utf8',
      shell: true,
    });
  }

  return spawnSync(commandName(command), args, {
    cwd: repoRoot,
    encoding: 'utf8',
  });
}

function checkPrerequisites() {
  const issues = [];
  const nodeMajor = Number.parseInt(process.versions.node.split('.')[0], 10);

  if (nodeMajor < 22 || nodeMajor >= 25) {
    issues.push(`Node 22, 23 ou 24 e necessario (atual: ${process.version})`);
  }

  const docker = probe('docker', ['--version']);
  if (docker.error?.code === 'ENOENT') {
    issues.push('Docker nao foi encontrado no PATH');
  } else if (docker.status !== 0) {
    issues.push('Docker esta instalado, mas nao respondeu corretamente');
  }

  const dotnet = probe('dotnet', ['--list-sdks']);
  if (dotnet.error?.code === 'ENOENT') {
    issues.push('.NET SDK 10 nao foi encontrado no PATH');
  } else if (!dotnet.stdout?.split('\n').some((line) => line.startsWith('10.'))) {
    issues.push('.NET SDK 10 nao esta instalado');
  }

  const pnpm = probe('pnpm', ['--version']);
  if (pnpm.error?.code === 'ENOENT' || pnpm.status !== 0) {
    issues.push('pnpm nao foi encontrado no PATH');
  }

  if (issues.length > 0) {
    throw new Error(`Pre-requisitos ausentes:\n  - ${issues.join('\n  - ')}`);
  }
}

function run(command, args, options = {}) {
  const spawnOptions = {
    cwd: repoRoot,
    env: { ...process.env, ...options.env },
    stdio: 'inherit',
  };

  const result = isWindowsShellCommand(command)
    ? spawnSync(shellCommand(command, args), { ...spawnOptions, shell: true })
    : spawnSync(commandName(command), args, spawnOptions);

  if (result.error?.code === 'ENOENT') {
    throw new Error(`Comando nao encontrado: ${command}`);
  }

  if (result.error) {
    throw result.error;
  }

  if (result.status !== 0) {
    throw new Error(`${command} terminou com o codigo ${result.status}`);
  }
}

function isPortListening(port) {
  return new Promise((resolve) => {
    const socket = net.createConnection({ host: '127.0.0.1', port });

    socket.setTimeout(500);
    socket.once('connect', () => {
      socket.destroy();
      resolve(true);
    });
    socket.once('error', () => resolve(false));
    socket.once('timeout', () => {
      socket.destroy();
      resolve(false);
    });
  });
}

function persistProcessState() {
  const processIds = children
    .filter((child) => child.pid && child.exitCode === null)
    .map((child) => child.pid);

  if (processIds.length === 0) {
    rmSync(processStatePath, { force: true });
    return;
  }

  mkdirSync(join(repoRoot, '.turbo'), { recursive: true });
  writeFileSync(
    processStatePath,
    `${JSON.stringify({ repoRoot, launcherPid: process.pid, processIds, startedAt: Date.now() }, null, 2)}\n`,
  );
}

function start(command, args, env = {}) {
  const child = isWindowsShellCommand(command)
    ? spawn(shellCommand(command, args), {
        cwd: repoRoot,
        detached: process.platform !== 'win32',
        env: { ...process.env, ...env },
        shell: true,
        stdio: 'inherit',
      })
    : spawn(commandName(command), args, {
        cwd: repoRoot,
        detached: process.platform !== 'win32',
        env: { ...process.env, ...env },
        stdio: 'inherit',
      });

  children.push(child);
  persistProcessState();
  child.once('error', (error) => {
    console.error(`Falha ao iniciar ${command}: ${error.message}`);
    shutdown(1);
  });
  child.once('exit', (code, signal) => {
    persistProcessState();
    if (!shuttingDown && code !== 0) {
      console.error(`${command} terminou inesperadamente (${signal ?? `codigo ${code}`}).`);
      shutdown(code ?? 1);
    }
  });
}

function shutdown(exitCode = 0) {
  if (shuttingDown) return;
  shuttingDown = true;

  for (const child of children) {
    if (!child.pid || child.exitCode !== null) continue;

    try {
      process.kill(process.platform === 'win32' ? child.pid : -child.pid, 'SIGTERM');
    } catch (error) {
      if (error.code !== 'ESRCH') console.error(error.message);
    }
  }

  process.exitCode = exitCode;
}

process.once('SIGINT', () => shutdown(130));
process.once('SIGTERM', () => shutdown(143));
process.once('SIGHUP', () => shutdown(129));

try {
  checkPrerequisites();

  const edgeConnection = buildConnection('donabetinha_edge_dev');
  const cloudConnection = buildConnection('donabetinha_cloud_dev');

  console.log('==> Subindo Postgres e Redis (Docker)...');
  run('docker', ['compose', '-f', 'infra/dev/docker-compose.yml', 'up', '-d', '--wait']);

  console.log('==> Restaurando pacotes NuGet do backend...');
  run('dotnet', ['restore', 'backend/Nexora.slnx']);

  console.log('==> Aplicando migrations EF Core (Nexora.Api.Edge)...');
  run('dotnet', ['ef', 'database', 'update', '--project', 'backend/src/Nexora.Infrastructure'], {
    env: { NEXORA_MIGRATIONS_CONNECTION: edgeConnection },
  });

  console.log('==> Aplicando migrations EF Core (Nexora.Api.Cloud)...');
  run('dotnet', ['ef', 'database', 'update', '--project', 'backend/src/Nexora.Infrastructure'], {
    env: { NEXORA_MIGRATIONS_CONNECTION: cloudConnection },
  });

  console.log('==> Criando massa de usuarios de teste (Cloud + Edge)...');
  run('dotnet', [
    'run',
    '--project',
    'backend/src/Nexora.DevSeeder',
    '--',
    '--connection',
    cloudConnection,
    '--mode',
    'cloud',
  ]);
  run('dotnet', [
    'run',
    '--project',
    'backend/src/Nexora.DevSeeder',
    '--',
    '--connection',
    edgeConnection,
    '--mode',
    'edge',
  ]);

  if (await isPortListening(5000)) {
    console.log('==> Nexora.Api.Edge ja esta rodando em http://localhost:5000, pulando.');
  } else {
    console.log('==> Subindo Nexora.Api.Edge em http://localhost:5000 ...');
    start('dotnet', ['run', '--project', 'backend/src/Nexora.Api.Edge'], {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: 'http://localhost:5000',
    });
  }

  if (await isPortListening(5100)) {
    console.log('==> Nexora.Api.Cloud ja esta rodando em http://localhost:5100, pulando.');
  } else {
    console.log('==> Subindo Nexora.Api.Cloud em http://localhost:5100 ...');
    start('dotnet', ['run', '--project', 'backend/src/Nexora.Api.Cloud'], {
      ASPNETCORE_ENVIRONMENT: 'Development',
      ASPNETCORE_URLS: 'http://localhost:5100',
    });
  }

  if (!existsSync(join(repoRoot, 'node_modules'))) {
    console.log('==> node_modules ausente, rodando pnpm install...');
    run('pnpm', ['install']);
  }

  const frontendPorts = [5173, 5174, 5175, 5176, 5177];
  const frontendStatus = await Promise.all(frontendPorts.map(isPortListening));

  if (frontendStatus.every(Boolean)) {
    console.log('==> Os 5 apps do frontend ja estao rodando (portas 5173-5177), pulando.');
  } else {
    console.log('==> Subindo os 5 apps do frontend (pnpm dev / turbo --parallel)...');
    start('pnpm', ['dev']);
  }

  console.log(`
Ambiente completo no ar (os logs compartilham este terminal):
  Postgres          -> localhost:5432 (donabetinha_edge_dev / donabetinha_cloud_dev)
  Redis             -> localhost:6379
  Nexora.Api.Edge   -> http://localhost:5000/swagger
  Nexora.Api.Cloud  -> http://localhost:5100/swagger
  Gestao local      -> http://localhost:5173/admin
  Plataforma        -> http://localhost:5174
  Caixa (POS)       -> http://localhost:5175
  Cozinha (KDS)     -> http://localhost:5176
  Cardapio          -> http://localhost:5177

Primeiro pareamento:
  1. Abra http://localhost:5173/admin e use o codigo inicial exibido pelo seeder.
  2. Entre com o PIN 2101 (proprietario) ou 2102 (gerente).
  3. Clique em "Autorizar novo dispositivo" e use o novo codigo no Caixa/KDS.

Use Ctrl+C para encerrar os processos iniciados por este comando.`);
} catch (error) {
  console.error(`\nErro ao subir o ambiente: ${error.message}`);
  shutdown(1);
}
