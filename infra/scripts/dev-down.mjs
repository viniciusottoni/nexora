#!/usr/bin/env node

import { spawnSync } from 'node:child_process';
import { readFileSync, rmSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';

const repoRoot = fileURLToPath(new URL('../..', import.meta.url));
const processStatePath = join(repoRoot, '.turbo', 'dev-processes.json');

function isRunning(pid, processGroup = false) {
  try {
    process.kill(processGroup ? -pid : pid, 0);
    return true;
  } catch (error) {
    if (error.code === 'ESRCH') return false;
    throw error;
  }
}

function stopProcessTree(pid, signal = 'SIGTERM') {
  if (!Number.isSafeInteger(pid) || pid <= 0) return;

  if (process.platform === 'win32') {
    spawnSync('taskkill', ['/PID', String(pid), '/T', '/F'], { stdio: 'ignore' });
    return;
  }

  try {
    process.kill(-pid, signal);
  } catch (error) {
    if (error.code !== 'ESRCH') throw error;
  }
}

function loadProcessState() {
  try {
    const state = JSON.parse(readFileSync(processStatePath, 'utf8'));
    const isRecent = Date.now() - state.startedAt < 7 * 24 * 60 * 60 * 1000;

    if (state.repoRoot !== repoRoot || !isRecent || !Array.isArray(state.processIds)) return null;
    return state;
  } catch (error) {
    if (error.code === 'ENOENT') return null;
    throw error;
  }
}

async function stopLocalProcesses() {
  const state = loadProcessState();
  if (!state) return;

  console.log('==> Encerrando APIs e frontends locais...');

  if (Number.isSafeInteger(state.launcherPid) && isRunning(state.launcherPid)) {
    process.kill(state.launcherPid, 'SIGTERM');
  }

  await new Promise((resolve) => setTimeout(resolve, 750));

  for (const pid of state.processIds) {
    if (process.platform === 'win32' ? isRunning(pid) : isRunning(pid, true)) {
      stopProcessTree(pid);
    }
  }

  await new Promise((resolve) => setTimeout(resolve, 750));

  for (const pid of state.processIds) {
    if (process.platform !== 'win32' && isRunning(pid, true)) {
      stopProcessTree(pid, 'SIGKILL');
    }
  }

  rmSync(processStatePath, { force: true });
}

await stopLocalProcesses();

console.log('==> Encerrando Postgres e Redis (Docker)...');
const docker = spawnSync('docker', ['compose', '-f', 'infra/dev/docker-compose.yml', 'down'], {
  cwd: repoRoot,
  stdio: 'inherit',
});

if (docker.error?.code === 'ENOENT') {
  console.error('Comando nao encontrado: docker');
  process.exitCode = 1;
} else if (docker.error) {
  throw docker.error;
} else if (docker.status !== 0) {
  process.exitCode = docker.status ?? 1;
} else {
  console.log('Ambiente de desenvolvimento encerrado.');
}
