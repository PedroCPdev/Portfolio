#!/usr/bin/env node
// Gate único deste projeto. O agente, os hooks do pedro-harness, o CI e você rodam o MESMO comando,
// então "passou" significa a mesma coisa em todo lugar.
//
// Uso:
//   node scripts/verify.mjs quick              → lint + testes unitários (rodado automaticamente antes de o agente encerrar)
//   node scripts/verify.mjs full               → quick + integração/e2e
//   node scripts/verify.mjs build              → tudo + build de produção (fim de fase, DOD e CI)
//   node scripts/verify.mjs format <arquivos>  → formatter/lint --fix nos arquivos (rodado após cada edição)
//   ... --log <caminho>                         → grava o output real como evidência (ex.: .specs/features/x/evidencias/...)
//
// Cada nível é uma lista de comandos rodados em ordem; o primeiro que falhar interrompe com o mesmo exit code.
// Em `format`, um item pode ser { run, ext } para receber só os arquivos com aquelas extensões.
// Nível vazio = NÃO verificado (exit 1). Configure os comandos em vez de deixar passar em branco.

// Os testes da API com [Trait("Category", "Firestore")] precisam do emulador: ficam fora do quick e o
// full sobe o emulador sozinho com `emulators:exec` (exige JDK 21+), que também define FIRESTORE_EMULATOR_HOST.
const FIREBASE = 'npx -y firebase-tools@15.34.0';

const COMMANDS = {
  format: [
    { run: 'node scripts/eslint-frontend.mjs', ext: ['.ts', '.tsx'] },
    { run: 'dotnet format whitespace . --folder --exclude frontend --include', ext: ['.cs'] },
  ],
  quick: [
    'cd frontend && npm run lint',
    'cd frontend && npx tsc --noEmit',
    'cd frontend && npm test',
    'dotnet test PortfolioApi.Tests --filter Category!=Firestore',
  ],
  full: [
    '@quick',
    `${FIREBASE} emulators:exec --only firestore --project demo-portfolio "dotnet test PortfolioApi.Tests --filter Category=Firestore"`,
  ],
  build: [
    '@full',
    'cd frontend && npm run build',
    'dotnet build PortfolioApi -c Release',
  ],
};

// ─── daqui para baixo não precisa mexer ───────────────────────────────────────
import { spawnSync } from 'node:child_process';
import { appendFileSync, mkdirSync, writeFileSync } from 'node:fs';
import { dirname, extname } from 'node:path';

const args = process.argv.slice(2);
const level = args[0];
const logIdx = args.indexOf('--log');
const logPath = logIdx >= 0 ? args[logIdx + 1] : null;
const files = args.slice(1).filter((_, i) => i + 1 !== logIdx && i + 1 !== logIdx + 1);

if (!COMMANDS[level]) {
  console.error(`Nível inválido: "${level}". Use: ${Object.keys(COMMANDS).join(' | ')}`);
  process.exit(2);
}

function expand(name, seen = new Set()) {
  if (seen.has(name)) throw new Error(`Referência circular em @${name}`);
  seen.add(name);
  return COMMANDS[name].flatMap((c) => (typeof c === 'string' && c.startsWith('@') ? expand(c.slice(1), new Set(seen)) : [c]));
}

const quote = (f) => (/[\s"']/.test(f) ? `"${f.replace(/"/g, '\\"')}"` : f);

const steps = expand(level).map((c) => {
  if (level !== 'format') return typeof c === 'string' ? c : c.run;
  const run = typeof c === 'string' ? c : c.run;
  const targets = typeof c === 'string' || !c.ext ? files : files.filter((f) => c.ext.includes(extname(f).toLowerCase()));
  return targets.length ? `${run} ${targets.map(quote).join(' ')}` : null;
}).filter(Boolean);

if (level === 'format' && steps.length === 0) process.exit(0);
if (steps.length === 0) {
  console.error(`[verify] O nível "${level}" não tem comandos configurados em scripts/verify.mjs — nada foi verificado.`);
  process.exit(1);
}

const log = (text) => {
  process.stdout.write(text);
  if (logPath) appendFileSync(logPath, text);
};
if (logPath) {
  mkdirSync(dirname(logPath), { recursive: true });
  writeFileSync(logPath, `# verify ${level} — ${new Date().toISOString()}\n# cwd: ${process.cwd()}\n\n`);
}

for (const step of steps) {
  log(`\n$ ${step}\n`);
  const r = spawnSync(step, { shell: true, encoding: 'utf8', maxBuffer: 256 * 1024 * 1024 });
  log((r.stdout || '') + (r.stderr || ''));
  const code = r.status ?? 1;
  if (code !== 0) {
    log(`\n[verify] FALHOU: "${step}" (exit ${code})\n`);
    process.exit(code);
  }
}
log(`\n[verify] ${level}: OK (${steps.length} comando${steps.length > 1 ? 's' : ''})\n`);
