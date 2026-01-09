import { useState, useEffect } from 'react';
import { Bus, User, LogOut } from 'lucide-react';
import { useNavigate, Link } from 'react-router-dom';
import { Button } from './ui/button';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5000';

interface UserInfo {
  id: number;
  email: string;
  name: string;
  userType: 'operator' | 'requester' | 'admin';
  isAdmin?: boolean;
}

export function Header() {
  const [user, setUser] = useState<UserInfo | null>(null);
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
        setUser({
          id: data.id,
          email: data.email,
          name: data.name,
          userType: data.userType || (data.isAdmin ? 'admin' : 'operator'),
          isAdmin: data.isAdmin
        });
      }
    } catch (error) {
      console.error('Auth check failed:', error);
    } finally {
      setLoading(false);
    }
  };

  const handleLogout = async () => {
    try {
      await fetch(`${API_BASE_URL}/api/auth/logout`, {
        method: 'POST',
        credentials: 'include',
      });
      setUser(null);
      window.location.href = '/';
    } catch (error) {
      console.error('Logout failed:', error);
    }
  };

  const getDashboardPath = () => {
    if (!user) return '/requester/login';
    if (user.userType === 'requester') return '/requester/dashboard';
    if (user.userType === 'operator') return '/provider/dashboard';
    if (user.userType === 'admin') return '/admin/dashboard';
    return '/requester/login';
  };

  const handleMyRequestsClick = (e: React.MouseEvent<HTMLAnchorElement>) => {
    e.preventDefault();
    if (user) {
      // User is logged in, go to their dashboard
      navigate(getDashboardPath());
    } else {
      // User not logged in, go to login page
      navigate('/requester/login');
    }
  };

  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container flex h-16 items-center justify-between">
        <Link to="/" className="flex items-center gap-2 hover:opacity-80 transition-opacity">
          <Bus className="h-6 w-6 text-primary" />
          <h1 className="text-xl font-semibold">CharterCompare</h1>
        </Link>
        <nav className="flex items-center gap-6">
          <a href="#how-it-works" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
            How it works
          </a>
          {!loading && (
            <>
              {user ? (
                <>
                  <a 
                    href={getDashboardPath()} 
                    onClick={(e) => {
                      e.preventDefault();
                      navigate(getDashboardPath());
                    }}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    Dashboard
                  </a>
                  <div className="flex items-center gap-2 px-3 py-1.5 rounded-lg bg-muted/50 border">
                    <User className="h-4 w-4 text-muted-foreground" />
                    <span className="text-sm font-medium">{user.name}</span>
                  </div>
                  <Button variant="ghost" size="sm" onClick={handleLogout}>
                    <LogOut className="h-4 w-4 mr-2" />
                    <span className="hidden sm:inline">Logout</span>
                  </Button>
                </>
              ) : (
                <>
                  <a 
                    href="/requester/login" 
                    onClick={handleMyRequestsClick}
                    className="text-sm text-muted-foreground hover:text-foreground transition-colors"
                  >
                    My Requests
                  </a>
                  <a href="/provider/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    For Operators
                  </a>
                  <a href="/admin/login" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
                    Admin
                  </a>
                </>
              )}
            </>
          )}
        </nav>
      </div>
    </header>
  );
}
