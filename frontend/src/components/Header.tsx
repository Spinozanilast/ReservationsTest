import { BusIcon, CalendarDotsIcon, GearSixIcon } from "@phosphor-icons/react";
import { CustomLink } from "./CustomLink";
import { useLocation } from "@tanstack/react-router";

export function Header() {
  const currentLocation = useLocation();

  if (currentLocation.pathname === "/") {
    return (
      <header className="border-b border-border bg-surface">
        <div className="flex justify-center items-center gap-6 px-6 py-4 w-full">
          <CustomLink to="/" className="text-lg font-semibold">
            <span className="inline-flex items-center gap-2">
              <BusIcon weight="bold" />
              Autobus
            </span>
          </CustomLink>
        </div>
      </header>
    );
  }

  return (
    <header className="border-b border-border bg-surface">
      <div className="mx-auto flex max-w-4xl items-center gap-6 px-6 py-4">
        <CustomLink to="/" className="text-lg font-semibold">
          <span className="inline-flex items-center gap-2">
            <BusIcon weight="bold" />
            Autobus
          </span>
        </CustomLink>
        <nav className="flex items-center gap-4">
          <CustomLink to="/client">
            <span className="inline-flex items-center gap-1.5">
              <CalendarDotsIcon />
              Book
            </span>
          </CustomLink>
          <CustomLink to="/admin" search={{ tab: "services" }}>
            <span className="inline-flex items-center gap-1.5">
              <GearSixIcon />
              Admin
            </span>
          </CustomLink>
        </nav>
      </div>
    </header>
  );
}
