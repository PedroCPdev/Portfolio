# Tarefas: visual-decision-record

| # | Tarefa | Requisitos | Arquivos |
|---|---|---|---|
| T1 | Fundação: tokens de cor/tipo, reset, foco, reduced-motion | DR-01, DR-02, DR-07, DR-08 | `globals.css`, `layout.tsx` |
| T2 | Spine assimétrico + `RecordHeader` substituindo `SectionHeader` | DR-04, DR-06 | `SectionHeader.tsx` → `RecordHeader.tsx`, `page.tsx` |
| T3 | Hero como tese | DR-03 | `Hero.tsx` |
| T4 | About com hierarquia de tecnologias | DR-11 | `About.tsx` |
| T5 | Projects + trade-off ledger | DR-05 | `Projects.tsx` |
| T6 | Contact + form: vocabulário e estados | DR-09 | `Contact.tsx`, `ContactForm.tsx` |
| T7 | Navbar, footer, e atualização de `CONVENTIONS.md` (AD-007) | DR-01 | `Navbar.tsx`, `page.tsx`, `.specs/codebase/CONVENTIONS.md` |
| T8 | Testes de invariante + gate completo + screenshot | DR-10 | `src/__tests__/design-system.test.ts` |

Ordem obrigatória: T1 antes de todas (as demais consomem os tokens). T8 por último.
