import { TanStackRouterDevtools } from "@tanstack/react-router-devtools";
import { Outlet, createRootRoute } from "@tanstack/react-router";
import React from "react";
import { Header } from "~/components/Header";
import "../styles/globals.css";

export const Route = createRootRoute({
  component: RootComponent,
});

function RootComponent() {
  return (
    <div className="flex size-full flex-col">
      <Header />
      <main className="mx-auto size-[calc(100%-57px)] max-w-4xl px-6 py-6">
        <Outlet />
      </main>
      <TanStackRouterDevtools position="bottom-right" />
    </div>
  );
}