import { defineConfig } from "vite";
import { tanstackRouter } from "@tanstack/router-plugin/vite";
import tailwindcss from "@tailwindcss/vite";
import viteReact from "@vitejs/plugin-react";

export default defineConfig({
  server: {
    port: 8000,
  },
  resolve: {
    tsconfigPaths: true,
  },
  plugins: [tanstackRouter(), viteReact(), tailwindcss()],
});