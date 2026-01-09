import { useEffect, useRef } from 'react';
import L from 'leaflet';

// Import Leaflet CSS
import 'leaflet/dist/leaflet.css';

interface CoverageMapProps {
  latitude: number;
  longitude: number;
  radiusKm: number;
  locationName: string;
}

// Fix for default marker icon in Leaflet with Vite
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
  iconRetinaUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon-2x.png',
  iconUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-icon.png',
  shadowUrl: 'https://cdnjs.cloudflare.com/ajax/libs/leaflet/1.9.4/images/marker-shadow.png',
});

export function CoverageMap({ latitude, longitude, radiusKm, locationName }: CoverageMapProps) {
  const mapRef = useRef<HTMLDivElement>(null);
  const mapInstanceRef = useRef<L.Map | null>(null);
  const circleRef = useRef<L.Circle | null>(null);
  const markerRef = useRef<L.Marker | null>(null);

  useEffect(() => {
    if (!mapRef.current) return;

    // Initialize map if it doesn't exist
    if (!mapInstanceRef.current) {
      const map = L.map(mapRef.current).setView([latitude, longitude], 10);
      
      // Add OpenStreetMap tiles
      L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
        attribution: '© OpenStreetMap contributors',
        maxZoom: 19,
      }).addTo(map);

      mapInstanceRef.current = map;
    }

    const map = mapInstanceRef.current;
    if (!map) return;

    // Remove existing circle and marker
    if (circleRef.current) {
      map.removeLayer(circleRef.current);
      circleRef.current = null;
    }
    if (markerRef.current) {
      map.removeLayer(markerRef.current);
      markerRef.current = null;
    }

    // Add marker at base location
    const marker = L.marker([latitude, longitude])
      .addTo(map)
      .bindPopup(`<strong>${locationName}</strong><br/>Coverage: ${radiusKm} km`);
    markerRef.current = marker;

    // Add circle to show coverage radius (only if radius > 0)
    if (radiusKm > 0) {
      // Convert km to meters for Leaflet (which uses meters)
      const radiusMeters = radiusKm * 1000;
      const circle = L.circle([latitude, longitude], {
        radius: radiusMeters,
        color: '#3b82f6', // Blue color
        fillColor: '#3b82f6',
        fillOpacity: 0.15,
        weight: 2,
      }).addTo(map);
      circleRef.current = circle;

      // Fit map to show the circle with some padding
      const bounds = circle.getBounds();
      map.fitBounds(bounds, { padding: [30, 30] });
    } else {
      // Just center on the marker if no radius
      map.setView([latitude, longitude], 13);
    }

    // Cleanup function
    return () => {
      if (mapInstanceRef.current) {
        if (circleRef.current) {
          mapInstanceRef.current.removeLayer(circleRef.current);
        }
        if (markerRef.current) {
          mapInstanceRef.current.removeLayer(markerRef.current);
        }
      }
    };
  }, [latitude, longitude, radiusKm, locationName]);

  // Cleanup on unmount
  useEffect(() => {
    return () => {
      if (mapInstanceRef.current) {
        mapInstanceRef.current.remove();
        mapInstanceRef.current = null;
      }
    };
  }, []);

  return (
    <div className="w-full h-64 rounded border overflow-hidden" ref={mapRef} />
  );
}
