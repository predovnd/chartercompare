import { Card, CardDescription, CardHeader, CardTitle } from './ui/card';
import { MessageCircle, FileText, CheckCircle } from 'lucide-react';

export function HowItWorks() {
  return (
    <section id="how-it-works" className="container py-16 md:py-24">
      <div className="text-center mb-12">
        <h2 className="text-3xl md:text-4xl font-bold mb-4">How it works</h2>
        <p className="text-lg text-muted-foreground max-w-2xl mx-auto">
          Getting the best charter bus quote is simple and straightforward
        </p>
      </div>
      <div className="grid md:grid-cols-3 gap-6">
        <Card>
          <CardHeader>
            <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center mb-4">
              <MessageCircle className="h-6 w-6 text-primary" />
            </div>
            <CardTitle>1. Tell us about your trip</CardTitle>
            <CardDescription>
              Chat with Alex to share your trip details — destination, date, passenger count, and any special requirements.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center mb-4">
              <FileText className="h-6 w-6 text-primary" />
            </div>
            <CardTitle>2. We compare options</CardTitle>
            <CardDescription>
              Our system matches your request with licensed operators and prepares a personalized comparison.
            </CardDescription>
          </CardHeader>
        </Card>
        <Card>
          <CardHeader>
            <div className="h-12 w-12 rounded-full bg-primary/10 flex items-center justify-center mb-4">
              <CheckCircle className="h-6 w-6 text-primary" />
            </div>
            <CardTitle>3. Receive your quotes</CardTitle>
            <CardDescription>
              Get detailed quotes via email within 24 hours. Compare options and book directly with your preferred operator.
            </CardDescription>
          </CardHeader>
        </Card>
      </div>
    </section>
  );
}
