import { ChatWidget } from './chat/ChatWidget';

export function ChatSection() {
  return (
    <div className="space-y-8">
      <div className="max-w-2xl">
        <h1 className="text-4xl md:text-5xl font-bold tracking-tight mb-4">
          Plan your group travel{' '}
          <span className="text-primary">in minutes, not days.</span>
        </h1>
        <p className="text-lg text-muted-foreground">
          Chat with our AI to get instant comparisons from top charter operators.
        </p>
      </div>
      <div className="max-w-3xl">
        <ChatWidget />
      </div>
    </div>
  );
}
