import AdminPanel from "@/components/admin/AdminPanel";

// A página é server component só para carregar o painel; todo o estado é do cliente, porque a
// sessão vive no sessionStorage do browser e a escrita não pode passar por cache (design D-13).
export default function AdminPage() {
  return <AdminPanel />;
}
