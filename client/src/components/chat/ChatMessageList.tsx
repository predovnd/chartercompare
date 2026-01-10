import { useEffect, useRef } from 'react';
import { ScrollArea } from '../ui/scroll-area';
import { ChatMessage } from '@/types';
import { TypingDots } from './TypingDots';
import { motion } from 'framer-motion';
import * as Icons from 'lucide-react';

// Helper to get icon component by name
function getIconComponent(iconName?: string) {
  if (!iconName) return null;
  const IconComponent = (Icons as any)[iconName];
  return IconComponent ? <IconComponent className="h-4 w-4" /> : null;
}

interface ChatMessageListProps {
  messages: ChatMessage[];
  isTyping: boolean;
}

export function ChatMessageList({ messages, isTyping }: ChatMessageListProps) {
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const viewportRef = useRef<HTMLDivElement>(null);
  const containerRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const scrollToBottom = () => {
      if (viewportRef.current) {
        // Directly scroll the viewport element (preferred method)
        viewportRef.current.scrollTop = viewportRef.current.scrollHeight;
      } else if (containerRef.current) {
        // Fallback: find the viewport within this specific container
        const viewport = containerRef.current.querySelector('[data-radix-scroll-area-viewport]') as HTMLElement;
        if (viewport) {
          viewport.scrollTop = viewport.scrollHeight;
        }
      }
    };

    // Use a small delay to ensure DOM is fully updated, especially with framer-motion animations
    const timeoutId = setTimeout(scrollToBottom, 150);
    return () => clearTimeout(timeoutId);
  }, [messages, isTyping]);

  return (
    <div ref={containerRef} className="flex-1 min-h-0">
      <ScrollArea className="h-full px-4" ref={viewportRef}>
      <div className="space-y-4 py-4">
        {messages.map((message, index) => (
          <motion.div
            key={message.id}
            initial={{ opacity: 0, y: 10 }}
            animate={{ opacity: 1, y: 0 }}
            transition={{ duration: 0.2, delay: index * 0.05 }}
            className={`flex items-start gap-2 ${message.sender === 'user' ? 'justify-end' : 'justify-start'}`}
          >
            {message.sender === 'bot' && message.icon && (
              <div className="mt-1.5 h-5 w-5 flex items-center justify-center text-muted-foreground shrink-0">
                {getIconComponent(message.icon)}
              </div>
            )}
            <div
              className={`max-w-[80%] rounded-2xl px-4 py-2.5 ${
                message.sender === 'user'
                  ? 'bg-primary text-primary-foreground'
                  : 'bg-muted text-foreground'
              }`}
            >
              <p className="text-sm whitespace-pre-wrap">{message.text}</p>
            </div>
          </motion.div>
        ))}
        {isTyping && (
          <div className="flex justify-start">
            <div className="bg-muted rounded-2xl">
              <TypingDots />
            </div>
          </div>
        )}
        <div ref={messagesEndRef} />
      </div>
      </ScrollArea>
    </div>
  );
}
