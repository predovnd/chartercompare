import { useState, useEffect } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { Card, CardContent, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { DollarSign, Clock, CheckCircle, AlertCircle, ArrowLeft, MapPin, Calendar, Users } from 'lucide-react';
import { Link } from 'react-router-dom';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

interface Quote {
  id: number;
  providerName: string;
  price: number;
  currency: string;
  notes?: string;
  status: string;
  createdAt: string;
}

interface RequestStatus {
  id: number;
  sessionId: string;
  status: string;
  quoteCount: number;
  quoteDeadline?: string;
  hoursRemaining: number;
  isDeadlinePassed: boolean;
  quotes: Quote[];
  requestData: {
    trip: {
      type: string;
      passengerCount: number;
      date: {
        rawInput: string;
      };
      pickupLocation: {
        resolvedName?: string;
        rawInput: string;
      };
      destination: {
        resolvedName?: string;
        rawInput: string;
      };
    };
  };
}

export function ViewQuotes() {
  const { sessionId } = useParams<{ sessionId: string }>();
  const [inputSessionId, setInputSessionId] = useState(sessionId || '');
  const [requestStatus, setRequestStatus] = useState<RequestStatus | null>(null);
  const [loading, setLoading] = useState(!!sessionId);
  const [error, setError] = useState<string | null>(null);
  const navigate = useNavigate();

  useEffect(() => {
    if (sessionId) {
      fetchRequestStatus(sessionId);
    }
  }, [sessionId]);

  const fetchRequestStatus = async (sid: string) => {
    if (!sid) return;
    
    setLoading(true);
    setError(null);
    
    try {
      const response = await fetch(`${API_BASE_URL}/api/requester/requests/session/${sid}`, {
        credentials: 'include',
      });

      if (response.ok) {
        const data = await response.json();
        setRequestStatus(data);
        setError(null);
      } else if (response.status === 404) {
        setRequestStatus(null);
        setError('Request not found. Please check your link and try again.');
      } else {
        setError('Failed to load request. Please try again.');
      }
    } catch (err) {
      console.error('Error fetching request status:', err);
      setError('Failed to load request. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (inputSessionId.trim()) {
      navigate(`/quotes/${inputSessionId.trim()}`);
    }
  };

  if (!sessionId) {
    return (
      <div className="min-h-screen bg-muted/30 flex items-center justify-center p-4">
        <Card className="max-w-md w-full">
          <CardHeader>
            <CardTitle>View Your Quotes</CardTitle>
          </CardHeader>
          <CardContent>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div>
                <label className="text-sm font-medium mb-2 block">
                  Enter your quote link or session ID
                </label>
                <Input
                  value={inputSessionId}
                  onChange={(e) => setInputSessionId(e.target.value)}
                  placeholder="Paste your link or session ID"
                  className="w-full"
                />
              </div>
              <Button type="submit" className="w-full" disabled={!inputSessionId.trim()}>
                View Quotes
              </Button>
            </form>
            <div className="mt-4 pt-4 border-t">
              <Link to="/" className="text-sm text-primary hover:underline flex items-center gap-2">
                <ArrowLeft className="h-4 w-4" />
                Back to home
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (loading) {
    return (
      <div className="min-h-screen bg-muted/30 flex items-center justify-center">
        <div className="text-muted-foreground">Loading...</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="min-h-screen bg-muted/30 flex items-center justify-center p-4">
        <Card className="max-w-md w-full">
          <CardHeader>
            <CardTitle>Error</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-red-600 mb-4">{error}</p>
            <div className="space-y-2">
              <Button onClick={() => navigate('/quotes')} variant="outline" className="w-full">
                Try Different Link
              </Button>
              <Link to="/" className="block">
                <Button variant="ghost" className="w-full">
                  <ArrowLeft className="h-4 w-4 mr-2" />
                  Back to home
                </Button>
              </Link>
            </div>
          </CardContent>
        </Card>
      </div>
    );
  }

  if (!requestStatus) {
    return (
      <div className="min-h-screen bg-muted/30 flex items-center justify-center p-4">
        <Card className="max-w-md w-full">
          <CardHeader>
            <CardTitle>Request Not Found</CardTitle>
          </CardHeader>
          <CardContent>
            <p className="text-muted-foreground mb-4">
              We couldn't find a request with that link. Please check your link and try again.
            </p>
            <Button onClick={() => navigate('/quotes')} variant="outline" className="w-full">
              Try Different Link
            </Button>
          </CardContent>
        </Card>
      </div>
    );
  }

  const hasQuotes = requestStatus.quoteCount > 0;
  const isPublished = requestStatus.status === 'Published' || requestStatus.status === 'QuotesReceived';

  return (
    <div className="min-h-screen bg-muted/30">
      <div className="container py-8 max-w-4xl">
        <div className="mb-6">
          <Link to="/" className="text-primary hover:underline flex items-center gap-2 mb-4">
            <ArrowLeft className="h-4 w-4" />
            Back to home
          </Link>
          <h1 className="text-3xl font-bold">Your Charter Bus Quotes</h1>
        </div>

        <Card className="mb-6">
          <CardHeader>
            <CardTitle>Request Details</CardTitle>
          </CardHeader>
          <CardContent>
            <div className="grid md:grid-cols-2 gap-4">
              <div className="flex items-center gap-2 text-sm">
                <Users className="h-4 w-4 text-muted-foreground" />
                <span>{requestStatus.requestData.trip.passengerCount} passengers</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <Calendar className="h-4 w-4 text-muted-foreground" />
                <span>{requestStatus.requestData.trip.date.rawInput || 'Date not specified'}</span>
              </div>
              <div className="flex items-center gap-2 text-sm">
                <MapPin className="h-4 w-4 text-muted-foreground" />
                <span>
                  {requestStatus.requestData.trip.pickupLocation.resolvedName || 
                   requestStatus.requestData.trip.pickupLocation.rawInput || 
                   'Pickup not specified'}
                </span>
              </div>
              {requestStatus.requestData.trip.destination && (
                <div className="flex items-center gap-2 text-sm">
                  <MapPin className="h-4 w-4 text-muted-foreground" />
                  <span>
                    {requestStatus.requestData.trip.destination.resolvedName || 
                     requestStatus.requestData.trip.destination.rawInput || 
                     'Destination not specified'}
                  </span>
                </div>
              )}
            </div>
          </CardContent>
        </Card>

        <Card className="border-2 border-blue-200 bg-blue-50/30">
          <CardHeader>
            <CardTitle className="flex items-center gap-2 text-lg">
              <DollarSign className="h-5 w-5 text-blue-700" />
              Quote Status
            </CardTitle>
          </CardHeader>
          <CardContent className="space-y-4">
            <div className="flex items-center justify-between">
              <div>
                <p className="font-medium">
                  {hasQuotes ? (
                    <>
                      {requestStatus.quoteCount} Quote{requestStatus.quoteCount !== 1 ? 's' : ''} Received
                    </>
                  ) : (
                    <>Waiting for quotes...</>
                  )}
                </p>
                {isPublished && requestStatus.quoteDeadline && (
                  <p className="text-sm text-muted-foreground mt-1">
                    {requestStatus.isDeadlinePassed ? (
                      <span className="flex items-center gap-1 text-orange-600">
                        <Clock className="h-3 w-3" />
                        Quote collection period has ended
                      </span>
                    ) : (
                      <span className="flex items-center gap-1">
                        <Clock className="h-3 w-3" />
                        {requestStatus.hoursRemaining} hour{requestStatus.hoursRemaining !== 1 ? 's' : ''} remaining to receive more quotes
                      </span>
                    )}
                  </p>
                )}
              </div>
            </div>

            {hasQuotes && (
              <div className="space-y-3 pt-3 border-t">
                {requestStatus.quotes.map((quote) => (
                  <Card key={quote.id} className="bg-white">
                    <CardContent className="pt-4">
                      <div className="flex justify-between items-start mb-2">
                        <div>
                          <p className="font-medium">{quote.providerName}</p>
                          <p className="text-xs text-muted-foreground">
                            Submitted {new Date(quote.createdAt).toLocaleDateString()}
                          </p>
                        </div>
                        <span className={`text-xs px-2 py-1 rounded ${
                          quote.status === 'Submitted' ? 'bg-blue-100 text-blue-800' :
                          quote.status === 'Accepted' ? 'bg-green-100 text-green-800' :
                          'bg-gray-100 text-gray-800'
                        }`}>
                          {quote.status}
                        </span>
                      </div>
                      <div className="flex justify-between items-center">
                        <p className="text-2xl font-bold">${quote.price} {quote.currency}</p>
                      </div>
                      {quote.notes && (
                        <p className="text-sm text-muted-foreground mt-2 pt-2 border-t">
                          <span className="font-medium">Notes:</span> {quote.notes}
                        </p>
                      )}
                    </CardContent>
                  </Card>
                ))}
              </div>
            )}

            {hasQuotes && requestStatus.quoteCount === 1 && !requestStatus.isDeadlinePassed && (
              <div className="p-3 bg-green-50 border border-green-200 rounded">
                <div className="flex items-center gap-2 text-green-800">
                  <CheckCircle className="h-4 w-4" />
                  <p className="text-sm font-medium">
                    Great! You've received your first quote. More may be coming in the next {requestStatus.hoursRemaining} hours.
                  </p>
                </div>
              </div>
            )}

            {requestStatus.isDeadlinePassed && hasQuotes && (
              <div className="p-3 bg-orange-50 border border-orange-200 rounded">
                <div className="flex items-center gap-2 text-orange-800">
                  <AlertCircle className="h-4 w-4" />
                  <p className="text-sm font-medium">
                    All quotes are in! Compare your options above and choose the best one for you.
                  </p>
                </div>
              </div>
            )}
          </CardContent>
        </Card>
      </div>
    </div>
  );
}
