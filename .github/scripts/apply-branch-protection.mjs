import { readFileSync } from 'node:fs';

const repository = process.env.GITHUB_REPOSITORY;
const token = process.env.GITHUB_TOKEN;
if (!repository || !token) {
  console.error(
    'Defina GITHUB_REPOSITORY=owner/repo e GITHUB_TOKEN com permissão Administration:write.',
  );
  process.exit(1);
}

const headers = {
  Accept: 'application/vnd.github+json',
  Authorization: `Bearer ${token}`,
  'X-GitHub-Api-Version': '2022-11-28',
  'Content-Type': 'application/json',
};

const settingsResponse = await fetch(`https://api.github.com/repos/${repository}`, {
  method: 'PATCH',
  headers,
  body: JSON.stringify({
    allow_squash_merge: true,
    allow_merge_commit: false,
    allow_rebase_merge: false,
    delete_branch_on_merge: true,
  }),
});
if (!settingsResponse.ok) {
  console.error(
    `GitHub recusou as regras de merge (${settingsResponse.status}): ${await settingsResponse.text()}`,
  );
  process.exit(1);
}

const response = await fetch(
  `https://api.github.com/repos/${repository}/branches/main/protection`,
  {
    method: 'PUT',
    headers,
    body: readFileSync(new URL('../branch-protection.json', import.meta.url), 'utf8'),
  },
);

if (!response.ok) {
  console.error(`GitHub recusou a proteção de main (${response.status}): ${await response.text()}`);
  process.exit(1);
}
console.log('Proteção de main aplicada com checks bloqueantes.');
