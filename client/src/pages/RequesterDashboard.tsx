import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Badge } from '../components/ui/badge';
import { LogOut, Bus, FileText, DollarSign, Calendar, MapPin, User, Award, TrendingDown, Star } from 'lucide-react';
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

  // Helper function to get status badge styling
  const getStatusBadge = (status: string) => {
    const statusMap: Record<string, { label: string; className: string }> = {
      'Draft': { label: 'Draft', className: 'bg-gray-100 text-gray-800 border-gray-200' },
      'UnderReview': { label: 'Under Review', className: 'bg-blue-100 text-blue-800 border-blue-200' },
      'Published': { label: 'Published', className: 'bg-green-100 text-green-800 border-green-200' },
      'QuotesReceived': { label: 'Quotes Received', className: 'bg-yellow-100 text-yellow-800 border-yellow-200' },
      'Accepted': { label: 'Accepted', className: 'bg-emerald-100 text-emerald-800 border-emerald-200' },
      'Completed': { label: 'Completed', className: 'bg-purple-100 text-purple-800 border-purple-200' },
      'Cancelled': { label: 'Cancelled', className: 'bg-red-100 text-red-800 border-red-200' },
    };
    const statusInfo = statusMap[status] || { label: status, className: 'bg-gray-100 text-gray-800 border-gray-200' };
    return (
      <Badge variant="outline" className={`${statusInfo.className} border`}>
        {statusInfo.label}
      </Badge>
    );
  };

  // Helper function to process quotes and add ranking info
  const processQuotes = (quotes: Request['quotes']) => {
    if (quotes.length === 0) return [];
    
    const sortedQuotes = [...quotes].sort((a, b) => a.price - b.price);
    const cheapestPrice = sortedQuotes[0].price;
    const prices = sortedQuotes.map(q => q.price);
    const averagePrice = prices.reduce((a, b) => a + b, 0) / prices.length;
    
    return sortedQuotes.map((quote, index) => {
      const isCheapest = quote.price === cheapestPrice;
      const isBestValue = quote.price <= averagePrice * 0.9; // Within 10% of average or below
      const isBestMatch = index === 0 && isCheapest; // First (cheapest) is best match
      
      return {
        ...quote,
        rank: index + 1,
        isCheapest,
        isBestValue,
        isBestMatch,
        savings: index > 0 ? ((quotes.find(q => q.id === quote.id)?.price || quote.price) - cheapestPrice) : 0,
      };
    });
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
          <div className="space-y-6">
            {requests.map((request) => {
              const processedQuotes = processQuotes(request.quotes);
              const hasQuotes = processedQuotes.length > 0;
              
              return (
                <Card key={request.id} className="overflow-hidden hover:shadow-lg transition-shadow">
                  <CardHeader className="pb-4">
                    <div className="flex justify-between items-start gap-4">
                      <div className="flex-1">
                        <div className="flex items-center gap-3 mb-2">
                          <CardTitle className="text-xl">Request #{request.id}</CardTitle>
                          {getStatusBadge(request.status)}
                        </div>
                        <CardDescription className="text-base">
                          {request.requestData?.trip?.type || 'Charter'} • {request.requestData?.trip?.passengerCount || 0} passengers
                        </CardDescription>
                      </div>
                    </div>
                  </CardHeader>
                  <CardContent className="space-y-6">
                    {/* Trip Details */}
                    <div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
                      <div className="flex items-start gap-3 p-3 rounded-lg bg-muted/30">
                        <Calendar className="h-5 w-5 text-muted-foreground mt-0.5 flex-shrink-0" />
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">Date</p>
                          <p className="text-sm font-medium">
                            {request.requestData?.trip?.date?.rawInput || 'Date not specified'}
                          </p>
                        </div>
                      </div>
                      <div className="flex items-start gap-3 p-3 rounded-lg bg-muted/30">
                        <MapPin className="h-5 w-5 text-muted-foreground mt-0.5 flex-shrink-0" />
                        <div>
                          <p className="text-xs font-medium text-muted-foreground mb-1">Pickup</p>
                          <p className="text-sm font-medium">
                            {request.requestData?.trip?.pickupLocation?.resolvedName || request.requestData?.trip?.pickupLocation?.rawInput || 'Not specified'}
                          </p>
                        </div>
                      </div>
                      {request.requestData?.trip?.destination && (
                        <div className="flex items-start gap-3 p-3 rounded-lg bg-muted/30">
                          <MapPin className="h-5 w-5 text-muted-foreground mt-0.5 flex-shrink-0" />
                          <div>
                            <p className="text-xs font-medium text-muted-foreground mb-1">Destination</p>
                            <p className="text-sm font-medium">
                              {request.requestData.trip.destination.resolvedName || request.requestData.trip.destination.rawInput || 'Not specified'}
                            </p>
                          </div>
                        </div>
                      )}
                    </div>

                    {/* Quotes Section */}
                    {hasQuotes ? (
                      <div className="space-y-4">
                        <div className="flex items-center justify-between">
                          <div className="flex items-center gap-2">
                            <DollarSign className="h-5 w-5 text-primary" />
                            <h3 className="text-lg font-semibold">
                              {processedQuotes.length} Quote{processedQuotes.length !== 1 ? 's' : ''} Received
                            </h3>
                          </div>
                          <p className="text-xs text-muted-foreground">
                            Created {new Date(request.createdAt).toLocaleDateString('en-US', { 
                              month: 'short', 
                              day: 'numeric', 
                              year: 'numeric' 
                            })}
                          </p>
                        </div>
                        
                        <div className="space-y-3">
                          {processedQuotes.map((quote) => (
                            <Card 
                              key={quote.id} 
                              className={`relative overflow-hidden transition-all ${
                                quote.isBestMatch 
                                  ? 'border-2 border-primary shadow-md bg-primary/5' 
                                  : quote.isCheapest 
                                  ? 'border-2 border-green-500 shadow-md bg-green-50/50' 
                                  : 'border'
                              }`}
                            >
                              <CardContent className="p-4">
                                <div className="flex items-start justify-between gap-4">
                                  <div className="flex-1 space-y-2">
                                    <div className="flex items-center gap-2 flex-wrap">
                                      <h4 className="font-semibold text-base">{quote.operatorName}</h4>
                                      {quote.isBestMatch && (
                                        <Badge className="bg-primary text-primary-foreground border-0">
                                          <Award className="h-3 w-3 mr-1" />
                                          Best Match
                                        </Badge>
                                      )}
                                      {quote.isCheapest && !quote.isBestMatch && (
                                        <Badge className="bg-green-600 text-white border-0">
                                          <TrendingDown className="h-3 w-3 mr-1" />
                                          Cheapest
                                        </Badge>
                                      )}
                                      {quote.isBestValue && !quote.isCheapest && (
                                        <Badge variant="outline" className="border-blue-300 text-blue-700 bg-blue-50">
                                          <Star className="h-3 w-3 mr-1" />
                                          Great Value
                                        </Badge>
                                      )}
                                    </div>
                                    <p className="text-sm text-muted-foreground">{quote.operatorEmail}</p>
                                    {quote.notes && (
                                      <p className="text-sm text-muted-foreground mt-2 p-2 bg-muted/50 rounded-md">
                                        {quote.notes}
                                      </p>
                                    )}
                                    {quote.savings > 0 && (
                                      <p className="text-xs text-green-600 font-medium">
                                        Save ${quote.savings.toFixed(2)} compared to average
                                      </p>
                                    )}
                                  </div>
                                  <div className="text-right flex-shrink-0">
                                    <div className="flex items-baseline gap-1">
                                      <span className="text-2xl font-bold text-foreground">
                                        ${quote.price.toLocaleString()}
                                      </span>
                                      <span className="text-sm text-muted-foreground">{quote.currency}</span>
                                    </div>
                                    {quote.rank === 1 && (
                                      <p className="text-xs text-primary font-medium mt-1">#1 Ranked</p>
                                    )}
                                    <p className="text-xs text-muted-foreground mt-1">
                                      {quote.status}
                                    </p>
                                  </div>
                                </div>
                              </CardContent>
                              {quote.isBestMatch && (
                                <div className="absolute top-0 right-0 bg-primary text-primary-foreground text-xs font-semibold px-3 py-1 rounded-bl-lg">
                                  RECOMMENDED
                                </div>
                              )}
                            </Card>
                          ))}
                        </div>
                      </div>
                    ) : (
                      <div className="text-center py-8 border-2 border-dashed rounded-lg bg-muted/20">
                        <DollarSign className="h-12 w-12 text-muted-foreground mx-auto mb-3 opacity-50" />
                        <p className="text-sm font-medium text-muted-foreground mb-1">No quotes yet</p>
                        <p className="text-xs text-muted-foreground">
                          Quotes will appear here once operators submit their offers
                        </p>
                      </div>
                    )}
                  </CardContent>
                </Card>
              );
            })}
          </div>
        )}
      </div>
    </div>
  );
}
