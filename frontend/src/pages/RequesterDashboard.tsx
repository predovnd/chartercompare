import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { LogOut, Bus, FileText, DollarSign, Calendar, Users, MapPin, User } from 'lucide-react';
import { useNavigate, Link } from 'react-router-dom';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface Requester {
  id: number;
  email: string;
  name: string;
  phone?: string;
}

interface Request {
  id: number;
  sessionId: string;
  requestData: any;
  status: string;
  createdAt: string;
  quoteCount: number;
  quotes: Array<{
    id: number;
    operatorName: string;
    operatorEmail: string;
    price: number;
    currency: string;
    notes?: string;
    status: string;
    createdAt: string;
  }>;
}

export function RequesterDashboard() {
  const [requester, setRequester] = useState<Requester | null>(null);
  const [requests, setRequests] = useState<Request[]>([]);
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  useEffect(() => {
    checkAuth();
  }, []);

  const checkAuth = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
        credentials: 'include',
      });
      
      if (!response.ok) {
        console.error('Auth check failed:', response.status, response.statusText);
        navigate('/requester/login');
        return;
      }

      let data;
      try {
        data = await response.json();
      } catch (parseError) {
        console.error('Failed to parse auth response:', parseError);
        navigate('/requester/login');
        return;
      }

      if (data.userType !== 'requester') {
        console.warn('User is not a requester:', data.userType);
        navigate('/requester/login');
        return;
      }

      setRequester(data);
      loadRequests();
    } catch (error) {
      console.error('Auth check failed:', error);
      navigate('/requester/login');
    } finally {
      setLoading(false);
    }
  };

  const loadRequests = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/requester/requests`, {
        credentials: 'include',
      });
      if (response.ok) {
        const data = await response.json();
        // Handle both array response and object with requests property
        if (Array.isArray(data)) {
          setRequests(data);
        } else if (data.requests && Array.isArray(data.requests)) {
          setRequests(data.requests);
        } else {
          setRequests([]);
        }
      } else {
        console.error('Failed to load requests:', response.status, response.statusText);
        setRequests([]);
      }
    } catch (error) {
      console.error('Failed to load requests:', error);
      setRequests([]);
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
            <span className="text-sm text-muted-foreground ml-2 hidden sm:inline">Customer Portal</span>
          </Link>
          
          <div className="flex items-center gap-4">
            <div className="hidden md:flex items-center gap-3 px-4 py-2 rounded-lg bg-muted/50 border">
              <User className="h-4 w-4 text-muted-foreground" />
              <div className="flex flex-col">
                <span className="text-sm font-medium leading-tight">{requester?.name}</span>
                <span className="text-xs text-muted-foreground leading-tight">{requester?.email}</span>
              </div>
            </div>
            
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
          <h1 className="text-3xl font-bold mb-2">My Requests</h1>
          <p className="text-muted-foreground">
            View and track your charter bus requests and quotes
          </p>
        </div>

        {requests.length === 0 ? (
          <Card>
            <CardContent className="py-12">
              <div className="text-center">
                <FileText className="h-12 w-12 text-muted-foreground mx-auto mb-4" />
                <h3 className="text-lg font-semibold mb-2">No requests yet</h3>
                <p className="text-muted-foreground mb-4">
                  Start a new request by chatting with our AI assistant
                </p>
                <Link to="/">
                  <Button>Start New Request</Button>
                </Link>
              </div>
            </CardContent>
          </Card>
        ) : (
          <div className="space-y-4">
            {requests.map((request) => (
              <Card key={request.id}>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <div>
                      <CardTitle className="text-lg">Request #{request.id}</CardTitle>
                      <CardDescription>
                        {request.requestData?.trip?.type} • {request.requestData?.trip?.passengerCount} passengers
                      </CardDescription>
                    </div>
                    <span className={`text-xs px-2 py-1 rounded ${
                      request.status === 'Open' ? 'bg-blue-100 text-blue-800' :
                      request.status === 'QuotesReceived' ? 'bg-yellow-100 text-yellow-800' :
                      'bg-gray-100 text-gray-800'
                    }`}>
                      {request.status}
                    </span>
                  </div>
                </CardHeader>
                <CardContent>
                  <div className="grid gap-4 md:grid-cols-2">
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <Calendar className="h-4 w-4" />
                        <span>{request.requestData?.trip?.date?.rawInput || 'Date not specified'}</span>
                      </div>
                      <div className="flex items-center gap-2 text-sm text-muted-foreground">
                        <MapPin className="h-4 w-4" />
                        <span>
                          {request.requestData?.trip?.pickupLocation?.resolvedName || request.requestData?.trip?.pickupLocation?.rawInput || 'Pickup not specified'}
                        </span>
                      </div>
                      {request.requestData?.trip?.destination && (
                        <div className="flex items-center gap-2 text-sm text-muted-foreground">
                          <MapPin className="h-4 w-4" />
                          <span>
                            {request.requestData.trip.destination.resolvedName || request.requestData.trip.destination.rawInput || 'Destination not specified'}
                          </span>
                        </div>
                      )}
                    </div>
                    <div className="space-y-2">
                      <div className="flex items-center gap-2 text-sm">
                        <DollarSign className="h-4 w-4 text-muted-foreground" />
                        <span className="font-medium">{request.quoteCount} quote{request.quoteCount !== 1 ? 's' : ''} received</span>
                      </div>
                      <p className="text-xs text-muted-foreground">
                        Created {new Date(request.createdAt).toLocaleDateString()}
                      </p>
                    </div>
                  </div>

                  {request.quotes.length > 0 && (
                    <div className="mt-4 pt-4 border-t">
                      <h4 className="text-sm font-medium mb-3">Quotes</h4>
                      <div className="space-y-2">
                        {request.quotes.map((quote) => (
                          <div key={quote.id} className="flex items-center justify-between p-3 bg-muted/50 rounded-lg">
                            <div>
                              <p className="font-medium">{quote.operatorName}</p>
                              <p className="text-sm text-muted-foreground">{quote.operatorEmail}</p>
                              {quote.notes && (
                                <p className="text-xs text-muted-foreground mt-1">{quote.notes}</p>
                              )}
                            </div>
                            <div className="text-right">
                              <p className="font-bold text-lg">${quote.price} {quote.currency}</p>
                              <p className="text-xs text-muted-foreground">{quote.status}</p>
                            </div>
                          </div>
                        ))}
                      </div>
                    </div>
                  )}
                </CardContent>
              </Card>
            ))}
          </div>
        )}
      </div>
    </div>
  );
}
