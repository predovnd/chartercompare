import type { ChatMessage, CharterRequest } from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export interface StartChatResponse {
  sessionId: string;
  replyText: string;
  icon?: string;
}

export interface SendMessageResponse {
  replyText: string;
  isComplete: boolean;
  finalPayload?: CharterRequest;
  icon?: string;
}

async function fetchAPI<T>(endpoint: string, options?: RequestInit): Promise<T> {
  const response = await fetch(`${API_BASE_URL}${endpoint}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...options?.headers,
    },
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({ error: 'Unknown error' }));
    throw new Error(error.error || `HTTP error! status: ${response.status}`);
  }

  return response.json();
}

export async function startChat(): Promise<StartChatResponse> {
  return fetchAPI<StartChatResponse>('/api/chat/start', {
    method: 'POST',
    body: JSON.stringify({}),
  });
}

export async function sendMessage(
  sessionId: string,
  text: string
): Promise<SendMessageResponse> {
  return fetchAPI<SendMessageResponse>('/api/chat/message', {
    method: 'POST',
    body: JSON.stringify({
      sessionId,
      text,
    }),
  });
}
