"use client";

import { useEffect, useRef } from "react";
import { useTheme } from "next-themes";
import "leaflet/dist/leaflet.css";

/** Free interactive map (Leaflet + CARTO basemap over OpenStreetMap data — no API key, no
 * account) with a branded pin on the university and a label. Follows the light/dark theme. */
export function UniversityMap({
  lat,
  lon,
  name,
  city,
}: {
  lat: number;
  lon: number;
  name: string;
  city: string;
}) {
  const ref = useRef<HTMLDivElement>(null);
  const { resolvedTheme } = useTheme();
  const dark = resolvedTheme === "dark";

  useEffect(() => {
    let map: import("leaflet").Map | null = null;
    let cancelled = false;

    (async () => {
      const L = (await import("leaflet")).default;
      if (cancelled || !ref.current) return;

      map = L.map(ref.current, { scrollWheelZoom: false, zoomControl: true, attributionControl: true }).setView([lat, lon], 15);

      L.tileLayer(
        `https://{s}.basemaps.cartocdn.com/${dark ? "dark_all" : "rastertiles/voyager"}/{z}/{x}/{y}{r}.png`,
        {
          maxZoom: 19,
          subdomains: "abcd",
          attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> &copy; <a href="https://carto.com/attributions">CARTO</a>',
        }
      ).addTo(map);

      const pin = L.divIcon({
        className: "",
        iconSize: [34, 44],
        iconAnchor: [17, 44],
        html: `<svg width="34" height="44" viewBox="0 0 34 44" xmlns="http://www.w3.org/2000/svg" style="filter:drop-shadow(0 3px 4px rgba(0,0,0,.35))">
          <path d="M17 0C7.6 0 0 7.5 0 16.8 0 29.4 17 44 17 44s17-14.6 17-27.2C34 7.5 26.4 0 17 0z" fill="#14b8a6"/>
          <circle cx="17" cy="16.5" r="6.5" fill="#fff"/></svg>`,
      });

      // Textual content is escaped by building DOM nodes, never interpolated as HTML.
      const label = document.createElement("div");
      const strong = document.createElement("strong");
      strong.textContent = name;
      const sub = document.createElement("div");
      sub.textContent = city;
      sub.style.opacity = "0.7";
      label.append(strong, sub);

      L.marker([lat, lon], { icon: pin, title: name }).addTo(map).bindPopup(label, { closeButton: false }).openPopup();
    })();

    return () => {
      cancelled = true;
      map?.remove();
    };
  }, [lat, lon, name, city, dark]);

  return (
    <div
      ref={ref}
      role="img"
      aria-label={name}
      className="z-0 h-64 w-full overflow-hidden rounded-[var(--radius-md)] border border-[var(--border-subtle)]"
    />
  );
}
