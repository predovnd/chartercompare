import { useState, useEffect } from 'react';
import { Card, CardContent, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { DollarSign, Clock, CheckCircle, AlertCircle } from 'lucide-react';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

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
}

interface QuoteStatusProps {
  sessionId: string;
}

export function QuoteStatus({ sessionId }: QuoteStatusProps) {
  const [requestStatus, setRequestStatus] = useState<RequestStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const fetchRequestStatus = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/requester/requests/session/${sessionId}`, {
        credentials: 'include',
      });

      if (response.ok) {
        const data = await response.json();
        setRequestStatus(data);
        setError(null);
      } else if (response.status === 404) {
        // Request not found yet (might not be published)
        setRequestStatus(null);
        setError(null);
      } else {
        setError('Failed to load quote status');
      }
    } catch (err) {
      console.error('Error fetching request status:', err);
      setError('Failed to load quote status');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    if (!sessionId) return;

    // Initial fetch
    fetchRequestStatus();

    // Poll every 30 seconds if request exists and deadline hasn't passed
    const interval = setInterval(() => {
      if (requestStatus && !requestStatus.isDeadlinePassed) {
        fetchRequestStatus();
      }
    }, 30000);

    return () => clearInterval(interval);
  }, [sessionId, requestStatus?.isDeadlinePassed]);

  if (loading) {
    return (
      <Card className="mt-4">
        <CardContent className="pt-6">
          <p className="text-sm text-muted-foreground">Loading quote status...</p>
        </CardContent>
      </Card>
    );
  }

  if (error) {
    return (
      <Card className="mt-4 border-red-200 bg-red-50">
        <CardContent className="pt-6">
          <p className="text-sm text-red-800">{error}</p>
        </CardContent>
      </Card>
    );
  }

  if (!requestStatus) {
    return (
      <Card className="mt-4 border-blue-200 bg-blue-50">
        <CardContent className="pt-6">
          <div className="flex items-center gap-2 text-blue-800">
            <AlertCircle className="h-4 w-4" />
            <p className="text-sm">
              Your request is being reviewed. We'll notify you when quotes are available.
            </p>
          </div>
        </CardContent>
      </Card>
    );
  }

  const hasQuotes = requestStatus.quoteCount > 0;
  const isPublished = requestStatus.status === 'Published' || requestStatus.status === 'QuotesReceived';

  return (
    <Card className="mt-4 border-2 border-blue-200 bg-blue-50/30">
      <CardHeader>
        <CardTitle className="flex items-center gap-2 text-lg">
          <DollarSign className="h-5 w-5 text-blue-700" />
          Quote Status
        </CardTitle>
      </CardHeader>
      <CardContent className="space-y-4">
        {/* Status and Countdown */}
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
          <Button
            variant="outline"
            size="sm"
            onClick={fetchRequestStatus}
          >
            Refresh
          </Button>
        </div>

        {/* Quotes List */}
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

        {/* First Quote Notification */}
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

        {/* Deadline Reached */}
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
  );
}
