const API_URL = process.env.NEXT_PUBLIC_API_URL ?? "http://localhost:5002";

export interface Project {
  id: number;
  title: string;
  description: string;
  tags: string[];
  githubUrl?: string;
  liveUrl?: string;
  imageUrl?: string;
  createdAt: string;
}

export async function getProjects(): Promise<Project[]> {
  try {
    const res = await fetch(`${API_URL}/api/projects`, {
      next: { revalidate: 60 },
    });
    if (!res.ok) return [];
    return res.json();
  } catch {
    return [];
  }
}

export interface ContactPayload {
  name: string;
  email: string;
  message: string;
}

export interface ContactResult {
  success: boolean;
  error?: string;
}

export async function sendContactMessage(payload: ContactPayload): Promise<ContactResult> {
  try {
    const res = await fetch(`${API_URL}/api/contact`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    });

    if (res.status === 503) {
      return { success: false, error: "Too many messages sent. Try again in a minute." };
    }
    if (!res.ok) {
      return { success: false, error: "Failed to send message. Try again later." };
    }
    return { success: true };
  } catch {
    return { success: false, error: "Could not reach the server. Try again later." };
  }
}
