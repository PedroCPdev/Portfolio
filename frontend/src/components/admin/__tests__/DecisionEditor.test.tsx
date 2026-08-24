// @vitest-environment jsdom
import { it, expect, vi, afterEach } from "vitest";
import { render, screen, cleanup, fireEvent } from "@testing-library/react";
import DecisionEditor from "../DecisionEditor";
import type { Decision } from "@/lib/api";

afterEach(cleanup);

function renderizar(decisions: Decision[]) {
  const onChange = vi.fn();
  render(<DecisionEditor decisions={decisions} onChange={onChange} />);
  return onChange;
}

const tres: Decision[] = [
  { title: "primeira", why: "p1", cost: "c1" },
  { title: "segunda", why: "p2", cost: "c2" },
  { title: "terceira", why: "p3", cost: "c3" },
];

// ADM-17 (sucesso): adicionar
it("adicionar cria uma decisão vazia no fim da lista", () => {
  const onChange = renderizar([tres[0]]);

  fireEvent.click(screen.getByRole("button", { name: /add decision/i }));

  expect(onChange).toHaveBeenCalledWith([tres[0], { title: "", why: "", cost: "" }]);
});

// ADM-17 (sucesso): editar
it("digitar num campo emite a decisão alterada na posição certa", () => {
  const onChange = renderizar(tres);

  fireEvent.change(screen.getAllByLabelText(/^what was decided/i)[1], { target: { value: "segunda editada" } });

  expect(onChange).toHaveBeenCalledWith([
    tres[0],
    { ...tres[1], title: "segunda editada" },
    tres[2],
  ]);
});

// ADM-17 (sem sucesso): lista vazia é estado legítimo, não erro
it("sem decisões, convida a adicionar e não quebra", () => {
  renderizar([]);

  expect(screen.getByText(/no decisions yet/i)).toBeDefined();
  expect(screen.getByRole("button", { name: /add decision/i })).toBeDefined();
});

// ADM-17 (falha): remover tira a decisão certa, não a última
it("remover tira exatamente a decisão pedida", () => {
  const onChange = renderizar(tres);

  fireEvent.click(screen.getAllByRole("button", { name: /remove decision/i })[1]);

  expect(onChange).toHaveBeenCalledWith([tres[0], tres[2]]);
});

// ADM-17: a ordem é informação (AD-008) — mover troca posições
it("mover para cima troca a decisão com a anterior", () => {
  const onChange = renderizar(tres);

  fireEvent.click(screen.getAllByRole("button", { name: /move decision up/i })[2]);

  expect(onChange).toHaveBeenCalledWith([tres[0], tres[2], tres[1]]);
});

it("mover para baixo troca a decisão com a seguinte", () => {
  const onChange = renderizar(tres);

  fireEvent.click(screen.getAllByRole("button", { name: /move decision down/i })[0]);

  expect(onChange).toHaveBeenCalledWith([tres[1], tres[0], tres[2]]);
});

// ADM-17 (falha): nas pontas não há para onde mover
it("subir na primeira e descer na última ficam desabilitados", () => {
  renderizar(tres);

  const subir = screen.getAllByRole("button", { name: /move decision up/i }) as HTMLButtonElement[];
  const descer = screen.getAllByRole("button", { name: /move decision down/i }) as HTMLButtonElement[];

  expect(subir[0].disabled).toBe(true);
  expect(subir[2].disabled).toBe(false);
  expect(descer[2].disabled).toBe(true);
  expect(descer[0].disabled).toBe(false);
});

// A ordem exibida é a ordem que sai — o índice mostrado acompanha a posição
it("a numeração exibida segue a posição na lista", () => {
  renderizar(tres);

  const titulos = screen.getAllByLabelText(/^what was decided/i) as HTMLInputElement[];
  expect(titulos.map((i) => i.value)).toEqual(["primeira", "segunda", "terceira"]);
  expect(screen.getByText("01")).toBeDefined();
  expect(screen.getByText("03")).toBeDefined();
});
