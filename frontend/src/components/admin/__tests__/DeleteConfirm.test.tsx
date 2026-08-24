// @vitest-environment jsdom
import { it, expect, vi, afterEach } from "vitest";
import { render, screen, cleanup, fireEvent } from "@testing-library/react";
import DeleteConfirm from "../DeleteConfirm";

afterEach(cleanup);

const TITULO = "Portfolio API";

function renderizar() {
  const onConfirm = vi.fn();
  const onCancel = vi.fn();
  render(<DeleteConfirm title={TITULO} onConfirm={onConfirm} onCancel={onCancel} />);
  const botao = screen.getByRole("button", { name: /delete project/i }) as HTMLButtonElement;
  const campo = screen.getByLabelText(/type the title/i);
  return { onConfirm, onCancel, botao, campo };
}

// ADM-21 (sucesso)
it("com o título exato, confirmar chama onConfirm uma vez", () => {
  const { onConfirm, botao, campo } = renderizar();

  fireEvent.change(campo, { target: { value: TITULO } });

  expect(botao.disabled).toBe(false);
  fireEvent.click(botao);
  expect(onConfirm).toHaveBeenCalledTimes(1);
});

// ADM-21 (falha): quase certo não é certo
it.each([["Portfolio"], ["portfolio api"], ["Portfolio API "], [""], ["  "]])(
  "mantém o botão desabilitado quando o texto é %j",
  (digitado) => {
    const { onConfirm, botao, campo } = renderizar();

    fireEvent.change(campo, { target: { value: digitado } });

    expect(botao.disabled).toBe(true);
    fireEvent.click(botao);
    expect(onConfirm).not.toHaveBeenCalled();
  },
);

// ADM-21 (sem sucesso): sair sem apagar
it("cancelar chama onCancel e nunca onConfirm", () => {
  const { onConfirm, onCancel } = renderizar();

  fireEvent.click(screen.getByRole("button", { name: /cancel/i }));

  expect(onCancel).toHaveBeenCalledTimes(1);
  expect(onConfirm).not.toHaveBeenCalled();
});

// ADM-21: o texto diz o que vai acontecer, com o nome do projeto
it("mostra o título do projeto que será apagado", () => {
  renderizar();

  expect(screen.getByText(new RegExp(TITULO)).textContent).toContain(TITULO);
});
