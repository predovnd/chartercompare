import type { ChatState, CharterRequest, ChatStep } from '@/types';

const STORAGE_KEY = 'charter_compare_chat_state';

// Simulate API latency
const delay = (ms: number) => new Promise(resolve => setTimeout(resolve, ms));

// Generate session ID
function generateSessionId(): string {
  return `session_${Date.now()}_${Math.random().toString(36).slice(2, 11)}`;
}

// Validate email
function isValidEmail(email: string): boolean {
  const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
  return regex.test(email);
}

// Parse passenger count
function parsePassengerCount(text: string): number | null {
  const match = text.match(/\d+/);
  return match ? parseInt(match[0], 10) : null;
}

// Check if date input suggests multi-day
function isMultiDayInput(text: string): boolean {
  const lower = text.toLowerCase();
  return /(\d+\s*-\s*\d+|\d+\s*to\s*\d+|\d+\s*thru\s*\d+)/.test(lower) ||
         /\d+\s*days?/.test(lower) ||
         /overnight|multi.?day|multiple\s*days?/.test(lower);
}

// Try to parse date
function parseDate(text: string): { resolvedDate: string; confidence: 'low' | 'medium' | 'high' } {
  // Simple YYYY-MM-DD format
  const isoMatch = text.match(/(\d{4})-(\d{2})-(\d{2})/);
  if (isoMatch) {
    return { resolvedDate: isoMatch[0], confidence: 'high' };
  }

  // Check for day + month name (medium confidence)
  const monthNames = ['january', 'february', 'march', 'april', 'may', 'june',
    'july', 'august', 'september', 'october', 'november', 'december'];
  const monthAbbr = ['jan', 'feb', 'mar', 'apr', 'may', 'jun',
    'jul', 'aug', 'sep', 'oct', 'nov', 'dec'];
  
  const lower = text.toLowerCase();
  const hasMonth = monthNames.some(m => lower.includes(m)) || 
                   monthAbbr.some(m => lower.includes(m));
  const hasDay = /\d{1,2}/.test(text);

  if (hasMonth && hasDay) {
    return { resolvedDate: '', confidence: 'medium' };
  }

  return { resolvedDate: '', confidence: 'low' };
}

// Check if trip format is clear
function isTripFormatClear(text: string): 'one_way' | 'return_same_day' | 'unclear' {
  const lower = text.toLowerCase();
  if (/one.?way|single.?way|one.?direction/.test(lower)) {
    return 'one_way';
  }
  if (/return|round.?trip|same.?day|back|returning/.test(lower)) {
    return 'return_same_day';
  }
  return 'unclear';
}

// Check if requirements are "none"
function parseRequirements(text: string): string[] {
  const lower = text.toLowerCase().trim();
  if (/^(none|no|n\/a|na|nothing)$/.test(lower)) {
    return [];
  }
  // Split by common delimiters and clean
  return text.split(/[,;]/).map(s => s.trim()).filter(s => s.length > 0);
}

export function loadChatState(): ChatState | null {
  try {
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored) {
      const parsed = JSON.parse(stored);
      // Convert timestamp strings back to Date objects for messages
      return parsed;
    }
  } catch (e) {
    console.error('Failed to load chat state:', e);
  }
  return null;
}

export function saveChatState(state: ChatState): void {
  try {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(state));
  } catch (e) {
    console.error('Failed to save chat state:', e);
  }
}

export function clearChatState(): void {
  try {
    localStorage.removeItem(STORAGE_KEY);
  } catch (e) {
    console.error('Failed to clear chat state:', e);
  }
}

// Get icon for chat step
function getIconForStep(step: ChatStep, isError?: boolean): string {
  if (isError) return 'AlertCircle';
  
  switch (step) {
    case 'tripType':
      return 'Calendar';
    case 'passengerCount':
      return 'Users';
    case 'date':
      return 'CalendarDays';
    case 'pickup':
      return 'MapPin';
    case 'destination':
      return 'Navigation';
    case 'tripFormat':
      return 'ArrowLeftRight';
    case 'timing':
      return 'Clock';
    case 'requirements':
      return 'ListChecks';
    case 'email':
      return 'Mail';
    case 'complete':
      return 'CheckCircle';
    default:
      return 'MessageCircle';
  }
}

export async function startChat(): Promise<{ sessionId: string; replyText: string; icon?: string }> {
  await delay(300 + Math.random() * 500);
  
  const sessionId = generateSessionId();
  const replyText = "Hey — it's Alex from Charter Compare. I can help you sort a charter bus. What's the trip for — for example a school trip, corporate event, wedding, sports team, or something else?";

  const initialState: ChatState = {
    sessionId,
    step: 'tripType',
    data: {},
    isComplete: false,
  };

  saveChatState(initialState);

  return { sessionId, replyText, icon: getIconForStep('tripType') };
}

export async function sendMessage(
  sessionId: string,
  text: string
): Promise<{ replyText: string; isComplete: boolean; finalPayload?: CharterRequest; icon?: string }> {
  await delay(300 + Math.random() * 500);

  const state = loadChatState();
  if (!state || state.sessionId !== sessionId) {
    throw new Error('Invalid session');
  }

  let newState: ChatState = { ...state };
  let replyText = '';
  let isComplete = false;
  let finalPayload: CharterRequest | undefined;
  let icon: string | undefined;
  let isError = false;

  switch (state.step) {
    case 'tripType':
      newState.data.trip = { ...newState.data.trip, type: text.trim() };
      newState.step = 'passengerCount';
      replyText = "About how many passengers will be travelling?";
      icon = getIconForStep('passengerCount');
      break;

    case 'passengerCount': {
      const count = parsePassengerCount(text);
      if (count === null || count <= 0) {
        replyText = "I need a number — about how many passengers will be travelling?";
        icon = getIconForStep('passengerCount', true);
        isError = true;
        break;
      }
      newState.data.trip = { ...newState.data.trip, passengerCount: count };
      newState.step = 'date';
      replyText = "What date is the trip? We're just booking single-day trips at the moment.";
      icon = getIconForStep('date');
      break;
    }

    case 'date': {
      // Check if previous message was asking for confirmation
      if (state.waitingForDateConfirmation) {
        const lower = text.toLowerCase();
        if (lower.includes('yes') || lower.includes('yeah') || lower.includes('yep') || lower.includes('sure') || lower.includes('ok')) {
          newState.waitingForDateConfirmation = false;
          replyText = "What date is the trip?";
          icon = getIconForStep('date');
          break;
        } else {
          replyText = "No worries — we'll keep that in mind for future. For now, what date would work for a single-day trip?";
          newState.waitingForDateConfirmation = false;
          icon = getIconForStep('date');
          break;
        }
      }
      if (isMultiDayInput(text)) {
        replyText = "At the moment we can only help with single-day trips — would it still work as a one-day booking?";
        newState.waitingForDateConfirmation = true;
        icon = getIconForStep('date');
        break;
      }
      const dateParse = parseDate(text);
      newState.data.trip = {
        ...newState.data.trip,
        date: {
          rawInput: text.trim(),
          resolvedDate: dateParse.resolvedDate,
          confidence: dateParse.confidence,
        },
      };
      newState.step = 'pickup';
      replyText = "Where will everyone be picked up from? A suburb, landmark, or postcode is fine.";
      icon = getIconForStep('pickup');
      break;
    }

    case 'pickup': {
      newState.data.trip = {
        ...newState.data.trip,
        pickupLocation: {
          rawInput: text.trim(),
          resolvedName: '',
          suburb: '',
          state: '',
          lat: null,
          lng: null,
          confidence: 'low',
        },
      };
      newState.step = 'destination';
      replyText = "And where's the main destination or drop-off?";
      icon = getIconForStep('destination');
      break;
    }

    case 'destination': {
      newState.data.trip = {
        ...newState.data.trip,
        destination: {
          rawInput: text.trim(),
          resolvedName: '',
          suburb: '',
          state: '',
          lat: null,
          lng: null,
          confidence: 'low',
        },
      };
      newState.step = 'tripFormat';
      replyText = "Is it a one-way trip or a return on the same day?";
      icon = getIconForStep('tripFormat');
      break;
    }

    case 'tripFormat': {
      const format = isTripFormatClear(text);
      if (format === 'unclear') {
        replyText = "Just to confirm — one-way, or return on the same day?";
        icon = getIconForStep('tripFormat', true);
        isError = true;
        break;
      }
      newState.data.trip = { ...newState.data.trip, tripFormat: format };
      newState.step = 'timing';
      replyText = "Do you have rough pickup and return times?";
      icon = getIconForStep('timing');
      break;
    }

    case 'timing': {
      newState.data.trip = {
        ...newState.data.trip,
        timing: {
          rawInput: text.trim(),
          pickupTime: '',
          returnTime: '',
        },
      };
      newState.step = 'requirements';
      replyText = "Any special requirements? For example luggage space, wheelchair access, or onboard features.";
      icon = getIconForStep('requirements');
      break;
    }

    case 'requirements': {
      const requirements = parseRequirements(text);
      newState.data.trip = { ...newState.data.trip, specialRequirements: requirements };
      newState.step = 'email';
      replyText = "What's the best email address to send the comparison results to?";
      icon = getIconForStep('email');
      break;
    }

    case 'email': {
      if (!isValidEmail(text.trim())) {
        replyText = "That doesn't look like a valid email — what's the best email to send the results to?";
        icon = getIconForStep('email', true);
        isError = true;
        break;
      }
      newState.data.customer = { ...newState.data.customer, email: text.trim() };
      newState.step = 'complete';
      replyText = "Great — I've got everything I need. I'll pass this through now and someone will be in touch shortly with the best available options.";
      isComplete = true;
      icon = getIconForStep('complete');

      // Build final payload
      const trip = newState.data.trip!;
      finalPayload = {
        customer: {
          firstName: newState.data.customer?.firstName || '',
          lastName: newState.data.customer?.lastName || '',
          phone: newState.data.customer?.phone || '',
          email: newState.data.customer?.email || '',
        },
        trip: {
          type: trip.type || '',
          passengerCount: trip.passengerCount || 0,
          date: trip.date || { rawInput: '', resolvedDate: '', confidence: 'low' },
          pickupLocation: trip.pickupLocation || {
            rawInput: '',
            resolvedName: '',
            suburb: '',
            state: '',
            lat: null,
            lng: null,
            confidence: 'low',
          },
          destination: trip.destination || {
            rawInput: '',
            resolvedName: '',
            suburb: '',
            state: '',
            lat: null,
            lng: null,
            confidence: 'low',
          },
          tripFormat: trip.tripFormat || 'one_way',
          timing: trip.timing || { rawInput: '', pickupTime: '', returnTime: '' },
          specialRequirements: trip.specialRequirements || [],
        },
        meta: {
          source: 'webchat',
          createdAt: new Date().toISOString(),
        },
      };
      break;
    }

    default:
      replyText = "I'm not sure how to help with that. Could you try rephrasing?";
      icon = 'HelpCircle';
  }

  newState.isComplete = isComplete;
  saveChatState(newState);

  return { replyText, isComplete, finalPayload, icon };
}
