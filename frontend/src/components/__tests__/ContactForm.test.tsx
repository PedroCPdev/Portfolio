// @vitest-environment jsdom
import { it, expect, vi, afterEach } from "vitest";
import { render, screen, cleanup, fireEvent, waitFor } from "@testing-library/react";
import ContactForm from "../ContactForm";
import { sendContactMessage } from "@/lib/api";

vi.mock("@/lib/api", () => ({ sendContactMessage: vi.fn(async () => ({ success: true })) }));

afterEach(() => {
  cleanup();
  vi.clearAllMocks();
});

function honeypot(container: HTMLElement) {
  return container.querySelector<HTMLInputElement>('input[name="website"]');
}

// AS-02: o campo existe, mas pessoa nenhuma chega nele pela tela, pelo teclado ou pelo leitor de tela.
it("tem o campo website fora do alcance de quem usa o formulário", () => {
  const { container } = render(<ContactForm />);
  const campo = honeypot(container);

  expect(campo).not.toBeNull();
  expect(campo!.tabIndex).toBe(-1);
  expect(campo!.autocomplete).toBe("off");
  expect(campo!.closest('[aria-hidden="true"]')).not.toBeNull();
  expect(screen.queryByRole("textbox", { name: /website/i })).toBeNull();
});

// AS-02: o valor vai no corpo, que é o que a API usa para descartar a mensagem.
it("envia o valor do campo website junto com a mensagem", async () => {
  const { container } = render(<ContactForm />);

  fireEvent.change(screen.getByLabelText("Name"), { target: { value: "Visitante" } });
  fireEvent.change(screen.getByLabelText("Email"), { target: { value: "visitante@exemplo.dev" } });
  fireEvent.change(screen.getByLabelText("Message"), { target: { value: "Olá" } });
  fireEvent.change(honeypot(container)!, { target: { value: "https://spam.example" } });
  fireEvent.submit(container.querySelector("form")!);

  await waitFor(() => expect(sendContactMessage).toHaveBeenCalledTimes(1));
  expect(sendContactMessage).toHaveBeenCalledWith({
    name: "Visitante",
    email: "visitante@exemplo.dev",
    message: "Olá",
    website: "https://spam.example",
  });
});
