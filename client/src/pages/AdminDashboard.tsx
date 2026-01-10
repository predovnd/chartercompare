import { useState, useEffect } from 'react';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '../components/ui/card';
import { Button } from '../components/ui/button';
import { Input } from '../components/ui/input';
import { Label } from '../components/ui/label';
import { LogOut, Bus, Shield, Users, FileText, DollarSign, TrendingUp, User, UserCheck, Edit, X, AlertCircle, Search, MapPin, Trash2, Settings, CheckCircle, XCircle, ChevronDown, ChevronUp } from 'lucide-react';
import { CoverageMap } from '../components/CoverageMap';
import { LocationEditor } from '../components/LocationEditor';
import { useNavigate, Link } from 'react-router-dom';

const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';

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
  coverage?: {
    id: number;
    baseLocationName: string;
    latitude?: number;
    longitude?: number;
    coverageRadiusKm: number;
    minPassengerCapacity: number;
    maxPassengerCapacity: number;
    isGeocoded: boolean;
  };
}

interface Request {
  id: number;
  sessionId: string;
  requestData: any;
  rawJsonPayload?: string;
  status: string;
  createdAt: string;
  quoteCount: number;
  requesterId?: number;
  requesterEmail?: string;
  requesterName?: string;
  hasLowConfidence?: boolean;
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

interface EditingRequest {
  id: number;
  pickupLocation: {
    name: string;
    latitude?: number;
    longitude?: number;
    confidence: string;
  };
  destination: {
    name: string;
    latitude?: number;
    longitude?: number;
    confidence: string;
  };
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
  const [expandedJsonRequests, setExpandedJsonRequests] = useState<Set<number>>(new Set());
  const [showCoverageDialog, setShowCoverageDialog] = useState(false);
  const [editingRequest, setEditingRequest] = useState<EditingRequest | null>(null);
  const [requestSaving, setRequestSaving] = useState(false);
  const [expandedQuotes, setExpandedQuotes] = useState<Set<number>>(new Set());
  const [coverageConfig, setCoverageConfig] = useState({
    baseLocationName: '',
    coverageRadiusKm: 50,
    minPassengerCapacity: 1,
    maxPassengerCapacity: 50
  });
  const [existingCoverage, setExistingCoverage] = useState<{
    id: number;
    baseLocationName: string;
    latitude?: number;
    longitude?: number;
    coverageRadiusKm: number;
    minPassengerCapacity: number;
    maxPassengerCapacity: number;
    isGeocoded: boolean;
  } | null>(null);
  const [coverageSaving, setCoverageSaving] = useState(false);
  const [loadingCoverage, setLoadingCoverage] = useState(false);
  const [isEditingCoverage, setIsEditingCoverage] = useState(false);
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

  const handleSaveCoverage = async () => {
    if (!editingUser) return;

    setCoverageSaving(true);
    try {
      const url = existingCoverage && isEditingCoverage
        ? `${API_BASE_URL}/api/admin/operators/coverage/${existingCoverage.id}`
        : `${API_BASE_URL}/api/admin/operators/${editingUser.id}/coverage`;
      
      const method = existingCoverage && isEditingCoverage ? 'PUT' : 'POST';

      const response = await fetch(url, {
        method,
        headers: { 'Content-Type': 'application/json' },
        credentials: 'include',
        body: JSON.stringify(coverageConfig),
      });

      if (response.ok) {
        const data = await response.json();
        if (data.isGeocoded) {
          alert(existingCoverage && isEditingCoverage 
            ? 'Coverage updated successfully! Location was geocoded.' 
            : 'Coverage configured successfully! Location was geocoded.');
        } else {
          alert(`Coverage saved, but location could not be geocoded: ${data.geocodingError || 'Unknown error'}`);
        }
        // Reload coverage
        if (editingUser) {
          const coverageResponse = await fetch(`${API_BASE_URL}/api/admin/operators/${editingUser.id}/coverage`, {
            credentials: 'include',
          });
          if (coverageResponse.ok) {
            const coverage = await coverageResponse.json();
            if (coverage && coverage.id) {
              setExistingCoverage(coverage);
              setCoverageConfig({
                baseLocationName: coverage.baseLocationName,
                coverageRadiusKm: coverage.coverageRadiusKm,
                minPassengerCapacity: coverage.minPassengerCapacity,
                maxPassengerCapacity: coverage.maxPassengerCapacity
              });
              setIsEditingCoverage(false);
            }
          }
        }
      } else {
        const error = await response.json();
        alert(error.error || 'Failed to save coverage');
      }
    } catch (error) {
      console.error('Failed to save coverage:', error);
      alert('Failed to save coverage');
    } finally {
      setCoverageSaving(false);
    }
  };

  const handleDeleteCoverage = async () => {
    if (!editingUser || !existingCoverage) return;

    if (!confirm('Are you sure you want to delete this coverage location?')) {
      return;
    }

    setCoverageSaving(true);
    try {
      const response = await fetch(`${API_BASE_URL}/api/admin/operators/coverage/${existingCoverage.id}`, {
        method: 'DELETE',
        credentials: 'include',
      });

      if (response.ok) {
        alert('Coverage deleted successfully');
        setExistingCoverage(null);
        setCoverageConfig({
          baseLocationName: '',
          coverageRadiusKm: 50,
          minPassengerCapacity: 1,
          maxPassengerCapacity: 50
        });
        setIsEditingCoverage(false);
      } else {
        const error = await response.json();
        alert(error.error || 'Failed to delete coverage');
      }
    } catch (error) {
      console.error('Failed to delete coverage:', error);
      alert('Failed to delete coverage');
    } finally {
      setCoverageSaving(false);
    }
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

  const normalizeLocationName = (name: string): string => {
    if (!name) return name;
    
    // Split by common delimiters and capitalize each word
    // Handle common abbreviations and special cases
    const words = name.split(/[\s,]+/);
    const abbreviations = ['NSW', 'VIC', 'QLD', 'SA', 'WA', 'TAS', 'NT', 'ACT', 'St', 'Ave', 'Rd', 'Dr'];
    
    return words.map((word) => {
      const upperWord = word.toUpperCase();
      // Keep abbreviations as uppercase
      if (abbreviations.includes(upperWord)) {
        return upperWord;
      }
      // Capitalize first letter, lowercase the rest
      return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
    }).join(' ');
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
                        className={`flex items-start justify-between p-4 border rounded-lg ${
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
                            
                            {/* Coverage Information for Operators */}
                            {user.userType === 'operator' && user.coverage && (
                              <div className="mt-2 p-2 bg-blue-50 rounded border border-blue-200 max-w-md">
                                <div className="flex items-center gap-2 mb-1">
                                  <MapPin className="h-3.5 w-3.5 text-blue-600" />
                                  <span className="text-xs font-medium text-blue-900">Coverage</span>
                                </div>
                                <div className="text-xs text-blue-800 space-y-0.5">
                                  <p>
                                    <span className="font-medium">{normalizeLocationName(user.coverage.baseLocationName)}</span>
                                    {user.coverage.isGeocoded ? (
                                      <span className="ml-2 text-green-600">✓ Geocoded</span>
                                    ) : (
                                      <span className="ml-2 text-yellow-600">⚠ Not geocoded</span>
                                    )}
                                  </p>
                                  <p>
                                    Radius: <span className="font-medium">{user.coverage.coverageRadiusKm} km</span> • 
                                    Capacity: <span className="font-medium">{user.coverage.minPassengerCapacity}-{user.coverage.maxPassengerCapacity}</span> passengers
                                  </p>
                                </div>
                              </div>
                            )}
                            {user.userType === 'operator' && !user.coverage && (
                              <div className="mt-2 p-2 bg-gray-50 rounded border border-gray-200 max-w-md">
                                <div className="flex items-center gap-2">
                                  <MapPin className="h-3.5 w-3.5 text-gray-400" />
                                  <span className="text-xs text-gray-500">No coverage configured</span>
                                </div>
                              </div>
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
                        <div className="flex items-start gap-2">
                          {!user.isAdmin && (
                            <>
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={() => openEditDialog(user)}
                                className="w-28"
                              >
                                <Settings className="h-4 w-4 mr-1" />
                                Attributes
                              </Button>
                              {(user.userType === 'operator' || user.isAdmin) && (
                                <Button
                                  variant="outline"
                                  size="sm"
                                  className="w-28"
                                  onClick={async () => {
                                    setEditingUser(user);
                                    setShowCoverageDialog(true);
                                    setLoadingCoverage(true);
                                    setIsEditingCoverage(false);
                                    try {
                                      const response = await fetch(`${API_BASE_URL}/api/admin/operators/${user.id}/coverage`, {
                                        credentials: 'include',
                                      });
                                      if (response.ok) {
                                        const coverage = await response.json();
                                        if (coverage && coverage.id) {
                                          setExistingCoverage(coverage);
                                          setCoverageConfig({
                                            baseLocationName: coverage.baseLocationName,
                                            coverageRadiusKm: coverage.coverageRadiusKm,
                                            minPassengerCapacity: coverage.minPassengerCapacity,
                                            maxPassengerCapacity: coverage.maxPassengerCapacity
                                          });
                                        } else {
                                          setExistingCoverage(null);
                                          setCoverageConfig({
                                            baseLocationName: '',
                                            coverageRadiusKm: 50,
                                            minPassengerCapacity: 1,
                                            maxPassengerCapacity: 50
                                          });
                                        }
                                      } else {
                                        setExistingCoverage(null);
                                        setCoverageConfig({
                                          baseLocationName: '',
                                          coverageRadiusKm: 50,
                                          minPassengerCapacity: 1,
                                          maxPassengerCapacity: 50
                                        });
                                      }
                                    } catch (error) {
                                      console.error('Failed to load coverage:', error);
                                      setExistingCoverage(null);
                                      setCoverageConfig({
                                        baseLocationName: '',
                                        coverageRadiusKm: 50,
                                        minPassengerCapacity: 1,
                                        maxPassengerCapacity: 50
                                      });
                                    } finally {
                                      setLoadingCoverage(false);
                                    }
                                  }}
                                >
                                  <MapPin className="h-4 w-4 mr-1" />
                                  Coverage
                                </Button>
                              )}
                              {!user.isAdmin && (
                                <Button
                                  variant="outline"
                                  size="sm"
                                  className="w-28"
                                  onClick={async () => {
                                    if (user.isAdmin) {
                                      alert('Admin users cannot be deactivated');
                                      return;
                                    }
                                    if (!confirm(`Are you sure you want to ${user.isActive ? 'deactivate' : 'activate'} this user?`)) {
                                      return;
                                    }
                                    try {
                                      const response = await fetch(`${API_BASE_URL}/api/admin/users/${user.id}/active`, {
                                        method: 'PUT',
                                        headers: { 'Content-Type': 'application/json' },
                                        credentials: 'include',
                                        body: JSON.stringify({ isActive: !user.isActive }),
                                      });
                                      if (response.ok) {
                                        await loadUsers();
                                      } else {
                                        const error = await response.json();
                                        alert(error.error || 'Failed to update user status');
                                      }
                                    } catch (error) {
                                      console.error('Failed to update user status:', error);
                                      alert('Failed to update user status');
                                    }
                                  }}
                              >
                                {user.isActive ? 'Deactivate' : 'Activate'}
                              </Button>
                              )}
                            </>
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
                  {requests.map((request) => {
                    const showJson = expandedJsonRequests.has(request.id);
                    const pickup = request.requestData?.trip?.pickupLocation;
                    const destination = request.requestData?.trip?.destination;
                    const pickupName = pickup?.resolvedName || pickup?.rawInput || 'N/A';
                    const destinationName = destination?.resolvedName || destination?.rawInput || 'N/A';
                    const pickupConfidence = pickup?.confidence || 'low';
                    const destinationConfidence = destination?.confidence || 'low';
                    const hasLowConfidence = request.hasLowConfidence || pickupConfidence === 'low' || destinationConfidence === 'low';
                    const isDraft = request.status === 'Draft' || request.status === 'UnderReview';
                    
                    const toggleJson = () => {
                      const newSet = new Set(expandedJsonRequests);
                      if (showJson) {
                        newSet.delete(request.id);
                      } else {
                        newSet.add(request.id);
                      }
                      setExpandedJsonRequests(newSet);
                    };

                    const openEditRequest = () => {
                      setEditingRequest({
                        id: request.id,
                        pickupLocation: {
                          name: pickupName,
                          latitude: pickup?.lat,
                          longitude: pickup?.lng,
                          confidence: pickupConfidence
                        },
                        destination: {
                          name: destinationName,
                          latitude: destination?.lat,
                          longitude: destination?.lng,
                          confidence: destinationConfidence
                        }
                      });
                    };

                    const handlePublishRequest = async () => {
                      if (!confirm('Publish this request? Operators will be able to see it and submit quotes.')) {
                        return;
                      }

                      setRequestSaving(true);
                      try {
                        const response = await fetch(`${API_BASE_URL}/api/admin/requests/${request.id}/publish`, {
                          method: 'POST',
                          credentials: 'include',
                        });

                        if (response.ok) {
                          alert('Request published successfully!');
                          await loadRequests();
                        } else {
                          const error = await response.json();
                          alert(error.error || 'Failed to publish request');
                        }
                      } catch (error) {
                        console.error('Failed to publish request:', error);
                        alert('Failed to publish request');
                      } finally {
                        setRequestSaving(false);
                      }
                    };

                    const handleWithdrawRequest = async () => {
                      if (!confirm('Are you sure you want to withdraw this request? This action cannot be undone.')) {
                        return;
                      }

                      setRequestSaving(true);
                      try {
                        const response = await fetch(`${API_BASE_URL}/api/admin/requests/${request.id}/withdraw`, {
                          method: 'POST',
                          credentials: 'include',
                        });

                        if (response.ok) {
                          alert('Request withdrawn successfully!');
                          await loadRequests();
                        } else {
                          const error = await response.json();
                          alert(error.error || 'Failed to withdraw request');
                        }
                      } catch (error) {
                        console.error('Failed to withdraw request:', error);
                        alert('Failed to withdraw request');
                      } finally {
                        setRequestSaving(false);
                      }
                    };
                    
                    return (
                      <div 
                        key={request.id} 
                        className={`p-4 border rounded-lg ${
                          hasLowConfidence ? 'border-yellow-400 bg-yellow-50/50' : ''
                        }`}
                      >
                        <div className="flex justify-between items-start mb-2">
                          <div className="flex-1">
                            <div className="flex items-center gap-2 mb-1">
                              <h3 className="font-medium">Request #{request.id}</h3>
                              {hasLowConfidence && (
                                <span className="text-xs px-2 py-0.5 rounded bg-yellow-100 text-yellow-800 font-medium flex items-center gap-1">
                                  <AlertCircle className="h-3 w-3" />
                                  Low Confidence
                                </span>
                              )}
                              {isDraft && (
                                <span className="text-xs px-2 py-0.5 rounded bg-gray-100 text-gray-800 font-medium">
                                  Needs Review
                                </span>
                              )}
                            </div>
                            <div className="mt-2 space-y-1">
                              {/* Requester Information */}
                              <div className="mb-2 p-2 rounded border bg-muted/30 max-w-md">
                                <div className="flex items-center gap-2">
                                  <User className="h-4 w-4 text-muted-foreground" />
                                  <div className="flex-1">
                                    {request.requesterId && (request.requesterName || request.requesterEmail) ? (
                                      <div>
                                        <p className="text-sm font-medium">
                                          {request.requesterName || 'Unknown User'}
                                        </p>
                                        {request.requesterEmail && (
                                          <p className="text-xs text-muted-foreground">{request.requesterEmail}</p>
                                        )}
                                        <span className="text-xs text-green-600 mt-0.5 inline-block">✓ Authenticated</span>
                                      </div>
                                    ) : (
                                      <div>
                                        <p className="text-sm font-medium text-muted-foreground">Anonymous</p>
                                        <span className="text-xs text-gray-500 mt-0.5 inline-block">Not logged in</span>
                                      </div>
                                    )}
                                  </div>
                                </div>
                              </div>
                              <p className="text-sm">
                                <span className="font-medium">From:</span> {pickupName}
                                {pickupConfidence === 'low' && (
                                  <span className="text-xs text-yellow-600 ml-2">⚠ Low confidence</span>
                                )}
                              </p>
                              <p className="text-sm">
                                <span className="font-medium">To:</span> {destinationName}
                                {destinationConfidence === 'low' && (
                                  <span className="text-xs text-yellow-600 ml-2">⚠ Low confidence</span>
                                )}
                              </p>
                            </div>
                          </div>
                          <div className="flex flex-col gap-2 items-end">
                            <span className={`text-xs px-2 py-1 rounded ${
                              request.status === 'Draft' ? 'bg-gray-100 text-gray-800' :
                              request.status === 'UnderReview' ? 'bg-blue-100 text-blue-800' :
                              request.status === 'Published' ? 'bg-green-100 text-green-800' :
                              request.status === 'QuotesReceived' ? 'bg-yellow-100 text-yellow-800' :
                              request.status === 'Cancelled' ? 'bg-red-100 text-red-800' :
                              'bg-gray-100 text-gray-800'
                            }`}>
                              {request.status}
                            </span>
                            {isDraft && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={openEditRequest}
                                className="w-28"
                              >
                                <Edit className="h-4 w-4 mr-1" />
                                Edit
                              </Button>
                            )}
                            {isDraft && (
                              <Button
                                size="sm"
                                onClick={handlePublishRequest}
                                disabled={requestSaving || !pickup?.lat || !destination?.lat}
                                className="w-28"
                              >
                                <CheckCircle className="h-4 w-4 mr-1" />
                                Publish
                              </Button>
                            )}
                            {request.status !== 'Cancelled' && request.status !== 'Completed' && (
                              <Button
                                variant="outline"
                                size="sm"
                                onClick={handleWithdrawRequest}
                                disabled={requestSaving}
                                className="w-28 text-red-600 hover:text-red-700 hover:bg-red-50"
                              >
                                <XCircle className="h-4 w-4 mr-1" />
                                Withdraw
                              </Button>
                            )}
                          </div>
                        </div>
                        <div className="flex items-center gap-4 text-xs text-muted-foreground mb-3">
                          <span>Created {new Date(request.createdAt).toLocaleDateString()}</span>
                          <span>•</span>
                          <span>{request.quoteCount} quote(s)</span>
                          {request.requesterEmail && (
                            <>
                              <span>•</span>
                              <span>{request.requesterEmail}</span>
                            </>
                          )}
                        </div>
                        {request.rawJsonPayload && (
                          <div className="mt-3">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={toggleJson}
                              className="mb-2"
                            >
                              {showJson ? 'Hide' : 'Show'} Raw JSON
                            </Button>
                            {showJson && (
                              <div className="mt-2 p-3 bg-gray-50 rounded border overflow-auto max-h-96">
                                <pre className="text-xs font-mono whitespace-pre-wrap break-words">
                                  {request.rawJsonPayload}
                                </pre>
                              </div>
                            )}
                          </div>
                        )}
                        {request.quotes && request.quotes.length > 0 && (
                          <div className="mt-4">
                            <div 
                              className="bg-blue-50 border-2 border-blue-200 rounded-lg p-3 cursor-pointer hover:bg-blue-100 transition-colors"
                              onClick={() => {
                                const newExpanded = new Set(expandedQuotes);
                                if (newExpanded.has(request.id)) {
                                  newExpanded.delete(request.id);
                                } else {
                                  newExpanded.add(request.id);
                                }
                                setExpandedQuotes(newExpanded);
                              }}
                            >
                              <div className="flex items-center justify-between">
                                <div className="flex items-center gap-2">
                                  <DollarSign className="h-4 w-4 text-blue-700" />
                                  <p className="font-semibold text-blue-900">
                                    {request.quotes.length} Quote{request.quotes.length !== 1 ? 's' : ''}
                                  </p>
                                  {!expandedQuotes.has(request.id) && (
                                    <span className="text-xs text-blue-700">
                                      (Click to view)
                                    </span>
                                  )}
                                </div>
                                {expandedQuotes.has(request.id) ? (
                                  <ChevronUp className="h-4 w-4 text-blue-700" />
                                ) : (
                                  <ChevronDown className="h-4 w-4 text-blue-700" />
                                )}
                              </div>
                              {!expandedQuotes.has(request.id) && (
                                <div className="mt-2 flex items-center gap-4 text-xs text-blue-700">
                                  <span>
                                    Lowest: ${Math.min(...request.quotes.map(q => q.price))} {request.quotes[0]?.currency}
                                  </span>
                                  <span>
                                    Highest: ${Math.max(...request.quotes.map(q => q.price))} {request.quotes[0]?.currency}
                                  </span>
                                </div>
                              )}
                            </div>
                            {expandedQuotes.has(request.id) && (
                              <div className="mt-3 space-y-2 bg-white border border-blue-200 rounded-lg p-3">
                                {request.quotes.map((quote) => (
                                  <div key={quote.id} className="p-3 bg-gray-50 rounded border border-gray-200 hover:bg-gray-100 transition-colors">
                                    <div className="flex justify-between items-start mb-2">
                                      <div>
                                        <p className="font-medium text-sm">{quote.providerName}</p>
                                        <p className="text-xs text-muted-foreground">{quote.providerEmail}</p>
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
                                      <p className="text-lg font-bold">${quote.price} {quote.currency}</p>
                                      <p className="text-xs text-muted-foreground">
                                        {new Date(quote.createdAt).toLocaleDateString()}
                                      </p>
                                    </div>
                                    {quote.notes && (
                                      <p className="text-sm text-muted-foreground mt-2 pt-2 border-t">
                                        <span className="font-medium">Notes:</span> {quote.notes}
                                      </p>
                                    )}
                                  </div>
                                ))}
                              </div>
                            )}
                          </div>
                        )}
                      </div>
                    );
                  })}
                </div>
              )}
            </CardContent>
          </Card>
        )}
      </div>

      {/* Edit User Attributes Dialog */}
      {editingUser && !showCoverageDialog && (
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

      {/* Configure Operator Coverage Dialog */}
      {showCoverageDialog && editingUser && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg shadow-lg w-full max-w-2xl mx-4 max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold">Configure Operator Coverage</h2>
                <Button variant="ghost" size="sm" onClick={() => {
                  setShowCoverageDialog(false);
                  setEditingUser(null);
                  setExistingCoverage(null);
                  setIsEditingCoverage(false);
                }}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              
              <div className="space-y-4">
                <div>
                  <p className="text-sm font-medium mb-1">{editingUser.name}</p>
                  <p className="text-sm text-muted-foreground">{editingUser.email}</p>
                </div>

                {loadingCoverage ? (
                  <p className="text-muted-foreground text-center py-4">Loading coverage...</p>
                ) : (
                  <>
                    {existingCoverage && !isEditingCoverage && (
                      <div className="border rounded-lg p-4 bg-muted/30 mb-4">
                        <div className="flex items-start justify-between mb-3">
                          <div className="flex-1">
                            <h3 className="text-sm font-medium mb-2">Current Coverage</h3>
                            <p className="font-medium text-sm">{normalizeLocationName(existingCoverage.baseLocationName)}</p>
                            <p className="text-xs text-muted-foreground mt-1">
                              Radius: {existingCoverage.coverageRadiusKm} km • Capacity: {existingCoverage.minPassengerCapacity}-{existingCoverage.maxPassengerCapacity} passengers
                            </p>
                            {existingCoverage.isGeocoded && existingCoverage.latitude && existingCoverage.longitude ? (
                              <p className="text-xs text-green-600 mt-1">✓ Location geocoded</p>
                            ) : (
                              <p className="text-xs text-yellow-600 mt-1">⚠ Location not geocoded</p>
                            )}
                          </div>
                          <div className="flex gap-2">
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={() => setIsEditingCoverage(true)}
                            >
                              <Edit className="h-4 w-4 mr-1" />
                              Edit
                            </Button>
                            <Button
                              variant="outline"
                              size="sm"
                              onClick={handleDeleteCoverage}
                              disabled={coverageSaving}
                            >
                              <Trash2 className="h-4 w-4 mr-1" />
                              Delete
                            </Button>
                          </div>
                        </div>
                        {existingCoverage.isGeocoded && existingCoverage.latitude && existingCoverage.longitude && (
                          <div className="mt-3">
                            <CoverageMap
                              latitude={existingCoverage.latitude}
                              longitude={existingCoverage.longitude}
                              radiusKm={existingCoverage.coverageRadiusKm}
                              locationName={normalizeLocationName(existingCoverage.baseLocationName)}
                            />
                          </div>
                        )}
                      </div>
                    )}

                    {(isEditingCoverage || !existingCoverage) && (
                      <div className="border-t pt-4 space-y-4">
                        <h3 className="text-sm font-medium">{existingCoverage ? 'Edit Coverage Location' : 'Add Coverage Location'}</h3>
                      
                      <div>
                        <Label htmlFor="baseLocation" className="text-sm font-medium mb-2 block">
                          Base Location <span className="text-muted-foreground">(e.g., "Sydney, NSW" or "123 Main St, Melbourne")</span>
                        </Label>
                        <Input
                          id="baseLocation"
                          value={coverageConfig.baseLocationName}
                          onChange={(e) => setCoverageConfig({ ...coverageConfig, baseLocationName: e.target.value })}
                          placeholder="Enter location name"
                        />
                        <p className="text-xs text-muted-foreground mt-1">
                          We'll automatically find the coordinates for this location.
                        </p>
                      </div>

                      <div>
                        <Label htmlFor="coverageRadius" className="text-sm font-medium mb-2 block">
                          Coverage Radius (km)
                        </Label>
                        <Input
                          id="coverageRadius"
                          type="number"
                          min="1"
                          step="1"
                          value={coverageConfig.coverageRadiusKm}
                          onChange={(e) => setCoverageConfig({ ...coverageConfig, coverageRadiusKm: parseFloat(e.target.value) || 0 })}
                          placeholder="50"
                        />
                      </div>

                      <div className="grid grid-cols-2 gap-4">
                        <div>
                          <Label htmlFor="minCapacity" className="text-sm font-medium mb-2 block">
                            Min Passengers
                          </Label>
                          <Input
                            id="minCapacity"
                            type="number"
                            min="1"
                            step="1"
                            value={coverageConfig.minPassengerCapacity}
                            onChange={(e) => setCoverageConfig({ ...coverageConfig, minPassengerCapacity: parseInt(e.target.value) || 1 })}
                            placeholder="1"
                          />
                        </div>
                        <div>
                          <Label htmlFor="maxCapacity" className="text-sm font-medium mb-2 block">
                            Max Passengers
                          </Label>
                          <Input
                            id="maxCapacity"
                            type="number"
                            min="1"
                            step="1"
                            value={coverageConfig.maxPassengerCapacity}
                            onChange={(e) => setCoverageConfig({ ...coverageConfig, maxPassengerCapacity: parseInt(e.target.value) || 1 })}
                            placeholder="50"
                          />
                        </div>
                      </div>

                        <div className="flex justify-end gap-2 pt-4 border-t">
                          {isEditingCoverage && (
                            <Button variant="outline" onClick={() => {
                              setIsEditingCoverage(false);
                              // Reset to existing values
                              if (existingCoverage) {
                                setCoverageConfig({
                                  baseLocationName: existingCoverage.baseLocationName,
                                  coverageRadiusKm: existingCoverage.coverageRadiusKm,
                                  minPassengerCapacity: existingCoverage.minPassengerCapacity,
                                  maxPassengerCapacity: existingCoverage.maxPassengerCapacity
                                });
                              }
                            }} disabled={coverageSaving}>
                              Cancel
                            </Button>
                          )}
                          <Button variant="outline" onClick={() => {
                            setShowCoverageDialog(false);
                            setEditingUser(null);
                            setExistingCoverage(null);
                            setIsEditingCoverage(false);
                          }} disabled={coverageSaving}>
                            {isEditingCoverage ? 'Close' : 'Close'}
                          </Button>
                          <Button onClick={handleSaveCoverage} disabled={coverageSaving || !coverageConfig.baseLocationName.trim()}>
                            {coverageSaving ? 'Saving...' : existingCoverage && isEditingCoverage ? 'Update Coverage' : 'Add Coverage'}
                          </Button>
                        </div>
                      </div>
                    )}
                  </>
                )}
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Edit Request Locations Dialog */}
      {editingRequest && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-background rounded-lg shadow-lg w-full max-w-3xl mx-4 max-h-[90vh] overflow-y-auto">
            <div className="p-6">
              <div className="flex items-center justify-between mb-4">
                <h2 className="text-xl font-semibold">Edit Request Locations</h2>
                <Button variant="ghost" size="sm" onClick={() => setEditingRequest(null)}>
                  <X className="h-4 w-4" />
                </Button>
              </div>
              
              <div className="space-y-6">
                <div>
                  <h3 className="text-sm font-medium mb-4">Request #{editingRequest.id}</h3>
                </div>

                <LocationEditor
                  label="Pickup Location"
                  locationName={editingRequest.pickupLocation.name}
                  latitude={editingRequest.pickupLocation.latitude}
                  longitude={editingRequest.pickupLocation.longitude}
                  isGeocoded={!!(editingRequest.pickupLocation.latitude && editingRequest.pickupLocation.longitude)}
                  onLocationChange={(name) => {
                    setEditingRequest({
                      ...editingRequest,
                      pickupLocation: { ...editingRequest.pickupLocation, name }
                    });
                  }}
                  onGeocodeResult={(lat, lng) => {
                    setEditingRequest({
                      ...editingRequest,
                      pickupLocation: { ...editingRequest.pickupLocation, latitude: lat, longitude: lng, confidence: 'high' }
                    });
                  }}
                  placeholder="Enter pickup location (e.g., 'Sydney, NSW')"
                />

                <LocationEditor
                  label="Destination"
                  locationName={editingRequest.destination.name}
                  latitude={editingRequest.destination.latitude}
                  longitude={editingRequest.destination.longitude}
                  isGeocoded={!!(editingRequest.destination.latitude && editingRequest.destination.longitude)}
                  onLocationChange={(name) => {
                    setEditingRequest({
                      ...editingRequest,
                      destination: { ...editingRequest.destination, name }
                    });
                  }}
                  onGeocodeResult={(lat, lng) => {
                    setEditingRequest({
                      ...editingRequest,
                      destination: { ...editingRequest.destination, latitude: lat, longitude: lng, confidence: 'high' }
                    });
                  }}
                  placeholder="Enter destination (e.g., 'Melbourne, VIC')"
                />

                <div className="flex justify-end gap-2 pt-4 border-t">
                  <Button variant="outline" onClick={() => setEditingRequest(null)} disabled={requestSaving}>
                    Cancel
                  </Button>
                  <Button 
                    onClick={async () => {
                      if (!editingRequest) return;
                      setRequestSaving(true);
                      try {
                        // Save pickup location
                        const pickupResponse = await fetch(`${API_BASE_URL}/api/admin/requests/${editingRequest.id}/location`, {
                          method: 'PUT',
                          headers: { 'Content-Type': 'application/json' },
                          credentials: 'include',
                          body: JSON.stringify({
                            locationType: 'pickup',
                            locationName: editingRequest.pickupLocation.name,
                            latitude: editingRequest.pickupLocation.latitude,
                            longitude: editingRequest.pickupLocation.longitude
                          }),
                        });

                        // Save destination
                        const destResponse = await fetch(`${API_BASE_URL}/api/admin/requests/${editingRequest.id}/location`, {
                          method: 'PUT',
                          headers: { 'Content-Type': 'application/json' },
                          credentials: 'include',
                          body: JSON.stringify({
                            locationType: 'destination',
                            locationName: editingRequest.destination.name,
                            latitude: editingRequest.destination.latitude,
                            longitude: editingRequest.destination.longitude
                          }),
                        });

                        if (pickupResponse.ok && destResponse.ok) {
                          alert('Locations updated successfully!');
                          setEditingRequest(null);
                          await loadRequests();
                        } else {
                          const error = await (pickupResponse.ok ? destResponse : pickupResponse).json();
                          alert(error.error || 'Failed to update locations');
                        }
                      } catch (error) {
                        console.error('Failed to update locations:', error);
                        alert('Failed to update locations');
                      } finally {
                        setRequestSaving(false);
                      }
                    }}
                    disabled={requestSaving}
                  >
                    {requestSaving ? 'Saving...' : 'Save Locations'}
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
