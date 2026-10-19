#!/usr/bin/env node
// Falha quando uma dependência de produção tem aviso de segurança conhecido.
// Fica fora do verify de propósito: o resultado muda quando sai um aviso novo, sem nenhum commit,
// e o verify precisa dar o mesmo resultado para o mesmo código. Roda no CI como passo separado.
//
//   frontend: npm audit --omit=dev, falha a partir de "high"
//   API:      dotnet list package --vulnerable --include-transitive, falha com qualquer pacote listado
import { spawnSync } from 'node:child_process';

const run = (cmd, cwd) => spawnSync(cmd, { cwd, shell: true, encoding: 'utf8', maxBuffer: 64 * 1024 * 1024 });
let failed = false;

const npm = run('npm audit --omit=dev --audit-level=high', 'frontend');
process.stdout.write(npm.stdout + npm.stderr);
if (npm.status !== 0) failed = true;

const dotnet = run('dotnet list PortfolioApi package --vulnerable --include-transitive --format json', '.');
let report;
try {
  report = JSON.parse(dotnet.stdout);
} catch {
  process.stdout.write(dotnet.stdout + dotnet.stderr);
  console.error('[audit] saída do dotnet list não é JSON.');
  process.exit(1);
}
const vulnerable = report.projects.flatMap((p) =>
  (p.frameworks ?? []).flatMap((f) => [...(f.topLevelPackages ?? []), ...(f.transitivePackages ?? [])]),
);
for (const pkg of vulnerable) {
  const severities = (pkg.vulnerabilities ?? []).map((v) => v.severity).join(', ');
  console.log(`[audit] NuGet vulnerável: ${pkg.id} ${pkg.resolvedVersion} (${severities})`);
}
if (vulnerable.length) failed = true;
else console.log('[audit] NuGet: nenhum pacote vulnerável.');

process.exit(failed ? 1 : 0);
