#!/usr/bin/env node
// Roda `eslint --fix` com cwd em frontend/. O verify passa caminhos relativos à raiz do repo, e o
// eslint-config-next resolve `pages/` e o config a partir do cwd: rodado da raiz, ele avisa que não
// acha `pages/` e não acha o eslint.config.mjs.
import { spawnSync } from 'node:child_process';
import { join, relative, resolve } from 'node:path';

const frontend = resolve(import.meta.dirname, '..', 'frontend');
const files = process.argv.slice(2).map((f) => relative(frontend, resolve(f)));
const eslint = join(frontend, 'node_modules', 'eslint', 'bin', 'eslint.js');

const r = spawnSync(process.execPath, [eslint, '--fix', ...files], { cwd: frontend, stdio: 'inherit' });
process.exit(r.status ?? 1);
