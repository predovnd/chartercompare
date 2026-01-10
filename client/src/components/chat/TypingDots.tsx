import { motion } from 'framer-motion';

export function TypingDots() {
  return (
    <div className="flex items-center gap-1 px-4 py-2">
      <motion.div
        className="h-2 w-2 rounded-full bg-muted-foreground"
        animate={{ opacity: [0.4, 1, 0.4] }}
        transition={{ duration: 1.4, repeat: Infinity, delay: 0 }}
      />
      <motion.div
        className="h-2 w-2 rounded-full bg-muted-foreground"
        animate={{ opacity: [0.4, 1, 0.4] }}
        transition={{ duration: 1.4, repeat: Infinity, delay: 0.2 }}
      />
      <motion.div
        className="h-2 w-2 rounded-full bg-muted-foreground"
        animate={{ opacity: [0.4, 1, 0.4] }}
        transition={{ duration: 1.4, repeat: Infinity, delay: 0.4 }}
      />
    </div>
  );
}
