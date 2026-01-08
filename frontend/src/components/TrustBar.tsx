import { Badge } from './ui/badge';
import { Shield, Clock, MapPin } from 'lucide-react';

export function TrustBar() {
  return (
    <section className="border-y bg-muted/30 py-8">
      <div className="container">
        <div className="flex flex-wrap items-center justify-center gap-6 md:gap-12">
          <div className="flex items-center gap-2">
            <Shield className="h-5 w-5 text-primary" />
            <Badge variant="outline" className="text-sm font-normal">
              Licensed operators
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <Clock className="h-5 w-5 text-primary" />
            <Badge variant="outline" className="text-sm font-normal">
              Fast turnaround
            </Badge>
          </div>
          <div className="flex items-center gap-2">
            <MapPin className="h-5 w-5 text-primary" />
            <Badge variant="outline" className="text-sm font-normal">
              Australia-wide
            </Badge>
          </div>
        </div>
      </div>
    </section>
  );
}
