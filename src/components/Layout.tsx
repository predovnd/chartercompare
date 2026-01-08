import { Header } from './Header';
import { ChatSection } from './ChatSection';
import { Sidebar } from './Sidebar';

export function Layout() {
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
