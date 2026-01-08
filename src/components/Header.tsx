import { Bus } from 'lucide-react';

export function Header() {
  return (
    <header className="sticky top-0 z-40 w-full border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
      <div className="container flex h-16 items-center justify-between">
        <div className="flex items-center gap-2">
          <Bus className="h-6 w-6 text-primary" />
          <h1 className="text-xl font-semibold">CharterCompare</h1>
        </div>
        <nav className="flex items-center gap-6">
          <a href="#how-it-works" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
            How it works
          </a>
          <a href="#" className="text-sm text-muted-foreground hover:text-foreground transition-colors">
            For Providers
          </a>
        </nav>
      </div>
    </header>
  );
}
