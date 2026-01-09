import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { LogOut, Bus, Shield, Users, FileText, DollarSign, TrendingUp, User, UserCheck, Edit, X, AlertCircle, Search } from 'lucide-react';
import { useNavigate, Link } from 'react-router-dom';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface Stats {
  totalOperators: number;
  totalRequests: number;
  openRequests: number;
  totalQuotes: number;
}

interface User {
  id: number;
  email: string;
  name: string;
  companyName?: string;
  phone?: string;
  externalProvider: string;
  isAdmin: boolean;
  isActive: boolean;
  createdAt: string;
  lastLoginAt?: string;
  quoteCount: number;
  requestCount: number;
  userType: string; // "operator" or "requester"
  attributes: string[]; // List of attribute types (Bus, Airplane, Individual, Business, etc.)
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
    providerName: string;
    providerEmail: string;
    price: number;
    currency: string;
    notes?: string;
    status: string;
    createdAt: string;
  }>;
}

// Attribute type mapping
const ATTRIBUTE_TYPES = {
  'Bus': 1,
  'Airplane': 2,
  'Train': 3,
  'Boat': 4,
  'Individual': 10,
  'Business': 11,
} as const;

const OPERATOR_ATTRIBUTES = ['Bus', 'Airplane', 'Train', 'Boat'];
const REQUESTER_ATTRIBUTES = ['Individual', 'Business'];

export function AdminDashboard() {
  const [stats, setStats] = useState<Stats | null>(null);
  const [users, setUsers] = useState<User[]>([]);
  const [requests, setRequests] = useState<Request[]>([]);
  const [loading, setLoading] = useState(true);
  const [activeTab, setActiveTab] = useState<'overview' | 'users' | 'requests'>('overview');
  const [editingUser, setEditingUser] = useState<User | null>(null);
  const [selectedAttributes, setSelectedAttributes] = useState<string[]>([]);
  const [companyName, setCompanyName] = useState<string>('');
  const [saving, setSaving] = useState(false);
  const [userSearchQuery, setUserSearchQuery] = useState<string>('');
  const navigate = useNavigate();

  useEffect(() => {
    checkAuth();
  }, []);

  useEffect(() => {
    if (activeTab === 'overview') {
      loadStats();
    } else if (activeTab === 'users') {
      loadUsers();
    } else if (activeTab === 'requests') {
      loadRequests();
    }
  }, [activeTab]);

  const checkAuth = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/auth/me`, {
        credentials: 'include',
      });
      
      if (!response.ok) {
        console.error('Auth check failed:', response.status, response.statusText);
        navigate('/admin/login');
        return;
      }

      let data;
      try {
        data = await response.json();
      } catch (parseError) {
        console.error('Failed to parse auth response:', parseError);
        navigate('/admin/login');
        return;
      }

      if (!data.isAdmin) {
        console.warn('User is not an admin');
        navigate('/admin/login');
        return;
      }

      loadStats();
    } catch (error) {
      console.error('Auth check failed:', error);
      navigate('/admin/login');
    } finally {
      setLoading(false);
    }
  };

  const loadStats = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/stats`, {
        credentials: 'include',
      });
      if (response.ok) {
        const data = await response.json();
        setStats(data);
      }
    } catch (error) {
      console.error('Failed to load stats:', error);
    }
  };

  const loadUsers = async () => {
    try {
      console.log('Loading users from:', `${API_BASE_URL}/api/admin/users`);
      const response = await fetch(`${API_BASE_URL}/api/admin/users`, {
        credentials: 'include',
      });
      console.log('Users response status:', response.status, response.statusText);
      
      if (response.ok) {
        const data = await response.json();
        console.log('Users data received:', data);
        console.log('Is array?', Array.isArray(data));
        console.log('Data length:', Array.isArray(data) ? data.length : 'not an array');
        
        // Handle both array response and object with users property
        if (Array.isArray(data)) {
          console.log('Setting users array, count:', data.length);
          setUsers(data);
        } else if (data.users && Array.isArray(data.users)) {
          console.log('Setting users from data.users, count:', data.users.length);
          setUsers(data.users);
        } else {
          console.warn('Unexpected data format:', data);
          setUsers([]);
        }
      } else {
        const errorText = await response.text();
        console.error('Failed to load users:', response.status, response.statusText, errorText);
        setUsers([]);
      }
    } catch (error) {
      console.error('Failed to load users:', error);
      setUsers([]);
    }
  };

  const loadRequests = async () => {
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/requests`, {
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

  const openEditDialog = (user: User) => {
    setEditingUser(user);
    setSelectedAttributes([...user.attributes]);
    setCompanyName(user.companyName || '');
  };

  const closeEditDialog = () => {
    setEditingUser(null);
    setSelectedAttributes([]);
    setCompanyName('');
  };

  const toggleAttribute = (attribute: string) => {
    // Individual and Business are mutually exclusive
    if (attribute === 'Individual' && selectedAttributes.includes('Business')) {
      setSelectedAttributes([...selectedAttributes.filter(a => a !== 'Business'), attribute]);
    } else if (attribute === 'Business' && selectedAttributes.includes('Individual')) {
      setSelectedAttributes([...selectedAttributes.filter(a => a !== 'Individual'), attribute]);
    } else if (selectedAttributes.includes(attribute)) {
      setSelectedAttributes(selectedAttributes.filter(a => a !== attribute));
    } else {
      setSelectedAttributes([...selectedAttributes, attribute]);
    }
  };

  const handleSaveAttributes = async () => {
    if (!editingUser) return;

    setSaving(true);
    try {
      const attributeTypes = selectedAttributes.map(attr => ATTRIBUTE_TYPES[attr as keyof typeof ATTRIBUTE_TYPES]);
      
      const response = await fetch(`${API_BASE_URL}/api/admin/users/${editingUser.id}/attributes`, {
        method: 'PUT',
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify({
          attributes: attributeTypes,
          companyName: selectedAttributes.includes('Business') ? companyName : null,
        }),
      });

      if (response.ok) {
        // Reload users to get updated data
        await loadUsers();
        closeEditDialog();
      } else {
        const error = await response.json();
        alert(error.error || 'Failed to update user attributes');
      }
    } catch (error) {
      console.error('Failed to update attributes:', error);
      alert('Failed to update user attributes');
    } finally {
      setSaving(false);
    }
  };

  const getAttributeBadgeColor = (attribute: string) => {
    if (OPERATOR_ATTRIBUTES.includes(attribute)) {
      return 'bg-blue-100 text-blue-800';
    } else if (attribute === 'Individual') {
      return 'bg-green-100 text-green-800';
    } else if (attribute === 'Business') {
      return 'bg-purple-100 text-purple-800';
    }
    return 'bg-gray-100 text-gray-800';
  };

  const getAttributeBadgeStyle = (attribute: string) => {
    if (OPERATOR_ATTRIBUTES.includes(attribute)) {
      return 'border-blue-300 text-blue-700 bg-blue-50';
    } else if (attribute === 'Individual') {
      return 'border-green-300 text-green-700 bg-green-50';
    } else if (attribute === 'Business') {
      return 'border-purple-300 text-purple-700 bg-purple-50';
    }
    return 'border-gray-300 text-gray-700 bg-gray-50';
  };

  const shouldHighlightMissingAttributes = (user: User) => {
    // Don't highlight admins
    if (user.isAdmin) return false;
    
    // Operators should have at least one operator attribute
    if (user.userType === 'operator') {
      return !user.attributes.some(attr => OPERATOR_ATTRIBUTES.includes(attr));
    }
    
    // Requesters should have Individual or Business
    if (user.userType === 'requester') {
      return !user.attributes.some(attr => REQUESTER_ATTRIBUTES.includes(attr));
    }
    
    return false;
  };

  const filterUsers = (users: User[], query: string): User[] => {
    if (!query.trim()) {
      return users;
    }

    const lowerQuery = query.toLowerCase().trim();
    
    return users.filter(user => {
      // Search in name
      if (user.name.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in email
      if (user.email.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in company name
      if (user.companyName && user.companyName.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in phone
      if (user.phone && user.phone.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in user type
      if (user.userType.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in external provider
      if (user.externalProvider.toLowerCase().includes(lowerQuery)) return true;
      
      // Search in attributes
      if (user.attributes && user.attributes.some(attr => attr.toLowerCase().includes(lowerQuery))) return true;
      
      return false;
    });
  };

  const filteredUsers = filterUsers(users, userSearchQuery);

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
            <Shield className="h-5 w-5 text-primary" />
            <span className="text-xl font-semibold">CharterCompare</span>
            <span className="text-sm text-muted-foreground ml-2 hidden sm:inline">Admin Portal</span>
          </Link>
          
          <Button variant="outline" onClick={handleLogout} size="sm">
            <LogOut className="h-4 w-4 mr-2" />
            <span className="hidden sm:inline">Logout</span>
          </Button>
        </div>
      </header>

      {/* Main Content */}
      <div className="container py-8">
        <div className="mb-8">
          <h1 className="text-3xl font-bold mb-2">Admin Dashboard</h1>
          <p className="text-muted-foreground">
            Manage operators, requests, and quotes
          </p>
        </div>

        {/* Tabs */}
        <div className="flex gap-2 mb-6 border-b">
          <button
            onClick={() => setActiveTab('overview')}
            className={`px-4 py-2 font-medium transition-colors ${
              activeTab === 'overview'
                ? 'border-b-2 border-primary text-primary'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Overview
          </button>
          <button
            onClick={() => setActiveTab('users')}
            className={`px-4 py-2 font-medium transition-colors ${
              activeTab === 'users'
                ? 'border-b-2 border-primary text-primary'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Users
          </button>
          <button
            onClick={() => setActiveTab('requests')}
            className={`px-4 py-2 font-medium transition-colors ${
              activeTab === 'requests'
                ? 'border-b-2 border-primary text-primary'
                : 'text-muted-foreground hover:text-foreground'
            }`}
          >
            Requests
          </button>
        </div>

        {/* Overview Tab */}
        {activeTab === 'overview' && stats && (
          <div className="grid gap-6 md:grid-cols-2 lg:grid-cols-4">
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Operators</CardTitle>
                <Users className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stats.totalOperators}</div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Requests</CardTitle>
                <FileText className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stats.totalRequests}</div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Open Requests</CardTitle>
                <TrendingUp className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stats.openRequests}</div>
              </CardContent>
            </Card>
            <Card>
              <CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
                <CardTitle className="text-sm font-medium">Total Quotes</CardTitle>
                <DollarSign className="h-4 w-4 text-muted-foreground" />
              </CardHeader>
              <CardContent>
                <div className="text-2xl font-bold">{stats.totalQuotes}</div>
              </CardContent>
            </Card>
          </div>
        )}

        {/* Users Tab */}
        {activeTab === 'users' && (
          <Card>
            <CardHeader>
              <CardTitle>All Users</CardTitle>
              <CardDescription>View and manage all users (operators, requesters, and admins) in the system. Edit user attributes to configure their capabilities.</CardDescription>
            </CardHeader>
            <CardContent>
              {/* Search Input */}
              <div className="mb-6">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-muted-foreground" />
                  <Input
                    type="text"
                    placeholder="Search users by name, email, company, phone, type, or attributes..."
                    value={userSearchQuery}
                    onChange={(e) => setUserSearchQuery(e.target.value)}
                    className="w-full pl-10"
                  />
                </div>
                {userSearchQuery && (
                  <p className="text-xs text-muted-foreground mt-2">
                    Showing {filteredUsers.length} of {users.length} users
                  </p>
                )}
              </div>

              {users.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No users found</p>
              ) : filteredUsers.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No users match your search</p>
              ) : (
                <div className="space-y-4">
                  {filteredUsers.map((user) => {
                    // Determine user type
                    const getUserType = () => {
                      if (user.isAdmin) {
                        return { type: 'Admin', icon: Shield, color: 'text-purple-600', bgColor: 'bg-purple-100' };
                      } else if (user.userType === 'operator') {
                        return { type: 'Operator', icon: UserCheck, color: 'text-blue-600', bgColor: 'bg-blue-100' };
                      } else if (user.userType === 'requester') {
                        return { type: 'Requester', icon: User, color: 'text-green-600', bgColor: 'bg-green-100' };
                      } else {
                        // Fallback for backward compatibility
                        return { type: 'Operator', icon: UserCheck, color: 'text-blue-600', bgColor: 'bg-blue-100' };
                      }
                    };

                    const userTypeInfo = getUserType();
                    const IconComponent = userTypeInfo.icon;

                    const hasMissingAttributes = shouldHighlightMissingAttributes(user);

                    return (
                      <div 
                        key={user.id} 
                        className={`flex items-center justify-between p-4 border rounded-lg ${
                          hasMissingAttributes ? 'border-yellow-400 bg-yellow-50/50' : ''
                        }`}
                      >
                        <div className="flex items-start gap-3 flex-1">
                          <div className={`p-2 rounded-lg ${userTypeInfo.bgColor}`}>
                            <IconComponent className={`h-5 w-5 ${userTypeInfo.color}`} />
                          </div>
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1 flex-wrap">
                              <span className="font-medium">{user.name}</span>
                              <span className={`text-xs px-2.5 py-0.5 rounded-md ${userTypeInfo.bgColor} ${userTypeInfo.color} font-medium`}>
                                {userTypeInfo.type}
                              </span>
                              {/* Display Attributes next to role */}
                              {user.attributes && user.attributes.length > 0 && (
                                <div className="flex items-center gap-1.5 flex-wrap">
                                  {user.attributes.map((attr) => (
                                    <span
                                      key={attr}
                                      className={`text-xs px-2.5 py-0.5 rounded-full border ${getAttributeBadgeStyle(attr)} font-medium`}
                                    >
                                      {attr}
                                    </span>
                                  ))}
                                </div>
                              )}
                              {hasMissingAttributes && (
                                <span className="text-xs px-2.5 py-0.5 rounded-md bg-yellow-100 text-yellow-800 font-medium flex items-center gap-1">
                                  <AlertCircle className="h-3 w-3" />
                                  Missing Attributes
                                </span>
                              )}
                            </div>
                            <p className="text-sm text-muted-foreground">{user.email}</p>
                            {user.companyName && (
                              <p className="text-sm text-muted-foreground">{user.companyName}</p>
                            )}
                            
                            <div className="flex items-center gap-4 mt-2 text-xs text-muted-foreground">
                              <span>{user.externalProvider} auth</span>
                              {user.quoteCount > 0 && (
                                <span>• {user.quoteCount} quote{user.quoteCount !== 1 ? 's' : ''}</span>
                              )}
                              {user.requestCount > 0 && (
                                <span>• {user.requestCount} request{user.requestCount !== 1 ? 's' : ''}</span>
                              )}
                              <span>• Created {new Date(user.createdAt).toLocaleDateString()}</span>
                            </div>
                          </div>
                        </div>
                        <div className="flex items-center gap-2">
                          {!user.isAdmin && (
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => openEditDialog(user)}
                            >
                              <Edit className="h-4 w-4 mr-1" />
                              Edit
                            </Button>
                          )}
                          <span className={`text-xs px-2 py-1 rounded ${
                            user.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-800'
                          }`}>
                            {user.isActive ? 'Active' : 'Inactive'}
                          </span>
                        </div>
                      </div>
                    );
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        )}

        {/* Requests Tab */}
        {activeTab === 'requests' && (
          <Card>
            <CardHeader>
              <CardTitle>All Requests</CardTitle>
              <CardDescription>View all charter bus requests and their quotes</CardDescription>
            </CardHeader>
            <CardContent>
              {requests.length === 0 ? (
                <p className="text-muted-foreground text-center py-8">No requests found</p>
              ) : (
                <div className="space-y-4">
                  {requests.map((request) => (
                    <div key={request.id} className="p-4 border rounded-lg">
                      <div className="flex justify-between items-start mb-2">
                        <div>
                          <h3 className="font-medium">Request #{request.id}</h3>
                          <p className="text-sm text-muted-foreground">
                            {request.requestData?.trip?.type} • {request.requestData?.trip?.passengerCount} passengers
                          </p>
                        </div>
                        <span className={`text-xs px-2 py-1 rounded ${
                          request.status === 'Open' ? 'bg-blue-100 text-blue-800' :
                          request.status === 'QuotesReceived' ? 'bg-yellow-100 text-yellow-800' :
                          'bg-gray-100 text-gray-800'
                        }`}>
                          {request.status}
                        </span>
                      </div>
                      <p className="text-xs text-muted-foreground mb-3">
                        Created {new Date(request.createdAt).toLocaleDateString()} • {request.quoteCount} quote(s)
                      </p>
                      {request.quotes.length > 0 && (
                        <div className="mt-3 pt-3 border-t">
                          <p className="text-sm font-medium mb-2">Quotes:</p>
                          {request.quotes.map((quote) => (
                            <div key={quote.id} className="text-sm text-muted-foreground mb-1">
                              {quote.providerName}: ${quote.price} {quote.currency} - {quote.status}
                            </div>
                          ))}
                        </div>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>

      {/* Edit User Attributes Dialog */}
      {editingUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg shadow-lg w-full max-w-md mx-4 max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold">Edit User Attributes</h2>
                <Button variant="ghost" size="sm" onClick={closeEditDialog}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              
              <div className="space-y-4">
                <div>
                  <p className="text-sm font-medium mb-1">{editingUser.name}</p>
                  <p className="text-sm text-muted-foreground">{editingUser.email}</p>
                  <p className="text-xs text-muted-foreground mt-1">
                    Type: {editingUser.isAdmin ? 'Admin' : editingUser.userType === 'operator' ? 'Operator' : 'Requester'}
                  </p>
                </div>

                <div className="border-t pt-4">
                  <Label className="text-sm font-medium mb-3 block">
                    {editingUser.userType === 'operator' || editingUser.isAdmin ? 'Operator Attributes' : 'Requester Attributes'}
                  </Label>
                  
                  <div className="space-y-2">
                    {(editingUser.userType === 'operator' || editingUser.isAdmin) && (
                      <div className="space-y-2">
                        {OPERATOR_ATTRIBUTES.map((attr) => (
                          <label key={attr} className="flex items-center space-x-2 cursor-pointer">
                            <input
                              type="checkbox"
                              checked={selectedAttributes.includes(attr)}
                              onChange={() => toggleAttribute(attr)}
                              className="rounded border-gray-300"
                            />
                            <span className="text-sm">{attr}</span>
                          </label>
                        ))}
                      </div>
                    )}
                    
                    {editingUser.userType === 'requester' && (
                      <div className="space-y-2">
                        {REQUESTER_ATTRIBUTES.map((attr) => (
                          <label key={attr} className="flex items-center space-x-2 cursor-pointer">
                            <input
                              type="checkbox"
                              checked={selectedAttributes.includes(attr)}
                              onChange={() => toggleAttribute(attr)}
                              className="rounded border-gray-300"
                            />
                            <span className="text-sm">{attr}</span>
                          </label>
                        ))}
                      </div>
                    )}
                  </div>
                </div>

                {selectedAttributes.includes('Business') && (
                  <div className="border-t pt-4">
                    <Label htmlFor="companyName" className="text-sm font-medium mb-2 block">
                      Company Name
                    </Label>
                    <Input
                      id="companyName"
                      value={companyName}
                      onChange={(e) => setCompanyName(e.target.value)}
                      placeholder="Enter company name"
                    />
                  </div>
                )}

                <div className="flex justify-end gap-2 pt-4 border-t">
                  <Button variant="outline" onClick={closeEditDialog} disabled={saving}>
                    Cancel
                  </Button>
                  <Button onClick={handleSaveAttributes} disabled={saving}>
                    {saving ? 'Saving...' : 'Save Changes'}
                  </Button>
                </div>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
