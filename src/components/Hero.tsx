import { Button } from './ui/button';
import { Check } from 'lucide-react';

export function Hero() {
  return (
    <section className="container py-16 md:py-24">
      <div className="max-w-3xl">
        <h1 className="text-4xl md:text-5xl font-bold tracking-tight mb-6">
          Find the perfect charter bus in minutes
        </h1>
        <p className="text-xl text-muted-foreground mb-8">
          Compare quotes from licensed operators across Australia. Get personalized recommendations based on your trip details.
        </p>
        <div className="mb-12">
          <Button size="lg" variant="outline" className="text-base rounded-full" asChild>
            <a href="#how-it-works">Learn more</a>
          </Button>
        </div>
        <div className="space-y-3">
          <div className="flex items-center gap-3 text-muted-foreground">
            <Check className="h-5 w-5 text-primary" />
            <span>Licensed operators only</span>
          </div>
          <div className="flex items-center gap-3 text-muted-foreground">
            <Check className="h-5 w-5 text-primary" />
            <span>Fast turnaround — quotes in 24 hours</span>
          </div>
          <div className="flex items-center gap-3 text-muted-foreground">
            <Check className="h-5 w-5 text-primary" />
            <span>No obligation, free comparison</span>
          </div>
        </div>
      </div>
    </section>
  );
}
