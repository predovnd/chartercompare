import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Textarea } from '../components/ui/textarea';
import { LogOut, DollarSign, Bus, User, Building2 } from 'lucide-react';
import { useNavigate, Link } from 'react-router-dom';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface Provider {
  id: number;
  email: string;
  name: string;
  companyName?: string;
}

interface Request {
  id: number;
  sessionId: string;
  requestData: any;
  createdAt: string;
  status: string;
}

export function ProviderDashboard() {
  const [provider, setProvider] = useState<Provider | null>(null);
  const [requests, setRequests] = useState<Request[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    checkAuth();
    loadRequests();
  }, []);

  const checkAuth = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
        credentials: 'include',
      });
      if (response.ok) {
        const data = await response.json();
        setProvider(data);
      } else {
        navigate('/provider/login');
      }
    } catch (error) {
      console.error('Auth check failed:', error);
      navigate('/provider/login');
    } finally {
      setLoading(false);
    }
  };

  const loadRequests = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/provider/requests`, {
        credentials: 'include',
      });
      if (response.ok) {
        const data = await response.json();
        setRequests(data);
      }
    } catch (error) {
      console.error('Failed to load requests:', error);
    }
  };

  const handleLogout = async () => {
    try {
      await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
      });
      navigate('/');
    } catch (error) {
      console.error('Logout failed:', error);
      // Even if logout fails, redirect to home page
      navigate('/');
    }
  };

  if (loading) {
    return (
      <div className="min-h-screen flex items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-muted/30">
      {/* Header */}
      <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
        <div className="container flex h-16 items-center justify-between">
          <Link to="/" className="flex items-center gap-2 hover:opacity-80 transition-opacity">
            <Bus className="h-6 w-6 text-primary" />
            <span className="text-xl font-semibold">CharterCompare</span>
            <span className="text-sm text-muted-foreground ml-2 hidden sm:inline">Operator Portal</span>
          </Link>
          
          <div className="flex items-center gap-4">
            {/* Operator Info */}
            <div className="hidden md:flex items-center gap-3 px-4 py-2 rounded-lg bg-muted/50 border">
              {provider?.companyName ? (
                <Building2 className="h-4 w-4 text-muted-foreground" />
              ) : (
                <User className="h-4 w-4 text-muted-foreground" />
              )}
              <div className="flex flex-col">
                <span className="text-sm font-medium leading-tight">{provider?.name}</span>
                {provider?.companyName && (
                  <span className="text-xs text-muted-foreground leading-tight">{provider.companyName}</span>
                )}
              </div>
            </div>
            
            {/* Logout Button */}
            <Button variant="outline" onClick={handleLogout} size="sm">
              <LogOut className="h-4 w-4 mr-2" />
              <span className="hidden sm:inline">Logout</span>
            </Button>
          </div>
        </div>
      </header>

      {/* Main Content */}
      <div className="container py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">Dashboard</h1>
          <p className="text-muted-foreground">
            Manage quotes and view charter bus requests
          </p>
        </div>

        <div className="grid gap-6">
          <Card>
            <CardHeader>
              <CardTitle>Open Requests</CardTitle>
              <CardDescription>
                View and submit quotes for charter bus requests
              </CardDescription>
            </CardHeader>
            <CardContent>
              {requests.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">
                  No open requests at the moment
                </p>
              ) : (
                <div className="space-y-4">
                  {requests.map((request) => (
                    <RequestCard
                      key={request.id}
                      request={request}
                      onQuoteSubmitted={loadRequests}
                    />
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        </div>
      </div>
    </div>
  );
}

function RequestCard({ request, onQuoteSubmitted }: { request: Request; onQuoteSubmitted: () => void }) {
  const [showQuoteForm, setShowQuoteForm] = useState(false);
  const [price, setPrice] = useState('');
  const [notes, setNotes] = useState('');
  const [submitting, setSubmitting] = useState(false);

  const handleSubmitQuote = async () => {
    if (!price || isNaN(parseFloat(price))) {
      alert('Please enter a valid price');
      return;
    }

    setSubmitting(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/provider/requests/${request.id}/quotes`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          price: parseFloat(price),
          currency: 'AUD',
          notes: notes || null,
        }),
      });

      if (response.ok) {
        setShowQuoteForm(false);
        setPrice('');
        setNotes('');
        onQuoteSubmitted();
        alert('Quote submitted successfully!');
      } else {
        alert('Failed to submit quote');
      }
    } catch (error) {
      console.error('Failed to submit quote:', error);
      alert('Failed to submit quote');
    } finally {
      setSubmitting(false);
    }
  };

  const data = request.requestData;
  return (
    <Card>
      <CardHeader>
        <div className="flex justify-between items-start">
          <div>
            <CardTitle className="text-lg">Request #{request.id}</CardTitle>
            <CardDescription>
              {data.trip?.type} • {data.trip?.passengerCount} passengers • {data.trip?.date?.rawInput}
            </CardDescription>
          </div>
          <Button
            onClick={() => setShowQuoteForm(!showQuoteForm)}
            variant={showQuoteForm ? 'outline' : 'default'}
          >
            <DollarSign className="h-4 w-4 mr-2" />
            {showQuoteForm ? 'Cancel' : 'Submit Quote'}
          </Button>
        </div>
      </CardHeader>
      <CardContent>
        <div className="grid md:grid-cols-2 gap-4 mb-4">
          <div>
            <p className="text-sm font-medium text-muted-foreground">Pickup</p>
            <p>{data.trip?.pickupLocation?.rawInput || 'N/A'}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-muted-foreground">Destination</p>
            <p>{data.trip?.destination?.rawInput || 'N/A'}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-muted-foreground">Trip Format</p>
            <p>{data.trip?.tripFormat?.replace('_', ' ') || 'N/A'}</p>
          </div>
          <div>
            <p className="text-sm font-medium text-muted-foreground">Customer Email</p>
            <p>{data.customer?.email || 'N/A'}</p>
          </div>
        </div>

        {data.trip?.specialRequirements && data.trip.specialRequirements.length > 0 && (
          <div className="mb-4">
            <p className="text-sm font-medium text-muted-foreground mb-2">Special Requirements</p>
            <ul className="list-disc list-inside text-sm">
              {data.trip.specialRequirements.map((req: string, idx: number) => (
                <li key={idx}>{req}</li>
              ))}
            </ul>
          </div>
        )}

        {showQuoteForm && (
          <Card className="mt-4 bg-muted/50">
            <CardHeader>
              <CardTitle className="text-lg">Submit Quote</CardTitle>
            </CardHeader>
            <CardContent className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">Price (AUD)</label>
                <Input
                  type="number"
                  step="0.01"
                  value={price}
                  onChange={(e) => setPrice(e.target.value)}
                  placeholder="0.00"
                />
              </div>
              <div>
                <label className="text-sm font-medium mb-2 block">Notes (optional)</label>
                <Textarea
                  value={notes}
                  onChange={(e) => setNotes(e.target.value)}
                  placeholder="Add any additional details or conditions..."
                  className="min-h-[100px]"
                />
              </div>
              <Button
                onClick={handleSubmitQuote}
                disabled={submitting || !price}
                className="w-full"
              >
                {submitting ? 'Submitting...' : 'Submit Quote'}
              </Button>
            </CardContent>
          </Card>
        )}
      </CardContent>
    </Card>
  );
}
