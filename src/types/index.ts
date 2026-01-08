export interface ChatMessage {
  id: string;
  text: string;
  sender: 'user' | 'bot';
  timestamp: Date;
  icon?: string; // Icon name from lucide-react
}

export interface ChatState {
  sessionId: string;
  step: ChatStep;
  data: Partial<CharterRequest>;
  isComplete: boolean;
  waitingForDateConfirmation?: boolean;
}

export type ChatStep =
  | 'tripType'
  | 'passengerCount'
  | 'date'
  | 'pickup'
  | 'destination'
  | 'tripFormat'
  | 'timing'
  | 'requirements'
  | 'email'
  | 'complete';

export interface CharterRequest {
  customer: {
    firstName: string;
    lastName: string;
    phone: string;
    email: string;
  };
  trip: {
    type: string;
    passengerCount: number;
    date: {
      rawInput: string;
      resolvedDate: string;
      confidence: 'low' | 'medium' | 'high';
    };
    pickupLocation: {
      rawInput: string;
      resolvedName: string;
      suburb: string;
      state: string;
      lat: number | null;
      lng: number | null;
      confidence: 'low' | 'medium';
    };
    destination: {
      rawInput: string;
      resolvedName: string;
      suburb: string;
      state: string;
      lat: number | null;
      lng: number | null;
      confidence: 'low' | 'medium';
    };
    tripFormat: 'one_way' | 'return_same_day';
    timing: {
      rawInput: string;
      pickupTime: string;
      returnTime: string;
    };
    specialRequirements: string[];
  };
  meta: {
    source: string;
    createdAt: string;
  };
}
