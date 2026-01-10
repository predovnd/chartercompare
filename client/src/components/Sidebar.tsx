import { Sparkles, ArrowRight } from 'lucide-react';
import { Input } from './ui/input';
import { Button } from './ui/button';
import { useState } from 'react';
import { useNavigate } from 'react-router-dom';

function QuoteLinkInput() {
  const [sessionId, setSessionId] = useState('');
  const navigate = useNavigate();

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (sessionId.trim()) {
      navigate(`/quotes/${sessionId.trim()}`);
    }
  };

  return (
    <form onSubmit={handleSubmit} className="flex gap-2">
      <Input
        value={sessionId}
        onChange={(e) => setSessionId(e.target.value)}
        placeholder="Paste your link or code"
        className="flex-1 rounded-lg"
      />
      <Button type="submit" size="icon" className="shrink-0 rounded-lg" disabled={!sessionId.trim()}>
        <ArrowRight className="h-4 w-4" />
      </Button>
    </form>
  );
}

export function Sidebar() {
  return (
    <div className="space-y-8">
      {/* How It Works */}
      <div>
        <div className="flex items-center justify-center mb-6">
          <Sparkles className="h-12 w-12 text-primary stroke-2" />
        </div>
        <h2 className="text-2xl font-bold mb-6">How It Works</h2>
        <ol className="space-y-4 text-muted-foreground">
          <li className="flex gap-3">
            <span className="font-semibold text-foreground">1.</span>
            <span>Chat with us about your trip details</span>
          </li>
          <li className="flex gap-3">
            <span className="font-semibold text-foreground">2.</span>
            <span>Operators will send you quotes</span>
          </li>
          <li className="flex gap-3">
            <span className="font-semibold text-foreground">3.</span>
            <span>Get a link via email/SMS to view and compare quotes anytime</span>
          </li>
        </ol>
      </div>

      {/* Already have a quote link */}
      <div className="pt-8 border-t">
        <p className="text-sm font-medium mb-3">Already have a quote link?</p>
        <QuoteLinkInput />
      </div>
    </div>
  );
}
