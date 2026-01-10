import { useState, useEffect } from 'react';
import { Button } from './ui/button';
import { Input } from './ui/input';
import { Label } from './ui/label';
import { MapPin, Loader2 } from 'lucide-react';
import { CoverageMap } from './CoverageMap';

interface LocationEditorProps {
  locationName: string;
  latitude?: number;
  longitude?: number;
  isGeocoded: boolean;
  onLocationChange: (locationName: string) => void;
  onGeocodeResult?: (latitude: number, longitude: number) => void;
  label?: string;
  placeholder?: string;
}

export function LocationEditor({
  locationName,
  latitude,
  longitude,
  isGeocoded,
  onLocationChange,
  onGeocodeResult,
  label = 'Location',
  placeholder = 'Enter location name (e.g., "Sydney, NSW")'
}: LocationEditorProps) {
  const [localLocationName, setLocalLocationName] = useState(locationName);
  const [isGeocoding, setIsGeocoding] = useState(false);
  const [geocodeError, setGeocodeError] = useState<string | null>(null);
  const [currentLat, setCurrentLat] = useState<number | undefined>(latitude);
  const [currentLng, setCurrentLng] = useState<number | undefined>(longitude);

  useEffect(() => {
    setLocalLocationName(locationName);
    setCurrentLat(latitude);
    setCurrentLng(longitude);
  }, [locationName, latitude, longitude]);

  const handleGeocode = async () => {
    if (!localLocationName.trim()) {
      setGeocodeError('Please enter a location name');
      return;
    }

    setIsGeocoding(true);
    setGeocodeError(null);

    try {
      // Call backend geocoding endpoint
      const API_BASE_URL = import.meta.env.VITE_API_URL ?? 'http://localhost:5000';
      const response = await fetch(`${API_BASE_URL}/api/admin/geocode?location=${encodeURIComponent(localLocationName)}`, {
        credentials: 'include',
      });

      if (response.ok) {
        const data = await response.json();
        if (data.latitude && data.longitude) {
          setCurrentLat(data.latitude);
          setCurrentLng(data.longitude);
          if (onGeocodeResult) {
            onGeocodeResult(data.latitude, data.longitude);
          }
        } else {
          setGeocodeError('Could not resolve location. Please check the location name.');
        }
      } else {
        setGeocodeError('Failed to geocode location');
      }
    } catch (error) {
      console.error('Geocoding error:', error);
      setGeocodeError('Failed to geocode location');
    } finally {
      setIsGeocoding(false);
    }
  };

  const normalizeLocationName = (name: string): string => {
    if (!name) return name;
    const words = name.split(/[\s,]+/);
    const abbreviations = ['NSW', 'VIC', 'QLD', 'SA', 'WA', 'TAS', 'NT', 'ACT', 'St', 'Ave', 'Rd', 'Dr'];
    
    return words.map((word) => {
      const upperWord = word.toUpperCase();
      if (abbreviations.includes(upperWord)) {
        return upperWord;
      }
      return word.charAt(0).toUpperCase() + word.slice(1).toLowerCase();
    }).join(' ');
  };

  return (
    <div className="space-y-3">
      <div>
        <Label className="text-sm font-medium mb-2 block">
          {label}
        </Label>
        <div className="flex gap-2">
          <Input
            value={localLocationName}
            onChange={(e) => {
              setLocalLocationName(e.target.value);
              onLocationChange(e.target.value);
            }}
            placeholder={placeholder}
            className="flex-1"
          />
          <Button
            type="button"
            variant="outline"
            onClick={handleGeocode}
            disabled={isGeocoding || !localLocationName.trim()}
          >
            {isGeocoding ? (
              <Loader2 className="h-4 w-4 animate-spin" />
            ) : (
              <MapPin className="h-4 w-4" />
            )}
          </Button>
        </div>
        {geocodeError && (
          <p className="text-xs text-destructive mt-1">{geocodeError}</p>
        )}
        {isGeocoded && currentLat && currentLng && (
          <p className="text-xs text-green-600 mt-1">✓ Location geocoded</p>
        )}
        {!isGeocoded && localLocationName && (
          <p className="text-xs text-yellow-600 mt-1">⚠ Location not geocoded - click the map icon to geocode</p>
        )}
      </div>

      {currentLat && currentLng && (
        <div className="border rounded overflow-hidden">
          <div className="w-full h-64">
            <CoverageMap
              latitude={currentLat}
              longitude={currentLng}
              radiusKm={0} // No radius for location display
              locationName={normalizeLocationName(localLocationName)}
            />
          </div>
        </div>
      )}
    </div>
  );
}
