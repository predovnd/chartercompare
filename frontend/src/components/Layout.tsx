import { useState, useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { Header } from './Header';
import { ChatSection } from './ChatSection';
import { Sidebar } from './Sidebar';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from './ui/card';
import { Button } from './ui/button';
import { ArrowRight, UserCheck, Shield } from 'lucide-react';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

export function Layout() {
  const [user, setUser] = useState<any>(null);
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
      if (response.ok) {
        const data = await response.json();
        setUser(data);
      }
    } catch (error) {
      console.error('Auth check failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const getDashboardPath = () => {
    if (!user) return null;
    if (user.userType === 'requester') return '/requester/dashboard';
    if (user.userType === 'operator') return '/provider/dashboard';
    if (user.userType === 'admin' || user.isAdmin) return '/admin/dashboard';
    return null;
  };

  // If operator or admin is logged in, show a message instead of the chat
  if (!loading && user) {
    // Check if it's an operator or admin (not a requester)
    const isOperator = user.userType === 'operator';
    const isAdmin = user.isAdmin || user.userType === 'admin';
    
    if (isOperator || isAdmin) {
      const dashboardPath = getDashboardPath();
      return (
        <div className="min-h-screen flex flex-col">
          <Header />
          <main className="flex-1 flex items-center justify-center p-4">
            <Card className="w-full max-w-md">
              <CardHeader className="text-center">
                <div className="flex justify-center mb-4">
                  {isAdmin ? (
                    <Shield className="h-12 w-12 text-primary" />
                  ) : (
                    <UserCheck className="h-12 w-12 text-primary" />
                  )}
                </div>
                <CardTitle className="text-2xl">
                  {isAdmin ? 'Admin Account' : 'Operator Account'}
                </CardTitle>
                <CardDescription>
                  You're currently logged in as an {isAdmin ? 'admin' : 'operator'}. 
                  To create a new request, please log out and use a customer account.
                </CardDescription>
              </CardHeader>
              <CardContent className="space-y-4">
                {dashboardPath && (
                  <Button 
                    onClick={() => navigate(dashboardPath)} 
                    className="w-full"
                    size="lg"
                  >
                    Go to Dashboard
                    <ArrowRight className="h-4 w-4 ml-2" />
                  </Button>
                )}
                <Button 
                  variant="outline"
                  onClick={() => {
                    fetch(`${API_BASE_URL}/api/auth/logout`, {
                      method: 'POST',
                      credentials: 'include',
                    }).then(() => {
                      window.location.href = '/';
                    });
                  }}
                  className="w-full"
                >
                  Logout to Create Request
                </Button>
              </CardContent>
            </Card>
          </main>
        </div>
      );
    }
  }

  return (
    <div className="min-h-screen flex flex-col">
      <Header />
      <main className="flex-1">
        <div className="container grid lg:grid-cols-[1.2fr_0.8fr] gap-12 py-8">
          {/* Left: Chat Section */}
          <div>
            <ChatSection />
          </div>
          {/* Right: Sidebar */}
          <div className="hidden lg:block">
            <div className="sticky top-20">
              <Sidebar />
            </div>
          </div>
        </div>
        {/* Mobile: Sidebar below chat */}
        <div className="lg:hidden container pb-8">
          <Sidebar />
        </div>
      </main>
    </div>
  );
}
