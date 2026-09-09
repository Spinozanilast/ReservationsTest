import { createRoot } from "react-dom/client";
import { RouterProvider } from "@tanstack/react-router";
import { QueryClientProvider } from "@tanstack/react-query";
import { router } from "./router";
import { getQueryClient } from "./lib/query";
import "./styles/globals.css";

const rootElement = document.getElementById("root");
if (!rootElement) throw new Error("Root element #root not found");

createRoot(rootElement).render(
  <QueryClientProvider client={getQueryClient()}>
    <RouterProvider router={router} />
  </QueryClientProvider>,
);