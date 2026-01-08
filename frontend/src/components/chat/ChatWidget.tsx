import { useState, useEffect, useRef } from 'react';
import { Card, CardContent, CardHeader } from '../ui/card';
import { Button } from '../ui/button';
import { ScrollArea } from '../ui/scroll-area';
import { ChatMessageList } from './ChatMessageList';
import { ChatComposer } from './ChatComposer';
import { RotateCcw } from 'lucide-react';
import { startChat, sendMessage } from '@/lib/api';
import type { ChatMessage, CharterRequest } from '@/types';
import { motion } from 'framer-motion';
import { Card as SuccessCard, CardContent as SuccessCardContent, CardHeader as SuccessCardHeader, CardTitle as SuccessCardTitle } from '../ui/card';
import {
  Accordion,
  AccordionContent,
  AccordionItem,
  AccordionTrigger,
} from '../ui/accordion';

export function ChatWidget() {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [isTyping, setIsTyping] = useState(false);
  const [sessionId, setSessionId] = useState<string | null>(null);
  const [isComplete, setIsComplete] = useState(false);
  const [finalPayload, setFinalPayload] = useState<CharterRequest | null>(null);

  useEffect(() => {
    // Always initialize chat on first load
    initializeChat();
  }, []);

  const initializeChat = async () => {
    setIsTyping(true);
    try {
      const { sessionId: newSessionId, replyText, icon } = await startChat();
      setSessionId(newSessionId);
      const botMessage: ChatMessage = {
        id: `msg-${Date.now()}`,
        text: replyText,
        sender: 'bot',
        timestamp: new Date(),
        icon,
      };
      setMessages([botMessage]);
    } catch (error) {
      console.error('Failed to start chat:', error);
      const errorMessage: ChatMessage = {
        id: `msg-${Date.now()}-error`,
        text: "Sorry, I'm having trouble connecting. Please check if the API server is running.",
        sender: 'bot',
        timestamp: new Date(),
        icon: 'AlertCircle',
      };
      setMessages([errorMessage]);
    } finally {
      setIsTyping(false);
    }
  };

  const handleSend = async (text: string) => {
    if (!sessionId || isTyping) return;

    const userMessage: ChatMessage = {
      id: `msg-${Date.now()}-user`,
      text,
      sender: 'user',
      timestamp: new Date(),
    };

    setMessages((prev) => [...prev, userMessage]);
    setIsTyping(true);

    try {
      const { replyText, isComplete: complete, finalPayload: payload, icon } = await sendMessage(sessionId, text);
      
      if (complete && payload) {
        setIsComplete(true);
        setFinalPayload(payload);
      }

      const botMessage: ChatMessage = {
        id: `msg-${Date.now()}-bot`,
        text: replyText,
        sender: 'bot',
        timestamp: new Date(),
        icon,
      };

      setMessages((prev) => [...prev, botMessage]);
    } catch (error) {
      console.error('Failed to send message:', error);
      const errorMessage: ChatMessage = {
        id: `msg-${Date.now()}-error`,
        text: error instanceof Error 
          ? `Sorry, ${error.message}. Please try again.`
          : "Sorry, something went wrong. Please try again.",
        sender: 'bot',
        timestamp: new Date(),
        icon: 'AlertCircle',
      };
      setMessages((prev) => [...prev, errorMessage]);
    } finally {
      setIsTyping(false);
    }
  };

  const handleReset = () => {
    setMessages([]);
    setSessionId(null);
    setIsComplete(false);
    setFinalPayload(null);
    initializeChat();
  };

  return (
    <Card className="h-full flex flex-col shadow-sm min-h-[600px] lg:min-h-0">
      <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-3 border-b">
        <div className="flex items-center gap-2">
          <div className="h-2 w-2 rounded-full bg-green-500"></div>
          <span className="text-sm text-muted-foreground">AI Concierge Active</span>
        </div>
        <Button
          variant="ghost"
          size="icon"
          className="h-8 w-8"
          onClick={handleReset}
          title="Reset chat"
        >
          <RotateCcw className="h-4 w-4" />
        </Button>
      </CardHeader>
      <CardContent className="flex-1 flex flex-col p-0 min-h-0">
        <div className="flex-1 min-h-0">
          <ChatMessageList messages={messages} isTyping={isTyping} />
        </div>
        {isComplete && finalPayload && (
          <motion.div
            initial={{ opacity: 0, y: 20 }}
            animate={{ opacity: 1, y: 0 }}
            className="p-4 border-t bg-muted/30"
          >
            <SuccessCard>
              <SuccessCardHeader>
                <SuccessCardTitle className="text-lg">Request submitted!</SuccessCardTitle>
              </SuccessCardHeader>
              <SuccessCardContent>
                <p className="text-sm text-muted-foreground mb-4">
                  Your request has been received. We'll send comparison results to {finalPayload.customer.email} within 24 hours.
                </p>
                <Accordion type="single" collapsible>
                  <AccordionItem value="debug">
                    <AccordionTrigger className="text-sm">Request JSON (debug)</AccordionTrigger>
                    <AccordionContent>
                      <pre className="text-xs bg-muted p-3 rounded-md overflow-auto max-h-64">
                        {JSON.stringify(finalPayload, null, 2)}
                      </pre>
                    </AccordionContent>
                  </AccordionItem>
                </Accordion>
              </SuccessCardContent>
            </SuccessCard>
          </motion.div>
        )}
        {!isComplete && (
          <ChatComposer onSend={handleSend} disabled={isTyping} />
        )}
      </CardContent>
    </Card>
  );
}
