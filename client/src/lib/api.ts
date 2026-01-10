import type { CharterRequest } from '@/types';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

console.log('API_BASE_URL configured as:', API_BASE_URL);

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
  const url = `${API_BASE_URL}${endpoint}`;
  console.log('API Request:', url, options);
  
  try {
    const response = await fetch(url, {
      ...options,
      credentials: 'include', // Include cookies for authentication
      headers: {
        'Content-Type': 'application/json',
        ...options?.headers,
      },
    });

    console.log('API Response:', response.status, response.statusText);

    if (!response.ok) {
      const error = await response.json().catch(() => ({ error: 'Unknown error' }));
      console.error('API Error:', error);
      throw new Error(error.error || `HTTP error! status: ${response.status}`);
    }

    const data = await response.json();
    console.log('API Data:', data);
    return data;
  } catch (error) {
    console.error('API Fetch Error:', error);
    if (error instanceof TypeError && error.message.includes('fetch')) {
      throw new Error(`Cannot connect to API at ${url}. Is the backend running?`);
    }
    throw error;
  }
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
